using TaskApi.Application.Abstractions;
using TaskApi.Application.Services;
using TaskApi.Presentation.Security;

namespace TaskApi.Presentation.Endpoints;

public static class BankingLlmEndpoints
{
    public static IEndpointRouteBuilder MapBankingLlmEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/ai/v1/transactions").WithTags("Banking LLM Lab")
            .AddEndpointFilter<ApiKeyAuthorizationFilter>()
            .MapPost("/explain", async Task<IResult> (ExplainTransactionRequest request, TransactionExplanationService service,
                CancellationToken ct) =>
            {
                try { return Results.Ok(await service.ExplainAsync(request, ct)); }
                catch (ArgumentException e) { return Results.Problem(statusCode: 400, title: "Invalid input", detail: e.Message); }
                catch (ModelUnavailableException) { return Results.Problem(statusCode: 503, title: "ML models unavailable"); }
                catch (LlmUnavailableException) { return Results.Problem(statusCode: 503, title: "LLM configuration unavailable"); }
                catch (LlmTimeoutException) { return Results.Problem(statusCode: 504, title: "LLM request timed out"); }
                catch (LlmProviderException) { return Results.Problem(statusCode: 502, title: "LLM provider failed"); }
            })
            .WithName("ExplainTransaction")
            .Produces<ExplainTransactionResponse>()
            .ProducesProblem(400).ProducesProblem(502).ProducesProblem(503).ProducesProblem(504);
        return app;
    }
}
