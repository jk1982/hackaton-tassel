using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using phonetics.Models;
using phonetics.Services;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config["OPENAI_API_KEY"]);
}).AddPolicyHandler(CreateRetryPolicy());

builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
}).AddPolicyHandler(CreateRetryPolicy());

builder.Services.AddHttpClient("ElevenLabs", (serviceProvider, client) =>
{
    var cfg = serviceProvider.GetRequiredService<IConfiguration>();
    var apiKey = cfg["ELEVENLABS_API_KEY"] ?? throw new ArgumentNullException(nameof(cfg));
    client.BaseAddress = new Uri("https://api.elevenlabs.io/v1/");
    client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(config["REDIS_CONNECTION"] ?? "localhost"));

builder.Services.AddSingleton<IDistributedCache>(sp =>
    new RedisCache(new RedisCacheOptions
    {
        Configuration = config["REDIS_CONNECTION"] ?? "localhost"
    }));

builder.Services.AddSingleton<NameService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/", async (NameRequestDto request, NameService nameService) =>
{
    var result = await nameService.ProcessNameAsync(request);
    return Results.Ok(result);
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
}