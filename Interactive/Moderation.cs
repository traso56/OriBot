using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.EntityFrameworkCore;
using OriBot.Services;
using OriBot.Utility;
using System.ComponentModel;
using static OriBot.Interactive.Moderation;

namespace OriBot.Interactive;

[Group("moderation", "mod commands")]
[RequireContext(ContextType.Guild)]
public class Moderation : InteractionModuleBase<SocketInteractionContext>
{
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required DiscordSocketClient Client { get; set; }
    public required PaginatorFactory PaginatorFactory { get; set; }
    public required Globals Globals { get; set; }

    [ModCommand]
    [SlashCommand("dm", "dm")]
    public async Task DMUser(SocketGuildUser target, string message)
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        EmbedBuilder builder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritCyan)
            .AddUserAvatar(target)
            .WithTitle($"{Context.User.Username} sent a DM with the bot to {target.Username}")
            .AddField("Mention", target.Mention)
            .AddLongField("Message", message, "Message was empty")
            .WithCurrentTimestamp();

        if (await MessageUtilities.TrySendDmAsync(target, message, builder))
            await FollowupAsync("Sent DM");
        else
            await FollowupAsync("DM failed to send");

        await Globals.LogChannel.SendMessageAsync(embed: builder.Build());
    }
    [ModCommand]
    [SlashCommand("purge", "Clears messages")]
    public async Task Purge([MinValue(1)][MaxValue(99)] int amount)
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
        await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);

        await FollowupAsync("Success");

        EmbedBuilder builder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritRed)
            .AddField("Message purge executed", $"{amount} messages deleted from <#{Context.Channel.Id}> by {Context.User.Username}")
            .WithCurrentTimestamp();


        await Globals.LogChannel.SendMessageAsync(embed: builder.Build());
    }

    [ModCommand]
    [SlashCommand("dellog", "Deletes an infraction / log on someone")]
    public async Task DelLog(SocketGuildUser target, Notify notifyIn, [Discord.Interactions.Summary("punishmentid"), Autocomplete] ulong? punishmentid = null, string reason = "No reason was given.")
    {
        await DeferAsync(ephemeral: true);
        var db = DbContextFactory.CreateDbContext();
        if (punishmentid != null)
        {
            

            var toremove = db.Punishments.Where(x => x.PunishmentId == punishmentid).FirstOrDefault();
            if (toremove is null)
            {
                await ModifyOriginalResponseAsync(m =>
                {
                    m.Content = "That punishment ID does not exist.";
                    m.Components = new ComponentBuilder().Build();
                });
                return;
            }
            db.Punishments.Remove(toremove!);

            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritGreen)
            .AddUserAvatar(target)
            .WithTitle($"{Context.User.Username} has removed an infraction log for {target.Username}")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Punishment ID", punishmentid)
            .AddField("Infraction log removed by", Context.User.Mention)
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();
            if (notifyIn is Notify.UserDm)
            {
                string muteMessage =
                $"{Context.User.Username} has removed an infraction log for you in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}";
                await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
            }

            await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = "Success";
                m.Components = new ComponentBuilder().Build();
            });
        } else
        {
            if (!db.Punishments.Where(x => x.PunishedId == target.Id).Any())
            {
                await ModifyOriginalResponseAsync(m =>
                {
                    m.Content = "This user has no punishments.";
                    m.Components = new ComponentBuilder().Build();
                });
                return;
            }
            bool? response = await MessageUtilities.UserConfirmation(Context, Context.User,
            $"This will remove all infraction logs for the user: {target.Username}\n Confirm", MessageUtilities.ResponseType.FollowUp);

            if (response != true || response is null)
            {
                await ModifyOriginalResponseAsync(m =>
                {
                    m.Content = "Command cancelled";
                    m.Components = new ComponentBuilder().Build();
                });
                return;
            }
            var toremove = db.Punishments.Where(x => x.PunishedId == target.Id);
            db.Punishments.RemoveRange(toremove);
            db.SaveChanges();

            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritGreen)
            .AddUserAvatar(target)
            .WithTitle($"{Context.User.Username} has removed all infraction logs for {target.Username}")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Infraction logs removed by", Context.User.Mention)
            .AddField("Amount removed",toremove.Count())
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();

            await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

            if (notifyIn is Notify.UserDm)
            {
                string muteMessage =
                $"{Context.User.Username} has removed all infraction logs for you in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}";
                await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
            }

            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = "Success";
                m.Components = new ComponentBuilder().Build();
            });

        }

        
    }

    [ModCommand]
    [SlashCommand("loglist", "shows the action logs of a certain user")]
    public async Task LogList(SocketUser target)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbPunishments = db.Punishments.Where(p => p.PunishedId == target.Id).OrderBy(p => p.Issued).ToArray();
      

        if (dbPunishments.Length == 0)
        {
            await FollowupAsync("This user doesn't have any actions taken towards them");
            return;
        }

        List<Embed> embeds = [];

        int itemsPerPage = 5;

        //(records - 1) / recordsPerPage + 1
        int totalPages = (dbPunishments.Length - 1) / itemsPerPage + 1;
        for (int i = 0, page = 1; i < dbPunishments.Length; i += itemsPerPage, page++)
        {
            var logs = dbPunishments.Skip(i).Take(itemsPerPage);

            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithColor(ColorConstants.SpiritCyan)
                .AddUserAvatar(target)
                .WithTitle($"Log list for {target.Username}");
            foreach (var log in logs)
            {
                SocketUser? issuer = Client.GetUser(log.IssuerId);
                embedBuilder.AddField($"{log.Type} by {issuer?.Username ?? log.IssuerId.ToString()}",
                    $"**Issued:** {Utilities.FullDateTimeStamp((DateTimeOffset)log.Issued)}\n" +
                    $"**Expiry:** {(log.Expiry is not null ? Utilities.FullDateTimeStamp((DateTimeOffset)log.Expiry) : "`Never`")}\n" +
                    $"**Reason:** {log.Reason}\n" +
                    $"**Punishment ID: {log.PunishmentId}**");
            }
            embedBuilder.WithFooter($"Page {page}/{totalPages}");

            embeds.Add(embedBuilder.Build());
        }

        var paginator = PaginatorFactory.CreateEagerPaginator(embeds);
        await paginator.SendPaginatorAsync(Context.Interaction, null);
    }



    [ModCommand]
    [SlashCommand("modactions", "shows the actions someone made")]
    public async Task ModActions(SocketUser target)
    {
        await DeferAsync();
        using var db = DbContextFactory.CreateDbContext();

        var punishments = db.Punishments.Where(p => p.IssuerId == target.Id).ToArray();
        int bans = 0;
        int warns = 0;
        int mutes = 0;
        int notes = 0;
        foreach (var punishment in punishments)
        {
            switch (punishment.Type)
            {
                case PunishmentType.Ban:
                    bans++;
                    break;
                case PunishmentType.Warn:
                    warns++;
                    break;
                case PunishmentType.Mute:
                    mutes++;
                    break;
                case PunishmentType.Note:
                    notes++;
                    break;
                default:
                    throw new InvalidEnumArgumentException("Punishment type not handled.");
            }
        }
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritCyan)
            .AddUserAvatar(target)
            .WithTitle($"Actions taken by {target}")
            .AddField("Action", "Bans\nWarns\nMutes\nNotes", true)
            .AddField("Count", $"{bans}\n{warns}\n{mutes}\n{notes}", true);

        await FollowupAsync(embed: embedBuilder.Build());
    }
    public enum Notify
    {
        [ChoiceDisplay("To the user via DM and log channel")]
        UserDm,
        [ChoiceDisplay("Only in the log channel")]
        OnlyLogChannel
    }
    [ModCommand]
    [SlashCommand("mute", "Mutes an user")]
    public async Task Mute(SocketGuildUser target, [MinLength(3)] string reason, TimeSpan duration, Notify notifyIn)
    {
        await DeferAsync(ephemeral: true);

        if (duration > TimeSpan.FromDays(30))
        {
            await FollowupAsync("the maximum mute time is 30 days");
            return;
        }
        IGuildUser self = Context.Guild.GetUser(Client.CurrentUser.Id);
        if (target.Hierarchy >= self.Hierarchy)
        {
            await FollowupAsync("Cannot mute this user");
            return;
        }
        if (target.TimedOutUntil is not null)
        {
            await FollowupAsync("This user is already muted");
            return;
        }

        using var db = DbContextFactory.CreateDbContext();

        await target.SetTimeOutAsync(duration);

        var issuerDbUser = db.Users.FindOrCreate(Context.User);
        var punishedDbUser = db.Users.FindOrCreate(target);

        var dbPunishment = new Punishment()
        {
            Type = PunishmentType.Mute,
            Reason = reason,
            Issued = DateTime.Now,
            Expiry = DateTime.Now + duration,
            CheckForExpiry = false,
            Punished = punishedDbUser,
            Issuer = issuerDbUser
        };
        db.Punishments.Add(dbPunishment);
        db.SaveChanges();

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritRed)
            .AddUserAvatar(target)
            .WithTitle($"{target.Username} has been muted for {duration.ToReadableString()}")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Muted by:", Context.User.Mention)
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();

        if (notifyIn is Notify.UserDm)
        {
            string muteMessage =
                $"You've been muted in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}\n" +
                $"You were muted for {duration.ToReadableString()}";
            await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
        }

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await FollowupAsync("Success");
    }
    [ModCommand]
    [SlashCommand("unmute", "Unmutes an user")]
    public async Task Unmute(SocketGuildUser target, [MinLength(3)] string reason, Notify notifyIn)
    {
        await DeferAsync(ephemeral: true);

        if (target.TimedOutUntil is null)
        {
            await FollowupAsync("This user is not muted");
            return;
        }

        using var db = DbContextFactory.CreateDbContext();

        await target.RemoveTimeOutAsync();

        var dbPunishment = db.Punishments.FirstOrDefault(p => p.PunishedId == target.Id
            && p.Type == PunishmentType.Mute && p.Expiry > DateTime.Now);

        if (dbPunishment is null)
        {
            await FollowupAsync("This user is not muted... or they may be and there is a mismatch in the database");
            return;
        }
        dbPunishment.Reason += $" (This mute was removed for the following reason: {reason})";
        dbPunishment.Expiry = DateTime.Now;
        db.SaveChanges();

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritGreen)
            .AddUserAvatar(target)
            .WithTitle($"{target.Username} has been unmuted")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Unmuted by", Context.User.Mention)
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();

        if (notifyIn is Notify.UserDm)
        {
            string muteMessage =
                $"You've been unmuted in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}";
            await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
        }

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await FollowupAsync("Success");
    }
    [ModCommand]
    [SlashCommand("warn", "Warns an user")]
    public async Task Warn(SocketGuildUser target, [MinLength(3)] string reason, Notify notifyIn)
    {
        await DeferAsync(ephemeral: true);

        IGuildUser self = Context.Guild.GetUser(Client.CurrentUser.Id);
        if (target.Hierarchy >= self.Hierarchy)
        {
            await FollowupAsync("Cannot warn this user");
            return;
        }

        using var db = DbContextFactory.CreateDbContext();

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritRed)
            .AddUserAvatar(target)
            .WithTitle($"{target.Username} has been warned")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Warned by", Context.User.Mention)
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();

        var issuerDbUser = db.Users.FindOrCreate(Context.User);
        var punishedDbUser = db.Users.FindOrCreate(target);

        var dbWarnPunishment = new Punishment()
        {
            Type = PunishmentType.Warn,
            Reason = reason,
            Issued = DateTime.Now,
            Expiry = DateTime.Now.AddDays(40),
            CheckForExpiry = false,
            Punished = punishedDbUser,
            Issuer = issuerDbUser
        };
        db.Punishments.Add(dbWarnPunishment);

        db.SaveChanges();

        if (notifyIn is Notify.UserDm)
        {
            string muteMessage =
                $"You've been warned in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}\n" +
                $"Warns eventually expire so if you follow the rules you'll be okay";
            await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
        }

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await FollowupAsync("Success");
    }
    public enum PruneDays
    {
        [ChoiceDisplay("Don't prune")]
        ZeroDays = 0,
        [ChoiceDisplay("1 day")]
        OneDay = 1,
        [ChoiceDisplay("3 days")]
        ThreeDays = 3,
        [ChoiceDisplay("7 days")]
        SevenDays = 7
    }
    [ModCommand]
    [SlashCommand("ban", "Bans an user")]
    public async Task Ban(SocketUser target, [MinLength(3)] string reason, TimeSpan? duration = null,
        Notify notifyIn = Notify.OnlyLogChannel, PruneDays pruneDays = PruneDays.ZeroDays)
    {
        await DeferAsync(ephemeral: true);

        IGuildUser self = Context.Guild.GetUser(Client.CurrentUser.Id);
        SocketGuildUser? guildTarget = target as SocketGuildUser;
        if (guildTarget is not null && guildTarget.Hierarchy > self.Hierarchy)
        {
            await FollowupAsync("Cannot ban this user");
            return;
        }

        using var db = DbContextFactory.CreateDbContext();

        var currentBan = db.Punishments.FirstOrDefault(p => p.PunishedId == target.Id
            && p.Type == PunishmentType.Ban && (!p.Expiry.HasValue || p.Expiry > DateTime.Now));

        if (currentBan is not null)
        {
            await FollowupAsync("This user is already banned... or if they're not there is a mismatch in the database");
            return;
        }

        await Context.Guild.AddBanAsync(target, (int)pruneDays, $"Banned by {Context.User.Username}: {reason}, " +
                $"Expected expiry: {(duration is not null ? (DateTime.Now + duration).Value.ToString("dd/MM/yyyy") : "Never")}");

        var issuerDbUser = db.Users.FindOrCreate(Context.User);
        var punishedDbUser = db.Users.FindOrCreate(target);

        var dbPunishment = new Punishment()
        {
            Type = PunishmentType.Ban,
            Reason = reason,
            Issued = DateTime.Now,
            Expiry = duration is not null ? DateTime.Now + duration : null,
            CheckForExpiry = duration is not null,
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
            .AddField("Reason", reason)
            .AddField("Banned by:", Context.User.Mention)
            .AddField("Duration", duration is not null ? duration.Value.ToReadableString() : "forever")
            .AddField("Notification setting", notifyIn.ToString())
            .WithCurrentTimestamp();

        if (notifyIn is Notify.UserDm)
        {
            string muteMessage =
                $"You've been banned in {Context.Guild.Name}!\n" +
                $"**Reason:** {reason}\n" +
                $"Bans last a very long time";
            await MessageUtilities.TrySendDmAsync(target, muteMessage, embedBuilder);
        }

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await FollowupAsync("Success");
    }
    [ModCommand]
    [SlashCommand("unban", "Unbans an user")]
    public async Task Unban(SocketUser target, [MinLength(3)] string reason)
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        var dbPunishment = db.Punishments.FirstOrDefault(p => p.PunishedId == target.Id
            && p.Type == PunishmentType.Ban && (!p.Expiry.HasValue || p.Expiry > DateTime.Now));

        if (dbPunishment is null)
        {
            await FollowupAsync("This user is not banned... or they may be and there is a mismatch in the database");
            return;
        }

        bool? response = await MessageUtilities.UserConfirmation(Context, Context.User,
            $"This will unban the user: {target.Username}\n Confirm", MessageUtilities.ResponseType.FollowUp);

        if (response != true || response is null)
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = "Command cancelled";
                m.Components = new ComponentBuilder().Build();
            });
            return;
        }

        await Context.Guild.RemoveBanAsync(target.Id);

        dbPunishment.Reason += $" (This ban was removed for the following reason: {reason})";
        dbPunishment.Expiry = DateTime.Now;
        db.SaveChanges();

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritGreen)
            .AddUserAvatar(target)
            .WithTitle($"{target.Username} has been unbanned")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Unbanned by", Context.User.Mention)
            .WithCurrentTimestamp();

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = "Success";
            m.Components = new ComponentBuilder().Build();
        });
    }
    [ModCommand]
    [SlashCommand("note", "Adds a note to a user")]
    public async Task Log(SocketGuildUser target, [MinLength(3)] string reason)
    {
        await DeferAsync(ephemeral: true);

        IGuildUser self = Context.Guild.GetUser(Client.CurrentUser.Id);
        if (target.Hierarchy >= self.Hierarchy)
        {
            await FollowupAsync("Cannot log this user");
            return;
        }

        using var db = DbContextFactory.CreateDbContext();

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritBlue)
            .AddUserAvatar(target)
            .WithTitle($"Note added to {target.Username}")
            .AddField("Mention", target.Mention)
            .AddField("Reason", reason)
            .AddField("Note added by", Context.User.Mention)
            .WithCurrentTimestamp();

        var issuerDbUser = db.Users.FindOrCreate(Context.User);
        var punishedDbUser = db.Users.FindOrCreate(target);

        var dbWarnPunishment = new Punishment()
        {
            Type = PunishmentType.Note,
            Reason = reason,
            Issued = DateTime.Now,
            Expiry = null,
            CheckForExpiry = false,
            Punished = punishedDbUser,
            Issuer = issuerDbUser
        };
        db.Punishments.Add(dbWarnPunishment);

        db.SaveChanges();

        await Globals.LogChannel.SendMessageAsync(embed: embedBuilder.Build());

        await FollowupAsync("Success");
    }

    #region Autocomplete runners
    [AutocompleteCommand("punishmentid", "dellog")]
    public async Task Autocomplete()
    {
        var targetoption = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Options.Where(x => x.Name == "target");
        var db = DbContextFactory.CreateDbContext();
        if (!targetoption.Any())
        {
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(new AutocompleteResult[] { new("To get autocomplete for this command, please fill the target user first.","none") });
            return;
        }
        var possiblekeys = new List<AutocompleteResult>();
        foreach (var item in db.Punishments.Where(x => x.PunishedId == ulong.Parse((string)targetoption.First().Value))) 
        {
            possiblekeys.Add(new AutocompleteResult($"Punishment ID: {item.PunishmentId}, type: {item.Type}", item.PunishmentId));
        }

        var filtered = possiblekeys.Where(x =>
        {
            if (!((Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value as ulong?).HasValue)
            {
                return !((Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value as ulong?).ToString()!.StartsWith(x.Value.ToString()!);
            }
            else
            {
                return true;
            }
        });
        // max - 25 suggestions at a time
        await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(filtered.Take(25));
    }
    #endregion
}
