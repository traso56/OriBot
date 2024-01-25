using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot.Utility;
using System.Text.RegularExpressions;

namespace OriBot.Services;

public class ExceptionContext
{
    public IChannel? Channel { get; private set; }
    public IMessage? Message { get; private set; }

    public ExceptionContext(IMessage message)
    {
        Channel = message.Channel;
        Message = message;
    }
    public ExceptionContext(IChannel channel)
    {
        Channel = channel;
    }
    public ExceptionContext()
    {
    }
}
public partial class ExceptionReporter
{
    [GeneratedRegex("```", RegexOptions.Compiled)]
    private static partial Regex BlockDelimiterRegex();

    private readonly ILogger<ExceptionReporter> _logger;
    private readonly Globals _globals;

    public ExceptionReporter(ILogger<ExceptionReporter> logger, Globals globals)
    {
        _logger = logger;
        _globals = globals;
    }

    public async Task NotifyExceptionAsync(Exception exception, ExceptionContext context, string errorReason, bool notifyInPlace)
    {
        string errorLog = "";
        try
        {
            // report in my server
            errorLog = $"There was an error";

            if (context.Message is not null)
                errorLog += $" in: {context.Message.GetJumpUrl()}";
            else if (context.Channel is not null)
                errorLog += $" in: <#{context.Channel.Id}>";

            errorLog += $"\n**__{exception.GetType()}__** {errorReason}\n{exception.Message}\n```{exception.StackTrace}```";

            if (exception.InnerException is not null)
                errorLog += $"\nInner Exception: **{exception.InnerException.GetType()}:**\n{exception.InnerException.Message}";

            errorLog = errorLog.Replace("\r\n", "\n"); // CRLF -> LF
            await SendToLogChannelAsync(errorLog);

            // report in the place it happened
            if (notifyInPlace && context.Channel is IMessageChannel messageChannel)
            {
                string errorMessage = $"There was an internal error, please check the logs, pinging {_globals.Traso.Mention}";
                if (exception is DbUpdateConcurrencyException)
                    errorMessage = $"There was an error updating records in the database, perhaps it was updated elsewhere during this command, pinging {_globals.Traso.Mention}";
                else if (exception is OverflowException)
                    errorMessage = exception.Message + $", pinging {_globals.Traso.Mention}";

                await messageChannel.SendMessageAsync(errorMessage);
            }
        }
        catch (Exception e)
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            errorLog += "\n**__This exception couldn't be sent previously__** time of exception: " + Utilities.FullDateTimeStamp(currentTime);
            while (true)
            {
                try
                {
                    _logger.LogError(e, "Couldn't send exception report to the logs channel, retrying in 15 min");
                    await Task.Delay(new TimeSpan(0, 15, 0));
                    await SendToLogChannelAsync(errorLog);
                    break;
                }
                catch
                {
                    // discord being dumb, retry later
                }
            }
        }
    }
    private async Task SendToLogChannelAsync(string errorLog)
    {
        if (errorLog.Length <= 1990)
        {
            await _globals.InfoChannel.SendMessageAsync(errorLog);
        }
        else
        {
            bool nextNeedsBlock = false;

            for (int i = 0; i < errorLog.Length;)
            {
                int endIndex = Math.Min(i + 1990, errorLog.Length - 1);
                int length = Math.Min(1990, errorLog.Length - i);

                int lastNewLine;
                if (endIndex + 1 == errorLog.Length)
                    lastNewLine = errorLog.Length;
                else
                    lastNewLine = errorLog.LastIndexOf('\n', endIndex, length); // subtract 1 because we won't include the newline character

                string subString = errorLog[i..lastNewLine];
                i = lastNewLine + 1; // add 1 to skip the newline character

                if (nextNeedsBlock)
                {
                    subString = "```" + subString;
                }
                if (BlockDelimiterRegex().Matches(subString).Count % 2 == 1)
                {
                    subString += "```";
                    nextNeedsBlock = true;
                }
                else
                {
                    nextNeedsBlock = false;
                }
                await _globals.InfoChannel.SendMessageAsync(subString);
            }
        }
    }
}
