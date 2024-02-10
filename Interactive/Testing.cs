using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OriBot.Services;
using OriBot.Utility;

namespace OriBot.Interactive;

[RequireContext(ContextType.Guild)]
public class Testing : InteractionModuleBase<SocketInteractionContext>
{
    public required Globals Globals { get; set; }

    public required GenAI GenAIService { get; set; }

    [ModCommand]
    [SlashCommand("throw", "throws")]
    public async Task Throw(bool withResponse)
    {
        if (withResponse)
            await RespondAsync("response");
        throw new InvalidOperationException("test exception");
    }

    [ModCommand]
    [SlashCommand("ai", "Tests Gen AI")]
    public async Task AI(string query)
    {
        var response = await GenAIService.QueryAsync(
            new GenAI.Query.RootBuilder()
            .Build()
        );
        await RespondAsync(response.Candidates.First().Content.Parts.First().Text);
    }
}
