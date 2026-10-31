using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TaskApi.Application.Abstractions;

namespace TaskApi.Infrastructure.Llm;

// OpenAI Chat Completions wire format. No automatic retry: a failed request may already be billed.
public sealed class ChatCompletionClient : ILlmClient
{
    private readonly HttpClient http;
    private readonly IConfiguration configuration;
    public ChatCompletionClient(HttpClient http, IConfiguration configuration)
    {
        this.http = http;
        this.configuration = configuration;
    }
    public async Task<LlmCompletion> CompleteAsync(string systemPrompt, string userData, CancellationToken cancellationToken)
    {
        var key = configuration["Llm:ApiKey"];
        var model = configuration["Llm:Model"];
        var endpoint = configuration["Llm:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        var timeout = configuration.GetValue("Llm:TimeoutSeconds", 30);
        var outputLimit = configuration.GetValue("Llm:MaxOutputTokens", 400);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model) ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || timeout is < 1 or > 120 || outputLimit is < 1 or > 2000)
            throw new LlmUnavailableException();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeout));
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            model, store = false, max_completion_tokens = outputLimit,
            messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userData } }
        });
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
            if (!response.IsSuccessStatusCode) throw new LlmProviderException();
            // Bound the response even when a provider ignores the requested token limit.
            await using var stream = await response.Content.ReadAsStreamAsync(timeoutSource.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(chunk.AsMemory(), timeoutSource.Token)) != 0)
            {
                if (buffer.Length + read > 65536) throw new LlmProviderException();
                buffer.Write(chunk, 0, read);
            }
            using var json = JsonDocument.Parse(buffer.ToArray());
            var root = json.RootElement;
            var choice = root.GetProperty("choices")[0];
            if (choice.GetProperty("finish_reason").GetString() != "stop") throw new LlmProviderException();
            var text = choice.GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(text)) throw new LlmProviderException();
            int? input = null, output = null;
            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                if (usage.TryGetProperty("prompt_tokens", out var i) && i.TryGetInt32(out var iv)) input = iv;
                if (usage.TryGetProperty("completion_tokens", out var o) && o.TryGetInt32(out var ov)) output = ov;
            }
            return new(text, root.GetProperty("model").GetString() ?? model, input, output);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new LlmTimeoutException(); }
        catch (Exception e) when (e is HttpRequestException or IOException or JsonException or
            KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        { throw new LlmProviderException(); }
    }
}
