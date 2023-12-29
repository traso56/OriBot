using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OriBot.Services;
using OriBot.Utility;

namespace OriBot.Interactive;

[RequireContext(ContextType.Guild)]
public class GuildManagement : InteractionModuleBase<SocketInteractionContext>
{
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }
    public required IOptionsMonitor<UserJoinOptions> UserJoinOptions { get; set; }
    public required Globals Globals { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required VolatileData VolatileData { get; set; }

    public required Personality personality { get; set; }

    /********************************************
        BADGES
    ********************************************/
    [RequireOwner]
    [SlashCommand("createbadge", "adds a role to the store")]
    public async Task CreateGlobalBadge(string name, string emote, string description)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbBadgeToEdit = db.Badges.FirstOrDefault(b => b.BadgeName == name);
        if (dbBadgeToEdit is not null)
        {
            dbBadgeToEdit.BadgeEmote = emote;
            dbBadgeToEdit.BadgeDescription = description;
        }
        else
        {
            var dbBadge = new Badge
            {
                BadgeName = name,
                BadgeDescription = description,
                BadgeEmote = emote
            };
            db.Badges.Add(dbBadge);
        }
        db.SaveChanges();
        // en-US: $"Badge {name} added"
        await FollowupAsync(personality.Format("commands.guildmanagement.createbadge.response"));
    }
    [RequireOwner]
    [SlashCommand("addbadgetouser", "adds a badge to an user")]
    public async Task AddGlobalBadgeToUser(SocketUser target)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbBadges = db.Badges.ToList();

        var selectMenuBuilder = new SelectMenuBuilder()
            // en-US: Select a badge
            .WithPlaceholder(personality.Format("commands.guildmanagement.addbadgetouser.selectmenuplaceholder"))
            .WithCustomId("badgesMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        for (int i = 0; i < dbBadges.Count; i++)
        {
            selectMenuBuilder.AddOption(dbBadges[i].BadgeName, i.ToString());
        }
        // en-US: Badges
        await FollowupAsync("Badges", components: new ComponentBuilder().WithSelectMenu(selectMenuBuilder).Build());
        var question = await GetOriginalResponseAsync();
        var selectedOption = await MessageUtilities.AwaitComponentAsync(question.Id, Context.User.Id, MessageUtilities.ComponentType.SelectMenu);
        await question.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

        if (selectedOption is null)
            return;

        int idToAdd = int.Parse(selectedOption.Data.Values.ElementAt(0));
        var dbBadge = dbBadges[idToAdd];

        var dbUser = db.Users.FindOrCreate(target);

        dbUser.Badges = [dbBadge];
        db.SaveChanges();
        // en-US: $"Added {dbBadge.BadgeName} to {target.Username}"
        await question.ModifyAsync(x => x.Content = personality.Format("commands.guildmanagement.addbadgetouser.modifyresponse",dbBadge.BadgeName,target.Username));
    }
    /********************************************
        WELCOME BUTTON
    ********************************************/
    [ComponentInteraction(ComponentIds.WelcomeButtonId)]
    public async Task WelcomeButton()
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        var guildUser = (SocketGuildUser)Context.User;

        if (guildUser.Roles.Contains(Globals.MemberRole))
        {
            // en-US: "You have already clicked this button"
            await FollowupAsync(personality.Format("commands.guildmanagement.welcomebutton.alreadyclicked"), ephemeral: true);
            return;
        }

        await guildUser.AddRoleAsync(Globals.MemberRole);

        int imageRoleDays = UserJoinOptions.CurrentValue.ImageRoleDays;
        var pendingImageRoleUser = db.PendingImageRoles.FirstOrDefault(u => u.UserId == Context.User.Id);
        if (pendingImageRoleUser is null)
        {
            pendingImageRoleUser = new PendingImageRole()
            {
                UserId = Context.User.Id,
                ImageRoleDateTime = DateTime.Now.AddDays(imageRoleDays)
            };
            db.PendingImageRoles.Add(pendingImageRoleUser);
            db.SaveChanges();
        }
        else
        {
            pendingImageRoleUser.ImageRoleDateTime = DateTime.Now.AddDays(imageRoleDays);
            db.SaveChanges();
        }

        // en-US: "Successfully joined!"
        await FollowupAsync(personality.Format("commands.guildmanagement.welcomebutton.success"), ephemeral: true);
    }
    /********************************************
        USER COMMANDS
    ********************************************/
    [SlashCommand("ticket", "Creates a ticket")]
    public async Task Ticket([MinLength(3)][MaxLength(20)] string reason)
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        if (db.Tickets.Any(t => t.TicketUserId == Context.User.Id))
        {
            // en-US: "You have an active ticket"
            await FollowupAsync(personality.Format("commands.guildmanagement.ticket.already"));
            return;
        }

        IThreadChannel thread = await Globals.FeedbackChannel.CreateThreadAsync($"{Context.User}: {reason}", ThreadType.PrivateThread, invitable: false);

        // en-US: $"{Globals.ModRole.Mention} a new ticket has been created: {thread.Mention}"
        await Globals.FeedbackChannel.SendMessageAsync(personality.Format("commands.guildmanagement.ticket.created", Globals.ModRole.Mention,thread.Mention));

        // en-US: $"Hello {Context.User.Mention}, what can we help you with?"
        await thread.SendMessageAsync(personality.Format("commands.guildmanagement.ticket.thread.hello",Context.User.Mention));

        var dbTicket = new Ticket
        {
            TicketId = thread.Id,
            TicketUserId = Context.User.Id
        };
        db.Tickets.Add(dbTicket);
        db.SaveChanges();

        VolatileData.TicketThreads.TryAdd(thread.Id, Context.User.Id);

        // en-US: $"Ticket created: {Globals.FeedbackChannel.Mention}"
        await FollowupAsync(personality.Format("commands.guildmanagement.ticket.feedbackchannel.created", Globals.FeedbackChannel.Mention));
    }
}
