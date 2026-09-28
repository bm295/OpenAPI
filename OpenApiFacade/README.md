# OpenApiFacade

ASP.NET Core library for explicitly mapping public HTTP endpoints to handlers that use the host application's services. It creates no endpoints until `MapOpenApiFacade` registers them. Public request and response DTOs can differ from internal models.

## Build and package

```sh
dotnet test OpenApiFacade.slnx
dotnet pack src/OpenApiFacade/OpenApiFacade.csproj -c Release
```

The package is written to `src/OpenApiFacade/bin/Release`. Add the resulting `.nupkg` to a NuGet feed, then reference `OpenApiFacade` from an ASP.NET Core project.

## Use

```csharp
using OpenApiFacade;

builder.Services.AddOpenApiFacade();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("PublicOrders", policy => policy.RequireAuthenticatedUser()));

// Configure authentication and authorization middleware in the host application.
app.MapOpenApiFacade(routes =>
{
    routes.MapPost<CreatePublicOrderRequest, PublicOrderResponse>(
        "/public/v1/orders",
        async (request, services, cancellationToken) =>
        {
            var orderService = services.GetRequiredService<IOrderService>();
            var order = await orderService.CreateAsync(
                request.ProductId, request.Quantity, cancellationToken);
            return new PublicOrderResponse(order.Id, order.Status);
        },
        authorizationPolicy: "PublicOrders");
});
```

Use `Map<TRequest, TResponse>(method, route, handler, authorizationPolicy)` for other HTTP methods. Requests are read as JSON and validated with data annotations. Invalid requests return HTTP 400 with `{ "code": "invalid_request", "message": "..." }`; handler failures return HTTP 500 with a generic `processing_error` response. Configure authentication and `UseAuthorization()` in the host when using policies.

The library controls only the public endpoints it creates. It does not block internal endpoints that the host project has independently exposed to the Internet.
