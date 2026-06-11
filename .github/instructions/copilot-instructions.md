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
| Caching | **Redis** (`redis:7-alpine` via Docker) + `StackExchange.Redis` — cache-aside for Products & SubCategories |
| Messaging | **Apache Kafka** (KRaft mode, `confluentinc/cp-kafka:7.6.1` via Docker) + `Confluent.Kafka` — async order billing events |
| Authentication | **JWT** (access + refresh tokens) via `Microsoft.IdentityModel.JsonWebTokens`, HttpOnly cookies |
| Social Auth | Google OAuth (`Google.Apis.Auth`) + Microsoft OAuth (`Microsoft.Identity.Client`) |
| Password hashing | **BCrypt.Net-Next** (adaptive hashing, work-factor 11, auto-salting) |
| Password scoring | zxcvbn-core |
| Rate limiting | **ASP.NET Core built-in** (`Microsoft.AspNetCore.RateLimiting`) — sliding window, 100 req/min per IP |
| Mapping | AutoMapper 16 (single `Profile` in `Services/Mapper.cs`) |
| Logging | NLog (`nlog.config`) |
| AI integration | Google Gemini via `Google.GenAI` NuGet package |
| PDF generation | PDFsharp |
| API docs | Swagger via OpenAPI (Development only) |
| Testing | xUnit + Moq + Moq.EntityFrameworkCore; integration tests use in-memory SQLite |
| Containerization | **Docker Compose** — Redis, Kafka, Kafka UI, API |

## Solution Structure (7 projects)

```
WebApiShop.sln
├── WebApiShop/               → ASP.NET Core API host (Controllers, Middlewares, Program.cs, Dockerfile)
│   ├── Controllers/          → 15 controllers (Auth, Users, Products, Cart, Orders, etc.)
│   ├── Middlewares/          → ErrorMiddleware, JwtMiddleware, RateLimitMiddleware, RatingMiddleware
│   └── Prompts/              → BasicPrompt.md template for order prompt assembly
├── Services/                 → Business logic, AutoMapper, AI, JWT, Auth, caching, Kafka publisher
├── Repositories/             → EF Core data access, DbContext (MyShopContext)
├── Entities/                 → Auto-generated EF Core entity classes (DO NOT hand-edit)
├── DTO/                      → Data Transfer Objects (C# records)
├── BillingServiceConsumer/   → **Standalone .NET Worker Service** — Kafka consumer for order billing
│   ├── KafkaConsumerService  → BackgroundService consuming `orders` topic
│   ├── BillingService        → Bill processing logic
│   ├── KafkaHealthCheck      → /healthz probe for Kafka connectivity
│   ├── Bill / Invoice        → Billing domain entities
│   └── KafkaSettings         → Configuration model
└── Tests/                    → xUnit tests
    ├── UnitTests/            → Service & repository unit tests (Moq)
    └── IntegretionTests/     → SQLite integration tests
```

**Dependency flow:** `WebApiShop → Services → Repositories → DTO → Entities`

**BillingServiceConsumer** is an independent project — it shares `DTO/` for `OrderDetailsDTO` deserialization but runs as a separate process.

## Docker Infrastructure

The project uses Docker Compose (`docker-compose.yml`) with 4 containers:

| Container | Image | Purpose | Local Address |
|---|---|---|---|
| `redis` | `redis:7-alpine` | Product + SubCategory read cache (cache-aside pattern) | `localhost:6380` |
| `kafka` | `confluentinc/cp-kafka:7.6.1` | Order event message broker (KRaft, no Zookeeper) | `localhost:9093` |
| `kafka-ui` | `provectuslabs/kafka-ui:latest` | Kafka browser UI | `http://localhost:8090` |
| `web-api-shop` | Custom Dockerfile (ASP.NET 9) | API container (multi-stage build) | `http://localhost:8080` |

**Required for local dev:** `docker compose up -d` to start Redis + Kafka before running the API.

**Redis password:** Set via `REDIS_PASSWORD` env var in `.env` file (copy `.env.example`). Default: `dev-password-change-me`.

## Build & Run

```bash
# Restore and build the full solution
dotnet build WebApiShop.sln

# Start Docker infrastructure (Redis, Kafka, Kafka UI)
docker compose up -d

# Run the API (launches on http://localhost:5010, Swagger at /swagger)
dotnet run --project WebApiShop

# Run the Billing Worker (separate terminal)
dotnet run --project BillingServiceConsumer

# Run all tests
dotnet test Tests/Tests.csproj
```

**Prerequisites:** .NET 9 SDK, SQL Server instance, Docker Desktop. Update `ConnectionStrings:Home` in `WebApiShop/appsettings.Development.json` for your environment.

**Known connection strings:** `"Home"` (personal dev) and `"School"` (classroom) are both in `appsettings.Development.json`. The app uses `"Home"` (see `Program.cs`).

**Secret management:** JWT secret, API keys, and OAuth credentials are stored in .NET User Secrets. See `secrets.template.json` for the required structure.

## Authentication & Authorization (JWT)

### Token Flow
- **Login/Register** → `AuthService` issues access token (JWT, 15min) + refresh token (opaque, 7 days)
- **Token transport** → Both tokens set as `HttpOnly + Secure + SameSite` cookies via `AuthCookieService`
- **Refresh token cookie** scoped to `/api/auth/refresh` only
- **Refresh token storage** → SHA-256 hash stored in `User.RefreshToken` column, expiry in `User.RefreshTokenExpiry`

### Middleware Pipeline
`JwtMiddleware` reads `access_token` cookie → validates via `IJwtService.ValidateAccessTokenAsync()` → populates `HttpContext.User`.

ASP.NET `JwtBearerEvents.OnMessageReceived` also extracts from cookie for `[Authorize]` attribute support.

### Key Classes
| Class | Project | Role |
|---|---|---|
| `JwtService` | Services | Generate access/refresh tokens, validate access tokens |
| `JwtSettings` | Services | Config model (SecretKey, Issuer, Audience, TTLs) |
| `AuthService` | Services | Login, register, refresh, logout, social login orchestration |
| `AuthCookieService` | Services | Set/delete/read HttpOnly cookies |
| `AuthController` | WebApiShop | Auth endpoints (login, register, refresh, logout, me, social-login) |
| `JwtMiddleware` | WebApiShop | Per-request token validation middleware |

### Password Hashing (BCrypt)
```csharp
// Registration — hash with auto-salt
user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

// Login — timing-safe verification
bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
```
No separate `Salt` column — BCrypt embeds the salt inside the hash string.

## Rate Limiting

Implemented via `RateLimitMiddleware` using ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting`:

| Setting | Value |
|---|---|
| Algorithm | Sliding window |
| Limit | 100 requests per minute per IP |
| Segments | 6 (10-second windows) |
| Queue limit | 0 (no queueing — immediate rejection) |
| Rejection status | `429 Too Many Requests` |
| Response body | `{ "message": "...", "policy": "SpecificPolicy", "retryAfterSeconds": N }` |

Applied globally: `app.MapControllers().RequireRateLimiting(RateLimitMiddleware.PolicyName)`.

## Redis Caching

### Product Caching (`ProductCacheService`)
- **Pattern:** Cache-aside (read-through/write-through)
- **Single product:** Key `product:{id}`, TTL 10 minutes
- **Product lists:** Key `products:v{version}:{position}:{skip}:{desc}:{subCategoryIds}`, TTL 5 minutes
- **Invalidation:** Version counter `products:version` — bumped on any product write, invalidating all list cache keys

### SubCategory Caching (`SubCategoryCacheService`)
- **Pattern:** Cache-aside
- **SubCategory lists:** Key `subcategories:v{version}:{position}:{skip}:{desc}:{mainCategoryIds}`, TTL 5 minutes
- **Invalidation:** Version counter `subcategories:version` — bumped on any subcategory write

### Resilience
All Redis operations are wrapped in `try/catch (RedisException)`. On failure, the API **always falls back to the database** and logs a warning. The application never crashes due to a cache failure.

## Kafka Event-Driven Billing

### Producer (API side)
- `OrderEventPublisher` publishes `OrderDetailsDTO` to the `orders` Kafka topic on order creation
- Config: `Acks.All`, idempotent producer, `Flush()` on shutdown
- Startup probe: tests broker connectivity and logs result

### Consumer (BillingServiceConsumer)
- `KafkaConsumerService` (BackgroundService) consumes from `orders` topic
- **Manual offset commit** — only after successful processing
- **Per-message DI scope** — `IServiceScopeFactory.CreateAsyncScope()` per message
- **Retry ×3** with exponential back-off (2s, 4s, 6s)
- **Dead-letter topic** (`orders.dead-letter`) — poison messages forwarded with diagnostic headers:
  - `x-failed-topic`, `x-failed-partition`, `x-failed-offset`, `x-failure-reason`, `x-failed-at`
- **Health check** — `KafkaHealthCheck` verifies broker connectivity for `/healthz`

### Kafka Settings
```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9093",
    "Topic": "orders",
    "GroupId": "billing-service",
    "DeadLetterTopic": "orders.dead-letter"
  }
}
```

## Coding Conventions

### Naming

- **Enforced by `.editorconfig`:** Types → `PascalCase`, methods → `PascalCase`, parameters → `camelCase`, private fields → `_camelCase`.
- Controllers: `{PluralNoun}Controller` (e.g., `ProductsController`). Exception: `AuthController` (singular).
- Services: `{Entity}Service` / `I{Entity}Service`. Cache services: `{Entity}CacheService` / `I{Entity}CacheService`.
- Repositories: `{Entity}Repository` / `I{Entity}Repository`.
- DTOs: suffix `DTO`. Input DTOs prefixed with action: `AddXxxDTO`, `UpdateXxxDTO`. Response: `XxxDTO`, `XxxSummaryDTO`, `XxxDetailsDTO`. Auth: `LoginDTO`, `RegisterDTO`, `AuthResultDTO`, `AuthResponseDTO`.
- Async methods: end with `Async` suffix. `SuppressAsyncSuffixInActionNames = false` is set, so route names keep the suffix.

### Key Patterns

- **All I/O is `async/await`** — never use `.Result` or `.Wait()`.
- **DTOs are C# `record` types** — use positional records for simple inputs, property-init records for complex responses.
- **Validation attributes** (`[Required]`, `[Range]`, etc.) go on DTO record parameters.
- **Controller actions** return `Task<ActionResult<T>>` or `Task<ActionResult>` and use `Ok()`, `CreatedAtAction()`, `NoContent()`, `NotFound()`, `BadRequest()`, `Unauthorized()`.
- **Authorization**: Use `[Authorize]` for protected endpoints, `[AllowAnonymous]` for public endpoints (login, register, social-login).
- **Services** never expose entities to controllers — always map to/from DTOs via AutoMapper.
- **Repositories** work only with entities — never reference DTOs.
- **Cache services** sit between controllers/services and repositories — check cache first, fallback to DB, populate cache on miss.
- **DI registration** in `Program.cs`: `AddScoped` for all service/repository pairs. `AddSingleton` for `IConnectionMultiplexer` (Redis) and `IOrderEventPublisher` (Kafka producer).
- **AutoMapper** config is centralized in `Services/Mapper.cs` (extends `Profile`).
- **Middleware pipeline order:** `UseStaticFiles → UseErrorMiddleware → UseRatingMiddleware → UseHttpsRedirection → UseCors → UseRateLimiter → UseAuthentication → UseAuthorization → MapControllers.RequireRateLimiting(...)`.

### Entity Rules

Entities in `Entities/` are **auto-generated** by EF Core Power Tools. **Do not hand-edit** these files except for the `UserRole.cs` partial class (adds `Role`, `RefreshToken`, `RefreshTokenExpiry` properties). Schema changes must go through the database and be re-scaffolded. PKs are `long`. Navigation properties are `virtual`.

### Testing Conventions

- **Unit tests:** One test class per service/repository, using Moq. Pattern: `MethodName_Condition_ExpectedResult`. Use `[Fact]` attribute.
- **Integration tests:** Use `DatabaseFixture` (SQLite in-memory) via `[Collection("Database collection")]`. Seed data directly via context. Group tests with `#region Happy Paths` / `#region Unhappy Paths`.
- **TestBase helper:** `Tests/TestBase.cs` provides `GetMockContext<TContext, TEntity>()` for mocking DbSets.

## Important Quirks (Do Not "Fix" Without Asking)

- **Misspelled folder/file names** are established and referenced in `using`/`namespace` declarations. Keep them: `IntegretionTests/`, `ProductReposetory.cs`, `MainCategoryReposetory.cs`, `PlatformReposetory.cs`, `CartRepository .cs` (trailing space), `OrderAndReviewDTO .cs` (trailing space).
- **Two integration test folders:** `IntegretionTests/` has real tests; `IntegrationTests/` has stubs with TODOs.
- **`TestBase.cs`** uses `namespace Test` (singular) — this is intentional.
- **`SiteTypeRepository` / `SiteTypeService`** are registered as `AddTransient` while all others use `AddScoped`.
- **`RatingMiddleware.cs`** uses non-extension-method style class but is invoked via extension `UseRatingMiddleware()`.
- **Redis connection fallback**: If `Redis:ConnectionString` config is empty, falls back to `localhost:6380` with `abortConnect=false`.

## Adding a New Feature Checklist

1. **Entity** — If a new table: add to DB, re-scaffold with EF Core Power Tools, add `DbSet` to `MyShopContext`. For auth-related fields, use a partial class (see `UserRole.cs` pattern).
2. **DTO** — Create record(s) in `DTO/` project with validation attributes.
3. **Repository** — Create interface `I{Entity}Repository` and class `{Entity}Repository` in `Repositories/`. Inject `MyShopContext`. Return entities.
4. **Service** — Create interface `I{Entity}Service` and class `{Entity}Service` in `Services/`. Inject repository + `IMapper`. Return DTOs.
5. **Cache service** (if applicable) — Create `I{Entity}CacheService` and `{Entity}CacheService` in `Services/`. Inject `IConnectionMultiplexer`. Follow `ProductCacheService` patterns (version-based invalidation, `RedisException` catch, graceful fallback).
6. **Mapper** — Add `CreateMap<Entity, DTO>()` entries in `Services/Mapper.cs`.
7. **Controller** — Create `{PluralNoun}Controller` in `WebApiShop/Controllers/`. Inject service interface. Use `[Route("api/[controller]")]` and `[ApiController]`. Add `[Authorize]` for protected endpoints.
8. **DI** — Register interface→implementation pair as `AddScoped` in `Program.cs`.
9. **Tests** — Add unit tests in `Tests/UnitTests/` and integration tests in `Tests/IntegretionTests/`.
10. **Docker** (if new infrastructure) — Add container to `docker-compose.yml`, update `appsettings.json` with connection config.
