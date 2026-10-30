# Thực hành classification và regression

Mục tiêu: tự chạy từ CSV → train → chọn model bằng validation → đánh giá test →
lưu/tải model → dự đoán qua API. Dùng C# với ML.NET; không cần database hoặc dịch vụ AI ngoài.

## 1. Chạy bài thực hành

Yêu cầu .NET SDK theo global.json; chạy các lệnh từ thư mục gốc repo.

```bash
dotnet restore OpenAPI.sln
dotnet build OpenAPI.sln
dotnet test OpenAPI.sln
dotnet run --project tools/BankingMl.Trainer -- datasets/banking-ml/transactions.csv artifacts/banking-ml/demo-v1
```

Console cần đúng hai đối số: CSV và thư mục output mới hoặc rỗng.
Nếu đã chạy trước đó, dùng demo-v2 thay vì ghi đè demo-v1.
Dataset CSV được commit sẵn; không cần chạy Python. Có thể tái tạo với
`python datasets/banking-ml/generate.py`. Xem [data dictionary](../datasets/banking-ml/README.md).

Output:
- classification.zip: toàn bộ pipeline tiền xử lý và model phân loại.
- regression.zip: toàn bộ pipeline tiền xử lý và model hồi quy.
- report.json: seed, SHA-256 dataset, số dòng mỗi split, baseline, metrics train/validation,
  model được chọn, metrics test, confusion matrix và ví dụ lỗi.
- test-predictions.csv: 300 mẫu test, nhãn/số tiền thực tế, dự đoán và residual.

Trainer tự tải lại hai model trong MLContext mới và so sánh dự đoán của 10 mẫu test
với model trong bộ nhớ. Một bundle chỉ được dùng khi trainer kết thúc thành công.
Không commit bundle nhị phân vào Git; output mặc định đã được .gitignore bỏ qua.

## 2. Classification — giao dịch thuộc nhóm nào?

Đầu vào: Description + MerchantName. Đầu ra: một trong năm nhóm chi tiêu.
Tiền xử lý: featurize văn bản → ghép vector → phân loại đa lớp SDCA Maximum Entropy.
So sánh hai cấu hình 30 và 100 iterations; chọn Macro-F1 validation cao nhất,
dùng accuracy validation để phân xử nếu bằng nhau.

Category chỉ là label, không đi vào Features. Mọi bộ biến đổi đều Fit trên train.
Macro-F1 là trung bình F1 của từng nhóm; không nhầm với MacroAccuracy.
Confusion matrix trong report có hàng là nhãn thực tế, cột là dự đoán; thứ tự ở Labels.

Baseline luôn đoán nhóm phổ biến nhất trên TRAIN. Nếu đồng hạng, chọn theo tên.
Xem Accuracy, Macro-F1 và ClassificationErrors để hiểu model thường nhầm nhóm nào.
Dữ liệu có mô tả mơ hồ và nhiễu nên cần xem lỗi cụ thể, không chỉ nhìn một con số.

## 3. Regression — số tiền giao dịch khoảng bao nhiêu?

Đầu vào: Category đã biết, MerchantType, DayOfWeek và Quantity. Đầu ra: Amount, VND.
Đây là ước lượng chi tiêu từ thuộc tính giao dịch, không phải dự báo số dư tương lai.
API hồi quy không tự gọi classifier và không trộn USD từ dữ liệu demo Open Banking.

Tiền xử lý: one-hot nhóm/loại người bán → ghép số → chuẩn hóa trên train.
So sánh hồi quy tuyến tính SDCA và FastTree; chọn RMSE validation thấp nhất.
Amount không nằm trong Features. Baseline luôn dự đoán trung bình Amount của TRAIN.

- MAE: sai số tuyệt đối trung bình, đơn vị VND.
- RMSE: phạt sai số lớn mạnh hơn, đơn vị VND.
- R²: mức cải thiện so với trung bình của tập đang đánh giá; có thể âm.
  Nếu target không biến thiên thì report trả null.
- Residual = actual − predicted. Xem các mẫu sai nhiều nhất trong RegressionExamples.

Model trả dự đoán thô, không làm tròn hoặc ép về số dương để tránh che giấu lỗi mô hình.
Miền input hợp lệ: Category/ MerchantType đúng chữ hoa thường theo dictionary,
DayOfWeek nguyên 0–6 (Monday=0), Quantity nguyên 1–8.

## 4. Tránh leakage và kiểm tra overfitting

Dùng 1.400 train / 300 validation / 300 test; người bán không trùng giữa các tập.
Đây là lựa chọn có chủ đích: random split từng giao dịch có thể làm cùng người bán xuất hiện
ở cả train và test, tạo kết quả quá lạc quan. Split và MerchantId không phải features.

Trainer Fit cả preprocessing và model bằng train; validation chỉ chọn cấu hình;
test chỉ đánh giá model đã chọn. Không huấn luyện lại sau khi nhìn test.
Nếu liên tục thay đổi thiết kế dựa vào kết quả test, cần giữ một tập holdout mới.

Trong report, đối chiếu train với validation của từng ứng viên:
train tốt nhưng validation kém là dấu hiệu cần kiểm tra overfitting hoặc khác biệt phân phối.
SDCA dùng ngẫu nhiên với seed 42; sai số số học/threading có thể làm metrics khác nhẹ giữa nền tảng.

Dữ liệu có quy luật nhân tạo nên R² cao không chứng minh model hữu ích trên giao dịch thật.
Không đặt mục tiêu phải đạt một accuracy cố định; trước hết so sánh baseline và phân tích lỗi.

## 5. Chạy API với model đã lưu

Thiết lập đường dẫn tuyệt đối tới bundle. Đường dẫn tương đối được hiểu từ ContentRoot của TaskApi,
không nhất thiết từ thư mục shell hiện tại.

Bash:

```bash
export BankingMl__ModelDirectory="$PWD/artifacts/banking-ml/demo-v1"
dotnet run --project src/TaskApi --no-launch-profile --urls http://localhost:5080
```

PowerShell:

```powershell
$env:BankingMl__ModelDirectory = (Resolve-Path artifacts/banking-ml/demo-v1).Path
dotnet run --project src/TaskApi --no-launch-profile --urls http://localhost:5080
```

```bash
curl http://localhost:5080/ml/v1/transactions/classify \
  -H "X-API-Key: dev-open-banking-key" -H "Content-Type: application/json" \
  -d '{"description":"coffee and cake card payment","merchantName":"New Merchant"}'

curl http://localhost:5080/ml/v1/transactions/predict-amount \
  -H "X-API-Key: dev-open-banking-key" -H "Content-Type: application/json" \
  -d '{"category":"Dining","merchantType":"Local","dayOfWeek":1,"quantity":2}'
```

Classification trả `{"category":"..."}`; regression trả `{"amount":...,"currency":"VND"}`.
Không trả score như một xác suất tin cậy đã hiệu chỉnh.

| HTTP | Ý nghĩa |
| --- | --- |
| 200 | Dự đoán thành công |
| 400 | Input thiếu/không hợp lệ |
| 401 | Thiếu API key |
| 403 | Sai API key |
| 503 | Chưa cấu hình, thiếu hoặc không tải được model bundle |

Không có model vẫn chạy được các endpoint Open Banking hiện tại.
API tải model khi nhận yêu cầu hợp lệ đầu tiên, cache model trong bộ nhớ;
mỗi request có PredictionEngine riêng để tránh dùng chung engine không thread-safe.
Huấn luyện bundle mới rồi đổi cấu hình và khởi động lại API để chuyển phiên bản.

## 6. Bài tập tự kiểm chứng

1. Đọc một dòng CSV: chỉ ra features và label của mỗi bài.
2. Đọc report: model nào được chọn và bằng metric nào?
3. Tìm một nhầm lẫn classification và một residual lớn; giải thích nguyên nhân có thể.
4. Thay cấu hình bằng validation, giữ nguyên test đến lần đánh giá cuối.
5. Chạy lại API sau khi restart và xác nhận đang dùng model đã lưu.
6. Thử mô tả tiếng Việt: giải thích vì sao dữ liệu train tiếng Anh chưa đảm bảo xử lý tốt.
7. Nếu ghép classifier → regressor, thiết kế phép đánh giá có tính lỗi phân loại truyền sang hồi quy.

## Cấu trúc code

| Vị trí | Trách nhiệm |
| --- | --- |
| src/BankingMl | Dataset contract, pipeline, metrics, training và inference |
| tools/BankingMl.Trainer | Console điều phối bài thực hành |
| TaskApi/Application/Abstractions/IBankingPredictionService | Hợp đồng use case, không phụ thuộc ML.NET |
| TaskApi/Infrastructure/BankingPredictionService | Tải bundle, chuyển sang module ML |
| TaskApi/Presentation/Endpoints/BankingMlEndpoints | HTTP, authorization và ánh xạ lỗi |
| tests/BankingMl.Tests | Metrics, split, leakage, training, reload và API |
