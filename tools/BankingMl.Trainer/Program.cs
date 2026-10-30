using BankingMl;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/BankingMl.Trainer -- <dataset.csv> <new-output-directory>");
    return 1;
}
try
{
    var report = LabTrainer.Train(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
    Console.WriteLine($"Rows: train={report.TrainRows}, validation={report.ValidationRows}, test={report.TestRows}");
    Console.WriteLine($"Classification: {report.SelectedClassifier}");
    Console.WriteLine($"  Test accuracy={report.ClassificationTest.Metrics.Accuracy:F3}, Macro-F1={report.ClassificationTest.Metrics.MacroF1:F3}");
    Console.WriteLine($"  Baseline accuracy={report.ClassificationTest.Baseline.Accuracy:F3}, Macro-F1={report.ClassificationTest.Baseline.MacroF1:F3}");
    Console.WriteLine($"Regression: {report.SelectedRegressor}");
    Console.WriteLine($"  Test MAE={report.RegressionTest.Metrics.Mae:F0} VND, RMSE={report.RegressionTest.Metrics.Rmse:F0} VND, R²={report.RegressionTest.Metrics.RSquared:F3}");
    Console.WriteLine($"  Baseline RMSE={report.RegressionTest.Baseline.Rmse:F0} VND");
    Console.WriteLine("Saved both models, report.json and test-predictions.csv. Reload verification passed.");
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
