using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

#nullable enable
namespace OriBot.Utility
{
    public static partial class PassiveResponseMatching
    {
        public static class RegexGenerators
        {
            public static string OR(string a, string b) => $"({a}|{b})";

            public static string MultichoiceOR(params string[] strings)
            {
                StringBuilder stringBuilder = new();
                stringBuilder.Append('(');
                for (int i = 0; i < strings.Length; i++)
                {
                    stringBuilder.Append(strings[i]);
                    if (i < strings.Length - 1)
                    {
                        stringBuilder.Append('|');
                    }
                }
                stringBuilder.Append(')');
                return stringBuilder.ToString();
            }

        }

        public static class RegexConstants
        {
            public static string atleastOnePunctuationWSpace => "(,|\\.|!|\\?|~|'|\"| )+";
            public static string atleastOnePunctuation => "(,|\\.|!|\\?|~|'|\")+";
            public static string atleastOneSpaceOrPeriod => "( |\\.)+";
            public static string atleastOneSpace => " +";

            public static string anyPunctuationWSpace => "(,|\\.|!|\\?|~|'|\"| )*";
            public static string anyPunctuation => "(,|\\.|!|\\?|~|'|\")*";
            public static string anySpaceOrPeriod => "( |\\.)*";
            public static string anySpace => " *";
        }


        public class Matcher(string pattern, bool casesensitive)
        {
            private readonly Regex _generatedRegex = new Regex(pattern, casesensitive ? RegexOptions.Compiled : RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public bool Match(string text)
            {
                var res = _generatedRegex.Match(text);
                return res.Success;

            }

            public bool MatchStrict(string text)
            {
                var res = _generatedRegex.Match(text);
                return res.Length == text.Length;

            }
        }

        public class MatcherBuilder()
        {
            private StringBuilder stringBuilder = new();
            public string Result => stringBuilder.ToString();

            public Matcher Build => new(Result,false);

            public MatcherBuilder AddSpace
            {
                get
                {
                    stringBuilder.Append(RegexConstants.atleastOneSpace);
                    return this;
                }
            }
            public MatcherBuilder AddBeginningMarker
            {
                get
                {
                    stringBuilder.Append('^');
                    return this;
                }
            }
            public MatcherBuilder AddPunctuation
            {
                get
                {
                    stringBuilder.Append(RegexConstants.atleastOnePunctuation);
                    return this;
                }
            }

            /// <summary>
            /// With space or period
            /// </summary>
            public MatcherBuilder AddSpaceOrPeriod
            {
                get
                {
                    stringBuilder.Append(RegexConstants.atleastOneSpaceOrPeriod);
                    return this;
                }
            }

            /// <summary>
            /// With any punctuation and spaces
            /// </summary>
            public MatcherBuilder AddPunctuationAndSpace
            {
                get
                {
                    stringBuilder.Append(RegexConstants.atleastOnePunctuationWSpace);
                    return this;
                }
            }

            public MatcherBuilder AddAnyLengthSpace
            {
                get
                {
                    stringBuilder.Append(RegexConstants.anySpace);
                    return this;
                }
            }
            public MatcherBuilder AddAnyPunctuation
            {
                get
                {
                    stringBuilder.Append(RegexConstants.anyPunctuation);
                    return this;
                }
            }

            /// <summary>
            /// With space or period
            /// </summary>
            public MatcherBuilder AddAnySpaceOrPeriod
            {
                get
                {
                    stringBuilder.Append(RegexConstants.anySpaceOrPeriod);
                    return this;
                }
            }

            /// <summary>
            /// With any punctuation and spaces
            /// </summary>
            public MatcherBuilder AddAnyPunctuationAndSpace
            {
                get
                {
                    stringBuilder.Append(RegexConstants.anyPunctuationWSpace);
                    return this;
                }
            }

            public MatcherBuilder AddTokens(params string[] tokens)
            {
                stringBuilder.Append('(');
                for (int i = 0; i < tokens.Length; i++)
                {
                    stringBuilder.Append(tokens[i]);
                    if (i < tokens.Length - 1)
                    {
                        stringBuilder.Append('|');
                    }
                }
                stringBuilder.Append(')');
                return this;
            }

            public MatcherBuilder AddCustom(string regex)
            {
                stringBuilder.Append(regex);
                return this;
            }
        }

    }
}
