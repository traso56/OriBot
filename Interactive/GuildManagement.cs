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
    public class TicketModal : IModal
    {
        public string Title => "Create ticket";

        [InputLabel("What do you need help with?")]
        [ModalTextInput("Reason", placeholder: "Ticket topic", minLength: 3, maxLength: 20)]
        public required string Reason { get; set; }
    }

    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }
    public required IOptionsMonitor<UserJoinOptions> UserJoinOptions { get; set; }
    public required Globals Globals { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required VolatileData VolatileData { get; set; }

    [ModCommand]
    [MessageCommand("add pin")]
    public async Task AddPin(IMessage message)
    {
        IEmote pinEmote = new Emoji("📌");
        await message.AddReactionAsync(pinEmote);
        await RespondAsync("added pin emote", ephemeral: true);
    }
    /********************************************
        BADGES
    ********************************************/
    [ModCommand]
    [SlashCommand("createbadge", "adds a role to the store")]
    public async Task CreateGlobalBadge(string name, string emote, string miniDescription, string description, int experience)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbBadgeToEdit = db.Badges.FirstOrDefault(b => b.Name == name);
        if (dbBadgeToEdit is not null)
        {
            dbBadgeToEdit.Emote = emote;
            dbBadgeToEdit.Description = description;
        }
        else
        {
            var dbBadge = new Badge
            {
                Name = name,
                MiniDescription = miniDescription,
                Description = description,
                Emote = emote,
                Experience = experience
            };
            db.Badges.Add(dbBadge);
        }
        db.SaveChanges();
        await FollowupAsync($"badge {name} added");
    }
    [ModCommand]
    [SlashCommand("addbadgetouser", "adds a badge to a user")]
    public async Task AddGlobalBadgeToUser(SocketUser target)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbBadges = db.Badges.ToList();

        var selectMenuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select a badge")
            .WithCustomId("badgesMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        for (int i = 0; i < dbBadges.Count; i++)
        {
            selectMenuBuilder.AddOption(dbBadges[i].Name, i.ToString());
        }
        var question = await FollowupAsync("Available badges", components: new ComponentBuilder().WithSelectMenu(selectMenuBuilder).Build());
        var selectedOption = await MessageUtilities.AwaitComponentAsync(question.Id, Context.User.Id, MessageUtilities.ComponentType.SelectMenu);

        if (selectedOption is null)
        {
            await question.ModifyAsync(m =>
            {
                m.Content = "Command cancelled";
                m.Components = new ComponentBuilder().Build();
            });
            return;
        }

        int idToAdd = int.Parse(selectedOption.Data.Values.ElementAt(0));
        var dbBadge = dbBadges[idToAdd];

        var dbUser = db.Users.FindOrCreate(target);

        dbUser.Badges = [dbBadge];
        db.SaveChanges();

        await question.ModifyAsync(m =>
        {
            m.Content = $"Added {dbBadge.Name} to {target.Username}";
            m.Components = new ComponentBuilder().Build();
        });
    }
    [ModCommand]
    [SlashCommand("addemojicreatorbadge", "adds the emoji creator badge to a user")]
    public async Task AddEmojiCreatorBadge(SocketUser target, string emoji)
    {
        await DeferAsync();

        using var db = DbContextFactory.CreateDbContext();

        var dbUser = db.Users.FindOrCreate(target);

        var dbUniqueBadge = new UniqueBadge()
        {
            Data = emoji,
            BadgeType = UniqueBadgeType.EmojiCreator,
            Experience = 300,
            User = dbUser
        };

        db.UniqueBadges.Add(dbUniqueBadge);
        db.SaveChanges();

        await FollowupAsync($"added new emoji {emoji} to {target}");
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
            await FollowupAsync("You have already clicked this button", ephemeral: true);
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

        await FollowupAsync("Successfully joined!", ephemeral: true);
    }
    [ComponentInteraction(ComponentIds.ImagesRoleButtonId)]
    public async Task ImagesRoleButton()
    {
        await DeferAsync(ephemeral: true);

        var guildUser = (SocketGuildUser)Context.User;

        if (guildUser.Roles.Contains(Globals.ImagesRole))
        {
            await FollowupAsync("You already have the images role", ephemeral: true);
        }
        else if ((DateTimeOffset.UtcNow - guildUser.JoinedAt!).Value.Days < 2)
        {
            await FollowupAsync("You haven't been in the server for 2 or more days yet", ephemeral: true);
        }
        else
        {
            await guildUser.AddRoleAsync(Globals.ImagesRole);
            await FollowupAsync("Images role given successfully", ephemeral: true);
        }
    }
    [ComponentInteraction(ComponentIds.TicketButtonId)]
    public async Task TicketButton()
    {
        await RespondWithModalAsync<TicketModal>("TicketModal");
    }
    [ModalInteraction("TicketModal")]
    public async Task TicketModalResponse(TicketModal modal)
    {
        await DeferAsync(ephemeral: true);

        using var db = DbContextFactory.CreateDbContext();

        if (db.Tickets.Any(t => t.TicketUserId == Context.User.Id))
        {
            await FollowupAsync("You have an active ticket", ephemeral: true);
            return;
        }

        IThreadChannel thread = await Globals.FeedbackChannel.CreateThreadAsync($"{Context.User}: {modal.Reason}", ThreadType.PrivateThread, invitable: false);
        await Globals.FeedbackChannel.SendMessageAsync($"{Globals.ModRole.Mention} a new ticket has been created: {thread.Mention}");
        await thread.SendMessageAsync($"Hello {Context.User.Mention}, what can we help you with?");

        var dbTicket = new Ticket
        {
            TicketId = thread.Id,
            TicketUserId = Context.User.Id
        };
        db.Tickets.Add(dbTicket);
        db.SaveChanges();

        VolatileData.TicketThreads.TryAdd(thread.Id, Context.User.Id);

        await FollowupAsync($"Ticket created: {Globals.FeedbackChannel.Mention}", ephemeral: true);
    }
}
