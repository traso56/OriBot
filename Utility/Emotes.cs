using Discord;

namespace OriBot.Utility;

public static class Emotes
{
    public static IEmote Pin { get; } = new Emoji("📌");
    public static IEmote CrossMark { get; } = new Emoji("❌");

    public static IEmote OriHeart { get; } = Emote.Parse("<:OriHeart:628302358182363167>");
}
