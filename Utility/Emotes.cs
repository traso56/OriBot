using Discord;

namespace OriBot.Utility;

public static class Emotes
{
    // default emotes
    public static IEmote Pin { get; } = new Emoji("📌");
    public static IEmote CrossMark { get; } = new Emoji("❌");
    public static IEmote PoliceCarEmote { get; } = new Emoji("🚓");
    public static IEmote LightBulbEmote { get; } = new Emoji("💡");

    // custom emotes
    public static IEmote OriHeart { get; } = Emote.Parse("<:OriHeart:628302358182363167>");
    public static IEmote NaruEmote { get; } = Emote.Parse("<:Naru:671886905440206849>");
    public static IEmote OriHugKu { get; } = Emote.Parse("<:OriHugKu:693635899312963605>");
    public static IEmote OriWave { get; } = Emote.Parse("<:OriWave:716054855197786113>");
    public static IEmote OriHype { get; } = Emote.Parse("<:OriHype:628302357922316329>");
    public static IEmote OriCry { get; } = Emote.Parse("<:OriCry:628302358098739220>");
    public static IEmote OriKu { get; } = Emote.Parse("<:Ku:628302358182625331>");
    public static IEmote OriFace { get; } = Emote.Parse("<:Ori:671886904773443584>");
}
