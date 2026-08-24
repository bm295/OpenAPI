using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

internal static class HealthEndpoints
{
    internal static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (TimeProvider timeProvider) =>
                Results.Ok(new HealthResponse("ok", timeProvider.GetUtcNow())))
            .WithName("GetHealth");

        return app;
    }
}
