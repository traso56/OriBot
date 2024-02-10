using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using static OriBot.Services.GenAI;
using static OriBot.Services.GenAI.General;
using static OriBot.Services.GenAI.Query;

namespace OriBot.Services
{
    public class GenAI
    {
        public class QnA
        {
            public string input { get; set; }
            public string output { get; set; }
        }
        public class General
        {
            public class Part
            {
                [JsonProperty("text")]
                public string Text { get; set; }
            }
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
                public List<Part> Parts { get; set; }

                [JsonProperty("role")]
                public string Role { get; set; }
            }
        }

        public class Query
        {


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
                public List<string> StopSequences { get; set; }
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
                public List<Content> Contents { get; set; }

                [JsonProperty("generationConfig")]
                public GenerationConfig GenerationConfig { get; set; }

                [JsonProperty("safetySettings")]
                public List<SafetySetting> SafetySettings { get; set; }

                public string BuildToString()
                {
                    return JsonConvert.SerializeObject(this);
                }
            }

            public class ContentBuilder
            {
                private readonly Content content = new Content();

                public ContentBuilder AddPart(string text)
                {
                    if (content.Parts == null)
                        content.Parts = new List<Part>();

                    content.Parts.Add(new Part { Text = text });
                    return this;
                }

                public ContentBuilder AddPair(string input, string output)
                {
                    if (content.Parts == null)
                        content.Parts = new List<Part>();

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
                private readonly Root root = new Root()
                {
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

                public RootBuilder AddContent(Content content)
                {
                    if (root.Contents == null)
                        root.Contents = new List<Content>();

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
                    if (root.SafetySettings == null)
                        root.SafetySettings = new List<SafetySetting>();

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
                public string FinishReason { get; set; }

                [JsonProperty("index")]
                public int Index { get; set; }

                [JsonProperty("safetyRatings")]
                public List<SafetyRating> SafetyRatings { get; set; }
            }

            public class Response
            {
                [JsonProperty("candidates")]
                public List<Candidate> Candidates { get; set; }

                [JsonProperty("promptFeedback")]
                public PromptFeedback PromptFeedback { get; set; }
            }

            public class Content
            {
                [JsonProperty("parts")]
                public List<Part> Parts { get; set; }

                [JsonProperty("role")]
                public string Role { get; set; }
            }

            public class SafetyRating
            {
                [JsonProperty("category")]
                public string Category { get; set; }

                [JsonProperty("probability")]
                public string Probability { get; set; }
            }

            public class PromptFeedback
            {
                [JsonProperty("safetyRatings")]
                public List<SafetyRating> SafetyRatings { get; set; }
            }

            // Parser methods for deserializing JSON data

            public static Candidate ParseCandidate(string json)
            {
                return JsonConvert.DeserializeObject<Candidate>(json);
            }

            public static Response ParseResponse(string json)
            {
                return JsonConvert.DeserializeObject<Response>(json);
            }

            public static PromptFeedback ParsePromptFeedback(string json)
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
                return Responses.ParseResponse(rescontent);
            }
        }

        public async Task<Responses.Response> QueryAsync(string query)
        {
            using (var client = clientFactory.CreateClient())
            {
                var content = new StringContent(query);
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={options.CurrentValue.ApiKey}", content);
                return Responses.ParseResponse(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
