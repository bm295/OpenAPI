using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenApiFacade.Tests;

public class FacadeTests
{
    private sealed record Request([property: Range(1, 100)] int Quantity);
    private sealed record Response(int Result);
    private sealed class InternalService { public int Double(int value) => value * 2; }
    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], "test")),
                "test")));
    }

    private static async Task<(WebApplication App, HttpClient Client)> Start(
        bool register = true, bool authorize = false, bool throws = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApiFacade();
        builder.Services.AddSingleton<InternalService>();
        if (authorize)
        {
            builder.Services.AddAuthentication("test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("test", _ => { });
            builder.Services.AddAuthorization(options =>
                options.AddPolicy("external", policy => policy.RequireAssertion(_ => false)));
        }
        var app = builder.Build();
        if (authorize) app.UseAuthorization();
        app.MapOpenApiFacade(routes =>
        {
            if (register)
                routes.MapPost<Request, Response>("/public/orders", (request, services, _) =>
                {
                    if (throws) throw new InvalidOperationException("secret detail");
                    return Task.FromResult(new Response(services.GetRequiredService<InternalService>()
                        .Double(request.Quantity)));
                }, authorize ? "external" : null);
        });
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task Registered_route_binds_request_uses_DI_and_returns_response()
    {
        var (app, client) = await Start();
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/public/orders", new Request(3));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(6, (await response.Content.ReadFromJsonAsync<Response>())!.Result);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.PostAsJsonAsync("/internal/orders", new Request(3))).StatusCode);
        }
    }

    [Fact]
    public async Task No_registration_exposes_no_route()
    {
        var (app, client) = await Start(register: false);
        await using (app)
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.PostAsJsonAsync("/public/orders", new Request(3))).StatusCode);
    }

    [Fact]
    public async Task Policy_rejects_unauthorized_request()
    {
        var (app, client) = await Start(authorize: true);
        await using (app)
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PostAsJsonAsync("/public/orders", new Request(3))).StatusCode);
    }

    [Fact]
    public async Task Invalid_request_has_structured_error()
    {
        var (app, client) = await Start();
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/public/orders", new Request(0));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("invalid_request", (await response.Content.ReadFromJsonAsync<FacadeError>())!.Code);
        }
    }

    [Fact]
    public async Task Handler_failure_hides_exception_details()
    {
        var (app, client) = await Start(throws: true);
        await using (app)
        {
            var response = await client.PostAsJsonAsync("/public/orders", new Request(1));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("processing_error", body);
            Assert.DoesNotContain("secret detail", body);
        }
    }
}
