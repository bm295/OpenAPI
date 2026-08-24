using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Application.Contracts;

namespace TaskApi.Tests.Presentation.Endpoints;

public sealed class OpenBankingEndpointTests : IClassFixture<OpenBankingApiFactory>
{
    private readonly HttpClient _client;
    private readonly OpenBankingApiFactory _factory;

    public OpenBankingEndpointTests(OpenBankingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public void Application_MapsEveryNamedEndpoint()
    {
        var endpointNames = _factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .OfType<string>()
            .ToHashSet();

        var expectedNames = new[]
        {
            "GetHealth",
            "ListAccounts",
            "GetAccount",
            "GetAccountBalance",
            "ListAccountTransactions",
            "CreateConsent",
            "GetConsent",
            "RevokeConsent",
            "CreatePayment",
            "GetPayment"
        };

        Assert.All(expectedNames, name => Assert.Contains(name, endpointNames));
    }

    [Fact]
    public async Task Health_ReturnsDeterministicTimestampWithoutAuthorization()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal(OpenBankingApiFactory.Now, body.Timestamp);
    }

    [Fact]
    public async Task Accounts_WithoutApiKey_ReturnsUnauthorizedContract()
    {
        var response = await _client.GetAsync("/open-banking/v1/accounts");
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED", body?.Code);
    }

    [Fact]
    public async Task Accounts_WithApiKey_ReturnsSeededAccounts()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/open-banking/v1/accounts?pageSize=1");
        request.Headers.Add("X-API-Key", "test-api-key");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AccountResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Single(body.Data);
        Assert.Equal("Everyday Current", body.Data.Single().DisplayName);
        Assert.Equal("1", body.NextCursor);
    }

    [Fact]
    public async Task ConsentExpiringNow_ReturnsExistingValidationContract()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/open-banking/v1/consents");
        request.Headers.Add("X-API-Key", "test-api-key");
        request.Content = JsonContent.Create(new CreateConsentRequest(
            "customer-1", ["accounts:read"], OpenBankingApiFactory.Now));

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_REQUEST", body?.Code);
        Assert.Equal("expiresAt must be in the future", body?.Message);
    }

    private sealed record AccountResponse(string DisplayName);
}

public sealed class OpenBankingApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenBanking:ApiKey"] = "test-api-key"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        });
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
