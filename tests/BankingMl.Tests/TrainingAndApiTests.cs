using System.Net;
using System.Net.Http.Json;
using BankingMl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Xunit;

namespace BankingMl.Tests;

public sealed class TrainedLabFixture : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "banking-ml-tests-" + Guid.NewGuid());
    public LabReport Report { get; }

    public TrainedLabFixture() =>
        Report = LabTrainer.Train(Path.Combine(AppContext.BaseDirectory, "transactions.csv"), DirectoryPath);

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}

public sealed class TrainingAndApiTests(TrainedLabFixture fixture) : IClassFixture<TrainedLabFixture>
{
    [Fact]
    public void TrainingBeatsBaselinesAndWritesInspectableArtifacts()
    {
        var report = fixture.Report;
        Assert.True(report.ClassificationTest.Metrics.MacroF1 > report.ClassificationTest.Baseline.MacroF1);
        Assert.True(report.RegressionTest.Metrics.Rmse < report.RegressionTest.Baseline.Rmse);
        Assert.Equal(2, report.ClassificationCandidates.Length);
        Assert.Equal(2, report.RegressionCandidates.Length);
        Assert.True(File.Exists(Path.Combine(fixture.DirectoryPath, "report.json")));
        Assert.Equal(301, File.ReadAllLines(Path.Combine(fixture.DirectoryPath, "test-predictions.csv")).Length);
        Assert.NotEmpty(report.ClassificationErrors);
        Assert.NotEmpty(report.RegressionExamples);
    }

    [Fact]
    public void SerializedPipelinesDoNotReadTheirTargetsAsFeatures()
    {
        var ml = new MLContext(42);
        var first = new TransactionSample { Description = "coffee and cake", MerchantName = "New Merchant",
            Category = "Dining", MerchantType = "Local", Quantity = 2, DayOfWeek = 1, Amount = 100000 };
        var second = new TransactionSample { Description = first.Description, MerchantName = first.MerchantName,
            Category = "Shopping", MerchantType = first.MerchantType, Quantity = 2, DayOfWeek = 1, Amount = 999999 };
        var classifier = ml.Model.Load(Path.Combine(fixture.DirectoryPath, "classification.zip"), out _);
        using var categories = ml.Model.CreatePredictionEngine<TransactionSample, CategoryPrediction>(classifier);
        Assert.Equal(categories.Predict(first).Category, categories.Predict(second).Category);
        second.Category = first.Category;
        var regressor = ml.Model.Load(Path.Combine(fixture.DirectoryPath, "regression.zip"), out _);
        using var amounts = ml.Model.CreatePredictionEngine<TransactionSample, AmountPrediction>(regressor);
        Assert.Equal(amounts.Predict(first).Amount, amounts.Predict(second).Amount);
    }

    [Fact]
    public async Task ApiUsesReloadedModelsAndSupportsConcurrentPredictions()
    {
        using var factory = CreateFactory(fixture.DirectoryPath);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "ml-test-key");
        var predictor = new BankingPredictor(fixture.DirectoryPath);
        var requests = Enumerable.Range(0, 8).Select(async _ =>
        {
            var response = await client.PostAsJsonAsync("/ml/v1/transactions/classify",
                new { description = "coffee and cake", merchantName = "New Merchant" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(TestContext.Current.CancellationToken);
            Assert.Equal(predictor.Classify("coffee and cake", "New Merchant"), body!.Category);
        });
        await Task.WhenAll(requests);
        var result = await client.PostAsJsonAsync("/ml/v1/transactions/predict-amount",
            new { category = "Dining", merchantType = "Local", dayOfWeek = 1, quantity = 2 }, TestContext.Current.CancellationToken);
        result.EnsureSuccessStatusCode();
        var amount = await result.Content.ReadFromJsonAsync<AmountResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("VND", amount!.Currency);
        Assert.Equal(predictor.PredictAmount("Dining", "Local", 1, 2), amount.Amount);
    }

    [Fact]
    public async Task ApiDistinguishesUnauthorizedInvalidAndMissingModels()
    {
        using var factory = CreateFactory(Path.Combine(fixture.DirectoryPath, "missing"));
        using var client = factory.CreateClient();
        var path = "/ml/v1/transactions/classify";
        var input = new { description = "coffee", merchantName = "Merchant" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(path, input, TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(path, input, TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-API-Key");
        client.DefaultRequestHeaders.Add("X-API-Key", "ml-test-key");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync(path, input, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path,
            new { description = "", merchantName = "Merchant" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/ml/v1/transactions/predict-amount",
            new { category = "Unknown", merchantType = "Local", dayOfWeek = 9, quantity = 0 }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/ml/v1/transactions/predict-amount",
            new { category = "Dining", merchantType = "Local", quantity = 2 }, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task CorruptModelBundleReturns503()
    {
        var path = Path.Combine(fixture.DirectoryPath, "corrupt");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "classification.zip"), "not a model");
        File.WriteAllText(Path.Combine(path, "regression.zip"), "not a model");
        using var factory = CreateFactory(path);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "ml-test-key");
        var response = await client.PostAsJsonAsync("/ml/v1/transactions/classify",
            new { description = "coffee", merchantName = "Merchant" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(string models) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["BankingMl:ModelDirectory"] = models,
                    ["OpenBanking:ApiKey"] = "ml-test-key" })));

    private sealed record CategoryResponse(string Category);
    private sealed record AmountResponse(float Amount, string Currency);
}
