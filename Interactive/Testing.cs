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

    [RequireOwner]
    [SlashCommand("throw", "throws")]
    public async Task Throw(bool withResponse)
    {
        if (withResponse)
            await RespondAsync("response");
        throw new InvalidOperationException("test exception");
    }
}
