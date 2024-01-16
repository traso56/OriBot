using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OriBot.Services;
using OriBot.Utility;
using System;
using System.Text;

namespace OriBot.Interactive;

[CommandsChannel]
[RequireContext(ContextType.Guild)]
public class Basic : InteractionModuleBase<SocketInteractionContext>
{
    public required InteractionService InteractionService { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }

    [SlashCommand("help", "Gives help (hopefully)")]
    public async Task Help()
    {
        IReadOnlyList<ModuleInfo> modules = InteractionService.Modules;

        string moduleName = "Basic";

        var buttonBuilder = new ComponentBuilder()
            .WithButton("Basic commands", "Basic");

        await RespondAsync("Here's a list of commands and their description:", components: buttonBuilder.Build());
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
                        sbuilder.Append("(optional)");
                    if (par.DefaultValue is not null && par.DefaultValue.ToString() != "")
                        sbuilder.Append($"(default: {par.DefaultValue})");
                    sbuilder.Append(", ");
                }
                if (sbuilder.Length == 0)
                    sbuilder.Append("None");
                else
                    sbuilder.Length -= 2;
                embedBuilder.AddField(command.Name, $"{command.Description}\n*Arguments*: {sbuilder}");
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
    [CommandsChannel]
    [SlashCommand("profile", "Gets the profile of someone")]
    public async Task UserInfo(SocketGuildUser? user = null)
    {
        const int MAX_LEVEL = 10000;
        int CalculateLevel(int experience)
        {
            const double PER_LEVEL_MULT = 1.0025;
            const double LEVEL_1_THRESHOLD = 40;

            double[] LevelToExperience = new double[MAX_LEVEL + 1];
            int highest = 0;
            double req = 0;
            LevelToExperience[0] = 0;
            for (int lvl = 1; lvl <= MAX_LEVEL; lvl++)
            {
                req += LEVEL_1_THRESHOLD * PER_LEVEL_MULT * lvl;
                LevelToExperience[lvl] = req;
            }
            //MAX_EXPERIENCE = req;
            for (int level = 0; level < LevelToExperience.Length - 0; level++)
            {
                if (LevelToExperience[level] <= experience)
                {
                    highest = level;
                }
                else
                {
                    // Specifically found a level higher. We didn't run out.
                    return highest;
                }
            }
            return MAX_LEVEL;
        }
        string LevelToRoman(int level) => level > 1 ? " " + Utilities.IntToRoman(level) : "";

        await DeferAsync();
        user = (SocketGuildUser)(user ?? Context.User);

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.Include(u => u.UniqueBadges).Include(u => u.UserBadges).ThenInclude(ub => ub.Badge).FirstOrDefault(u => u.UserId == user.Id);

        int userXp = 0;
        if (dbUser is not null)
        {
            foreach (var dbUserBadge in dbUser.UserBadges)
                userXp += dbUserBadge.Badge.Experience * dbUserBadge.Count;

            foreach (var dbUniqueBadge in dbUser.UniqueBadges)
                userXp += dbUniqueBadge.Experience;
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(dbUser?.Color ?? ColorConstants.SpiritBlack)
            .AddUserAvatar(user)
            .WithTitle(dbUser?.Title ?? $"Profile of {user}")
            .AddField("Created at", TimestampTag.FromDateTimeOffset(user.CreatedAt, TimestampTagStyles.LongDate), true)
            .AddField("Joined at", TimestampTag.FromDateTimeOffset(user.JoinedAt!.Value, TimestampTagStyles.LongDate), true)
            .AddField("Experience", userXp, true)
            .AddField("Level", CalculateLevel(userXp), true);

        if (dbUser?.Description is not null)
            embedBuilder.WithDescription(dbUser.Description);

        bool inline = false;
        if (user.GuildPermissions.BanMembers)
        {
            embedBuilder.AddField($"{Emotes.PoliceCarEmote} StaffMember", "Anti-Fun Enforcement Badge\n\n*I'm a staff member on this server.*", inline);
            inline = true;
        }
        if (dbUser is not null)
        {
            foreach (var userBadge in dbUser.UserBadges)
            {
                var badge = userBadge.Badge;
                embedBuilder.AddField($"{badge.Emote} {badge.Name}{LevelToRoman(userBadge.Count)}", $"*{badge.MiniDescription}*\n\n{badge.Description}", inline);
                inline = true;
            }
            UniqueBadge[] approvedIdeas = dbUser.UniqueBadges.Where(ub => ub.BadgeType == UniqueBadgeType.ApprovedIdea).ToArray();
            if (approvedIdeas.Length > 0)
            {
                StringBuilder sb = new StringBuilder("Ideas approved:");

                foreach (var dbUniqueBadge in approvedIdeas)
                    sb.Append('\n').Append(dbUniqueBadge.Data);

                embedBuilder.AddField($"{Emotes.LightBulbEmote} Creative Thinker {LevelToRoman(approvedIdeas.Length)}", sb.ToString(), inline);
                inline = true;
            }
            UniqueBadge[] emojisCreated = dbUser.UniqueBadges.Where(ub => ub.BadgeType == UniqueBadgeType.EmojiCreator).ToArray();
            if (emojisCreated.Length > 0)
            {
                StringBuilder sb = new StringBuilder("I created some emojis:\n");

                foreach (var dbUniqueBadge in emojisCreated)
                    sb.Append(dbUniqueBadge.Data);

                embedBuilder.AddField($"{Emotes.NaruEmote} Emoji Creator {LevelToRoman(emojisCreated.Length)}", sb.ToString(), inline);
            }
        }

        await FollowupAsync(embed: embedBuilder.Build());
    }
    [CommandsChannel]
    [SlashCommand("editprofile", "Edits your profile")]
    public async Task EditProfile([MinLength(6)][MaxLength(6)] string hexColor, string? title = null, string? description = null)
    {
        if (!Utilities.TryParseHexToColor(hexColor, out Color color))
        { await RespondAsync("This is not a valid color", ephemeral: true); return; }

        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.FindOrCreate(Context.User);

        dbUser.Title = title;
        dbUser.Description = description;
        dbUser.Color = color;

        db.SaveChanges();
        await FollowupAsync("Profile updated successfully!");
    }
    [CommandsChannel]
    [SlashCommand("hug", "Hugs Ori.")]
    public async Task Hug()
    {
        await RespondAsync(Emotes.OriHugKu.ToString());
    }

}
