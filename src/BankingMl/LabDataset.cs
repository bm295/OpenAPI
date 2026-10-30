using Microsoft.ML;

namespace BankingMl;

public sealed record DatasetSplit(TransactionSample[] Train, TransactionSample[] Validation, TransactionSample[] Test);

public static class LabDataset
{
    public static readonly string[] Categories = ["Groceries", "Dining", "Transport", "Utilities", "Shopping"];
    public static readonly string[] MerchantTypes = ["Local", "Chain", "Online"];

    public static DatasetSplit Load(MLContext ml, string path)
    {
        var data = ml.Data.LoadFromTextFile<TransactionSample>(path, separatorChar: ',', hasHeader: true,
            allowQuoting: true);
        var rows = ml.Data.CreateEnumerable<TransactionSample>(data, reuseRowObject: false).ToArray();
        return ValidateAndSplit(rows);
    }

    public static DatasetSplit ValidateAndSplit(TransactionSample[] rows)
    {
        if (rows.Length == 0) throw new ArgumentException("Dataset is empty.");
        foreach (var row in rows)
        {
            if (row.Split is not ("train" or "validation" or "test") ||
                string.IsNullOrWhiteSpace(row.MerchantId) || string.IsNullOrWhiteSpace(row.Description) ||
                string.IsNullOrWhiteSpace(row.MerchantName) || !Categories.Contains(row.Category) ||
                !MerchantTypes.Contains(row.MerchantType) || !float.IsFinite(row.Amount) || row.Amount <= 0 ||
                !float.IsFinite(row.DayOfWeek) || row.DayOfWeek < 0 || row.DayOfWeek > 6 ||
                row.DayOfWeek != MathF.Truncate(row.DayOfWeek) ||
                !float.IsFinite(row.Quantity) || row.Quantity < 1 || row.Quantity > 8 ||
                row.Quantity != MathF.Truncate(row.Quantity))
                throw new ArgumentException("Invalid dataset row; see datasets/banking-ml/README.md.");
        }
        if (rows.GroupBy(r => r.MerchantId).Any(g => g.Select(r => r.Split).Distinct().Count() > 1))
            throw new ArgumentException("A merchant must belong to exactly one split.");
        if (rows.GroupBy(r => r.MerchantName.Trim().ToUpperInvariant())
            .Any(g => g.Select(r => r.Split).Distinct().Count() > 1))
            throw new ArgumentException("Merchant names must not overlap between splits.");

        var result = new DatasetSplit(rows.Where(r => r.Split == "train").ToArray(),
            rows.Where(r => r.Split == "validation").ToArray(), rows.Where(r => r.Split == "test").ToArray());
        foreach (var split in new[] { result.Train, result.Validation, result.Test })
            if (Categories.Any(c => split.All(r => r.Category != c)))
                throw new ArgumentException("Each split must contain all five categories.");
        return result;
    }
}
