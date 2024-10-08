﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ROBot.Core.OpenAI
{
    public class ImageRequest
    {
        [JsonPropertyName("prompt")]
        public string? Prompt
        {
            get;
            set;
        }

        [JsonPropertyName("n")]
        public int? Count
        {
            get;
            set;
        }

        [JsonPropertyName("size")]
        public string Size
        {
            get;
            set;
        }

        public static ImageRequest Create(string prompt, string size = "512x512", int count = 1)
        {
            return new ImageRequest
            {
                Count = count,
                Prompt = prompt,
                Size = size
            };
        }
    }

    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string? Model
        {
            get;
            set;
        }
        [JsonPropertyName("messages")]
        public ChatMessage[] Messages
        {
            get;
            set;
        }
    }

    public class ChatMessage
    {

        [JsonPropertyName("role")]
        public string? Role { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        internal static ChatMessage Create(string role, string prompt)
        {
            return new ChatMessage
            {
                Role = role,
                Content = prompt,
            };
        }
    }

    public class ImageResponse
    {
        [JsonPropertyName("data")]
        public ImageResponseItem[] Data
        {
            get;
            set;
        }

        [JsonPropertyName("created")]
        public long? Created { get; set; }
    }

    public class ImageResponseItem
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatGPTChoice>? Choices
        {
            get;
            set;
        }
        [JsonPropertyName("usage")]
        public ChatGPTUsage? Usage
        {
            get;
            set;
        }
    }
    public class ChatGPTUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens
        {
            get;
            set;
        }
        [JsonPropertyName("completion_token")]
        public int CompletionTokens
        {
            get;
            set;
        }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens
        {
            get;
            set;
        }
    }

    public class ChatGPTChoice
    {
        [JsonPropertyName("index")]
        public int? Index
        {
            get;
            set;
        }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason
        {
            get;
            set;
        }

        [JsonPropertyName("message")]
        public ChatMessage Message
        {
            get;
            set;
        }
    }

    public interface IOpenAI
    {
        Task<ChatCompletionResponse> GetCompletionAsync(string prompt, params ChatMessage[] previousMessages);

        Task<ImageResponse> GenerateImageAsync(string prompt, string size = "512x512", int count = 1);
    }

    public interface IOpenAISettings
    {
        string AccessToken { get; }
    }

    public class OpenAITokenString : IOpenAISettings
    {
        public string AccessToken { get; }

        public OpenAITokenString(string accessToken)
        {
            AccessToken = accessToken;
        }
    }


    public class OpenAI : IOpenAI, IDisposable
    {
        private bool disposed;
        private readonly HttpClient client;
        private readonly IOpenAISettings settings;

        public OpenAI(IOpenAISettings settings)
        {
            this.settings = settings;
            client = new HttpClient();
        }

        public async Task<ImageResponse> GenerateImageAsync(string prompt, string size = "512x512", int count = 1)
        {
            return await RequestAsync<ImageRequest, ImageResponse>("https://api.openai.com/v1/images/generations", ImageRequest.Create(prompt, size, count));
        }

        public async Task<ChatCompletionResponse> GetCompletionAsync(string prompt, params ChatMessage[] previousMessages)
        {
            var msgs = new List<ChatMessage>();
            msgs.AddRange(previousMessages);
            msgs.Add(ChatMessage.Create("user", prompt));

            return await RequestAsync<ChatCompletionRequest, ChatCompletionResponse>("https://api.openai.com/v1/chat/completions", new ChatCompletionRequest
            {
                //Model = "text-davinci-003",
                Model = "gpt-4o-mini",
                //Model = "davinci:ft-shinobytes-2023-02-20-14-02-16",
                // Prompt = "What if Nicholas Cage played the lead role in Superman?",
                Messages = msgs.ToArray()
            });
        }

        private async Task<TResult> RequestAsync<TRequest, TResult>(string url, TRequest model)
        {
            using (var httpReq = new HttpRequestMessage(HttpMethod.Post, url))
            {
                httpReq.Headers.Add("Authorization", $"Bearer {settings.AccessToken}");

                var requestString = JsonSerializer.Serialize(model);
                httpReq.Content = new StringContent(requestString, Encoding.UTF8, "application/json");
                using (var httpResponse = await client.SendAsync(httpReq))
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        return JsonSerializer.Deserialize<TResult>(responseString);
                    }

                    return default;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            client.Dispose();
        }
    }
}
