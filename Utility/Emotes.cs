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
}
