namespace TaskApi.Application.Abstractions;

public interface IBankingPredictionService
{
    string Classify(string description, string merchantName);
    float PredictAmount(string category, string merchantType, float dayOfWeek, float quantity);
}

public sealed class ModelUnavailableException : Exception
{
    public ModelUnavailableException() : base("Train and configure a model bundle before requesting predictions.") { }
}
