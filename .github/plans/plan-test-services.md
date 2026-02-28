# Plan: Test Services (Prompt-fill first)

Goal: Add unit + integration tests for core services starting with high-priority targets: OrderService, GeminiService, ProductService. Unit tests will mock repositories/clients with Moq; integration tests will reuse the existing SQLite `DatabaseFixture`.

Scope
- Unit tests: `OrderService`, `GeminiService`, `ProductService` (mock deps with Moq)
- Integration tests: end-to-end flows for `OrderService` and `ProductService` using `Tests/IntegretionTests/DatabaseFixture.cs`

Files to add
- `Tests/UnitTests/OrderServiceUnitTests.cs`
- `Tests/UnitTests/GeminiServiceUnitTests.cs`
- `Tests/UnitTests/ProductServiceUnitTests.cs`
- `Tests/IntegrationTests/OrderServiceIntegrationTests.cs`
- `Tests/IntegrationTests/GeminiEngineIntegrationTests.cs` (component/integration placeholder)
- `Tests/IntegrationTests/ProductServiceIntegrationTests.cs`

Approach
1. Create unit test skeletons with TODOs and representative inputs; mock dependencies with Moq.
2. Create small integration skeletons that reuse the existing `DatabaseFixture` collection fixture and seed minimal test data.
3. Iterate: implement high-value assertions for `OrderService.AddOrderFromCartAsync` and `GeminiService.AddGeminiForUserProductAsync` next.

Run & Verify
- Build: `dotnet build`
- Run unit tests: `dotnet test --no-build --filter Category!=Integration`
- Run integration tests: `dotnet test --no-build --filter Category=Integration`

Notes
- Gemini network calls will be mocked by testing `IGemini` usage in `GeminiService`.
- If you approve, I'll implement detailed unit tests next for the high-priority methods.
