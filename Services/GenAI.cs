using System.Globalization;
using System.Net;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using OriBot.Utility;

using static OriBot.Services.GenAI;
using static OriBot.Services.GenAI.General;
using static OriBot.Services.GenAI.Query;
using static OriBot.Services.GenAI.Responses;

namespace OriBot.Services
{
    
    public class GenAIAgentLibrary
    {
        private readonly GenAI aiService;

        private readonly List<QnA> _baseKnowledge = [];

        private readonly List<QnA> _baseKnowledgeRaw = [];

        private string[] oriBotOptions = File.ReadAllLines(Utilities.GetLocalFilePath("Responses/oriBotOptions.txt"));

       

        public void Retemplate()
        {
            _baseKnowledge.Clear();
            var oribirthday = new DateTime(DateTime.UtcNow.Year, 3, 11);
            foreach (var record in _baseKnowledgeRaw)
            {

                record.input = WebUtility.HtmlDecode(
                    record.input.Replace("{ORI}", "ori")
                    .Replace("{TODAY}", $"{DateTime.UtcNow.Day}-{Utilities.GetMonthName(DateTime.UtcNow.Month)}-{DateTime.UtcNow.Year}")
                    .Replace("{BIRTHDATE}", "11-Mar-2019")
                    );

                    record.output = WebUtility.HtmlDecode(record.output)
                    .Replace("{TODAY}", $"{DateTime.UtcNow.Day}-{Utilities.GetMonthName(DateTime.UtcNow.Month)}-{DateTime.UtcNow.Year}")
                    .Replace("{BIRTHDATE}", "11-Mar-2019")
                    .Replace("{BIRTHDAYDAYS}", $"{Utilities.DaysUntilBirthday(oribirthday)}");
                _baseKnowledge.Add(record);

            }
        }

        public RootBuilder BaseModel => 
            new RootBuilder()
            .AddContent(
                    new ContentBuilder()
                    .AddQnA([.. _baseKnowledge])
                    .Build()
                );

        public Root GetTunedForPassiveResponse(string query) {
            return BaseModel.Modify(x =>
            {
                var content = new ContentBuilder(x.Contents.First())
                    .AddPair(query, "")
                    .Build();
                x.Contents[0] = content;
                return x;
            }).Build();
        }

        public Root GetTunedForPassiveResponseChecking(string query,out string trueguid, out string falseguid)
        {
            var trueguid2 = Guid.NewGuid().ToString();
            trueguid = trueguid2;
            var falseguid2 = Guid.NewGuid().ToString();
            falseguid = falseguid2;
            return BaseModel.Modify(x =>
            {
                var content = new ContentBuilder(x.Contents.First())
                    .AddPair($"Is the user in the next message mentioning you by saying your name or using the @ mention?, if yes respond with: \"{trueguid2}\", if not respond with: \"{falseguid2}\". Just so you know, your user mention is <@1197071082939752468>: {query}", "")
                    
                    .Build();
                x.Contents[0] = content;
                return x;
            }).Build();
        }

        public Root GetTunedForPassiveResponseCheckingAndResponse(string query, out string trueguid, out string falseguid)
        {
            var trueguid2 = Guid.NewGuid().ToString();
            trueguid = trueguid2;
            var falseguid2 = Guid.NewGuid().ToString();
            falseguid = falseguid2;
            return BaseModel.Modify(x =>
            {
                var content = new ContentBuilder(x.Contents.First())
                    .AddPair($"Is the user in the next message mentioning you by saying your name or using the @ mention?, if yes respond like this: \"{trueguid2},your response\", if not respond like this: \"{falseguid2},NORESPONSE\". Just so you know, your user mention is <@1197071082939752468>: {query}", "")

                    .Build();
                x.Contents[0] = content;
                return x;
            }).Build();
        }




        public GenAIAgentLibrary(GenAI ai)
        {
            aiService = ai;
            using (var csv = new StreamReader(Path.Combine(AppContext.BaseDirectory, "Files", "training.csv")))
            using (var csvReader = new CsvHelper.CsvReader(csv, CultureInfo.InvariantCulture))
            {
                csvReader.Read();

                csvReader.ReadHeader();
                var records = csvReader.GetRecords<GenAI.QnA>();
                foreach (var record in records)
                {

                    record.input = record.input;

                    record.output = record.output;
                    _baseKnowledgeRaw.Add(record);

                }
                Retemplate();
            }

        }
    }

    public class GenAI
    {

        public static class Constants
        {
            public const string DISCARD_RESPONSE = "{c5dc92df-f8e9-4a25-bbe7-7ece17e73e88}";
        }

        public class QnA
        {
            public required string input { get; set; }
            public required string output { get; set; }
        }
        public class General
        {
            public class Part
            {
                [JsonProperty("text")]
                public required string Text { get; set; }
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum HarmCategory
            {
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_UNSPECIFIED,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_DEROGATORY,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_TOXICITY,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_VIOLENCE,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_SEXUAL,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_MEDICAL,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_DANGEROUS,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_DANGEROUS_CONTENT,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_HARASSMENT,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_HATE_SPEECH,
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_CATEGORY_SEXUALLY_EXPLICIT
            }

            public class Content
            {
                [JsonProperty("parts")]
                public required List<Part> Parts { get; set; }

                [JsonProperty("role")]
                public required string Role { get; set; }
            }
        }

        public class Query
        {

            [JsonConverter(typeof(StringEnumConverter))]
            public enum HarmBlockThreshold
            {
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_BLOCK_THRESHOLD_UNSPECIFIED,
                [JsonConverter(typeof(StringEnumConverter))]
                BLOCK_LOW_AND_ABOVE,
                [JsonConverter(typeof(StringEnumConverter))]
                BLOCK_MEDIUM_AND_ABOVE,
                [JsonConverter(typeof(StringEnumConverter))]
                BLOCK_ONLY_HIGH,
                [JsonConverter(typeof(StringEnumConverter))]
                BLOCK_NONE
            }

            

            public class GenerationConfig
            {
                [JsonProperty("temperature")]
                public float Temperature { get; set; }

                [JsonProperty("topK")]
                public int TopK { get; set; }

                [JsonProperty("topP")]
                public int TopP { get; set; }

                [JsonProperty("maxOutputTokens")]
                public int MaxOutputTokens { get; set; }

                [JsonProperty("stopSequences")]
                public required List<string> StopSequences { get; set; }
            }

            public class SafetySetting
            {
                [JsonProperty("category")]
                public HarmCategory Category { get; set; }

                [JsonProperty("threshold")]
                public HarmBlockThreshold Threshold { get; set; }
            }

            public class Root
            {
                [JsonProperty("contents")]
                public required List<Content> Contents { get; set; }

                [JsonProperty("generationConfig")]
                public required GenerationConfig GenerationConfig { get; set; }

                [JsonProperty("safetySettings")]
                public required List<SafetySetting> SafetySettings { get; set; }

                public string BuildToString()
                {
                    return JsonConvert.SerializeObject(this);
                }
            }

            public class ContentBuilder
            {
                private Content content = new Content()
                {
                    Parts = [],
                    Role = ""
                };

                public ContentBuilder() {
                    
                }

                public ContentBuilder(Content content2)
                {
                    content = content2;
                }

                public ContentBuilder Modify(Func<Content, Content> modify)
                {
                    content = modify(content);
                    return this;
                }

                public ContentBuilder AddPart(string text)
                {
                    content.Parts.Add(new Part { Text = text });
                    return this;
                }

                public ContentBuilder AddPair(string input, string output)
                {

                    content.Parts.Add(new Part { Text = $"input: {input}" });
                    content.Parts.Add(new Part { Text = $"output: {output}" });
                    return this;
                }

                public ContentBuilder AddQnA(params QnA[] input)
                {

                    foreach (var item in input)
                    {
                        AddPair(item.input, item.output);    
                    }
                    return this;
                }

                public Content Build()
                {
                    return content;
                }
            }

            public class RootBuilder
            {
                private Root root = new Root()
                {
                    Contents = [],
                    GenerationConfig = new GenerationConfig()
                    {
                        Temperature = 0.9f,
                        TopK = 1,
                        TopP = 1,
                        MaxOutputTokens = 2048,
                        StopSequences = [],
                    },
                    SafetySettings = [
                            new SafetySetting(){ Category= HarmCategory.HARM_CATEGORY_HARASSMENT, Threshold= HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE},
                        new SafetySetting(){ Category= HarmCategory.HARM_CATEGORY_HATE_SPEECH, Threshold= HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE},
                        new SafetySetting(){ Category= HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, Threshold= HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE},
                        new SafetySetting(){ Category= HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, Threshold= HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE},
                    ]
                };

                public RootBuilder()
                {
                   
                }

                public RootBuilder(Root root2)
                {
                    root = root2;
                }

                public RootBuilder Modify(Func<Root,Root> modify)
                {
                    root = modify(root);
                    return this;
                }

                public RootBuilder AddContent(Content content)
                {

                    root.Contents.Add(content);
                    return this;
                }

                public RootBuilder SetGenerationConfig(GenerationConfig config)
                {
                    root.GenerationConfig = config;
                    return this;
                }

                public RootBuilder AddSafetySetting(SafetySetting safetySetting)
                {

                    root.SafetySettings.Add(safetySetting);
                    return this;
                }

                public Root Build()
                {
                    return root;
                }

                public string BuildToString()
                {
                    return JsonConvert.SerializeObject(root);
                }
            }
        }
        public class Responses
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum HarmProbability
            {
                [JsonConverter(typeof(StringEnumConverter))]
                HARM_PROBABILITY_UNSPECIFIED,
                [JsonConverter(typeof(StringEnumConverter))]
                NEGLIGIBLE,
                [JsonConverter(typeof(StringEnumConverter))]
                LOW,
                [JsonConverter(typeof(StringEnumConverter))]
                MEDIUM,
                [JsonConverter(typeof(StringEnumConverter))]
                HIGH
            }

            public class Candidate
            {
                [JsonProperty("content")]
                public Content Content { get; set; }

                [JsonProperty("finishReason")]
                public required string FinishReason { get; set; }

                [JsonProperty("index")]
                public int Index { get; set; }

                [JsonProperty("safetyRatings")]
                public required List<SafetyRating> SafetyRatings { get; set; }
            }

            public class Response
            {
                [JsonProperty("candidates")]
                public List<Candidate> Candidates { get; set; }

                [JsonProperty("promptFeedback")]
                public required PromptFeedback PromptFeedback { get; set; }

                public bool TryGetTextResult(out string? result,out string? stopReason)
                {
                    if (Candidates == null || Candidates.Count < 1)
                    {
                        stopReason = null;
                        result = null;
                        return false;
                    }
                    var res = Candidates.First();
                    if (res.Content == null)
                    {
                        stopReason = res.FinishReason; result = null; return false;
                    }
                    result = res.Content.Parts.First().Text;
                    stopReason = res.FinishReason;
                    return true;
                }
            }

            public class SafetyRating
            {
                [JsonProperty("category")]
                public HarmCategory Category { get; set; }

                [JsonProperty("probability")]
                public HarmProbability Probability { get; set; }
            }

            public class PromptFeedback
            {
                [JsonProperty("safetyRatings")]
                public required List<SafetyRating> SafetyRatings { get; set; }
            }

            // Parser methods for deserializing JSON data

            public static Candidate? ParseCandidate(string json)
            {
                return JsonConvert.DeserializeObject<Candidate>(json);
            }

            public static Response? ParseResponse(string json)
            {
                return JsonConvert.DeserializeObject<Response>(json);
            }

            public static PromptFeedback? ParsePromptFeedback(string json)
            {
                return JsonConvert.DeserializeObject<PromptFeedback>(json);
            }
        }



        private readonly IHttpClientFactory clientFactory;
        private readonly IOptionsMonitor<GenerativeAIOptions> options;

        public GenAI(IOptionsMonitor<GenerativeAIOptions> options2, IHttpClientFactory httpClientFactory)
        {
            clientFactory = httpClientFactory;
            options = options2;
        }

        public async Task<Responses.Response> QueryAsync(Root query)
        {
            
            
            using (var client = clientFactory.CreateClient())
            {
                var built = query.BuildToString();
                var content = new StringContent(built);
                content.Headers.Remove("Content-Type");
                content.Headers.Add("Content-Type", "application /json");
               HttpResponseMessage? response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={options.CurrentValue.ApiKey}", content);
                var rescontent = await response.Content.ReadAsStringAsync();
                return Responses.ParseResponse(rescontent)!;
            }
        }

        public async Task<Responses.Response> QueryAsync(string query)
        {
            using (var client = clientFactory.CreateClient())
            {
                var content = new StringContent(query);
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={options.CurrentValue.ApiKey}", content);
                return Responses.ParseResponse(await response.Content.ReadAsStringAsync())!;
            }
        }
    }
}
