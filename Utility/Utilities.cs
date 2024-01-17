using Discord.WebSocket;
using Discord;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OriBot.Utility;

public static partial class Utilities
{
    /// <summary>
    /// creates a filepath to retrieve files from the local program directory.
    /// </summary>
    /// <param name="relativeFileLocation">The filepath to append.</param>
    /// <returns>The full filepath.</returns>
    public static string GetLocalFilePath(string relativeFileLocation) => Path.Combine(AppContext.BaseDirectory, "Files", relativeFileLocation);

    /// <summary>
    /// A method to disable all buttons in a <see cref="ComponentBuilder"/>.
    /// </summary>
    /// <param name="buttonBuilder">The <see cref="ComponentBuilder"/> to get the buttons from.</param>
    /// <returns>A new <see cref="ComponentBuilder"/> with buttons disabled in i.t</returns>
    public static ComponentBuilder DisableAllButtons(ComponentBuilder buttonBuilder)
    {
        var newButtonBuilder = new ComponentBuilder();

        var rows = buttonBuilder.ActionRows;

        for (int i = 0; i < rows.Count; i++)
        {
            foreach (var component in rows[i].Components)
            {
                switch (component)
                {
                    case ButtonComponent button:
                        newButtonBuilder.WithButton(button.ToBuilder()
                            .WithDisabled(true), i);
                        break;
                    case SelectMenuComponent menu:
                        newButtonBuilder.WithSelectMenu(menu.ToBuilder()
                            .WithDisabled(true), i);
                        break;
                }
            }
        }
        return newButtonBuilder;
    }
    /// <summary>
    /// Tries to create a <see cref="Color"/> from the hex string provided.
    /// </summary>
    /// <param name="hex">The hex <see langword="string"/>.</param>
    /// <param name="color">The <see cref="Color"/> created.</param>
    /// <returns><see langword="true"/> if the parsing was succesful.</returns>
    public static bool TryParseHexToColor(string hex, out Color color)
    {
        try
        {
            color = Convert.ToUInt32(hex, 16);
            return true;
        }
        catch (FormatException)
        {
            color = default;
            return false;
        }
    }
    /// <summary>
    /// Creates an <see cref="EmbedBuilder"/> with the information from an user message.
    /// </summary>
    /// <param name="title">Title of the <see cref="EmbedBuilder"/></param>
    /// <param name="message">The <see cref="IUserMessage"/> to get data from.</param>
    /// <param name="color">The <see cref="Color"/> of the embed.</param>
    /// <param name="includeOriginChannel">If the origin channel of the message should be included.</param>
    /// <param name="includeDirectUserLink">If a special link should be included to check a user directly.</param>
    /// <param name="includeMessageReference">If the message reference should be included if it exists.</param>
    /// <returns>An <see cref="EmbedBuilder"/> that contains message content, user avatar, user mention, creation date, author id in footer and the sticker if any</returns>
    public static EmbedBuilder QuoteUserMessage(string title, IUserMessage message, Color color, bool includeOriginChannel, bool includeDirectUserLink, bool includeMessageReference)
    {
        var embedBuilder = new EmbedBuilder()
            .WithColor(color)
            .AddUserAvatar(message.Author)
            .WithTitle(title)
            .AddLongField("Message", message.Content, "Message does not have text")
            .AddField("Mention", message.Author.Mention, true)
            .AddField("Message was created on", FullDateTimeStamp(message.CreatedAt), true)
            .WithFooter($"Author ID: {message.Author.Id}")
            .WithCurrentTimestamp();

        if (includeOriginChannel)
            embedBuilder.AddField("Channel", $"<#{message.Channel.Id}>", true);
        if (includeDirectUserLink)
            embedBuilder.WithDirectUserLink(message.Author);
        if (includeMessageReference && message.ReferencedMessage is not null)
        {
            string messageReference = message.ReferencedMessage.Content != "" ? message.ReferencedMessage.Content : "Reference does not have text";
            if (messageReference.Length > 1024)
                messageReference = messageReference[..1024];
            embedBuilder.AddField($"Reference by {message.ReferencedMessage.Author} created on {FullDateTimeStamp(message.CreatedAt)}", messageReference);
        }

        var sticker = message.Stickers.FirstOrDefault() as SocketSticker;
        if (sticker is not null)
            embedBuilder
                .AddField("Sticker", " Format: " + sticker.Format)
                .WithImageUrl(sticker.GetStickerUrl());

        return embedBuilder;
    }
    /// <summary>
    /// Converts all the attachments in a <see cref="IUserMessage"/> to a string of urls to send.
    /// </summary>
    /// <param name="message">The message to take the attachmets from.</param>
    /// <returns>a string with all the attachment links, null if there are none.</returns>
    public static string? MessageAttachmentsToUrls(IUserMessage message)
    {
        if (message.Attachments.Count == 0)
            return null;

        var stringBuilder = new StringBuilder();

        stringBuilder.Append("Attachments:");
        foreach (var attachment in message.Attachments)
        {
            stringBuilder.Append("\n[").Append(attachment.Filename).Append("](").Append(attachment.Url).Append(')');
        }

        return stringBuilder.ToString();
    }
    /// <summary>
    /// Creates a combination of 2 timestamps so it can show the full date and time including seconds.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTimeOffset"/> to base the timestamps off.</param>
    /// <returns>A <see langword="string"/> with 2 timestamps to make the full datetime.</returns>
    public static string FullDateTimeStamp(DateTimeOffset dateTime)
    {
        return TimestampTag.FormatFromDateTimeOffset(dateTime, TimestampTagStyles.LongDate) +
                " " + TimestampTag.FormatFromDateTimeOffset(dateTime, TimestampTagStyles.LongTime);
    }
    /// <summary>
    /// Gives a badge to a user. If they already have it, then it adds one to their count.
    /// </summary>
    /// <param name="db">The context to use.</param>
    /// <param name="user">The user to give this badge to.</param>
    /// <param name="badgeName">The name of the badge to give.</param>
    public static void AddBadgeToUser(SpiritContext db, IUser user, string badgeName)
    {
        var dbBadge = db.Badges.Single(b => b.Name == badgeName);
        var dbUser = db.Users.FindOrCreate(user);
        db.Entry(dbUser).Collection(u => u.UserBadges).Load();
        var currentBadges = dbUser.UserBadges.Find(ub => ub.BadgeId == dbBadge.BadgeId);

        if (currentBadges is not null)
            currentBadges.Count++;
        else
            dbUser.Badges = [dbBadge];
    }
    /// <summary>
    /// Removes a badge from a user, if their badge count reaches 0 then the record is removed altogether
    /// </summary>
    /// <param name="db">The context to use.</param>
    /// <param name="user">The user to remove this badge from.</param>
    /// <param name="badgeName">The name of the badge to remove.</param>
    public static void RemoveBadgeFromUser(SpiritContext db, IUser user, string badgeName)
    {
        var dbBadge = db.Badges.Single(b => b.Name == badgeName);
        var dbUser = db.Users.FindOrCreate(user);
        db.Entry(dbUser).Collection(u => u.UserBadges).Load();
        var currentBadges = dbUser.UserBadges.Find(ub => ub.BadgeId == dbBadge.BadgeId);

        if (currentBadges is not null)
        {
            currentBadges.Count--;
            if (currentBadges.Count == 0)
                dbUser.UserBadges.Remove(currentBadges);
        }
    }
    /// <summary>
    /// Converts an integer to the romnan numeral representation. If the number is over 3999 then ite returns the number itself as that is the maximum roman numeral.
    /// </summary>
    /// <param name="num">The number to comvert.</param>
    /// <returns>The string representing the roman numeral.</returns>
    public static string IntToRoman(int num)
    {
        if (num >= 4000)
        {
            return num.ToString();
        }

        int[] values = {
            1000, 900, 500, 400,
            100, 90, 50, 40,
            10, 9, 5, 4,
            1
        };

        string[] symbols = {
            "M", "CM", "D", "CD",
            "C", "XC", "L", "XL",
            "X", "IX", "V", "IV",
            "I"
        };

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            while (num >= values[i])
            {
                num -= values[i];
                sb.Append(symbols[i]);
            }
        }

        return sb.ToString();
    }

    public static List<string> StringWithEmotesToSeparated(string text)
    {
        var info = new StringInfo(text);

        List<string> parsed = [];

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {

            var glyph = enumerator.GetTextElement();
            if (Discord.Emoji.TryParse(glyph, out var emote))
            {
                parsed.Add(emote.ToString());
            }
            else
            {
                parsed.Add(glyph);
            }
        }
        return parsed;
    }

    enum TypeOfText
    {
        Emote,
        String
    }

    class EmoteOrString
    {
        public TypeOfText typeOfText = TypeOfText.String;
        public StringBuilder content = new StringBuilder();
    }

    public static List<string> RecognizeDiscordEmotes(List<string> strings)
    {
        var areWeEscaping = false;
        var parsingEmote = false;
        var result = new List<EmoteOrString>();
        var emoteOrString = new EmoteOrString();
        for (int i = 0; i < strings.Count; i++)
        {
            var item = strings[i];
            if (item == @"\")
            {
                areWeEscaping = !areWeEscaping;
            }
            else
            {
                areWeEscaping = false;
            }

            if (Discord.Emoji.TryParse(item, out var emote))
            {
                emoteOrString.content.Append(emote.Name);
                result.Add(emoteOrString);
                emoteOrString = new EmoteOrString();
                continue;
            }

            // <...
            if (item == "<" && !areWeEscaping)
            {
                if (i == strings.Count - 1 || strings[i + 1] != ":")
                {
                    // <(anything other than :)
                    emoteOrString.content.Append(item);
                    continue;
                }
                // <:....
                parsingEmote = true;
                emoteOrString.typeOfText = TypeOfText.Emote;
                emoteOrString.content.Append(item);
                continue;
            }

            emoteOrString.content.Append(item);
        }
        return [];
    }

    public static int EmojiCounter(string text)
    {
        return Emoji().Matches(text).Count;
    }

    private static partial Regex Emoji();
}
