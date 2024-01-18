using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace OriBot.Services;

public class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly InteractionService _interactionService;
    private readonly ExceptionReporter _exceptionReporter;
    private readonly Globals _globals;

    public InteractionHandler(DiscordSocketClient client, ILogger<InteractionHandler> logger, IServiceProvider provider,
        InteractionService interactionService, ExceptionReporter exceptionReporter, Globals globals)
        : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
        _exceptionReporter = exceptionReporter;
        _globals = globals;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

        _interactionService.InteractionExecuted += OnInteractionExecuted;
        Client.InteractionCreated += OnInteractionCreated;
    }

    private Task OnInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        Task.Run(async () =>
        {
            if (result.IsSuccess || command is null)
                return;

            if (result.Error == InteractionCommandError.Exception)
            {
                if (result is ExecuteResult executeResult)
                {
                    bool isSlashCommand = context.Interaction.Type == InteractionType.ApplicationCommand;
                    string errorMessage = $"There was an internal error, please check the logs, pinging {_globals.Traso.Mention}";
                    if (executeResult.Exception is DbUpdateConcurrencyException)
                        errorMessage = $"There was an error updating records in the database, perhaps it was updated elsewhere during this command, pinging {_globals.Traso.Mention}";
                    else if (executeResult.Exception is OverflowException)
                        errorMessage = executeResult.Exception.Message + $", pinging {_globals.Traso.Mention}";

                    if (context.Interaction.CreatedAt.AddSeconds(3) < DateTimeOffset.UtcNow)
                    {
                        if (isSlashCommand)
                            await context.Channel.SendMessageAsync(errorMessage);
                    }
                    else if (!context.Interaction.HasResponded)
                    {
                        await context.Interaction.RespondAsync(errorMessage, ephemeral: !isSlashCommand);
                    }
                    else
                    {
                        await context.Interaction.FollowupAsync(errorMessage, ephemeral: !isSlashCommand);
                    }

                    var exceptionContext = new ExceptionContext(context.Channel);
                    await _exceptionReporter.NotifyExceptionAsync(executeResult.Exception, exceptionContext, "Exception while executing an interaction", false);
                }
            }
            else
            {
                if (!context.Interaction.HasResponded)
                    await context.Interaction.RespondAsync(result.ErrorReason, ephemeral: true);
                else
                    await context.Interaction.FollowupAsync(result.ErrorReason);
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(context.Channel);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing interaction executed event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    private Task OnInteractionCreated(SocketInteraction interaction)
    {
        Task.Run(async () =>
        {
            SocketInteractionContext context = new SocketInteractionContext(Client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _provider);
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(interaction.Channel);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing interaction created event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
}
