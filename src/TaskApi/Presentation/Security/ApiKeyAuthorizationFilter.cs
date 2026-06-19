using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;
using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Security;

public sealed class ApiKeyAuthorizationFilter : IEndpointFilter
{
    public const string HeaderName = "X-API-Key";
    public const string AuthorizationScheme = "Bearer";

    private readonly string _apiKey;

    public ApiKeyAuthorizationFilter(IConfiguration configuration)
    {
        _apiKey = configuration["OpenBanking:ApiKey"] ?? string.Empty;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return Results.Problem(
                title: "Open Banking API key is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var request = context.HttpContext.Request;
        var providedApiKey = GetApiKey(request.Headers);

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Results.Json(
                new ErrorResponse("UNAUTHORIZED", $"Supply an API key in the {HeaderName} header or Authorization: {AuthorizationScheme} header."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!ApiKeysMatch(providedApiKey, _apiKey))
        {
            return Results.Json(
                new ErrorResponse("FORBIDDEN", "The supplied API key is not authorized to access Open Banking resources."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    private static bool ApiKeysMatch(string providedApiKey, string configuredApiKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredApiKey);

        return providedBytes.Length == configuredBytes.Length &&
            CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }

    private static string? GetApiKey(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HeaderName, out var apiKeyHeader))
        {
            return apiKeyHeader.ToString();
        }

        if (!headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return null;
        }

        return GetBearerToken(authorizationHeader);
    }

    private static string? GetBearerToken(StringValues authorizationHeader)
    {
        var value = authorizationHeader.ToString();
        var prefix = $"{AuthorizationScheme} ";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..].Trim()
            : null;
    }
}
