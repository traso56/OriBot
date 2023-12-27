using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OriBot.Services;

public class Globals : DiscordClientService
{
    public IUser Traso { get; set; } = null!;

    // we will init these in ExecuteAsync()
    public IGuild MainGuild { get; private set; } = null!;

    public ITextChannel ArtChannel { get; private set; } = null!;

    /// <summary>
    /// Channel for exception logs
    /// </summary>
    public ITextChannel InfoChannel { get; private set; } = null!;

    /// <summary>
    /// Channel for moderation actions
    /// </summary>
    public ITextChannel LogChannel { get; set; } = null!;
    /// <summary>
    /// Channel for deleted messages
    /// </summary>
    public ITextChannel NotesChannel { get; set; } = null!;
    /// <summary>
    /// Channel for automatic actions
    /// </summary>
    public ITextChannel AutosChannel { get; set; } = null!;
    /// <summary>
    /// Channel for tickets/feedback
    /// </summary>
    public ITextChannel FeedbackChannel { get; set; } = null!;
    /// <summary>
    /// Channel for commands
    /// </summary>
    public ITextChannel CommandsChannel { get; set; } = null!;
    /// <summary>
    /// Channel for joins/leaves
    /// </summary>
    public ITextChannel MembersChannel { get; set; } = null!;
    /// <summary>
    /// Channel for pinned messages
    /// </summary>
    public ITextChannel StarBoardChannel { get; set; } = null!;

    public IRole MemberRole { get; set; } = null!;
    public IRole ImagesRole { get; set; } = null!;
    public IRole ModRole { get; set; } = null!;

    private readonly BotOptions _botOptions;

    public Globals(DiscordSocketClient client, ILogger<DiscordClientService> logger, IOptions<BotOptions> options, Personality personality)
        : base(client, logger)
    {
        personality.test();
        _botOptions = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken);

        Logger.LogInformation("Setting channels...");

        Traso = await Client.GetUserAsync(194108558177075201); // traso ID

        MainGuild = Client.GetGuild(_botOptions.MainGuildId);

        ArtChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.ArtChannelId);

        InfoChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.InfoChannelId);

        LogChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.LogChannelId);
        NotesChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.NotesChannelId);
        AutosChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.AutosChannelId);
        FeedbackChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.FeedbackChannelId);
        CommandsChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.CommandsChannelId);
        MembersChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.MembersChannelId);
        StarBoardChannel = (ITextChannel)await Client.GetChannelAsync(_botOptions.StarBoardChannelId);

        MemberRole = MainGuild.GetRole(_botOptions.MemberRoleId);
        ImagesRole = MainGuild.GetRole(_botOptions.ImagesRoleId);
        ModRole = MainGuild.GetRole(_botOptions.ModRoleId);

        Logger.LogInformation("Finished setting channels.");
    }
}
