# Synthetic banking transactions (VND)

This dataset is authored for this repository. It contains no real customer or bank data.
It is a learning fixture, not evidence of performance on real financial transactions.

Regenerate from the repository root with Python 3 (standard library only):

```bash
python datasets/banking-ml/generate.py
```

The committed CSV is ready to use; Python is optional. Seed: 42.
2,000 rows = 5 categories × 40 merchants × 10 transactions.
For each category, 28 merchants belong to train, 6 to validation, 6 to test:
1,400 / 300 / 300 rows (70/15/15). Merchant IDs and names do not overlap across splits.
Splits are assigned before training and persisted in the CSV. This is a test of new
merchants, not a temporal forecast or a test of new categories.

| Column | Type / values | Role |
| --- | --- | --- |
| Split | train, validation, test | Partition only; never a feature |
| MerchantId | Unique merchant identifier | Leakage guard only; never a feature |
| Description | Synthetic English payment text | Classification input |
| MerchantName | Neutral synthetic merchant name | Classification input |
| Category | Groceries, Dining, Transport, Utilities, Shopping | Classification target; regression input |
| MerchantType | Local, Chain, Online | Regression input |
| DayOfWeek | Integer 0–6; Monday=0, Sunday=6 | Regression input |
| Quantity | Integer 1–8 | Regression input |
| Amount | Positive float, whole VND | Regression target |

Amount is positive spending only; refunds, income and multiple currencies are excluded.
Quantity means the number of purchased units; for utilities it can represent service units.
It is a synthetic exercise attribute and is NOT currently part of the banking Transaction record.

Amounts combine category price, quantity, merchant type, weekend effects, an unobserved
merchant effect and Gaussian noise. Some descriptions are ambiguous or deliberately drawn
from another category. Consequently perfect prediction is neither expected nor desirable.
Merchant names do not encode the target category.

Classification never uses Category, Amount, MerchantType, DayOfWeek or Quantity as features.
Regression assumes the category is KNOWN; it does not consume the classifier's prediction.
Combining the models would require measuring propagated classification errors separately.

When replacing the CSV, preserve the header and column order, quoted CSV syntax and
the supported vocabulary/ranges. Every split must contain all five categories.
The trainer rejects merchant overlap, empty strings, invalid categories/types and invalid
numeric values. Changing labels, ranges, language or currency also requires code/contract changes.
