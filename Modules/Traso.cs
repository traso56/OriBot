using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OriBot.Services;
using OriBot.Utility;
using System.Diagnostics;

namespace OriBot.Modules;

public class Traso : ModuleBase
{
    public required IHostApplicationLifetime ApplicationLifetime { get; set; }
    public required Globals Globals { get; set; }
    public required Discord.Interactions.InteractionService InteractionService { get; set; }
    public required VolatileData VolatileData { get; set; }
    public required MessageUtilities MessageUtilities { get; set; }
    public required IDbContextFactory<SpiritContext> DbContextFactory { get; set; }

    [ModCommand]
    [Command("stop")]
    public async Task Stop()
    {
        await ReplyAsync("Stopping systems");
        ApplicationLifetime.StopApplication();
    }
    [ModCommand]
    [Command("register")]
    public async Task Register()
    {
        await InteractionService.RegisterCommandsToGuildAsync(Globals.MainGuild.Id);
        await ReplyAsync("Registered");
    }
    [RequireOwner]
    [Command("resetdb")]
    [Summary("resets the database")]
    public async Task ResetDb()
    {
        bool? response = await MessageUtilities.UserConfirmation(Context, Context.User, "This will completely **NUKE** the database, proceed?");

        if (response != true)
            return;

        var db = DbContextFactory.CreateDbContext();

        Stopwatch sw = Stopwatch.StartNew();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        sw.Stop();

        await ReplyAsync("database reset, time: " + sw.ElapsedMilliseconds + "ms");
    }
    /********************************************
        SERVER STUFF
    ********************************************/
    [ModCommand]
    [Command("welcomemessage")]
    [Summary("creates the welcome message of the server")]
    public async Task WelcomeMessage([Remainder] string message)
    {
        ComponentBuilder buttonBuilder = new ComponentBuilder()
            .WithButton("Join", ComponentIds.WelcomeButtonCreate(), ButtonStyle.Success);

        VolatileData.IgnoredDeletedMessagesIds.Add(Context.Message.Id);
        await Context.Message.DeleteAsync();
        await ReplyAsync(message, components: buttonBuilder.Build());
    }
    [ModCommand]
    [Command("imagesrole")]
    [Summary("creates the images role message of the server")]
    public async Task ImagesRoleMessage([Remainder] string message)
    {
        ComponentBuilder buttonBuilder = new ComponentBuilder()
            .WithButton("Obtain images role", ComponentIds.ImagesRoleButtonCreate(), ButtonStyle.Success);

        VolatileData.IgnoredDeletedMessagesIds.Add(Context.Message.Id);
        await Context.Message.DeleteAsync();
        await ReplyAsync(message, components: buttonBuilder.Build());
    }
    [ModCommand]
    [Command("ticketbutton")]
    [Summary("creates the button with the ticket command")]
    public async Task TicketButton([Remainder] string message)
    {
        ComponentBuilder buttonBuilder = new ComponentBuilder()
            .WithButton("Create a new ticket", ComponentIds.TicketButtonCreate(), ButtonStyle.Success);

        VolatileData.IgnoredDeletedMessagesIds.Add(Context.Message.Id);
        await Context.Message.DeleteAsync();
        await ReplyAsync(message, components: buttonBuilder.Build());
    }
}
