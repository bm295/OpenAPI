using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenApiFacade;

public delegate Task<TResponse> FacadeHandler<TRequest, TResponse>(
    TRequest request, IServiceProvider services, CancellationToken cancellationToken);

public static class OpenApiFacadeExtensions
{
    public static IServiceCollection AddOpenApiFacade(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRouting();
        return services;
    }

    public static IEndpointRouteBuilder MapOpenApiFacade(
        this IEndpointRouteBuilder endpoints, Action<FacadeRouteBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configure);
        configure(new FacadeRouteBuilder(endpoints));
        return endpoints;
    }
}

public sealed class FacadeRouteBuilder(IEndpointRouteBuilder endpoints)
{
    public IEndpointConventionBuilder MapPost<TRequest, TResponse>(
        string route, FacadeHandler<TRequest, TResponse> handler, string? authorizationPolicy = null)
        => Map<TRequest, TResponse>("POST", route, handler, authorizationPolicy);

    public IEndpointConventionBuilder Map<TRequest, TResponse>(
        string method, string route, FacadeHandler<TRequest, TResponse> handler,
        string? authorizationPolicy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        ArgumentNullException.ThrowIfNull(handler);

        var builder = endpoints.MapMethods(route, [method], async context =>
        {
            if (!context.Request.HasJsonContentType())
            {
                await WriteError(context, 400, "invalid_request", "A JSON request body is required.");
                return;
            }

            TRequest? request;
            try
            {
                request = await context.Request.ReadFromJsonAsync<TRequest>(context.RequestAborted);
            }
            catch (Exception ex) when (ex is JsonException or BadHttpRequestException)
            {
                await WriteError(context, 400, "invalid_request", "The request body is invalid.");
                return;
            }

            if (request is null)
            {
                await WriteError(context, 400, "invalid_request", "A request body is required.");
                return;
            }

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
            {
                await WriteError(context, 400, "invalid_request", "The request failed validation.");
                return;
            }

            try
            {
                var response = await handler(request, context.RequestServices, context.RequestAborted);
                await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The caller disconnected; no response can be sent reliably.
            }
            catch (Exception ex)
            {
                context.RequestServices.GetRequiredService<ILogger<FacadeRouteBuilder>>()
                    .LogError(ex, "Facade handler failed for {Route}", route);
                if (!context.Response.HasStarted)
                    await WriteError(context, 500, "processing_error", "The request could not be processed.");
            }
        });

        if (authorizationPolicy is not null)
            builder.RequireAuthorization(authorizationPolicy);
        return builder;
    }

    private static Task WriteError(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new FacadeError(code, message));
    }
}

public sealed record FacadeError(string Code, string Message);
