namespace OriBot;

public class BotOptions
{
    public const string BotSettings = "BotSettings";

    public required string Prefix { get; set; }
    public required string DiscordToken { get; set; }

    public required ulong MainGuildId { get; set; }

    public required ulong ArtChannelId { get; set; }

    public required ulong InfoChannelId { get; set; }

    public required ulong LogChannelId { get; set; }
    public required ulong NotesChannelId { get; set; }
    public required ulong AutosChannelId { get; set; }
    public required ulong FeedbackChannelId { get; set; }
    public required ulong CommandsChannelId { get; set; }
    public required ulong MembersChannelId { get; set; }
    public required ulong StarBoardChannelId { get; set; }
    public required ulong VoiceActivityChannelId { get; set; }

    public required ulong MemberRoleId { get; set; }
    public required ulong ImagesRoleId { get; set; }
    public required ulong ModRoleId { get; set; }
}
public class CooldownOptions
{
    public required int FrequentTaskIntervalHours { get; set; }
    public required int RareTaskIntervalHours { get; set; }
}
public class ComponentNegativeResponsesOptions
{
    public required string[] Responses { get; set; }

    private readonly Random _random = new Random();
    public string GetRandomResponse() => Responses[_random.Next(Responses.Length)];
}
public class UserJoinOptions
{
    public required int NewUsersMinDays { get; set; }
    public required int ImageRoleDays { get; set; }
}
public class PinOptions
{
    public required int PinAmount { get; set; }
}
