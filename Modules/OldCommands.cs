using Discord.Commands;

namespace OriBot.Modules;

public class OldCommands : ModuleBase
{
    [CommandsChannel]
    [Command("help")]
    public async Task Help()
    {
        await ReplyAsync("This command has been discontinued. Please use /help instead");
    }
    [CommandsChannel]
    [Command("role")]
    public async Task Role([Remainder] string role)
    {
        await ReplyAsync("This command has been discontinued. Please use <id:customize> instead.");
    }
    [CommandsChannel]
    [Command("colorme")]
    public async Task Color([Remainder] string color)
    {
        await ReplyAsync("This command has been discontinued. Please use <id:customize> instead.");
    }
    [CommandsChannel]
    [Command("profile")]
    public async Task Profile([Remainder] string? profile = null)
    {
        await ReplyAsync("This command has been discontinued. Please use /profile instead.");
    }
}
