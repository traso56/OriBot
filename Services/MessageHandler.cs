using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot.Utility;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace OriBot.Services;

public partial class MessageHandler : DiscordClientService
{
    [GeneratedRegex(@"<@(\d+)>")]
    private static partial Regex MentionRegex();

    [GeneratedRegex(@"hi ori", RegexOptions.IgnoreCase)]
    private static partial Regex GreetingRegex();

    private readonly IServiceProvider _provider;
    private readonly CommandService _commandService;
    private readonly ExceptionReporter _exceptionReporter;
    private readonly VolatileData _volatileData;
    private readonly Globals _globals;
    private readonly MessageUtilities _messageUtilities;
    private readonly IDbContextFactory<SpiritContext> _dbContextFactory;
    private readonly PassiveResponses _passiveResponses;
    private readonly BotOptions _botOptions;

    public MessageHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider, CommandService commandService,
         ExceptionReporter exceptionReporter, VolatileData volatileData, Globals globals, MessageUtilities messageUtilities,
         IDbContextFactory<SpiritContext> dbContextFactory, IOptions<BotOptions> options, PassiveResponses passiveResponses)
        : base(client, logger)
    {
        _provider = provider;
        _commandService = commandService;
        _exceptionReporter = exceptionReporter;
        _volatileData = volatileData;
        _globals = globals;
        _messageUtilities = messageUtilities;
        _dbContextFactory = dbContextFactory;
        _passiveResponses = passiveResponses;
        _botOptions = options.Value;

        commandService.CommandExecuted += OnCommandExecuted;

        Client.MessageDeleted += OnMessageDeleted;
        Client.MessageUpdated += OnMessageUpdated;
        Client.MessageReceived += OnMessageReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
    }
    private Task OnCommandExecuted(Optional<CommandInfo> info, ICommandContext context, IResult result)
    {
        Task.Run(async () =>
        {
            if (result.IsSuccess)
                return;
            if (result.Error == CommandError.Exception)
            {
                if (result is ExecuteResult executeResult)
                {
                    var exceptionContext = new ExceptionContext(context.Message);
                    await _exceptionReporter.NotifyExceptionAsync(executeResult.Exception, exceptionContext, "Exception while executing a text command", true);
                }
            }
            else
            {
                if (result.ErrorReason == "Unknown command.")
                    return;
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(context.Message);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing command executed event", true);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnMessageDeleted(Cacheable<IMessage, ulong> deletedMessage, Cacheable<IMessageChannel, ulong> channel)
    {
        Task.Run(async () =>
        {
            if (_volatileData.IgnoredDeletedMessagesIds.TryRemove(deletedMessage.Id)) return;
            if (channel.Value is not SocketGuildChannel) return;

            if (deletedMessage.Value is IUserMessage message)
            {
                EmbedBuilder embedBuilder = Utilities.QuoteUserMessage("Message deleted", message, ColorConstants.SpiritRed,
                    includeOriginChannel: true, includeDirectUserLink: true, includeMessageReference: true);

                await _messageUtilities.SendMessageWithFiles(_globals.NotesChannel, embedBuilder, message,
                    Utilities.CreateMessageJumpButton(message));

                if (channel.Id == _botOptions.ArtChannelId)
                {
                    using var db = _dbContextFactory.CreateDbContext();
                    Utilities.RemoveBadgeFromUser(db, message.Author, DbBadges.Creative);
                    db.SaveChanges();
                }
            }
            else if (deletedMessage.Value is not ISystemMessage)
            {
                DateTimeOffset deleteDate = SnowflakeUtils.FromSnowflake(deletedMessage.Id);

                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithColor(ColorConstants.SpiritRed)
                    .WithTitle("Message deleted")
                    .AddField("Message", "Message was not in cache")
                    .AddField("Message was created on", Utilities.FullDateTimeStamp(deleteDate), true)
                    .AddField("Channel", $"<#{channel.Id}>", true);

                await _globals.NotesChannel.SendMessageAsync(embed: embedBuilder.Build());
            }

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(channel.Value);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing message deleted event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnMessageUpdated(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
    {
        Task.Run(async () =>
        {
            if (newMessage is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(Client, message);
            if (context.Channel.GetChannelType() == ChannelType.DM)
            {
                // DM stuff
            }
            else //all other channels
            {
                // art channel check
                if (context.Channel.Id == _botOptions.ArtChannelId)
                {
                    await ArtChannelCheckAsync(context);
                }
            }
            if (oldMessage.Value is IUserMessage oldUserMessage && oldUserMessage.Content != newMessage.Content)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithColor(ColorConstants.SpiritYellow)
                    .WithTitle("Message edited")
                    .AddUserAvatar(newMessage.Author)
                    .AddLongField("Old message", oldUserMessage.Content, "Message did not have text")
                    .AddLongField("New message", newMessage.Content, "Message does not have text")
                    .AddField("Mention", newMessage.Author.Mention, true)
                    .AddField("Message was created on", Utilities.FullDateTimeStamp(newMessage.CreatedAt), true)
                    .AddField("Channel", $"<#{channel.Id}>", true)
                    .WithDirectUserLink(message.Author)
                    .WithCurrentTimestamp();

                await _globals.NotesChannel.SendMessageAsync(embed: embedBuilder.Build(), components: Utilities.CreateMessageJumpButton(newMessage).Build());
            }

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(newMessage);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing message updated event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnMessageReceived(SocketMessage arg)
    {
        Task.Run(async () =>
        {
            if (arg is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(Client, message);
            if (context.Channel.GetChannelType() == ChannelType.DM)
            {
                // handle DMs
            }
            else //all other channels (in this case guild text channels really)
            {
                if (_volatileData.MessagesSent.TryGetValue(message.Author.Id, out _))
                    _volatileData.MessagesSent[message.Author.Id]++;

                if (_volatileData.TicketThreads.TryGetValue(message.Channel.Id, out ulong threadUserId))
                {
                    MatchCollection matches = MentionRegex().Matches(message.Content);

                    if (matches.Count > 0)
                    {
                        var thread = (IThreadChannel)context.Channel;
                        foreach (Match match in matches)
                        {
                            IGuildUser user = await thread.Guild.GetUserAsync(ulong.Parse(match.Groups[1].Value));
                            if (!(user.RoleIds.Contains(_globals.ModRole.Id) || user.Id == Client.CurrentUser.Id || user.Id == threadUserId))
                                await thread.RemoveUserAsync(user);
                        }
                    }
                }

                int argPos = 0;
                // art channel check
                if (context.Channel.Id == _botOptions.ArtChannelId)
                {
                    bool valid = await ArtChannelCheckAsync(context);
                    if (valid)
                    {
                        await message.AddReactionAsync(Emotes.Pin);

                        using var db = _dbContextFactory.CreateDbContext();
                        Utilities.AddBadgeToUser(db, message.Author, DbBadges.Creative);
                        db.SaveChanges();
                    }
                }
                // command handling
                else if (message.HasStringPrefix(_botOptions.Prefix, ref argPos))
                {
                    await _commandService.ExecuteAsync(context, argPos, _provider);
                }
                // check for responses and that in the commands channel
                else if (context.Channel.Id == _globals.CommandsChannel.Id)
                {
                    await CommandsChannelMessage(context);
                }
                // any mod mention
                else if (message.Content.StartsWith(_globals.AnyModRole.Mention))
                {
                    await AnyModPingHandler(context);
                }
            }

        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(arg);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing message received event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    /********************************************
        MESSAGE OPERATIONS
    ********************************************/
    private async Task<bool> ArtChannelCheckAsync(SocketCommandContext context)
    {
        if (context.Message.Attachments.Count == 0)
        {
            string[] allowedSites = File.ReadAllLines(Utilities.GetLocalFilePath("AllowedSites.txt"));
            if (!Array.Exists(allowedSites, context.Message.Content.Contains))
            {
                string errorMessage = $"Hello there!, the message you sent in <#{context.Channel.Id}> does not contain a valid message that I recognize as art\n" +
                    $"if you think this is an error or want to suggest more sites to accept feel free to send a /ticket in {context.Guild.Name}";
                await MessageDeleterHandlerAsync(context, errorMessage);
                return false;
            }
        }
        else
        {
            var attachment = context.Message.Attachments.First(); // we really only have to check the first because all other situations would be valid anyways
            if (attachment.Width < 40 || attachment.Height < 40)
            {
                string errorMessage = $"Hello there!, the message you sent in <#{context.Channel.Id}> does not contain a valid message that I recognize as art\n" +
                    $"The image sent is too small\n" +
                    $"if you think this is an error feel free to send a /ticket in {context.Guild.Name}";
                await MessageDeleterHandlerAsync(context, errorMessage);
                return false;
            }
        }
        return true;
    }
    private async Task CommandsChannelMessage(SocketCommandContext context)
    {
        if (context.Message.Content.StartsWith('/'))
        {
            await context.Message.ReplyAsync("oh! to use slash commands make sure to click on the option!");
        }
        else
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var matcher = new PassiveResponseMatching.MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuationAndSpace
                .AddTokens("Don't let our food be denied you put our polyunsaturated fats and triglycerides inside you","thanks alot,","")
                .AddPunctuationAndSpace
                .AddTokens("customer")
                .AddPunctuationAndSpace
                .AddTokens("And also dont forget to rate our restaurant")
                .Build;
            watch.Stop();
            if (matcher.Match(context.Message.Content))
            {
                await context.Channel.SendMessageAsync($"" +
                    $"Matched nonstrict: True\n" +
                    $"Matched strict: {matcher.MatchStrict(context.Message.Content)}\n" +
                    $"Duration: {watch.ElapsedMilliseconds}ms" +
                    $"");
            }

                
            await _passiveResponses.ExecuteHandlerAsync(context);
        }
    }
    /********************************************
        HELPER METHODS
    ********************************************/
    private async Task AnyModPingHandler(SocketCommandContext context)
    {
        var mods = _globals.MainGuild.Users.Where(u => u.Roles.Contains(_globals.ModRole)
            && !u.Roles.Contains(_globals.UnAvailableModRole)
            && u.Status is UserStatus.Online or UserStatus.Idle or UserStatus.AFK)
            .ToArray();
        if (mods.Length == 0)
        {
            await context.Channel.SendMessageAsync($"No mods are readily available! " +
                $"I have to ping the whole role so that whoever is here can get to you. It's no problem! {_globals.ModRole.Mention}");
        }
        else
        {
            Random rng = new Random();
            var moderator = mods[rng.Next(mods.Length)];
            await context.Channel.SendMessageAsync($"{moderator.Mention} will be here to assist you!");
        }
    }
    private async Task MessageDeleterHandlerAsync(SocketCommandContext context, string dmMessage)
    {
        var embedBuilder = Utilities.QuoteUserMessage("Message autodeleted", context.Message, ColorConstants.SpiritRed,
            includeOriginChannel: true, includeDirectUserLink: true, includeMessageReference: true);

        _volatileData.IgnoredDeletedMessagesIds.Add(context.Message.Id);
        await context.Message.DeleteAsync();

        await _messageUtilities.TrySendDmAsync(context.User, dmMessage, embedBuilder);

        await _messageUtilities.SendMessageWithFiles(_globals.AutosChannel, embedBuilder, context.Message);
    }
}
