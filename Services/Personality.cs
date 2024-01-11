using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Discord.Addons.Hosting;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot;

public class Personality
{
    private readonly Dictionary<string, string> privateDict = new Dictionary<string, string>();
    private readonly ILogger<Personality> _logger = null!;

    public Personality(IOptions<BotOptions> options, ILogger<Personality> logger)
    {
        _logger = logger;

        try
        {
            privateDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Files", options.Value.PersonalityFile)))!;
        } catch (Exception e){
            _logger.LogCritical(e,"Personality failed to load: ");
        }
    }

    public string Format(string key, params string[] values)
    {
        if (!privateDict.ContainsKey(key))
        {
            _logger.LogWarning("Personality translation key not found: {key}",key);
            return key;
        } else
        {
            return string.Format(privateDict[key], values);
        }
    }

    public string Format(string key)
    {
        if (!privateDict.ContainsKey(key))
        {
            _logger.LogWarning("Personality translation key not found: {key}", key);
            return key;
        }
        else
        {
            return privateDict[key];
        }
    }
}
