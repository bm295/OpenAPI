namespace BankingMl;

public sealed record ClassificationMetrics(double Accuracy, double MacroF1, string[] Labels, int[][] ConfusionMatrix);
public sealed record RegressionMetrics(double Mae, double Rmse, double? RSquared);
public sealed record ClassificationError(string Description, string MerchantName, string Actual, string Predicted);
public sealed record RegressionExample(string Category, float Quantity, double Actual, double Predicted, double Residual);

public static class LabMetrics
{
    // Confusion matrix rows = actual; columns = predicted. Macro-F1 is NOT ML.NET MacroAccuracy.
    public static ClassificationMetrics Classification(IReadOnlyList<string> actual, IReadOnlyList<string> predicted)
    {
        if (actual.Count == 0 || actual.Count != predicted.Count)
            throw new ArgumentException("Metrics require equally sized, nonempty inputs.");
        var labels = LabDataset.Categories;
        var matrix = labels.Select(_ => new int[labels.Length]).ToArray();
        for (var i = 0; i < actual.Count; i++)
        {
            var a = Array.IndexOf(labels, actual[i]);
            var p = Array.IndexOf(labels, predicted[i]);
            if (a < 0 || p < 0) throw new ArgumentException("Unknown classification label.");
            matrix[a][p]++;
        }
        double macroF1 = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            var tp = matrix[i][i];
            var actualCount = matrix[i].Sum();
            var predictedCount = matrix.Sum(row => row[i]);
            macroF1 += actualCount + predictedCount == 0 ? 0 : 2.0 * tp / (actualCount + predictedCount);
        }
        return new ClassificationMetrics(
            Enumerable.Range(0, labels.Length).Sum(i => matrix[i][i]) / (double)actual.Count,
            macroF1 / labels.Length, labels.ToArray(), matrix);
    }

    public static RegressionMetrics Regression(IReadOnlyList<double> actual, IReadOnlyList<double> predicted)
    {
        if (actual.Count == 0 || actual.Count != predicted.Count ||
            actual.Any(x => !double.IsFinite(x)) || predicted.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("Metrics require equally sized, nonempty, finite inputs.");
        var mean = actual.Average();
        var squaredError = actual.Zip(predicted, (a, p) => (a - p) * (a - p)).Sum();
        var total = actual.Sum(a => (a - mean) * (a - mean));
        return new RegressionMetrics(actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average(),
            Math.Sqrt(squaredError / actual.Count), total == 0 ? null : 1 - squaredError / total);
    }
}
