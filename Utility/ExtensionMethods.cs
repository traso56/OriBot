using Discord;
using Microsoft.EntityFrameworkCore;

namespace OriBot.Utility;

public static class ExtensionMethods
{
    public static string ToReadableString(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? "" : "s") : "",
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? "" : "s") : "",
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? "" : "s") : "",
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? "" : "s") : "");

        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

        return formatted;
    }
    public static EmbedBuilder AddLongField(this EmbedBuilder builder, string name, string value, string textIfEmpty)
    {
        if (value.Length <= 1024)
        {
            builder.AddField(name, value == "" ? textIfEmpty : value);
        }
        else
        {
            for (int i = 0, j = 1; i < value.Length; i += 1024, j++)
                builder.AddField(name + " part " + j, value.Substring(i, Math.Min(1024, value.Length - i)));
        }
        return builder;
    }
    public static EmbedBuilder AddUserAvatar(this EmbedBuilder embedBuilder, IUser user)
    {
        if (user is IGuildUser guildUser)
            embedBuilder.WithThumbnailUrl(guildUser.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl());
        else
            embedBuilder.WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
        return embedBuilder;
    }
    public static EmbedBuilder WithDirectUserLink(this EmbedBuilder embedBuilder, IUser user)
    {
        embedBuilder.AddField("If user is not in cache this link may work", $"<discord://-/users/{user.Id}>");
        return embedBuilder;
    }
    public static User FindOrCreate(this DbSet<User> set, IUser user)
    {
        return set.Find(user.Id) ?? set.Add(new User()
        {
            UserId = user.Id,
            Title = null,
            Description = null,
            Color = ColorConstants.SpiritBlack
        }).Entity;
    }
}
