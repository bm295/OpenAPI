using TaskApi.Presentation.Security;

namespace TaskApi.Presentation.Endpoints;

public static class OpenBankingEndpoints
{
    public static IEndpointRouteBuilder MapOpenBankingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthEndpoints();

        var group = app.MapGroup("/open-banking/v1")
            .WithTags("Open Banking")
            .AddEndpointFilter<ApiKeyAuthorizationFilter>();

        group.MapAccountEndpoints();
        group.MapConsentEndpoints();
        group.MapPaymentEndpoints();

        return app;
    }
}
