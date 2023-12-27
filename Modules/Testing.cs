using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace OriBot.Modules;

public class Testing : ModuleBase
{
    public required DiscordSocketClient Client { get; set; }

    [RequireOwner]
    [Command("setstatus")]
    public async Task SetStatus([Remainder] string stauts)
    {
        await Client.SetCustomStatusAsync(stauts);
        await ReplyAsync("Status set");
    }
    [RequireOwner]
    [Command("test")]
    public async Task Test()
    {
        await ReplyAsync("Message Received");
    }
    [RequireOwner]
    [Command("testexception")]
    public Task TestException()
    {
        throw new NotImplementedException();
    }
}
