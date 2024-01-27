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
    public required ulong AnyModRoleId { get; set; }
    public required ulong UnavailableModRoleId { get; set; }
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
public class NewPassiveResponsesOptions
{
    public required bool Enabled { get; set; }
    public required bool AllowInAnyChannel { get; set; }

    /// <summary>
    /// The time that a user must wait before they can get another response from the bot.
    /// </summary>
    public required int CooldownTimeMS { get; set; }

    /// <summary>
    /// Whether or not the cooldown system is enabled.
    /// </summary>
    public required bool IsCooldownEnabled { get; set; }

    /// <summary>
    /// The chance of Ku chiming in.
    /// </summary>
    public required int KuChance { get; set; }

    /// <summary>
    /// Force the system to believe it's march 11.
    /// </summary>
    public required bool ForceBirthday { get; set; }
}
public class MessageAmountQuerying
{
    public required string ApiUrl { get; set; }
    public required string UserId { get; set; }
}

public class RuntimeCompileOptions
{
    
    public required string CompiledPath { get; set; }
    public required string DotnetBinPath { get; set; }
}
