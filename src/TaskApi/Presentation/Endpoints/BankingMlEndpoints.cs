using System.Text.Json.Serialization;
using TaskApi.Application.Abstractions;
using TaskApi.Presentation.Security;

namespace TaskApi.Presentation.Endpoints;

public sealed record ClassifyTransactionRequest(string Description, string MerchantName);
public sealed record PredictAmountRequest(string Category, string MerchantType,
    [property: JsonRequired] float DayOfWeek, [property: JsonRequired] float Quantity);

public static class BankingMlEndpoints
{
    public static IEndpointRouteBuilder MapBankingMlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ml/v1/transactions")
            .WithTags("Banking ML Lab")
            .AddEndpointFilter<ApiKeyAuthorizationFilter>();

        group.MapPost("/classify", (ClassifyTransactionRequest request, IBankingPredictionService service) =>
            Execute(() => Results.Ok(new { category = service.Classify(request.Description, request.MerchantName) })))
            .WithName("ClassifyTransaction");

        group.MapPost("/predict-amount", (PredictAmountRequest request, IBankingPredictionService service) =>
            Execute(() => Results.Ok(new { amount = service.PredictAmount(request.Category, request.MerchantType,
                request.DayOfWeek, request.Quantity), currency = "VND" })))
            .WithName("PredictTransactionAmount");
        return app;
    }

    private static IResult Execute(Func<IResult> action)
    {
        try { return action(); }
        catch (ArgumentException error)
        {
            return Results.Problem(title: "Invalid prediction input", detail: error.Message, statusCode: 400);
        }
        catch (ModelUnavailableException error)
        {
            return Results.Problem(title: "ML models unavailable", detail: error.Message, statusCode: 503);
        }
    }
}
