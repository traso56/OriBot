using System.Globalization;
using System.Net;
using System.Xml.Linq;

using CsvHelper.Configuration;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OriBot.Services;
using OriBot.Utility;

using static OriBot.Services.GenAI.Query;

namespace OriBot.Interactive;

[RequireContext(ContextType.Guild)]
public class Testing : InteractionModuleBase<SocketInteractionContext>
{
    public required Globals Globals { get; set; }

    public required GenAI GenAIService { get; set; }

    public required GenAIAgentLibrary GenAILibrary { get; set; }

    [ModCommand]
    [SlashCommand("throw", "throws")]
    public async Task Throw(bool withResponse)
    {
        if (withResponse)
            await RespondAsync("response");
        throw new InvalidOperationException("test exception");
    }
    
    [SlashCommand("ai", "Tests Gen AI")]
    public async Task AI(string query)
    {
        await DeferAsync();
        List<GenAI.QnA> res2 = [];
        var csvconfig = new CsvConfiguration(CultureInfo.InvariantCulture) { 
            
        };
        using (var csv = new StreamReader(Path.Combine(AppContext.BaseDirectory, "Files", "training.csv")))
        using (var csvReader = new CsvHelper.CsvReader(csv, CultureInfo.InvariantCulture))
        {
            csvReader.Read();
            
            csvReader.ReadHeader();
            var records = csvReader.GetRecords<GenAI.QnA>();
            foreach (var record in records)
            {
                res2.Add(record);
                record.input = WebUtility.HtmlDecode(record.input);
                record.output = WebUtility.HtmlDecode(record.output);

            }
        }
        var req = GenAILibrary.GetTunedForPassiveResponseCheckingAndResponse(query, out string trueguid, out string falseguid);
        var response = await GenAIService.QueryAsync(
            req
        );
        var res = response.Candidates.First();
        if (res.Content == null)
        {
            await FollowupAsync($"Whoops cannot answer this question due to: {res.FinishReason} reasons.");
            return;
        }
        await FollowupAsync(res.Content.Parts.First().Text);
    }
}
