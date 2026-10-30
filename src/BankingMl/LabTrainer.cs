using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.ML;

namespace BankingMl;

public sealed record ClassificationEvaluation(ClassificationMetrics Metrics, ClassificationMetrics Baseline);
public sealed record RegressionEvaluation(RegressionMetrics Metrics, RegressionMetrics Baseline);
public sealed record ClassificationCandidate(string Name, ClassificationMetrics Train, ClassificationMetrics Validation);
public sealed record RegressionCandidate(string Name, RegressionMetrics Train, RegressionMetrics Validation);
public sealed record LabReport(
    int Seed, string DatasetSha256, int TrainRows, int ValidationRows, int TestRows,
    string MajorityCategory, double MeanAmount,
    ClassificationCandidate[] ClassificationCandidates, string SelectedClassifier, ClassificationEvaluation ClassificationTest,
    RegressionCandidate[] RegressionCandidates, string SelectedRegressor, RegressionEvaluation RegressionTest,
    ClassificationError[] ClassificationErrors, RegressionExample[] RegressionExamples);

public static class LabTrainer
{
    public const int Seed = 42;

    public static LabReport Train(string datasetPath, string outputDirectory)
    {
        // Do not overwrite an artifact bundle in place: API instances load immutable bundles.
        if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
            throw new IOException("Output directory must be empty. Choose a new model bundle directory.");
        var ml = new MLContext(seed: Seed);
        var split = LabDataset.Load(ml, datasetPath);
        var train = ml.Data.LoadFromEnumerable(split.Train);
        var majority = split.Train.GroupBy(r => r.Category).OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal).First().Key;
        var mean = split.Train.Average(r => (double)r.Amount);

        // Each Fit includes preprocessing and sees TRAIN ONLY. Validation selects; test is never consulted.
        var classifiers = new[] { 30, 100 }.Select(iterations =>
        {
            var pipeline = ml.Transforms.Conversion.MapValueToKey("Label", nameof(TransactionSample.Category))
                .Append(ml.Transforms.Text.FeaturizeText("Text", nameof(TransactionSample.Description)))
                .Append(ml.Transforms.Text.FeaturizeText("MerchantText", nameof(TransactionSample.MerchantName)))
                .Append(ml.Transforms.Concatenate("Features", "Text", "MerchantText"))
                .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label", featureColumnName: "Features", maximumNumberOfIterations: iterations))
                .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            var model = pipeline.Fit(train);
            var candidate = new ClassificationCandidate($"SdcaMaximumEntropy-{iterations}",
                EvaluateCategory(ml, model, split.Train), EvaluateCategory(ml, model, split.Validation));
            return (Model: (ITransformer)model, Report: candidate);
        }).ToArray();
        var classifier = classifiers.OrderByDescending(c => c.Report.Validation.MacroF1)
            .ThenByDescending(c => c.Report.Validation.Accuracy).First();

        var regressionPrep = ml.Transforms.Categorical.OneHotEncoding("CategoryFeatures", nameof(TransactionSample.Category))
            .Append(ml.Transforms.Categorical.OneHotEncoding("MerchantFeatures", nameof(TransactionSample.MerchantType)))
            .Append(ml.Transforms.Concatenate("Features", "CategoryFeatures", "MerchantFeatures",
                nameof(TransactionSample.DayOfWeek), nameof(TransactionSample.Quantity)))
            .Append(ml.Transforms.NormalizeMeanVariance("Features"));
        var linear = regressionPrep.Append(ml.Regression.Trainers.Sdca(
            labelColumnName: nameof(TransactionSample.Amount), maximumNumberOfIterations: 100)).Fit(train);
        var tree = regressionPrep.Append(ml.Regression.Trainers.FastTree(
            labelColumnName: nameof(TransactionSample.Amount), numberOfLeaves: 16,
            numberOfTrees: 60, minimumExampleCountPerLeaf: 10)).Fit(train);
        var regressors = new[] { (Name: "Sdca", Model: (ITransformer)linear),
            (Name: "FastTree", Model: (ITransformer)tree) }.Select(c =>
            (c.Model, Report: new RegressionCandidate(c.Name, EvaluateAmount(ml, c.Model, split.Train),
                EvaluateAmount(ml, c.Model, split.Validation)))).ToArray();
        var regressor = regressors.OrderBy(c => c.Report.Validation.Rmse).First();

        var categoryPredictions = PredictCategories(ml, classifier.Model, split.Test);
        var amountPredictions = PredictAmounts(ml, regressor.Model, split.Test);
        var report = new LabReport(Seed,
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(datasetPath))).ToLowerInvariant(),
            split.Train.Length, split.Validation.Length, split.Test.Length, majority, mean,
            classifiers.Select(c => c.Report).ToArray(), classifier.Report.Name,
            new ClassificationEvaluation(
                LabMetrics.Classification(split.Test.Select(r => r.Category).ToArray(), categoryPredictions),
                LabMetrics.Classification(split.Test.Select(r => r.Category).ToArray(),
                    split.Test.Select(_ => majority).ToArray())),
            regressors.Select(c => c.Report).ToArray(), regressor.Report.Name,
            new RegressionEvaluation(
                LabMetrics.Regression(split.Test.Select(r => (double)r.Amount).ToArray(), amountPredictions),
                LabMetrics.Regression(split.Test.Select(r => (double)r.Amount).ToArray(),
                    split.Test.Select(_ => mean).ToArray())),
            split.Test.Select((r, i) => new ClassificationError(r.Description, r.MerchantName, r.Category, categoryPredictions[i]))
                .Where(r => r.Actual != r.Predicted).Take(20).ToArray(),
            split.Test.Select((r, i) => new RegressionExample(r.Category, r.Quantity, r.Amount,
                    amountPredictions[i], r.Amount - amountPredictions[i]))
                .OrderByDescending(r => Math.Abs(r.Residual)).Take(20).ToArray());

        Directory.CreateDirectory(outputDirectory);
        ml.Model.Save(classifier.Model, train.Schema, Path.Combine(outputDirectory, "classification.zip"));
        ml.Model.Save(regressor.Model, train.Schema, Path.Combine(outputDirectory, "regression.zip"));
        File.WriteAllText(Path.Combine(outputDirectory, "report.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        WritePredictionCsv(Path.Combine(outputDirectory, "test-predictions.csv"), split.Test,
            categoryPredictions, amountPredictions);
        // Verify both serialized pipelines can make predictions with a fresh MLContext.
        var loaded = new BankingPredictor(outputDirectory);
        foreach (var row in split.Test.Take(10))
        {
            var categoryBefore = PredictCategories(ml, classifier.Model, [row])[0];
            var amountBefore = PredictAmounts(ml, regressor.Model, [row])[0];
            if (loaded.Classify(row.Description, row.MerchantName) != categoryBefore ||
                Math.Abs(loaded.PredictAmount(row.Category, row.MerchantType, row.DayOfWeek, row.Quantity) - amountBefore) > 0.01)
                throw new InvalidOperationException("Reloaded model predictions differ.");
        }
        return report;
    }

    private static ClassificationMetrics EvaluateCategory(MLContext ml, ITransformer model, TransactionSample[] rows) =>
        LabMetrics.Classification(rows.Select(r => r.Category).ToArray(), PredictCategories(ml, model, rows));

    private static RegressionMetrics EvaluateAmount(MLContext ml, ITransformer model, TransactionSample[] rows) =>
        LabMetrics.Regression(rows.Select(r => (double)r.Amount).ToArray(), PredictAmounts(ml, model, rows));

    private static string[] PredictCategories(MLContext ml, ITransformer model, TransactionSample[] rows) =>
        ml.Data.CreateEnumerable<CategoryPrediction>(model.Transform(ml.Data.LoadFromEnumerable(rows)),
            reuseRowObject: false).Select(p => p.Category).ToArray();

    private static double[] PredictAmounts(MLContext ml, ITransformer model, TransactionSample[] rows) =>
        ml.Data.CreateEnumerable<AmountPrediction>(model.Transform(ml.Data.LoadFromEnumerable(rows)),
            reuseRowObject: false).Select(p => (double)p.Amount).ToArray();

    private static void WritePredictionCsv(string path, TransactionSample[] rows, string[] categories, double[] amounts)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("MerchantId,ActualCategory,PredictedCategory,ActualAmount,PredictedAmount,Residual");
        for (var i = 0; i < rows.Length; i++)
        {
            static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
            writer.WriteLine(FormattableString.Invariant(
                $"{Quote(rows[i].MerchantId)},{Quote(rows[i].Category)},{Quote(categories[i])},{rows[i].Amount},{amounts[i]},{rows[i].Amount - amounts[i]}"));
        }
    }
}
