using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OriBot.Utility
{
   public static partial class FilteringAndAntiSpam
    {

        public static List<string> StringWithEmotesToSeparated(string text)
        {
            var info = new StringInfo(text);

            List<string> parsed = [];

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                var glyph = enumerator.GetTextElement();
                parsed.Add(glyph);
            }
            return parsed;
        }

        public enum TypeOfText
        {
            Emote,
            String
        }

        public class EmoteOrString
        {
            public TypeOfText typeOfText = TypeOfText.String;
            public StringBuilder content = new StringBuilder();
        }

        [GeneratedRegex("<:[A-z_0-9]+:[0-9]+>", RegexOptions.Compiled)]
        public static partial Regex EmojiMatch();

        [GeneratedRegex("<a:[A-z_0-9]+:[0-9]+>", RegexOptions.Compiled)]
        public static partial Regex AnimatedEmojiMatch();

        public static List<EmoteOrString> RecognizeDiscordEmotes(List<string> strings)
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

                // (unicode emote)
                if (Discord.Emoji.TryParse(item, out var emote))
                {
                    result.Add(emoteOrString);
                    emoteOrString = new EmoteOrString();
                    emoteOrString.typeOfText = TypeOfText.Emote;
                    emoteOrString.content.Append(emote.Name);
                    result.Add(emoteOrString);
                    emoteOrString = new EmoteOrString();
                    continue;
                }

                // <...
                if (item == "<" && !areWeEscaping)
                {
                    if (i == strings.Count - 1 ||
                        !(strings[i + 1] == ":" || strings[i + 1] == "a")
                    )
                    {
                        // <(anything other than : and a)
                        emoteOrString.content.Append(item);
                        continue;
                    }
                    // <(a|:)....
                    result.Add(emoteOrString);
                    emoteOrString = new EmoteOrString();
                    parsingEmote = true;
                    emoteOrString.typeOfText = TypeOfText.Emote;
                    emoteOrString.content.Append(item);
                    continue;
                }
                if (parsingEmote)
                {
                    emoteOrString.content.Append(item);
                    if (item == ">")
                    {
                        parsingEmote = false;
                        var emote2 = emoteOrString.content.ToString();
                        if (EmojiMatch().Matches(emote2).Count == 1 || AnimatedEmojiMatch().Matches(emote2).Count == 1)
                        {
                            result.Add(emoteOrString);
                            emoteOrString = new EmoteOrString();
                        }
                    }
                }
                else
                {
                    emoteOrString.content.Append(item);
                }
                if (item != @"\")
                {
                    areWeEscaping = false;
                }
            }
            result.Add(emoteOrString);
            return result;
        }


    }
}
