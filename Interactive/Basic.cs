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
        int CalculateLevel(int experience)
        {
            int level = 0;
            double needed = 40;
            while (experience > needed)
            {
                level++;
                experience -= (int)needed;
                needed *= 1.0025;
            }
            return level;
        }

        await DeferAsync();
        user = (SocketGuildUser)(user ?? Context.User);

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.Include(u => u.UserBadges).ThenInclude(ub => ub.Badge).FirstOrDefault(u => u.UserId == user.Id);

        int userXp = 0;
        if (dbUser is not null)
            foreach (var dbUserBadge in dbUser.UserBadges)
            {
                userXp += dbUserBadge.Badge.Experience * dbUserBadge.Count;
            }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(dbUser?.Color ?? ColorConstants.SpiritBlack)
            .AddUserAvatar(user)
            .WithTitle(dbUser?.Title ?? $"Profile of {user}")
            .AddField("Created at", TimestampTag.FromDateTimeOffset(user.CreatedAt, TimestampTagStyles.LongDate), true)
            .AddField("Joined at", TimestampTag.FromDateTimeOffset(user.JoinedAt!.Value, TimestampTagStyles.LongDate), true)
            .AddField("Experience", userXp)
            .AddField("Level", CalculateLevel(userXp), true);

        if (dbUser?.Description is not null)
            embedBuilder.WithDescription(dbUser.Description);

        if (user.GuildPermissions.BanMembers)
        {
            IEmote sirenEmote = new Emoji("🚨");
            embedBuilder.AddField($"{sirenEmote} Moderator", "I'm a moderator of this community");
        }
        if (dbUser is not null)
            foreach (var userBadge in dbUser.UserBadges)
            {
                var badge = userBadge.Badge;
                string romanNumeral = userBadge.Count > 1 ? " " + Utilities.IntToRoman(userBadge.Count) : "";
                embedBuilder.AddField($"{badge.Emote} {badge.Name}{romanNumeral}", badge.Description, true);
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
}
