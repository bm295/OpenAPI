# Marketability TODO

This checklist breaks the work into very small, concrete tasks for turning the current Open Banking sample into a more marketable product. Each item should be small enough to complete in a focused pull request. Where possible, tasks name the exact class, record, endpoint group, or documentation artifact to create or update.

## Product positioning docs

- [x] Create `docs/product-overview.md`.
- [x] Add a `# Product Overview` heading to `docs/product-overview.md`.
- [x] Add a one-sentence product positioning statement to `docs/product-overview.md`.
- [x] Add a `Target customers` section to `docs/product-overview.md`.
- [x] Add a `Primary use cases` section to `docs/product-overview.md`.
- [x] Add a `Current limitations` section to `docs/product-overview.md`.
- [x] Add a `Production readiness` section to `docs/product-overview.md`.
- [x] Add a link from `README.md` to `docs/product-overview.md`.
- [x] Add a `Who this is for` section to `README.md`.
- [x] Add a `Who this is not for` section to `README.md`.
- [x] Add a `Roadmap` section to `README.md`.
- [x] Add a production-readiness disclaimer to `README.md`.
- [x] Add a `docs/glossary.md` file.
- [x] Define `Account`, `Balance`, `Transaction`, `Consent`, `Payment instruction`, and `OpenAPI` in `docs/glossary.md`.

## Developer setup docs

- [x] Create `docs/getting-started.md`.
- [x] Add a `.NET SDK` prerequisite section to `docs/getting-started.md`.
- [x] Add `dotnet restore OpenAPI.sln` to `docs/getting-started.md`.
- [x] Add `dotnet run --project src/TaskApi/TaskApi.csproj` to `docs/getting-started.md`.
- [x] Add a `Verify health` step using `GET /health` to `docs/getting-started.md`.
- [x] Add a `Verify authorization` step using `X-API-Key` to `docs/getting-started.md`.
- [x] Add a `Troubleshooting missing .NET SDK` section to `docs/getting-started.md`.
- [x] Add a `Troubleshooting port conflicts` section to `docs/getting-started.md`.
- [x] Add a `Troubleshooting 401 and 403 responses` section to `docs/getting-started.md`.
- [x] Add a `docs/http-examples.md` file.
- [x] Add a curl example for `GET /open-banking/v1/accounts` to `docs/http-examples.md`.
- [x] Add a curl example for `GET /open-banking/v1/accounts/{accountId}` to `docs/http-examples.md`.
- [x] Add a curl example for `GET /open-banking/v1/accounts/{accountId}/balances` to `docs/http-examples.md`.
- [x] Add a curl example for `GET /open-banking/v1/accounts/{accountId}/transactions` to `docs/http-examples.md`.
- [x] Add a curl example for `POST /open-banking/v1/consents` to `docs/http-examples.md`.
- [x] Add a curl example for `DELETE /open-banking/v1/consents/{consentId}` to `docs/http-examples.md`.
- [x] Add a curl example for `POST /open-banking/v1/payments` to `docs/http-examples.md`.

## Domain model improvements

- [ ] Create `src/TaskApi/Domain/Customer.cs`.
- [ ] Add a `Customer` record with `Id`, `DisplayName`, `Email`, and `CreatedAt` properties.
- [ ] Create `src/TaskApi/Domain/Institution.cs`.
- [ ] Add an `Institution` record with `Id`, `Name`, `CountryCode`, and `CreatedAt` properties.
- [ ] Create `src/TaskApi/Domain/ConsentPermission.cs`.
- [ ] Replace raw consent permission strings with a `ConsentPermission` enum.
- [ ] Add `ReadAccountsBasic` to `ConsentPermission`.
- [ ] Add `ReadAccountsDetail` to `ConsentPermission`.
- [ ] Add `ReadBalances` to `ConsentPermission`.
- [ ] Add `ReadTransactions` to `ConsentPermission`.
- [ ] Add `InitiatePayments` to `ConsentPermission`.
- [ ] Create `src/TaskApi/Domain/PaymentLimits.cs`.
- [ ] Add a `PaymentLimits` record with `DailyLimit`, `PerPaymentLimit`, and `Currency` properties.
- [ ] Create `src/TaskApi/Domain/AuditEvent.cs`.
- [ ] Add an `AuditEvent` record with `Id`, `Action`, `SubjectId`, `Metadata`, and `CreatedAt` properties.

## Application contracts

- [ ] Create `src/TaskApi/Application/Contracts/AccountContracts.cs`.
- [ ] Move account-specific response contracts into `AccountContracts.cs`.
- [ ] Create `src/TaskApi/Application/Contracts/ConsentContracts.cs`.
- [ ] Move `CreateConsentRequest` into `ConsentContracts.cs`.
- [ ] Add an `AuthorizeConsentRequest` record to `ConsentContracts.cs`.
- [ ] Add a `ConsentResponse` record to `ConsentContracts.cs`.
- [ ] Create `src/TaskApi/Application/Contracts/PaymentContracts.cs`.
- [ ] Move `CreatePaymentRequest` into `PaymentContracts.cs`.
- [ ] Add a `PaymentStatusResponse` record to `PaymentContracts.cs`.
- [ ] Add an `IdempotencyKey` property to `CreatePaymentRequest`.
- [ ] Create `src/TaskApi/Application/Contracts/ErrorContracts.cs`.
- [ ] Move `ErrorResponse` and `ErrorDetail` into `ErrorContracts.cs`.
- [ ] Add a `TraceId` property to `ErrorResponse`.
- [ ] Create `src/TaskApi/Application/Contracts/PaginationContracts.cs`.
- [ ] Move `PagedResponse<T>` into `PaginationContracts.cs`.
- [ ] Add a `PageInfo` record with `NextCursor`, `PageSize`, and `ReturnedCount` properties.

## Service interfaces

- [ ] Create `src/TaskApi/Application/Abstractions/IAccountService.cs`.
- [ ] Move `ListAccounts`, `GetAccount`, `GetBalance`, and `ListTransactions` signatures into `IAccountService`.
- [ ] Create `src/TaskApi/Application/Abstractions/IConsentService.cs`.
- [ ] Move `CreateConsent`, `GetConsent`, and `RevokeConsent` signatures into `IConsentService`.
- [ ] Add `AuthorizeConsent(Guid consentId, AuthorizeConsentRequest request)` to `IConsentService`.
- [ ] Create `src/TaskApi/Application/Abstractions/IPaymentService.cs`.
- [ ] Move `CreatePayment` and `GetPayment` signatures into `IPaymentService`.
- [ ] Add `GetPaymentStatus(Guid paymentId)` to `IPaymentService`.
- [ ] Create `src/TaskApi/Application/Abstractions/IAuditService.cs`.
- [ ] Add `RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken)` to `IAuditService`.
- [ ] Create `src/TaskApi/Application/Abstractions/IClock.cs`.
- [ ] Add `DateTimeOffset UtcNow { get; }` to `IClock`.

## Service implementation classes

- [ ] Create `src/TaskApi/Application/Services/AccountService.cs`.
- [ ] Move account listing, account lookup, balance lookup, and transaction listing logic from `OpenBankingService` into `AccountService`.
- [ ] Create `src/TaskApi/Application/Services/ConsentService.cs`.
- [ ] Move consent creation, lookup, and revocation logic from `OpenBankingService` into `ConsentService`.
- [ ] Add an `AuthorizeConsent` method to `ConsentService`.
- [ ] Update `ConsentService.CreateConsent` to reject empty trimmed permissions.
- [ ] Update `ConsentService.CreateConsent` to store typed `ConsentPermission` values.
- [ ] Update `ConsentService.RevokeConsent` to record revocation time.
- [ ] Create `src/TaskApi/Application/Services/PaymentService.cs`.
- [ ] Move payment creation and lookup logic from `OpenBankingService` into `PaymentService`.
- [ ] Update `PaymentService.CreatePayment` to require `ConsentStatus.Authorized`.
- [ ] Update `PaymentService.CreatePayment` to reject expired consents using `IClock.UtcNow`.
- [ ] Update `PaymentService.CreatePayment` to verify the consent has `ConsentPermission.InitiatePayments`.
- [ ] Update `PaymentService.CreatePayment` to validate the debtor account exists.
- [ ] Update `PaymentService.CreatePayment` to validate the debtor account belongs to the consent customer.
- [ ] Update `PaymentService.CreatePayment` to enforce a per-payment limit.
- [ ] Update `PaymentService.CreatePayment` to enforce an idempotency key.
- [ ] Create `src/TaskApi/Application/Services/AuditService.cs`.
- [ ] Add audit writes for consent creation in `AuditService`.
- [ ] Add audit writes for consent authorization in `AuditService`.
- [ ] Add audit writes for consent revocation in `AuditService`.
- [ ] Add audit writes for payment creation in `AuditService`.
- [ ] Create `src/TaskApi/Application/Services/SystemClock.cs`.
- [ ] Implement `IClock` in `SystemClock`.

## Persistence and infrastructure

- [ ] Create `src/TaskApi/Application/Abstractions/IAccountRepository.cs`.
- [ ] Add `ListAsync`, `GetAsync`, `GetBalanceAsync`, and `ListTransactionsAsync` methods to `IAccountRepository`.
- [ ] Create `src/TaskApi/Application/Abstractions/IConsentRepository.cs`.
- [ ] Add `CreateAsync`, `GetAsync`, `UpdateAsync`, and `ListByCustomerAsync` methods to `IConsentRepository`.
- [ ] Create `src/TaskApi/Application/Abstractions/IPaymentRepository.cs`.
- [ ] Add `CreateAsync`, `GetAsync`, and `GetByIdempotencyKeyAsync` methods to `IPaymentRepository`.
- [ ] Create `src/TaskApi/Application/Abstractions/IAuditRepository.cs`.
- [ ] Add `AppendAsync` and `ListBySubjectAsync` methods to `IAuditRepository`.
- [ ] Create `src/TaskApi/Infrastructure/InMemoryAccountRepository.cs`.
- [ ] Move account, balance, and transaction storage from `InMemoryOpenBankingStore` into `InMemoryAccountRepository`.
- [ ] Create `src/TaskApi/Infrastructure/InMemoryConsentRepository.cs`.
- [ ] Move consent storage from `InMemoryOpenBankingStore` into `InMemoryConsentRepository`.
- [ ] Create `src/TaskApi/Infrastructure/InMemoryPaymentRepository.cs`.
- [ ] Move payment storage from `InMemoryOpenBankingStore` into `InMemoryPaymentRepository`.
- [ ] Create `src/TaskApi/Infrastructure/InMemoryAuditRepository.cs`.
- [ ] Add in-memory audit event storage to `InMemoryAuditRepository`.
- [ ] Create `src/TaskApi/Infrastructure/SeedData.cs`.
- [ ] Move seeded account IDs and sample records into `SeedData`.
- [ ] Add a roadmap task to replace in-memory repositories with a database-backed implementation.

## Endpoint classes

- [ ] Create `src/TaskApi/Presentation/Endpoints/HealthEndpoints.cs`.
- [ ] Move the `GET /health` endpoint from `OpenBankingEndpoints` to `HealthEndpoints`.
- [ ] Create `src/TaskApi/Presentation/Endpoints/AccountEndpoints.cs`.
- [ ] Move account endpoints from `OpenBankingEndpoints` into `AccountEndpoints`.
- [ ] Create `src/TaskApi/Presentation/Endpoints/ConsentEndpoints.cs`.
- [ ] Move consent endpoints from `OpenBankingEndpoints` into `ConsentEndpoints`.
- [ ] Add `POST /open-banking/v1/consents/{consentId}/authorize` to `ConsentEndpoints`.
- [ ] Create `src/TaskApi/Presentation/Endpoints/PaymentEndpoints.cs`.
- [ ] Move payment endpoints from `OpenBankingEndpoints` into `PaymentEndpoints`.
- [ ] Add `GET /open-banking/v1/payments/{paymentId}/status` to `PaymentEndpoints`.
- [ ] Create `src/TaskApi/Presentation/Endpoints/EndpointValidation.cs`.
- [ ] Move `ValidatePageSize` into `EndpointValidation`.
- [ ] Add `ValidateDateRange` to `EndpointValidation`.
- [ ] Add `ValidateRequiredString` to `EndpointValidation`.
- [ ] Add `ValidateMoney` to `EndpointValidation`.
- [ ] Update `Program.cs` to map `HealthEndpoints`, `AccountEndpoints`, `ConsentEndpoints`, and `PaymentEndpoints`.

## Security classes

- [ ] Create `src/TaskApi/Presentation/Security/OpenBankingAuthorizationOptions.cs`.
- [ ] Add `ApiKey`, `Issuer`, `Audience`, and `RequiredScopes` properties to `OpenBankingAuthorizationOptions`.
- [ ] Update `ApiKeyAuthorizationFilter` to use `IOptions<OpenBankingAuthorizationOptions>`.
- [ ] Create `src/TaskApi/Presentation/Security/ScopeAuthorizationFilter.cs`.
- [ ] Add scope validation logic to `ScopeAuthorizationFilter`.
- [ ] Add account read scope checks to account endpoints.
- [ ] Add consent write scope checks to consent endpoints.
- [ ] Add payment write scope checks to payment endpoints.
- [ ] Create `docs/security-model.md`.
- [ ] Document current API-key behavior in `docs/security-model.md`.
- [ ] Document target OAuth2/OIDC behavior in `docs/security-model.md`.
- [ ] Document required account, consent, and payment scopes in `docs/security-model.md`.
- [ ] Document secrets-management requirements in `docs/security-model.md`.

## Error handling

- [ ] Create `src/TaskApi/Presentation/Errors/ErrorCodes.cs`.
- [ ] Add `InvalidRequest`, `Unauthorized`, `Forbidden`, `NotFound`, `InvalidConsent`, `ExpiredConsent`, and `DuplicateRequest` constants to `ErrorCodes`.
- [ ] Create `src/TaskApi/Presentation/Errors/ErrorResponseFactory.cs`.
- [ ] Add `BadRequest`, `Unauthorized`, `Forbidden`, `NotFound`, and `Conflict` helper methods to `ErrorResponseFactory`.
- [ ] Update account endpoints to use `ErrorResponseFactory`.
- [ ] Update consent endpoints to use `ErrorResponseFactory`.
- [ ] Update payment endpoints to use `ErrorResponseFactory`.
- [ ] Add trace IDs to every error response.
- [ ] Create `docs/errors.md`.
- [ ] Document every error code in `docs/errors.md`.
- [ ] Add one JSON example per error code to `docs/errors.md`.

## OpenAPI and examples

- [ ] Add OpenAPI summaries to account endpoints.
- [ ] Add OpenAPI descriptions to account endpoints.
- [ ] Add OpenAPI summaries to consent endpoints.
- [ ] Add OpenAPI descriptions to consent endpoints.
- [ ] Add OpenAPI summaries to payment endpoints.
- [ ] Add OpenAPI descriptions to payment endpoints.
- [ ] Create `examples/open-banking.accounts.list.json`.
- [ ] Create `examples/open-banking.account.detail.json`.
- [ ] Create `examples/open-banking.balance.json`.
- [ ] Create `examples/open-banking.transactions.list.json`.
- [ ] Create `examples/open-banking.consent.create.request.json`.
- [ ] Create `examples/open-banking.consent.created.json`.
- [ ] Create `examples/open-banking.payment.create.request.json`.
- [ ] Create `examples/open-banking.error.invalid-request.json`.
- [ ] Create `examples/open-banking.error.unauthorized.json`.
- [ ] Create `examples/open-banking.error.forbidden.json`.
- [ ] Create `examples/open-banking.error.not-found.json`.

## Testing tasks

- [ ] Create `tests/TaskApi.Tests/TaskApi.Tests.csproj`.
- [ ] Add `tests/TaskApi.Tests/Application/Services/AccountServiceTests.cs`.
- [ ] Add `ListAccounts_ReturnsPagedAccounts` to `AccountServiceTests`.
- [ ] Add `GetAccount_ReturnsNullForUnknownAccount` to `AccountServiceTests`.
- [ ] Add `ListTransactions_AppliesDateFilters` to `AccountServiceTests`.
- [ ] Add `tests/TaskApi.Tests/Application/Services/ConsentServiceTests.cs`.
- [ ] Add `CreateConsent_RejectsEmptyPermissions` to `ConsentServiceTests`.
- [ ] Add `AuthorizeConsent_MarksConsentAuthorized` to `ConsentServiceTests`.
- [ ] Add `RevokeConsent_MarksConsentRevoked` to `ConsentServiceTests`.
- [ ] Add `tests/TaskApi.Tests/Application/Services/PaymentServiceTests.cs`.
- [ ] Add `CreatePayment_RejectsAwaitingAuthorizationConsent` to `PaymentServiceTests`.
- [ ] Add `CreatePayment_RejectsExpiredConsent` to `PaymentServiceTests`.
- [ ] Add `CreatePayment_RejectsConsentWithoutPaymentPermission` to `PaymentServiceTests`.
- [ ] Add `CreatePayment_RejectsDuplicateIdempotencyKey` to `PaymentServiceTests`.
- [ ] Add `tests/TaskApi.Tests/Presentation/Security/ApiKeyAuthorizationFilterTests.cs`.
- [ ] Add `InvokeAsync_Returns401WhenApiKeyMissing` to `ApiKeyAuthorizationFilterTests`.
- [ ] Add `InvokeAsync_Returns403WhenApiKeyInvalid` to `ApiKeyAuthorizationFilterTests`.
- [ ] Add `InvokeAsync_CallsNextWhenApiKeyValid` to `ApiKeyAuthorizationFilterTests`.
- [ ] Add `tests/TaskApi.Tests/Presentation/Endpoints/OpenBankingEndpointTests.cs`.
- [ ] Add an integration test for `GET /health`.
- [ ] Add an integration test for `GET /open-banking/v1/accounts` with a valid key.
- [ ] Add an integration test for `GET /open-banking/v1/accounts` without a key.
- [ ] Add an integration test for `POST /open-banking/v1/payments` with an unauthorized consent.

## Operations and deployment

- [ ] Create `docs/deployment.md`.
- [ ] Add local deployment steps to `docs/deployment.md`.
- [ ] Add staging deployment requirements to `docs/deployment.md`.
- [ ] Add production deployment requirements to `docs/deployment.md`.
- [ ] Create `docs/operations.md`.
- [ ] Document health-check expectations in `docs/operations.md`.
- [ ] Document log fields in `docs/operations.md`.
- [ ] Document metric names in `docs/operations.md`.
- [ ] Document trace propagation expectations in `docs/operations.md`.
- [ ] Document alerting rules in `docs/operations.md`.
- [ ] Document incident-response steps in `docs/operations.md`.
- [ ] Create `Dockerfile`.
- [ ] Create `.dockerignore`.
- [ ] Add a container build command to `docs/deployment.md`.
- [ ] Add a container run command to `docs/deployment.md`.
- [ ] Add a CI restore step to the roadmap.
- [ ] Add a CI build step to the roadmap.
- [ ] Add a CI test step to the roadmap.

## Sales and launch assets

- [ ] Create `docs/launch-plan.md`.
- [ ] Add a launch audience section to `docs/launch-plan.md`.
- [ ] Add a launch message section to `docs/launch-plan.md`.
- [ ] Add a demo script section to `docs/launch-plan.md`.
- [ ] Add a pricing hypothesis section to `docs/launch-plan.md`.
- [ ] Add a competitor comparison section to `docs/launch-plan.md`.
- [ ] Add a release checklist section to `docs/launch-plan.md`.
- [ ] Create `docs/demo-script.md`.
- [ ] Add a two-minute demo flow to `docs/demo-script.md`.
- [ ] Add a five-minute demo flow to `docs/demo-script.md`.
- [ ] Add a developer-focused demo flow to `docs/demo-script.md`.
- [ ] Add a buyer-focused demo flow to `docs/demo-script.md`.

## Release readiness

- [ ] Create `CHANGELOG.md`.
- [ ] Add an `Unreleased` section to `CHANGELOG.md`.
- [ ] Create `LICENSE`.
- [ ] Add support/contact information to `README.md`.
- [ ] Create `docs/privacy-policy-placeholder.md`.
- [ ] Create `docs/terms-of-use-placeholder.md`.
- [ ] Create `docs/release-checklist.md`.
- [ ] Add a `0.1.0 demo release` checklist to `docs/release-checklist.md`.
- [ ] Add a `0.2.0 developer preview` checklist to `docs/release-checklist.md`.
- [ ] Add a `1.0.0 production candidate` checklist to `docs/release-checklist.md`.
- [ ] Add a final `dotnet restore OpenAPI.sln` release gate to `docs/release-checklist.md`.
- [ ] Add a final `dotnet build OpenAPI.sln --no-restore` release gate to `docs/release-checklist.md`.
- [ ] Add a final `dotnet test OpenAPI.sln --no-build` release gate to `docs/release-checklist.md`.
