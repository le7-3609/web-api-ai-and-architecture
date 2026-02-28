# Copilot Instructions for `web-api-shop`

## What this app does
- Backend for an AI-driven prompt store that helps users design a website and generate high-quality technical prompts.
- Users choose site type, platform, categories, and products; the API composes/stores prompt-related data and supports cart/order flows.
- Main business domains: users/auth, catalog (main/sub categories + products), site templates, cart, order/review, Gemini prompt generation, and request logging/rating.

## Tech stack
- Language/runtime: C#, .NET 9 (`net9.0`)
- Solution: `WebApiShop.sln`
- API host: ASP.NET Core Web API (`WebApiShop` project)
- Data access: EF Core, database-first style (`MyShopContext` + generated entities)
- Mapping: AutoMapper (`Services/Mapper.cs`)
- Logging/monitoring: NLog + custom middleware (`ErrorMiddleware`, `RatingMiddleware`)
- AI integration: Gemini client/service in `Services/gemini.cs` + `Services/GeminiService.cs`
- Tests: xUnit + Moq + EF Core SQLite in-memory integration fixture

## Architecture and ownership boundaries
- Layering must stay strict:
  - Controllers: HTTP layer only (routes, status codes, request/response handling).
  - Services: business logic, validation, orchestration across repositories/services.
  - Repositories: EF queries and persistence only, no business policy.
- Data flow is typically: `Controller -> Service Interface -> Service -> Repository Interface -> Repository -> DbContext`.
- DTOs are contract layer and should shield API consumers from EF entities.
- Prefer adding behavior in service layer first; repository updates should be query/storage-focused and minimal.

## Project map (high-value orientation)
- `WebApiShop/`
  - `Program.cs`: DI registration, middleware pipeline, DbContext setup, CORS, OpenAPI.
  - `Controllers/`: API endpoints (Users, Carts, Products, Orders, Categories, Gemini, etc.).
  - `Middlewares/`: centralized error handling and request rating logging.
  - `WebApiShop.http`: quick request samples.
  - `appsettings*.json`, `nlog.config`: runtime config.
- `Services/`
  - Service interfaces + implementations.
  - `Mapper.cs`: AutoMapper profile for most DTO/entity mappings.
- `Repositories/`
  - Repository interfaces + EF implementations.
  - `MyShopContext.cs`: EF model configuration (database-first generated style).
- `Entities/`
  - Generated entity classes; treat as generated source unless absolutely required.
- `DTO/`
  - Request/response records/classes.
  - File naming is not always obvious (example: `CatalogDTO.cs` contains `ProductDTO`).
- `Tests/`
  - `UnitTests/` for services/repositories with mocks.
  - `IntegretionTests/` for data behavior using `DatabaseFixture`/SQLite in-memory context.

## Endpoint area map (where to look first)
- Auth/users: `UsersController`
- Password policy: `PasswordValidityController`
- Catalog browsing/admin-like operations: `ProductsController`, `MainCategoriesController`, `SubCategoriesController`, `SiteTypeController`, `PlatformsController`
- Site setup data: `BasicSiteController`
- Cart lifecycle: `CartsController`
- Orders/reviews: `OrdersController`
- Prompt generation and storage: `GeminiController`

## Build, run, and test (Windows/PowerShell-first)
- Preferred shell: **PowerShell** (avoid bashisms and Unix-only command syntax).
- Use repo root (`web-api-shop`) before running commands.
- Safe baseline commands:
  - Restore: `dotnet restore WebApiShop.sln`
  - Build all: `dotnet build WebApiShop.sln`
  - Run API: `dotnet run --project WebApiShop/WebApiShop.csproj`
  - Run all tests: `dotnet test Tests/Tests.csproj`
- Scoped tests: prefer filter expressions over file-only targeting in runners that miss tests.
  - Example: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~ProductServiceUnitTests"`
- Local URLs are in `WebApiShop/Properties/launchSettings.json` (`https://localhost:7072`, `http://localhost:5010`).
- For fast manual API checks, use Swagger (Development) or `WebApiShop/WebApiShop.http`.

## Coding conventions and implementation rules
- Keep existing naming/style patterns even where typos exist in filenames (`Reposetory`, `Integretion`); do not rename broadly unless asked.
- Follow async conventions consistently (`Task`, `await`, `*Async`).
- Add validation/business guards in services, not in controllers/repositories.
- Keep repository methods nullability-consistent with their interfaces.
- Reuse existing DTO records and DataAnnotations rather than introducing parallel contract types.
- Update AutoMapper mappings when DTO/entity shape changes.
- If adding a new service/repository pair, register both in `WebApiShop/Program.cs` DI.
- Make minimal, targeted edits; avoid drive-by refactors.

## Change playbooks (fast, reliable edits)

### Add new API capability
1. Add/adjust DTO contract in `DTO/`.
2. Update service interface and implementation in `Services/`.
3. Update repository interface/implementation in `Repositories/` if data access changes.
4. Add/adjust mapping in `Services/Mapper.cs`.
5. Wire DI in `WebApiShop/Program.cs` (if new types).
6. Expose endpoint in relevant controller with proper status codes.
7. Add or update unit tests first; then integration tests if DB behavior changed.

### Modify query/filter/paging behavior
1. Validate/normalize input in service layer.
2. Keep repository query composable (`AsQueryable`, filter/order/page on server side).
3. Preserve return contracts (tuple/DTO shape).
4. Add tests for edge cases (null filters, empty results, negative paging values, etc.).

### Add entity-backed field
1. Confirm if this is DB-first generated territory (`Entities`, `MyShopContext`).
2. Prefer regeneration/migration-safe approach; avoid manual edits to generated sections.
3. Update DTO + mapper + service/repository + tests in one pass.

## Testing strategy in this repo
- Unit tests commonly mock repositories and AutoMapper for service-level logic checks.
- Integration tests use `Tests/IntegretionTests/DatabaseFixture.cs` with SQLite in-memory and custom context overrides.
- If integration tests fail unexpectedly, inspect fixture setup before changing production code (schema creation, FK pragmas, default values).
- Validate narrow scope first, then run full test project.

## Configuration and secrets handling
- `WebApiShop/appsettings.Development.json` may contain machine-specific connection strings and local placeholders.
- `WebApiShop/nlog.config` includes environment-specific paths/email settings; do not treat as universally valid.
- Never commit new real secrets, credentials, or machine-locked absolute paths.
- Keep config additions environment-aware and backward-compatible where possible.

## Known pitfalls and time-savers
- `WebApiShop.csproj` includes an absolute `.editorconfig` include from another machine; avoid copying this pattern.
- Some DTO files/records are organized in non-obvious files; search symbols, not filenames only.
- Nullable warnings already exist across projects; avoid introducing additional warnings in touched code.
- Controller default query parameters may not align perfectly with nullability annotations; keep endpoint behavior stable unless explicitly changing API contract.

## Existing tools and references
- Business/architecture context: `README.md`
- Test history/intent notes: `Tests/Promt gen tests.txt`
- EF Power Tools configs: `Entities/efpt.config.json`, `Repositories/efpt.config.json`
- Naming style hints: `WebApiShop/.editorconfig`
- Local VS Code note: `.vscode/settings.json`

## Practical agent workflow for this repository
1. Identify target layer and matching interfaces before coding.
2. Implement smallest end-to-end slice (DTO/service/repository/controller as needed).
3. Run focused test(s), then broader test/build validation.
4. If a failure occurs, fix root cause in changed area first; avoid unrelated cleanup.
5. Report touched files, behavior change, and any remaining warnings clearly.
