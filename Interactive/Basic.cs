using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot.Services;
using OriBot.Utility;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OriBot.Interactive;

[CommandsChannel]
[RequireContext(ContextType.Guild)]
public class Basic : InteractionModuleBase<SocketInteractionContext>
{
    public required InteractionService InteractionService { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required VolatileData VolatileData { get; set; }
    public required IHttpClientFactory HttpClientFactory { get; set; }
    public required IOptionsMonitor<MessageAmountQuerying> MessageAmountQuerying { get; set; }
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }
    public required ILogger<Basic> Logger { get; set; }

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
            double req = 0;
            int currentLevel = 1;
            while (true)
            {
                req += 40.1 * currentLevel;
                if (experience < req)
                    return currentLevel - 1;
                currentLevel++;
            }
        }

        string LevelToRoman(int level) => level > 1 ? " " + Utilities.IntToRoman(level) : "";

        await DeferAsync();
        user = (SocketGuildUser)(user ?? Context.User);

        if (!VolatileData.MessagesSent.TryGetValue(user.Id, out int userXp))
        {
            try
            {
                HttpClient httpClient = HttpClientFactory.CreateClient();

                string url = $"{MessageAmountQuerying.CurrentValue.ApiUrl}" +
                    $"?authorid={user.Id}" +
                    $"&serverid={Context.Guild.Id}" +
                    $"&userid={MessageAmountQuerying.CurrentValue.UserId}";

                string result = await httpClient.GetStringAsync(url);
                userXp = int.Parse(result);
                VolatileData.MessagesSent[user.Id] = userXp;
            }
            catch (HttpRequestException e)
            {
                Logger.LogError(e, "Exception while trying to fetch message amount");
            }
        }

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.Include(u => u.UniqueBadges).Include(u => u.UserBadges).ThenInclude(ub => ub.Badge).FirstOrDefault(u => u.UserId == user.Id);

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
            .AddField("Experience", userXp, false)
            .AddField("Level", CalculateLevel(userXp), false);

        if (dbUser?.Description is not null)
            embedBuilder.WithDescription(dbUser.Description);

        if (user.GuildPermissions.BanMembers)
            embedBuilder.AddField($"{Emotes.PoliceCarEmote} StaffMember", "Anti-Fun Enforcement Badge\n\n*I'm a staff member on this server.*", true);

        if (dbUser is not null)
        {
            foreach (var userBadge in dbUser.UserBadges)
            {
                var badge = userBadge.Badge;
                embedBuilder.AddField($"{badge.Emote} {badge.Name}{LevelToRoman(userBadge.Count)}", $"*{badge.MiniDescription}*\n\n{badge.Description}", true);
            }
            UniqueBadge[] approvedIdeas = dbUser.UniqueBadges.Where(ub => ub.BadgeType == UniqueBadgeType.ApprovedIdea).ToArray();
            if (approvedIdeas.Length > 0)
            {
                StringBuilder sb = new StringBuilder("Ideas approved:");

                foreach (var dbUniqueBadge in approvedIdeas)
                    sb.Append('\n').Append(dbUniqueBadge.Data);

                embedBuilder.AddField($"{Emotes.LightBulbEmote} Creative Thinker {LevelToRoman(approvedIdeas.Length)}", sb.ToString(), true);
            }
            UniqueBadge[] emojisCreated = dbUser.UniqueBadges.Where(ub => ub.BadgeType == UniqueBadgeType.EmojiCreator).ToArray();
            if (emojisCreated.Length > 0)
            {
                StringBuilder sb = new StringBuilder("I created some emojis:\n");

                foreach (var dbUniqueBadge in emojisCreated)
                    sb.Append(dbUniqueBadge.Data);

                embedBuilder.AddField($"{Emotes.NaruEmote} Emoji Creator {LevelToRoman(emojisCreated.Length)}", sb.ToString(), true);
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
    [CommandsChannel]
    [SlashCommand("whomade", "Checks what user created a specific emote")]
    public async Task WhoMade(string emote)
    {
        string ExtractNumbers(string input)
        {
            string pattern = @"\D+"; // \D matches any non-digit character
            string result = Regex.Replace(input, pattern, "");
            return result;
        }

        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        string numbers = ExtractNumbers(emote);

        var dbUniqueBadge = db.UniqueBadges.FirstOrDefault(ub => ub.Data.Contains(numbers));

        if (dbUniqueBadge is not null)
            await FollowupAsync($"{emote} was made by <@{dbUniqueBadge.UserId}>");
        else
            await FollowupAsync("I'm sorry. I don't know who made this emoji");
    }
}
