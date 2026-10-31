using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Application.Abstractions;
using TaskApi.Application.Services;
using TaskApi.Infrastructure.Llm;

namespace TaskApi.Tests;

public class LlmTests
{
    [Fact]
    public async Task ProviderReceivesSeparateMessagesAndReturnsUsage()
    {
        var handler = new Handler(async (request, ct) =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            var payload = await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
            Assert.False(payload.GetProperty("store").GetBoolean());
            Assert.Equal("system", payload.GetProperty("messages")[0].GetProperty("role").GetString());
            Assert.Equal("user", payload.GetProperty("messages")[1].GetProperty("role").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"model":"test-model","choices":[{"finish_reason":"stop","message":{"content":"Giải thích mẫu"}}],
                     "usage":{"prompt_tokens":20,"completion_tokens":10}}
                    """)
            };
        });
        var result = await Client(handler).CompleteAsync("instructions", "untrusted data", default);
        Assert.Equal(20, result.InputTokens);
        Assert.Equal(10, result.OutputTokens);
        Assert.Equal("test-model", result.Model);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(401)]
    [InlineData(500)]
    public async Task ProviderErrorsAreSanitized(int status)
    {
        var client = Client(new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent("sensitive provider details") })));
        var error = await Assert.ThrowsAsync<LlmProviderException>(() => client.CompleteAsync("s", "u", default));
        Assert.DoesNotContain("sensitive", error.Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"choices\":[{\"finish_reason\":\"length\"}]}")]
    public async Task InvalidOrTruncatedResponsesFail(string body)
    {
        var client = Client(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(body) })));
        await Assert.ThrowsAsync<LlmProviderException>(() => client.CompleteAsync("s", "u", default));
    }

    [Fact]
    public async Task MissingConfigurationDoesNotCallProvider()
    {
        var client = new ChatCompletionClient(new HttpClient(new Handler((_, _) => throw new Exception("must not call"))),
            new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<LlmUnavailableException>(() => client.CompleteAsync("s", "u", default));
    }

    [Fact]
    public async Task TimeoutAndCallerCancellationRemainDistinct()
    {
        var client = Client(new Handler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        await Assert.ThrowsAsync<LlmTimeoutException>(() => client.CompleteAsync("s", "u", default));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CompleteAsync("s", "u", cancel.Token));
    }

    [Fact]
    public async Task EndpointRequiresKeyAndReturnsServerPredictedCategory()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBankingPredictionService>();
                services.RemoveAll<ILlmClient>();
                services.AddSingleton<IBankingPredictionService>(new Predictions());
                services.AddSingleton<ILlmClient>(new FakeLlm());
            }));
        using var http = factory.CreateClient();
        var input = new ExplainTransactionRequest("Cafe demo", "Demo merchant");
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/ai/v1/transactions/explain", input)).StatusCode);
        http.DefaultRequestHeaders.Add("X-API-Key", "dev-open-banking-key");
        var response = await http.PostAsJsonAsync("/ai/v1/transactions/explain", input);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExplainTransactionResponse>();
        Assert.Equal("Food", result!.Category);
        Assert.Equal(TransactionExplanationService.PromptVersion, result.PromptVersion);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/ai/v1/transactions/explain",
            input with { Description = new string('x', 501) })).StatusCode);
    }

    private static ChatCompletionClient Client(HttpMessageHandler handler) => new(new HttpClient(handler),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Llm:ApiKey"] = "test-key", ["Llm:Model"] = "test-model", ["Llm:TimeoutSeconds"] = "1"
        }).Build());

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
    private sealed class Predictions : IBankingPredictionService
    {
        public string Classify(string description, string merchantName) => "Food";
        public float PredictAmount(string category, string merchantType, float dayOfWeek, float quantity) => 0;
    }
    private sealed class FakeLlm : ILlmClient
    {
        public Task<LlmCompletion> CompleteAsync(string systemPrompt, string userData, CancellationToken ct)
        {
            Assert.Contains("Food", userData);
            Assert.Contains("hypothesis", systemPrompt);
            return Task.FromResult(new LlmCompletion("Giả thuyết từ mô tả mẫu.", "fake", 10, 5));
        }
    }
}
