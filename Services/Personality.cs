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
    private readonly ILogger<DiscordClientService> _logger = null!;

    public Personality(IOptions<BotOptions> options, ILogger<DiscordClientService> logger)
    {
        _logger = logger;
        try
        {
            privateDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Files", options.Value.PersonalityFile)))!;
        } catch (Exception e){
            _logger.LogCritical("Personality failed to load, please check this bug: " + e.StackTrace);
        }
    }

    public string Format(string key, params string[] values)
    {
        if (!privateDict.ContainsKey(key))
        {
            _logger.LogWarning("Personality translation key not found: " +  key);
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
            _logger.LogWarning("Personality translation key not found: " + key);
            return key;
        }
        else
        {
            return privateDict[key];
        }
    }
}
