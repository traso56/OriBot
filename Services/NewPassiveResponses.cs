﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static OriBot.Utility.PassiveResponseMatching;

using OriBot.Utility;
using Discord.Commands;
using Discord;

namespace OriBot.Services
{
#nullable enable
    public static class QueryLibrary
    {
        #region Hellos / Goodbyes
        public static readonly string[] Greetings = [
                    "hi",
            "hello",
            "hey",
            "whats up",
            "heya",
            "hiya",
            "yo",
            "greetings",
            "sup",
            "whats poppin",
            "whats crackin",
            "howdy there",
            "howdy",
            "well howdy there",
            "well howdy",
            "gday",
            "gday 2u",
            "gday 2 you",
            "gday 2 u",
            "good day",
            "goodday",
            "good day to you",
            "gday to you",
            "gday to u",
            "good day to u",
            "good day 2 you",
            "good day 2 u",
            "good day 2u",
            "yello",
            "yellow",
            "yelo",
            "elo",
            "ey",
            "salutations",
            "henlo",
            "ello",
            "ayo",
            "eyo",
            "hio"
                ];

        public static readonly string[] Goodbyes = [
                    "bye",
            "peace",
            "see ya",
            "later",
            "see you",
            "see ya later",
            "see you later",
            "goodbye",
            "cya",
            "im out",
            "im headed off",
            "im headed out",
            "im heading off",
            "im heading out",
            "im leaving",
            "ill be back later",
            "brb",
            "gtg",
            "g2g",
            "gotta go",
            "got to go",
            "im gonna go",
            "im going",
            "bbl",
            "ima dip",
            "gonna dip",
            "im gonna dip",
            "see u",
            "see u later",
            "have a good day",
            "have a nice day"
                ];
        #endregion
        #region Good (time of day)s & Asking About Bot
        public static string[] goodnightOptions = new string[]
{
    "nightnight", "night", "night-night", "goodnight", "gnight", "bedtime", "time for bed",
    "going to bed", "going to sleep", "sleepytime", "time to sleep", "time for me to sleep",
    "off to sleep", "off to bed", "nightynight", "gonna go to bed", "gonna go to sleep", "good night"
};

        public static string[] goodmorningOptions = new string[]
{
    "morning", "good morning", "gmorning", "mornin", "good mornin"
};

        public static string[] goodafternoonOptions = new string[]
{
    "good afternoon to you", "good afternoon to ya", "good afternoon to ye", "good afternoon to u",
    "gafternoon to you", "gafternoon to ya", "gafternoon to ye", "gafternoon to u",
    "afternoon to you", "afternoon to ya", "afternoon to ye", "afternoon to u",
    "afternoon", "good afternoon", "after noon", "good after noon", "gafternoon"
};

        public static string[] goodeveningOptions = new string[]
{
    "good evening to you", "good evening to ya", "good evening to ye", "good evening to u",
    "gevening to you", "gevening to ya", "gevening to ye", "gevening to u",
    "evening to you", "evening to ya", "evening to ye", "evening to u",
    "evening", "good evening", "gevening"
};

        public static string[] asktimepastOptions = new string[]
{
    "how was your day", "how was ur day", "how did ur day go", "how did your day go", "howd your day go", "howd ur day go", "you have a good day", "u have a good day",
    "how was your night", "how was ur night", "how did ur night go", "how did your night go", "howd your night go", "howd ur night go", "you have a good night", "u have a good night"
};

        public static string[] asktimenowOptions = new string[]
{
    "how is your day", "how is ur day", "hows your day", "hows ur day",
    "how is your night", "how is ur night", "hows your night", "hows ur night"
};

        public static string[] favoritecolorOptions = new string[]
{
    "what is ur favorite color", "whats ur favorite color", "what is your favorite color", "whats your favorite color",
    "tell me your favorite color", "tell me ur favorite color", "which color do you like most", "which color do u like most",
    "which color do you like the most", "which color do u like the most", "what color do you like most", "what color do u like most",
    "what color do you like the most", "what color do u like the most"
};
        #endregion
        #region Statuses / Activities
        public static string[] askStatusOptions = new string[]
{
    "how are you", "how r u", "how are you feeling", "how r u feeling", "how are you doing", "how r u doing",
    "how are u", "how r you", "how r ya", "how are ya", "how are ye", "how r ye"
};

        public static string[] askStatusYNOptions = new string[]
{
    "are you doing well", "are you doing good", "you doing well", "you doing good",
};

        public static string[] askActivityOptions = new string[]
{
    "what are you up to", "what r u up to", "whatre you up to", "whatre u up to",
    "what r u doing", "what are you doing"
};

        public static string[] loveOptions = new string[]
{
    "i love", "i love you",
    "love you", "i love u", "love u",
    "i luv", "i luv you", "luv you", "luv u",
    "ily", "ly"
};
        #endregion
        #region Direct References to Individuals / Bot
        public static string[] askHowEtiOptions = new string[]
{
    "how is eti", "how is xan", "hows eti", "hows xan"
};

        public static string[] askWhatEtiOptions = new string[]
{
    "what is eti doing", "whats eti doing", "what is xan doing", "whats xan doing",
    "what is eti up to", "whats eti up to", "what is xan up to", "whats xan up to"
};

        public static string[] oriBotOptions = new string[]
{
    // ((<:)([a-z]|[A-Z]|[0-9]|_)+:)+
    "ori", "ori-o", "orio", "ori#8480", "616136907860213760", "<@616136907860213760>", "<@!616136907860213760>"
};
        #endregion
        #region Comments to the bot
        public static string[] tellActivityGoodOptions = new string[]
{
    "i hope you are doing well", "i hope you are doing good", "i hope u r doing well", "i hope u r doing good",
    "i hope youre doing well", "i hope youre doing good", "i hope ur doing well", "i hope ur doing good",
    "i hope your day is going well", "i hope your day is going good", "i hope ur day is going well", "i hope ur day is going good"
};

        public static string[] thanksOptions = new string[]
{
    "thank you", "thank u", "thx", "thanks", "thnx", "thank ya", "thank ye", "thanx", "thankies", "tanks", "tnx",
    "big mcthankies from mcspankies",
    "thx you", "thx u", "thnx you", "thnx u",
    "tnx you", "tnx u"
};
        #endregion
        #region Birthday
        public static string[] birthdayOptions = new string[]
{
    "happy birthday", "happy bday", "happy b-day"
};
        #endregion

        #region Ku
        public static string RandomKuResponse
        {
            get
            {
                var randomnumber = new Random().Next(100);

                List<string> kuresponses = ["Hoot.",
                    "*Feathers Ruffling.*",
                    "*Crunch. Mmm. Tasty grub.*",
                    "*Scratch. Scratch.*",
                    "*Not even paying attention at this point. Distracted by that cloud. It's a nice cloud.*",
                    "*Oh, look over there! ...Wait, nevermind it's just a leaf... ...Wait.*",
                    "*Flap. Flap. Wing stretch.*",
                    "*Blink.*",
                    "*Click. Click. Yes, beak clicking. Very good.*",
                    "*Confused at the possibility of owls being brown or white. How would that even work? They're always violet...*",
                    "*Poof.*",
                    "*Sneeze.*",
                    "*Blink. Again.*",
                    "*Staring at the top of your head.*",
                    "*Now has hold of your command. You're not getting it back now.*",
                    "*Looking around.*"
            ];

                List<string> kuresponsesrare = ["You know, I *am* capable of speech. ....Yes, mom, text to speech is real speech!",
                    "Why is everybody asking me to type in \"John Madden\" into this thing? And \"aeiou\"? What?"
            ];
                if (randomnumber == 1)
                {
                    return kuresponsesrare.Random();
                }
                return kuresponses.Random();
            }
        }
        #endregion
        #region Asking Ori's Gender
        public static readonly string[] AskingAboutOriGender = new string[] {
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a boy or a girl",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} boy or girl",

    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} male or female",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a male or a female",

    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a boy or girl",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a male or female",

    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a girl or a boy",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} girl or boy",

    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} female or male",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a female or a male",

    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a girl or boy",
    $"{PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a female or male",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a boy or a girl",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} boy or girl",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} male or female",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a male or a female",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a boy or girl",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a male or female",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a girl or a boy",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} girl or boy",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} female or male",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a female or a male",

    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a girl or boy",
    $"is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} a female or male",

    $"what is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}s gender",
    $"what is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}'s gender",
    $"whats {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}s gender",
    $"whats {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}'s gender",
    $"what is the gender of {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}",
    $"whats the gender of {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}",
    $"what gender is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)}",

    $"whats {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} gender",
    $"what is {PassiveResponseMatching.RegexGenerators.MultichoiceOR(oriBotOptions)} gender",
};
        #endregion
    }

    public class MatcherAndResponses(Matcher matcher, List<string> responses)
    {
        private readonly Matcher _matcher = matcher;
        private readonly List<string> _responses = responses;

        public bool Match(string query, out List<string>? responses2)
        {
            if (_matcher.MatchStrict(query))
            {
                responses2 = _responses;
                return true;
            }
            responses2 = null;
            return false;
        }

        public bool MatchRandom(string query, out string? response)
        {
            if (_matcher.MatchStrict(query))
            {
                response = _responses.Random();
                return true;
            }
            response = null;
            return false;
        }
    }

    public static class QuestionsAndResponses
    {
        public static MatcherAndResponses askingAboutGender = new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.AskingAboutOriGender)
                .Build,
                [
                    "You can refer to me by anything, really! Choose whatever term you think fits me best."
                ]
            );

        public static readonly MatcherAndResponses[] QnA = [
            #region Hi to ori
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.Greetings)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Hi, {USERPING}!",
                    "Hi!",
                    "Oh! How are you, {USERPING}? " + Emotes.OriHeart,
					// ":wave:",
					// As proposed by Stretch#0588 714588316845998121...
					Emotes.OriWave.ToString()!,
                    "Hey!",
                    "Good to see you, {USERPING}!" + Emotes.OriHeart.ToString()!,
                ]
            ),
            #endregion
            #region Good (time) ori
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.goodmorningOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Good morning!",
                    "Did you have a good night's rest? " + Emotes.OriHeart.ToString()!,
                    "Did you sleep well? " + Emotes.OriHeart.ToString()!,
                    "Are you well-rested?",
                    "Morning!",
                    "Hey! Did you remember to eat your breakfast?",
                    "Ready to start the day? " + Emotes.OriHype.ToString()!
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.goodafternoonOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "To you too!",
                    "Good afternoon!",
                    "Thanks, {USERPING} " + Emotes.OriHeart.ToString()!,
                    "It sure is!"
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.goodeveningOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "To you too!",
                    "Good evening!",
                    "Thanks, {USERPING} " + Emotes.OriHeart.ToString()!,
                    "It sure is!"
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.goodnightOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Goodnight!",
                    "Have a good rest! " + Emotes.OriHeart.ToString()!,
                    "I'll see you tomorrow, {USERPING}! " + Emotes.OriHeart.ToString()!,
                    "Oh! Have a good night. " + Emotes.OriHeart.ToString()!,
                    "Night!",
                    "Sleep tight! " + Emotes.OriHeart.ToString()!,
                ]
            ),
            #endregion
            #region Goodbye ori
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.Goodbyes)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Bye! " +  Emotes.OriHeart.ToString()!,
                    "See you later!",
                    "See you soon! " +  Emotes.OriHeart.ToString()!,
                    "Will I see you soon? " +  Emotes.OriCry.ToString()!,
                    ":wave: Goodbye!"
                ]
            ),
            #endregion
            #region Asking the bot
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.asktimepastOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "It was good! " +  Emotes.OriHype.ToString()!,
                    "It was great!",
                    "Pretty good.",
                    "Not bad at all!"
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.asktimenowOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "It's going great! " +  Emotes.OriHype.ToString()!,
                    "Really good.",
                    "Relaxing.",
                    "It's pretty enjoyable. " +  Emotes.OriHeart.ToString()!
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.askStatusOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "I'm doing good! Thanks for asking " +  Emotes.OriHeart.ToString()!,
                    "I'm doing well.",
                    "Not bad at all!",
                    "I'm pretty happy."
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.askStatusYNOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Yep! Thanks for asking " +  Emotes.OriHeart.ToString()!,
                    "Uh-huh!",
                    "Yeah! " +  Emotes.OriHype.ToString()!
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.askActivityOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Oh, not much. Just relaxing!",
                    "Thinking, thinking, thinking. Imagination is great!",
                    "Making sure everyone here's being nice to eachother. " +  Emotes.OriHeart.ToString()!,
                    "Nothing but talking to you, I guess!"
                ]
            ),
            #endregion
            #region Asking about the bot (as comments)
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.tellActivityGoodOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Thanks! " +  Emotes.OriHeart.ToString()!,
                    "To you too! " +  Emotes.OriHeart.ToString()!,
                    Emotes.OriHeart.ToString()!,
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.favoritecolorOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Oh... I don't know! I like greens and blues, oranges and reds, all of them really! I like all of the colors you can find in Nibel. " +  Emotes.OriHeart.ToString()!
                ]
            ),
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.loveOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                     Emotes.OriHeart.ToString()!,
                    "Aw, thanks {USERPING}! " +  Emotes.OriHype.ToString()!,
                    "Oh! " +  Emotes.OriHeart.ToString()!
                ]
            ),
            #endregion
            #region Asking about the developers (coming soon)
            #endregion
            #region Thanks
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.thanksOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                    "Oh! You're welcome " +  Emotes.OriHype.ToString()!,
                    "Of course!",
                    "It was the least I could do " +  Emotes.OriHeart.ToString()!,
                    "Sure thing!",
                    "Anything for a friend " +  Emotes.OriHeart.ToString()!
                ]
            ),
            #endregion
            #region Birthday
            new(
                new MatcherBuilder()
                .AddBeginningMarker
                .AddAnyPunctuation
                .AddTokens(QueryLibrary.birthdayOptions)
                .AddSpaceOrPeriod
                .AddTokens(QueryLibrary.oriBotOptions)
                .Build,
                [
                     Emotes.OriHype.ToString()! + " :tada:",
                    "Thank you!",
                    "Hooray! :tada:"
                ]
            ),
#endregion
        ];
    }

    

    public class NewPassiveResponses
    {
       // private readonly IOptionsMonitor<NewPassiveResponses> _passiveResponsesOptions;
        private readonly Globals _globals;
        private readonly ILogger<NewPassiveResponses> _logger;



        public NewPassiveResponses(/*IOptionsMonitor<NewPassiveResponses> passiveResponsesOptions, */Globals globals, ILogger<NewPassiveResponses> logger)
        {
            //_passiveResponsesOptions = passiveResponsesOptions;
            _globals = globals;
            _logger = logger;
        }

        public async Task Run(SocketCommandContext context)
        {
            if (QuestionsAndResponses.askingAboutGender.MatchRandom(context.Message.Content,out string response))
            {
                await context.Message.ReplyAsync(response);
            }
            foreach (var item in QuestionsAndResponses.QnA)
            {
                if (item.MatchRandom(context.Message.Content, out string response2))
                {
                    await context.Message.ReplyAsync(response2);
                }
            }
        }
    }
}
