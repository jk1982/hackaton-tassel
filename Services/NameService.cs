using Microsoft.Extensions.Caching.Distributed;
using phonetics.Helpers;
using phonetics.Models;
using System.Text;
using System.Text.Json;

namespace phonetics.Services;

public class NameService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IDistributedCache _cache;
    private readonly string _geminiKey;

    public NameService(IHttpClientFactory clientFactory, IDistributedCache cache, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _cache = cache;
        _geminiKey = config["GEMINI_API_KEY"] ?? string.Empty;
    }

    public async Task<object> ProcessNameAsync(NameRequestDto request)
    {
        var name = request.Name.Trim();
        bool useGemini = request.Model?.ToLower() == "gemini";
        var useSinglePrompt = request.SinglePrompt;
        var voiceId = request.VoiceId;

        string cacheKey = $"name-origin:{name.ToLower()}";
        var cachedJson = await _cache.GetStringAsync(cacheKey);

        OutputResponse? originObj = null;

        if (cachedJson != null) 
        {
            try
            {
                originObj = JsonSerializer.Deserialize<OutputResponse>(cachedJson);

                if ((originObj!.usesGemini && useGemini is false) ||
                    (originObj!.singlePrompt && useSinglePrompt is false))
                {
                    originObj = null;
                    await _cache.RemoveAsync(cacheKey); // Invalidate cache if Gemini was used but now OpenAI is preferred
                }
            }
            catch (JsonException)
            {
                originObj = null; // Handle deserialization failure
            }
        }

        if (cachedJson == null)
        {
            originObj = useGemini
                ? await CallGeminiAsync(name, useSinglePrompt)
                : await CallOpenAIAsync(name, useSinglePrompt);

            if (originObj != null)
            {
                originObj = originObj with { usesGemini = useGemini, singlePrompt = useSinglePrompt };

                var serialized = JsonSerializer.Serialize(originObj);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                });
            }
        }

        if (originObj == null)
        {
            return new { error = "Failed to determine name origin." };
        }

        var audioBytes = await GenerateAudioAsync(originObj.NativeScript, voiceId);
        var audioBase64 = Convert.ToBase64String(audioBytes);

        return new
        {
            native_script = originObj.NativeScript,
            ethnicity = originObj.Ethnicity,
            confidence = originObj.Confidence,
            alternatives = originObj.Alternatives,
            details = originObj.Details,
            audio_base64 = audioBase64
        };
    }

    private async Task<OutputResponse?> CallOpenAIAsync(string name, bool singlePrompt)
    {
        const string uri = "chat/completions";
        var client = _clientFactory.CreateClient("OpenAI");

        var firstPrompt = PromptBuilder.BuildAnalysisPrompt(name);
        var firstResponse = await PostOpenAIAsync(client, uri, firstPrompt);

        if (firstResponse == null) return null;

        var parsed = OutputParser.Parse(firstResponse);
        if (parsed == null) return null;

        if (singlePrompt)
        {
            return parsed;
        }

        var refinePrompt = PromptBuilder.BuildRefinementPrompt(name, parsed);
        var refinedScript = await PostOpenAIAsync(client, uri, refinePrompt);

        return refinedScript == null ? null : parsed with { NativeScript = refinedScript };
    }

    private async Task<OutputResponse?> CallGeminiAsync(string name, bool singlePrompt)
    {
        var client = _clientFactory.CreateClient("Gemini");
        var url = $"models/gemini-2.5-flash:generateContent?key={_geminiKey}";

        var firstPrompt = PromptBuilder.BuildAnalysisPrompt(name);
        var firstResponse = await PostGeminiAsync(client, url, firstPrompt);

        if (firstResponse == null) return null;

        var parsed = OutputParser.Parse(firstResponse);
        if (parsed == null) return null;

        if (singlePrompt)
        {
            return parsed;
        }

        var refinePrompt = PromptBuilder.BuildRefinementPrompt(name, parsed);
        var refinedScript = await PostGeminiAsync(client, url, refinePrompt);

        return refinedScript == null ? null : parsed with { NativeScript = refinedScript };
    }

    private static async Task<string?> PostOpenAIAsync(HttpClient client, string endpoint, string prompt)
    {
        var body = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = "You are an expert in name etymology and languages." },
                new { role = "user", content = prompt }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
    }

    private static async Task<string?> PostGeminiAsync(HttpClient client, string url, string prompt)
    {
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()?.Trim();
    }

    private async Task<byte[]> GenerateAudioAsync(string text, string voiceId)
    {
        var client = _clientFactory.CreateClient("ElevenLabs");

        var body = new
        {
            text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.5,
                use_speaker_boost = false
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"text-to-speech/{voiceId}", content);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
