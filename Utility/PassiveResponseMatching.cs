using System.Text;
using System.Text.RegularExpressions;

namespace OriBot.Utility;

public static class RegexGenerators
{
    public static string OR(string a, string b) => $"({a}|{b})";

    public static string MultichoiceOR(params string[] strings)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append('(');
        foreach (string s in strings)
            stringBuilder.Append(s).Append('|');

        stringBuilder[^1] = ')';
        return stringBuilder.ToString();
    }
}

public static class RegexConstants
{
    public const string AtleastOnePunctuationWithSpace = """(,|\.|!|\?|~|'|"| )+""";
    public const string AtleastOnePunctuation = """(,|\.|!|\?|~|'|\")+""";
    public const string AtleastOneSpaceOrPeriod = """( |\.)+""";
    public const string AtleastOneSpace = " +";

    public const string AnyPunctuationWithSpace = """(,|\.|!|\?|~|'|\"| )*""";
    public const string AnyPunctuation = """(,|\.|!|\?|~|'|")*""";
    public const string AnySpaceOrPeriod = """( |\.)*""";
    public const string AnySpace = " *";
}

public class Matcher
{
    private readonly Regex _generatedRegex;



    public string Replace(string query, string replacement)
    {
        return _generatedRegex.Replace(query, replacement);
    }

    public Matcher(string pattern, bool casesensitive) =>
        _generatedRegex = new Regex(pattern, (casesensitive ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.Compiled);

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

public class MatcherBuilder
{
    private readonly StringBuilder _stringBuilder = new StringBuilder();
    public string Result => _stringBuilder.ToString();

    public Matcher Build() => new Matcher(Result, false);

    public MatcherBuilder AddSpace()
    {
        _stringBuilder.Append(RegexConstants.AtleastOneSpace);
        return this;
    }
    public MatcherBuilder AddBeginningMarker()
    {
        _stringBuilder.Append('^');
        return this;
    }

    public MatcherBuilder AddPunctuation()
    {
        _stringBuilder.Append(RegexConstants.AtleastOnePunctuation);
        return this;
    }

    /// <summary>
    /// With space or period.
    /// </summary>
    public MatcherBuilder AddSpaceOrPeriod()
    {
        _stringBuilder.Append(RegexConstants.AtleastOneSpaceOrPeriod);
        return this;
    }

    /// <summary>
    /// With any punctuation and spaces.
    /// </summary>
    public MatcherBuilder AddPunctuationAndSpace()
    {
        _stringBuilder.Append(RegexConstants.AtleastOnePunctuationWithSpace);
        return this;
    }

    public MatcherBuilder AddAnyLengthSpace()
    {
        _stringBuilder.Append(RegexConstants.AnySpace);
        return this;
    }

    public MatcherBuilder AddAnyPunctuation()
    {
        _stringBuilder.Append(RegexConstants.AnyPunctuation);
        return this;
    }

    /// <summary>
    /// With space or period.
    /// </summary>
    public MatcherBuilder AddAnySpaceOrPeriod()
    {
        _stringBuilder.Append(RegexConstants.AnySpaceOrPeriod);
        return this;
    }

    /// <summary>
    /// With any punctuation and spaces.
    /// </summary>
    public MatcherBuilder AddAnyPunctuationAndSpace()
    {
        _stringBuilder.Append(RegexConstants.AnyPunctuationWithSpace);
        return this;
    }

    public MatcherBuilder AddTokens(params string[] tokens)
    {
        _stringBuilder.Append('(');
        foreach (string token in tokens)
            _stringBuilder.Append(token).Append("|");

        _stringBuilder[^1] = ')';
        return this;
    }

    /// <summary>
    /// This normally shouldnt be used, as this can cause unexpected matching behaviour, because unescaped strings will cause bad things.
    /// This method directly adds a string to the regex.
    /// </summary>
    /// <param name="regex"></param>
    /// <returns></returns>
    public MatcherBuilder AddCustom(string regex)
    {
        _stringBuilder.Append(regex);
        return this;
    }
}

