﻿using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot.Utility;
using System.ComponentModel;

namespace OriBot.Services;

public class EventHandler : DiscordClientService
{
    private readonly ExceptionReporter _exceptionReporter;
    private readonly IOptionsMonitor<UserJoinOptions> _userJoinOptions;
    private readonly MessageUtilities _messageUtilities;
    private readonly IDbContextFactory<SpiritContext> _dbContextFactory;
    private readonly VolatileData _volatileData;
    private readonly Globals _globals;
    private readonly IOptionsMonitor<PinOptions> _pinOptions;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public EventHandler(DiscordSocketClient client, ILogger<EventHandler> logger, ExceptionReporter exceptionReporter, IOptionsMonitor<UserJoinOptions> userJoinOptions,
        MessageUtilities messageUtilities, IDbContextFactory<SpiritContext> dbContextFactory, VolatileData volatileData, Globals globals,
        IOptionsMonitor<PinOptions> pinOptions)
        : base(client, logger)
    {
        _exceptionReporter = exceptionReporter;
        _userJoinOptions = userJoinOptions;
        _messageUtilities = messageUtilities;
        _dbContextFactory = dbContextFactory;
        _volatileData = volatileData;
        _globals = globals;
        _pinOptions = pinOptions;

        Client.AutoModActionExecuted += OnAutoModExecuted;
        Client.AuditLogCreated += OnAuditLogCreated;
        Client.UserJoined += OnUserJoined;
        Client.UserLeft += OnUserLeft;
        Client.ReactionAdded += OnReactionAdded;
        Client.UserVoiceStateUpdated += OnVoiceStateChanged;
        Client.GuildMemberUpdated += OnGuildMemberUpdated;
    }

    private enum VoiceActivityEventType
    {
        Leave,
        Join,
        Move,
        Other
    }

    private Task OnVoiceStateChanged(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        Task.Run(async () =>
        {
            var eventype = VoiceActivityEventType.Other;

            if (after.VoiceChannel is null)
                eventype = VoiceActivityEventType.Leave;
            else if (before.VoiceChannel is null)
                eventype = VoiceActivityEventType.Join;
            else if (before.VoiceChannel.Id != after.VoiceChannel.Id)
                eventype = VoiceActivityEventType.Move;

            if (eventype == VoiceActivityEventType.Other)
                return;

            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithColor(ColorConstants.SpiritBlue)
                .AddUserAvatar(user)
                .AddField("User", user.Mention, true)
                .WithCurrentTimestamp();

            switch (eventype)
            {
                case VoiceActivityEventType.Leave:
                    embedBuilder
                        .WithTitle($"{user.Username} left a voice channel")
                        .AddField("Voice channel", before.VoiceChannel!.Mention, true);
                    break;
                case VoiceActivityEventType.Join:
                    embedBuilder
                        .WithTitle($"{user.Username} joined a voice channel")
                        .AddField("Voice channel", after.VoiceChannel!.Mention, true);
                    break;
                case VoiceActivityEventType.Move:
                    embedBuilder
                        .WithTitle($"{user.Username} changed voice channels")
                        .AddField("Previous voice channel", before.VoiceChannel!.Mention, true)
                        .AddField("New voice channel", after.VoiceChannel!.Mention, true);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Voice enum stae is invalid.");
            }


            await _globals.VoiceActivityChannel.SendMessageAsync(embed: embedBuilder.Build());

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing voice state change event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    private Task OnAutoModExecuted(SocketGuild guild, AutoModRuleAction rule, AutoModActionExecutedData data)
    {
        Task.Run(async () =>
        {
            if (rule.Type == AutoModActionType.Timeout)
            {
                using var db = _dbContextFactory.CreateDbContext();

                SocketGuildUser target = data.User.Value ?? guild.GetUser(data.User.Id);

                var issuerDbUser = db.Users.FindOrCreate(Client.CurrentUser);
                var punishedDbUser = db.Users.FindOrCreate(target);

                string matched = data.MatchedKeyword;
                if (matched == "(?:(?:https?://)?(?:www)?discord(?:app)?\\.(?:(?:com|gg)/invite/[a-z0-9-_]+)|(?:https?://)?(?:www)?discord\\.gg/[a-z0-9-_]+)")
                    matched = "invite link";

                string muteReason = $"**Auto muted for sending:** {data.Content}\n**Matched word:** {matched}";

                var dbPunishment = new Punishment()
                {
                    Type = PunishmentType.Mute,
                    Reason = muteReason,
                    Issued = DateTime.Now,
                    Expiry = DateTime.Now + rule.TimeoutDuration,
                    CheckForExpiry = false,
                    Punished = punishedDbUser,
                    Issuer = issuerDbUser
                };
                db.Punishments.Add(dbPunishment);
                db.SaveChanges();

                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithColor(ColorConstants.SpiritRed)
                    .AddUserAvatar(target)
                    .WithTitle($"{target.Username} has been muted for {rule.TimeoutDuration!.Value.ToReadableString()}")
                    .AddField("Mention", target.Mention)
                    .AddField("Reason", muteReason)
                    .AddField("Muted by:", Client.CurrentUser.Mention)
                    .AddField("Notification setting", "UserDm")
                    .WithCurrentTimestamp();

                string muteMessage =
                    $"You've been muted in {guild.Name}!\n" +
                    $"**Reason:** {muteReason}\n" +
                    $"You were muted for {rule.TimeoutDuration!.Value.ToReadableString()}";
                await _messageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);

                await _globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing automod event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnAuditLogCreated(SocketAuditLogEntry log, SocketGuild guild)
    {
        Task.Run(async () =>
        {
            if (log.Action == ActionType.Ban && log.User.Id != Client.CurrentUser.Id)
            {
                var data = (SocketBanAuditLogData)log.Data;

                using var db = _dbContextFactory.CreateDbContext();

                IUser target = await Client.GetUserAsync(data.Target.Id);

                var issuerDbUser = db.Users.FindOrCreate(log.User);
                var punishedDbUser = db.Users.FindOrCreate(target);

                var dbPunishment = new Punishment()
                {
                    Type = PunishmentType.Ban,
                    Reason = log.Reason ?? "No reason was provided",
                    Issued = DateTime.Now,
                    Expiry = null,
                    CheckForExpiry = false,
                    Punished = punishedDbUser,
                    Issuer = issuerDbUser
                };
                db.Punishments.Add(dbPunishment);
                db.SaveChanges();

                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithColor(ColorConstants.SpiritRed)
                    .AddUserAvatar(target)
                    .WithTitle($"{target.Username} has been banned")
                    .AddField("Mention", target.Mention)
                    .AddField("Reason", log.Reason ?? "No reason was provided")
                    .AddField("Banned by", log.User.Mention)
                    .AddField("Duration", "forever")
                    .WithCurrentTimestamp();

                await _globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing audit log created event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnUserJoined(SocketGuildUser user)
    {
        Task.Run(async () =>
        {
            using var db = _dbContextFactory.CreateDbContext();

            if (user.CreatedAt > DateTimeOffset.UtcNow.AddDays(-_userJoinOptions.CurrentValue.NewUsersMinDays)) // check if the account is old enough
            {
                _volatileData.IgnoredKickedUsersIds.Add(user.Id);

                EmbedBuilder kickedEmbedBuilder = new EmbedBuilder()
                    .WithColor(ColorConstants.SpiritRed)
                    .AddUserAvatar(user)
                    .AddField($"{user} joined the server but was kicked because their account is younger than {_userJoinOptions.CurrentValue.NewUsersMinDays} days", user.Mention)
                    .AddField("Account creation date", Utilities.FullDateTimeStamp(user.CreatedAt))
                    .WithDirectUserLink(user)
                    .WithCurrentTimestamp();
                string errorMessage = $"Your account was kicked from {user.Guild.Name} because it's too new, wait some days to be able to join";
                await _messageUtilities.TrySendDmAsync(user, errorMessage, kickedEmbedBuilder);

                await user.KickAsync("Account too new");

                await _globals.AutosChannel.SendMessageAsync(embed: kickedEmbedBuilder.Build());
                return;
            }

            bool userMuted = user.TimedOutUntil > DateTimeOffset.UtcNow;

            EmbedBuilder joinEmbedBuilder = new EmbedBuilder()
                .WithColor(userMuted ? ColorConstants.SpiritYellow : ColorConstants.SpiritGreen)
                .AddUserAvatar(user)
                .AddField($"{user} has joined the server", user.Mention)
                .AddField("Account creation date", Utilities.FullDateTimeStamp(user.CreatedAt))
                .WithDirectUserLink(user)
                .WithCurrentTimestamp();

            if (userMuted)
                joinEmbedBuilder.AddField("User was previously muted, unmute time", Utilities.FullDateTimeStamp(user.TimedOutUntil!.Value));

            await _globals.MembersChannel.SendMessageAsync(embed: joinEmbedBuilder.Build());

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing user joined event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        Task.Run(async () =>
        {
            if (_volatileData.IgnoredKickedUsersIds.TryRemove(user.Id)) return;

            using var db = _dbContextFactory.CreateDbContext();

            bool wasBanned = _volatileData.IgnoredDeletedMessagesIds.TryRemove(user.Id);

            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithColor(ColorConstants.SpiritRed)
                .AddUserAvatar(user)
                .AddField(wasBanned ? $"{user} has been banned" : $"{user} has left the server", user.Mention)
                .WithDirectUserLink(user)
                .WithCurrentTimestamp();

            await _globals.MembersChannel.SendMessageAsync(embed: embedBuilder.Build());

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing user left event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        Task.Run(async () =>
        {
            if (channel.Id != _globals.ArtChannel.Id || reaction.UserId == Client.CurrentUser.Id) //art channel stuff
                return;

            if (reaction.Emote.Equals(Emotes.CrossMark))
            {
                await _semaphore.WaitAsync();
                try
                {
                    IUserMessage reactedMessage = message.Value ?? (IUserMessage)await _globals.ArtChannel.GetMessageAsync(message.Id);
                    if (reaction.UserId == reactedMessage.Author.Id)
                        await reactedMessage.RemoveAllReactionsForEmoteAsync(Emotes.Pin); //if image author reacts with "X" then remove all pins
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else if (reaction.Emote.Equals(Emotes.Pin))
            {
                await _semaphore.WaitAsync();
                try
                {
                    IUserMessage reactedMessage = message.Value ?? (IUserMessage)await _globals.ArtChannel.GetMessageAsync(message.Id);
                    if (reactedMessage.Reactions.TryGetValue(Emotes.Pin, out ReactionMetadata metaData)
                        && metaData.IsMe && metaData.ReactionCount >= _pinOptions.CurrentValue.PinAmount
                        && DateTimeOffset.UtcNow - reactedMessage.CreatedAt < new TimeSpan(45, 0, 0, 0)) //is it a pin and was it created less than 45 days ago?
                    {

                        await reactedMessage.RemoveReactionAsync(Emotes.Pin, Client.CurrentUser);

                        EmbedBuilder embedBuilder = Utilities.QuoteUserMessage($"Post by {reactedMessage.Author.Username}", reactedMessage,
                            ColorConstants.SpiritCyan, includeOriginChannel: false, includeDirectUserLink: false, includeMessageReference: false);

                        await _messageUtilities.SendMessageWithFiles(_globals.StarBoardChannel, embedBuilder, reactedMessage,
                            Utilities.CreateMessageJumpButton(reactedMessage));

                        using var db = _dbContextFactory.CreateDbContext();
                        Utilities.AddBadgeToUser(db, reactedMessage.Author, DbBadges.Pincushion);
                        db.SaveChanges();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing reaction added event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        Task.Run(async () =>
        {
            if (before.Value is null)
                return;

            if (before.Value.DisplayName == after.DisplayName)
                return;

            var embedBuilder = new EmbedBuilder()
                .WithColor(ColorConstants.SpiritCyan)
                .AddUserAvatar(after)
                .WithTitle($"{after} changed server profile info.")
                .AddField("Mention", after.Mention)
                .AddField("Nickname", $"Previous: `{before.Value.DisplayName}`\nNew: `{after.DisplayName}`")
                .WithDirectUserLink(after)
                .WithFooter($"Member ID: {after.Id}")
                .WithCurrentTimestamp();

            await _globals.MembersChannel.SendMessageAsync(embed: embedBuilder.Build());
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext();
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing member updated event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
}
