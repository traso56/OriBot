using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OriBot.Services;
using OriBot.Utility;
using System.Text;

namespace OriBot.Interactive;

[CommandsChannel]
[RequireContext(ContextType.Guild)]
public class Basic : InteractionModuleBase<SocketInteractionContext>
{
    public required InteractionService InteractionService { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }

    public required Personality personality { get; set; }

    [SlashCommand("help", "Gives help (hopefully)")]
    public async Task Help()
    {
        IReadOnlyList<ModuleInfo> modules = InteractionService.Modules;

        string moduleName = "Basic";

        var buttonBuilder = new ComponentBuilder()
            .WithButton("Basic commands", "Basic");

        // en-US: Basic commands
        await RespondAsync(personality.Format("commands.basic.help.button1"), components: buttonBuilder.Build());
        var infoMessage = await GetOriginalResponseAsync();
        while (true)
        {
            var embedBuilder = new EmbedBuilder()
                .WithColor(ColorConstants.SpiritBlue);
            using var enumerator = modules.GetEnumerator();

            while (true)
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("You tried to access a module that doesn't exist: " + moduleName);
                if (enumerator.Current.Name == moduleName)
                    break;
            }
            ModuleInfo? module = enumerator.Current;
            embedBuilder.WithTitle(module.Name);
            foreach (var command in module.SlashCommands)
            {
                if (command.Description is null)
                    continue;
                StringBuilder sbuilder = new StringBuilder();
                foreach (var par in command.Parameters)
                {
                    sbuilder.Append(par.Name);
                    if (!par.IsRequired)
                        // en-US: (optional)
                        sbuilder.Append(personality.Format("commands.basic.help.sbuilderoptional"));
                    if (par.DefaultValue is not null && par.DefaultValue.ToString() != "")
                        // en-US: (default: {par.DefaultValue})
                        sbuilder.Append(personality.Format("commands.basic.help.sbuilderdefault",par.DefaultValue.ToString()!));
                    sbuilder.Append(", ");
                }
                if (sbuilder.Length == 0)
                    sbuilder.Append(personality.Format("commands.basic.help.sbuildernone"));
                else
                    sbuilder.Length -= 2;
                //embedBuilder.AddField(command.Name, $"{command.Description}\n*Arguments*: {sbuilder}");
                //en-US: $"{command.Description}\n*Arguments*: {sbuilder}"
                embedBuilder.AddField(command.Name, personality.Format("commands.basic.help.sbuilderarguments", command.Description,sbuilder.ToString()));
            }

            await infoMessage.ModifyAsync(m => m.Embed = embedBuilder.Build());
            var selection = await MessageUtilities.AwaitComponentAsync(infoMessage.Id, Context.User.Id, MessageUtilities.ComponentType.Button);
            if (selection is null)
                break;
            moduleName = selection.Data.CustomId;
        }
        buttonBuilder = Utilities.DisableAllButtons(buttonBuilder);
        await infoMessage.ModifyAsync(m => m.Components = buttonBuilder.Build());
    }
    [SlashCommand("profile", "Gets the information of someone")]
    public async Task UserInfo(SocketGuildUser? user = null)
    {
        await DeferAsync();
        user = (SocketGuildUser)(user ?? Context.User);

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.Include(u => u.UserBadges).ThenInclude(ub => ub.Badge).FirstOrDefault(u => u.UserId == user.Id);

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(ColorConstants.SpiritBlue)
            .AddUserAvatar(user)
            // en-US: Profile of
            .WithTitle(personality.Format("commands.basic.profile.profileof",user.ToString()))
            // en-US: Created at
            .AddField(personality.Format("commands.basic.profile.createdat"), TimestampTag.FromDateTimeOffset(user.CreatedAt, TimestampTagStyles.LongDate), true)
            // en-US: Joined at
            .AddField(personality.Format("commands.basic.profile.joinedat"), TimestampTag.FromDateTimeOffset(user.JoinedAt!.Value, TimestampTagStyles.LongDate), false);

        if (user.GuildPermissions.BanMembers)
        {
            IEmote sirenEmote = new Emoji("🚨");
            // en-US: $"{sirenEmote} Moderator"
            // en-US: I'm a moderator of this community
            embedBuilder.AddField(personality.Format("commands.basic.profile.embed.moderatorbadgename",sirenEmote.ToString()!), personality.Format("commands.basic.profile.embed.moderatorbadgedescription"));
        }
        if (dbUser is not null)
            foreach (var userBadge in dbUser.UserBadges)
            {
                var badge = userBadge.Badge;
                string romanNumeral = userBadge.Count > 1 ? " " + Utilities.IntToRoman(userBadge.Count) : "";
                embedBuilder.AddField($"{badge.BadgeEmote} {badge.BadgeName}{romanNumeral}", badge.BadgeDescription, true);
            }

        await FollowupAsync(embed: embedBuilder.Build());
    }
}
