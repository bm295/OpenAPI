namespace TaskApi.Application.Abstractions;

public sealed record LlmCompletion(string Text, string Model, int? InputTokens, int? OutputTokens);

public interface ILlmClient
{
    Task<LlmCompletion> CompleteAsync(string systemPrompt, string userData, CancellationToken cancellationToken);
}

public sealed class LlmUnavailableException : Exception { }
public sealed class LlmTimeoutException : Exception { }
public sealed class LlmProviderException : Exception { }
