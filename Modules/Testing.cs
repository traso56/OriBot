using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using System.Linq;

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
    [ModCommand]
    [Command("debugroles")]
    public async Task DebugRoles()
    {
        var guildUser = (SocketGuildUser)Context.User;

        StringBuilder stringBuilder = new StringBuilder();
        foreach (var role in guildUser.Roles.Where(role => role.Name != "@everyone"))
            stringBuilder.AppendLine(role.Name);

        await ReplyAsync(stringBuilder.ToString());
    }
}
