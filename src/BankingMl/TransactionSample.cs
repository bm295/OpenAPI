using Microsoft.ML.Data;

namespace BankingMl;

// CSV field order is part of the lab's dataset contract. Neither pipeline uses Split or MerchantId.
public sealed class TransactionSample
{
    [LoadColumn(0)] public string Split { get; set; } = "";
    [LoadColumn(1)] public string MerchantId { get; set; } = "";
    [LoadColumn(2)] public string Description { get; set; } = "";
    [LoadColumn(3)] public string MerchantName { get; set; } = "";
    [LoadColumn(4)] public string Category { get; set; } = "";
    [LoadColumn(5)] public string MerchantType { get; set; } = "";
    [LoadColumn(6)] public float DayOfWeek { get; set; }
    [LoadColumn(7)] public float Quantity { get; set; }
    [LoadColumn(8)] public float Amount { get; set; }
}

public sealed class CategoryPrediction
{
    [ColumnName("PredictedLabel")] public string Category { get; set; } = "";
}

public sealed class AmountPrediction
{
    [ColumnName("Score")] public float Amount { get; set; }
}
