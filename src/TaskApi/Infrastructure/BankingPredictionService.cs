using BankingMl;
using TaskApi.Application.Abstractions;

namespace TaskApi.Infrastructure;

public sealed class BankingPredictionService : IBankingPredictionService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<BankingPredictionService> _logger;

    public BankingPredictionService(IConfiguration configuration, IHostEnvironment environment,
        ILogger<BankingPredictionService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    private readonly object _gate = new();
    private BankingPredictor? _predictor;

    public string Classify(string description, string merchantName)
    {
        BankingPredictor.ValidateClassification(description, merchantName);
        return GetPredictor().Classify(description, merchantName);
    }

    public float PredictAmount(string category, string merchantType, float dayOfWeek, float quantity)
    {
        BankingPredictor.ValidateRegression(category, merchantType, dayOfWeek, quantity);
        return GetPredictor().PredictAmount(category, merchantType, dayOfWeek, quantity);
    }

    private BankingPredictor GetPredictor()
    {
        lock (_gate)
        {
            if (_predictor is not null) return _predictor;
            var configured = _configuration["BankingMl:ModelDirectory"];
            if (string.IsNullOrWhiteSpace(configured)) throw new ModelUnavailableException();
            try
            {
                var directory = Path.GetFullPath(configured, _environment.ContentRootPath);
                if (!File.Exists(Path.Combine(directory, "classification.zip")) ||
                    !File.Exists(Path.Combine(directory, "regression.zip")))
                    throw new ModelUnavailableException();
                return _predictor = new BankingPredictor(directory);
            }
            catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException
                or FormatException or UnauthorizedAccessException)
            {
                _logger.LogError(error, "Could not load Banking ML model bundle");
                throw new ModelUnavailableException();
            }
        }
    }
}
