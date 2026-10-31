# Banking LLM Lab — tích hợp LLM vào backend

`POST /ai/v1/transactions/explain` dùng ML.NET có sẵn để phân loại giao dịch,
sau đó gọi LLM viết lời giải thích ngắn bằng tiếng Việt. Lời giải thích là **giả thuyết**
dựa trên dữ liệu đầu vào, không phải nguyên nhân nội tại của classifier. Chỉ dùng dữ liệu tổng hợp.

## Cấu trúc

- Presentation: kiểm tra demo API key, validate input, ánh xạ lỗi HTTP.
- Application: `TransactionExplanationService` lấy category từ ML và tạo prompt có version.
- Infrastructure: `ChatCompletionClient` triển khai `ILlmClient` bằng HttpClient.

System prompt và JSON dữ liệu được gửi bằng hai message riêng. Client không tự truyền category.
Không train model trong request; không cần embeddings hoặc vector database cho bài tập này.

## Chạy

Cần .NET 10, model ML đã train và API key/model có quyền dùng Chat Completions với
system message và `max_completion_tokens`. Repo không cố định model hoặc giả định giá.

Từ root repo, train theo [ML Lab](ml-lab.md):

```bash
dotnet run --project tools/BankingMl.Trainer -- datasets/banking-ml/transactions.csv artifacts/banking-ml/demo-v1
export BankingMl__ModelDirectory="$PWD/artifacts/banking-ml/demo-v1"
export Llm__Model='<model-id-from-your-provider>'
read -rs -p 'LLM API key: ' Llm__ApiKey
export Llm__ApiKey
export Llm__TimeoutSeconds=30
export Llm__MaxOutputTokens=400
dotnet run --project src/TaskApi/TaskApi.csproj
```

Key chỉ lưu ở môi trường máy chủ, không commit vào repo. Endpoint mặc định:
`https://api.openai.com/v1/chat/completions`. Có thể đặt `Llm__Endpoint` sang HTTPS provider
tin cậy có cùng wire format; key được gửi đến địa chỉ đó. Redirect HTTP bị tắt.
Thiếu cấu hình LLM không làm các API khác dừng chạy.

```bash
curl http://localhost:65348/ai/v1/transactions/explain \
  -H 'X-API-Key: dev-open-banking-key' \
  -H 'Content-Type: application/json' \
  -d '{"description":"Thanh toan cafe demo","merchantName":"Synthetic Cafe"}'
```

Response: `category`, `explanation`, `model`, `inputTokens`, `outputTokens`, `promptVersion`.
Usage thiếu trả `null`; không ước lượng hoặc bịa số tokens.

| HTTP | Ý nghĩa |
| --- | --- |
| 400 | Thiếu/rỗng/quá dài (description tối đa 500, merchant 200 ký tự), JSON không hợp lệ |
| 401/403 | Thiếu/sai demo API key |
| 502 | Provider lỗi, response sai, bị cắt/ngăn trả lời hoặc quá 64 KiB |
| 503 | ML bundle chưa sẵn sàng hoặc cấu hình LLM thiếu/không hợp lệ |
| 504 | Vượt timeout LLM |

Cancellation truyền xuống HttpClient. Không retry tự động vì request trước có thể đã tính phí.
Không trả kết quả giả khi provider lỗi. Giới hạn output cho phép 1–2000 tokens, timeout 1–120 giây.

## Kiểm thử và thực hành

```bash
dotnet test tests/TaskApi.Tests/TaskApi.Tests.csproj
```

Tests dùng fake HTTP/ML/LLM, không dùng key thật, không phát sinh phí. CI `Banking LLM Lab`
chạy trên push/PR. Smoke test thật cần tự cấu hình key/model và gọi curl bên trên.

Thử dữ liệu tổng hợp rõ nghĩa, mơ hồ và chứa “ignore previous instructions”. Kiểm tra category
vẫn do ML quyết định, lời giải thích thừa nhận giả thuyết và không bịa số tiền. Tăng độ dài input
và thay output limit để quan sát token usage. Giới hạn ký tự không phải bộ đếm tokens và không
bảo đảm context window của mọi model.

Prompt không bảo đảm chống prompt injection tuyệt đối. Text output là dữ liệu không tin cậy,
frontend phải escape khi hiển thị. Endpoint không có tool calling hoặc khởi tạo thanh toán,
không truy cập account/consent. Lab chưa có quota theo người dùng, rate limiting hoặc kiểm soát
chi phí production; không công khai với key demo hoặc gửi dữ liệu ngân hàng thật.

Wire format: https://developers.openai.com/api/reference/resources/chat
