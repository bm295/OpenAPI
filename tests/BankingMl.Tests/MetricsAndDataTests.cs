using BankingMl;
using Microsoft.ML;
using Xunit;

namespace BankingMl.Tests;

public sealed class MetricsAndDataTests
{
    [Fact]
    public void MacroF1UsesPrecisionAndRecallRatherThanMacroAccuracy()
    {
        var metrics = LabMetrics.Classification(
            ["Dining", "Dining", "Groceries", "Groceries"],
            ["Dining", "Groceries", "Groceries", "Groceries"]);
        Assert.Equal(0.75, metrics.Accuracy, 6);
        Assert.Equal(((2.0 / 3) + (4.0 / 5)) / 5, metrics.MacroF1, 6);
        Assert.Equal(1, metrics.ConfusionMatrix[1][0]);
    }

    [Fact]
    public void RegressionMetricsSupportNegativeR2AndConstantTargets()
    {
        var metrics = LabMetrics.Regression([1, 2, 3], [3, 3, 3]);
        Assert.Equal(1, metrics.Mae, 6);
        Assert.Equal(Math.Sqrt(5.0 / 3), metrics.Rmse, 6);
        Assert.Equal(-1.5, metrics.RSquared!.Value, 6);
        Assert.Null(LabMetrics.Regression([2, 2], [1, 3]).RSquared);
    }

    [Fact]
    public void DatasetIsBalancedAndMerchantDisjoint()
    {
        var data = LabDataset.Load(new MLContext(42), Path.Combine(AppContext.BaseDirectory, "transactions.csv"));
        Assert.Equal(1400, data.Train.Length);
        Assert.Equal(300, data.Validation.Length);
        Assert.Equal(300, data.Test.Length);
        Assert.Empty(data.Train.Select(r => r.MerchantId).Intersect(data.Test.Select(r => r.MerchantId)));
        data.Test[0].MerchantId = data.Train[0].MerchantId;
        Assert.Throws<ArgumentException>(() => LabDataset.ValidateAndSplit(
            [.. data.Train, .. data.Validation, .. data.Test]));
    }

    [Fact]
    public void InvalidNumericDataIsRejected()
    {
        var data = LabDataset.Load(new MLContext(42), Path.Combine(AppContext.BaseDirectory, "transactions.csv"));
        data.Train[0].Amount = float.NaN;
        Assert.Throws<ArgumentException>(() => LabDataset.ValidateAndSplit(
            [.. data.Train, .. data.Validation, .. data.Test]));
        Assert.Throws<ArgumentException>(() => BankingPredictor.ValidateRegression("Dining", "Local", 1.5f, 2));
        Assert.Throws<ArgumentException>(() => BankingPredictor.ValidateRegression("Dining", "Local", 1, float.NaN));
        Assert.Throws<ArgumentException>(() => BankingPredictor.ValidateClassification("", "Merchant"));
    }
}
