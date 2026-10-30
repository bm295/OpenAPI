using Microsoft.ML;

namespace BankingMl;

/// <summary>Loads once; each request uses its own PredictionEngine because engines are not thread-safe.</summary>
public sealed class BankingPredictor
{
    private readonly MLContext _ml = new(seed: LabTrainer.Seed);
    private readonly ITransformer _classifier;
    private readonly ITransformer _regressor;

    public BankingPredictor(string directory)
    {
        _classifier = _ml.Model.Load(Path.Combine(directory, "classification.zip"), out _);
        _regressor = _ml.Model.Load(Path.Combine(directory, "regression.zip"), out _);
    }

    public string Classify(string description, string merchantName)
    {
        ValidateClassification(description, merchantName);
        using var engine = _ml.Model.CreatePredictionEngine<TransactionSample, CategoryPrediction>(_classifier);
        return engine.Predict(new TransactionSample { Description = description, MerchantName = merchantName }).Category;
    }

    public static void ValidateClassification(string description, string merchantName)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500 ||
            string.IsNullOrWhiteSpace(merchantName) || merchantName.Length > 200)
            throw new ArgumentException("Description (1–500 chars) and MerchantName (1–200 chars) are required.");
    }

    public float PredictAmount(string category, string merchantType, float dayOfWeek, float quantity)
    {
        ValidateRegression(category, merchantType, dayOfWeek, quantity);
        using var engine = _ml.Model.CreatePredictionEngine<TransactionSample, AmountPrediction>(_regressor);
        var result = engine.Predict(new TransactionSample { Category = category, MerchantType = merchantType,
            DayOfWeek = dayOfWeek, Quantity = quantity }).Amount;
        if (!float.IsFinite(result)) throw new InvalidOperationException("Model returned a nonfinite prediction.");
        return result;
    }
    public static void ValidateRegression(string category, string merchantType, float dayOfWeek, float quantity)
    {
        if (!LabDataset.Categories.Contains(category) || !LabDataset.MerchantTypes.Contains(merchantType) ||
            !float.IsFinite(dayOfWeek) || dayOfWeek < 0 || dayOfWeek > 6 || dayOfWeek != MathF.Truncate(dayOfWeek) ||
            !float.IsFinite(quantity) || quantity < 1 || quantity > 8 || quantity != MathF.Truncate(quantity))
            throw new ArgumentException("Use a supported category/type, integer day 0–6 and integer quantity 1–8.");
    }
}
