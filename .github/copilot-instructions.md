# Copilot Instructions — WebApiShop

## What This App Does

An **ASP.NET Core 9 Web API** backend for an "AI-Driven Website Builder Prompt Store." Users browse website component products, build a cart, place orders, and receive AI-generated prompts (via Google Gemini) that describe how to build their chosen website. The API serves an Angular 19+ SPA frontend.

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 (C# with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`) |
| Web framework | ASP.NET Core Web API (REST, attribute routing) |
| ORM | Entity Framework Core 9 — **Database-First** via EF Core Power Tools |
| Database | SQL Server (connection string name: `"Home"` in `appsettings.Development.json`) |
| Mapping | AutoMapper 16 (single `Profile` in `Services/Mapper.cs`) |
| Logging | NLog (`nlog.config`) |
| AI integration | Google Gemini via `Google.GenAI` NuGet package |
| PDF generation | PDFsharp |
| Password scoring | zxcvbn-core |
| Auth | Google & Microsoft OAuth (`Google.Apis.Auth`, `Microsoft.Identity.Client`) |
| API docs | Swagger via Swashbuckle (Development only) |
| Testing | xUnit + Moq + Moq.EntityFrameworkCore; integration tests use in-memory SQLite |

## Solution Structure (6 projects)

```
WebApiShop.sln
├── WebApiShop/          → ASP.NET Core API host (Controllers, Middlewares, Program.cs)
├── Services/            → Business logic, AutoMapper profile, AI integration
├── Repositories/        → EF Core data access, DbContext (MyShopContext)
├── Entities/            → Auto-generated EF Core entity classes (DO NOT hand-edit)
├── DTO/                 → Data Transfer Objects (C# records)
└── Tests/               → xUnit tests
    ├── UnitTests/       → Service & repository unit tests (Moq)
    └── IntegretionTests/→ SQLite integration tests 
```

**Dependency flow:** `WebApiShop → Services → Repositories → DTO → Entities`

## Build & Run

```bash
# Restore and build the full solution
dotnet build WebApiShop.sln

# Run the API (launches on http://localhost:5010, Swagger at /swagger)
dotnet run --project WebApiShop

# Run all tests
dotnet test Tests/Tests.csproj
```

**Prerequisites:** .NET 9 SDK, SQL Server instance. Update `ConnectionStrings:Home` in `WebApiShop/appsettings.Development.json` for your environment.

**Known connection strings:** `"Home"` (personal dev) and `"School"` (classroom) are both in `appsettings.Development.json`. The app uses `"Home"` (see `Program.cs`).

## Coding Conventions

### Naming

- **Enforced by `.editorconfig`:** Types → `PascalCase`, methods → `PascalCase`, parameters → `camelCase`, private fields → `_camelCase`.
- Controllers: `{PluralNoun}Controller` (e.g., `ProductsController`).
- Services: `{Entity}Service` / `I{Entity}Service`.
- Repositories: `{Entity}Repository` / `I{Entity}Repository`.
- DTOs: suffix `DTO`. Input DTOs prefixed with action: `AddXxxDTO`, `UpdateXxxDTO`. Response: `XxxDTO`, `XxxSummaryDTO`, `XxxDetailsDTO`.
- Async methods: end with `Async` suffix. `SuppressAsyncSuffixInActionNames = false` is set, so route names keep the suffix.

### Key Patterns

- **All I/O is `async/await`** — never use `.Result` or `.Wait()`.
- **DTOs are C# `record` types** — use positional records for simple inputs, property-init records for complex responses.
- **Validation attributes** (`[Required]`, `[Range]`, etc.) go on DTO record parameters.
- **Controller actions** return `Task<ActionResult<T>>` or `Task<ActionResult>` and use `Ok()`, `CreatedAtAction()`, `NoContent()`, `NotFound()`, `BadRequest()`.
- **Services** never expose entities to controllers — always map to/from DTOs via AutoMapper.
- **Repositories** work only with entities — never reference DTOs.
- **DI registration** in `Program.cs`: `AddScoped` for all service/repository pairs.
- **AutoMapper** config is centralized in `Services/Mapper.cs` (extends `Profile`).
- **Middleware pipeline order:** `UseStaticFiles → UseErrorMiddleware → UseRatingMiddleware → UseHttpsRedirection → UseCors → UseAuthentication → UseAuthorization → MapControllers`.

### Entity Rules

Entities in `Entities/` are **auto-generated** by EF Core Power Tools. **Do not hand-edit** these files. Schema changes must go through the database and be re-scaffolded. PKs are `long`. Navigation properties are `virtual`.

### Testing Conventions

- **Unit tests:** One test class per service/repository, using Moq. Pattern: `MethodName_Condition_ExpectedResult`. Use `[Fact]` attribute.
- **Integration tests:** Use `DatabaseFixture` (SQLite in-memory) via `[Collection("Database collection")]`. Seed data directly via context. Group tests with `#region Happy Paths` / `#region Unhappy Paths`.
- **TestBase helper:** `Tests/TestBase.cs` provides `GetMockContext<TContext, TEntity>()` for mocking DbSets.

## Important Quirks (Do Not "Fix" Without Asking)

- **Misspelled folder/file names** are established and referenced in `using`/`namespace` declarations. Keep them: `IntegretionTests/`, `ProductReposetory.cs`, `MainCategoryReposetory.cs`, `PlatformReposetory.cs`, `CartRepository .cs` (trailing space), `OrderAndReviewDTO .cs` (trailing space).
- **Two integration test folders:** `IntegretionTests/` has real tests; `IntegrationTests/` has stubs with TODOs.
- **`TestBase.cs`** uses `namespace Test` (singular) — this is intentional.
- **`SiteTypeRepository` / `SiteTypeService`** are registered as `AddTransient` while all others use `AddScoped`.

## Adding a New Feature Checklist

1. **Entity** — If a new table: add to DB, re-scaffold with EF Core Power Tools, add `DbSet` to `MyShopContext`.
2. **DTO** — Create record(s) in `DTO/` project with validation attributes.
3. **Repository** — Create interface `I{Entity}Repository` and class `{Entity}Repository` in `Repositories/`. Inject `MyShopContext`. Return entities.
4. **Service** — Create interface `I{Entity}Service` and class `{Entity}Service` in `Services/`. Inject repository + `IMapper`. Return DTOs.
5. **Mapper** — Add `CreateMap<Entity, DTO>()` entries in `Services/Mapper.cs`.
6. **Controller** — Create `{PluralNoun}Controller` in `WebApiShop/Controllers/`. Inject service interface. Use `[Route("api/[controller]")]` and `[ApiController]`.
7. **DI** — Register interface→implementation pair as `AddScoped` in `Program.cs`.
8. **Tests** — Add unit tests in `Tests/UnitTests/` and integration tests in `Tests/IntegretionTests/`.
