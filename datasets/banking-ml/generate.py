"""Reproducible synthetic lab data; Python standard library only. No real transactions."""
import csv
import random
from pathlib import Path

rng = random.Random(42)
categories = {
    "Groceries": (65000, ["fresh vegetables", "weekly food", "milk and bread", "daily supplies"]),
    "Dining": (90000, ["lunch meal", "dinner order", "coffee and cake", "daily supplies"]),
    "Transport": (35000, ["city ride", "bus ticket", "parking fee", "daily service"]),
    "Utilities": (140000, ["water service", "electricity bill", "internet fee", "monthly service"]),
    "Shopping": (220000, ["clothing order", "home accessory", "book purchase", "daily supplies"]),
}
types = ["Local", "Chain", "Online"]
rows = []
for category, (base, descriptions) in categories.items():
    merchant_indices = list(range(40))
    rng.shuffle(merchant_indices)
    for position, merchant in enumerate(merchant_indices):
        split = "train" if position < 28 else "validation" if position < 34 else "test"
        merchant_id = f"{category}-{merchant:02d}"
        # Random neutral merchant names do not encode the target category.
        name = f"Merchant {rng.randrange(10**8, 10**9)}"
        merchant_type = rng.choice(types)
        merchant_effect = rng.uniform(0.85, 1.15)
        for _ in range(10):
            day = rng.randrange(7)
            quantity = rng.randrange(1, 9)
            # Some ambiguous text and label noise make error analysis meaningful.
            text_category = rng.choice(list(categories)) if rng.random() < 0.08 else category
            description = rng.choice(categories[text_category][1])
            description += " " + rng.choice(["card payment", "online purchase", "payment", "order"])
            price = (base * quantity * merchant_effect *
                     (1.12 if day >= 5 else 1) *
                     {"Local": 1.0, "Chain": 1.08, "Online": 0.95}[merchant_type])
            amount = max(1000, round((price + rng.gauss(0, base * 0.3)) / 1000) * 1000)
            rows.append([split, merchant_id, description, name, category, merchant_type, day, quantity, amount])
rng.shuffle(rows)
with Path(__file__).with_name("transactions.csv").open("w", newline="", encoding="utf-8") as handle:
    writer = csv.writer(handle, lineterminator="\n")
    writer.writerow(["Split", "MerchantId", "Description", "MerchantName", "Category", "MerchantType",
                     "DayOfWeek", "Quantity", "Amount"])
    writer.writerows(rows)
print(f"Generated {len(rows)} synthetic rows (VND), seed=42")
