---
applyTo: "Services/**/*.cs"
---

Service-layer guidance for this solution:

## Role in architecture

- Services are the **business-orchestration layer** between controllers and repositories.
- Required flow remains: `Controller -> Service Interface -> Service -> Repository Interface -> Repository`.
- Services define business behavior, while controllers remain HTTP-only and repositories remain persistence-only.

## Functional responsibilities (what belongs here)

- Validate and normalize inputs (IDs, paging params, optional filters, required payload rules).
- Enforce business rules (state transitions, ownership checks, allowed combinations, invariants).
- Coordinate one or more repositories for a single use-case.
- Map Entities <-> DTOs using AutoMapper (`Services/Mapper.cs`).
- Return contract-safe outputs expected by controllers and existing API behavior.

## What must stay out of services

- Raw EF query composition that belongs in repositories.
- HTTP concerns (`IActionResult`, response code decisions, model binding concerns).
- UI-facing formatting or presentation-only transformations not tied to business logic.
- Direct `DbContext` access — always go through repository interfaces.

## Service categories in this project

### Standard business services
- `UserService`, `ProductService`, `CartService`, `OrderService`, `MainCategoryService`, `SubCategoryService`, `PlatformService`, `SiteTypeService`, `BasicSiteService`, `StatusService`, `ReviewService`, `RatingService`
- Follow the standard pattern: inject `I*Repository` + `IMapper`, return DTOs.

### Authentication & JWT services
- **`AuthService`** — Orchestrates login, register, refresh, logout, social login. Issues JWT tokens via `IJwtService`, stores hashed refresh tokens via `IUserRepository`, checks password strength via `IPasswordValidityService`.
- **`JwtService`** — Generates access tokens (HMAC-SHA256, claims: userId, email, role, jti), generates opaque refresh tokens, validates access tokens. Config via `JwtSettings` (SecretKey, Issuer, Audience, AccessTokenExpiryMinutes, RefreshTokenExpiryDays).
- **`AuthCookieService`** — Sets/deletes/reads HttpOnly + Secure + SameSite cookies. Access token cookie: path `/`, TTL matches token. Refresh token cookie: path `/api/auth/refresh`, TTL 7 days. Secure flag and SameSite policy adapt based on `localhost` detection.
- **`PasswordValidityService`** — Password strength scoring via `zxcvbn-core`. Returns strength level (0-4).

### Password hashing conventions
- Use `BCrypt.Net.BCrypt.HashPassword(password)` for hashing (work-factor 11, auto-salt).
- Use `BCrypt.Net.BCrypt.Verify(password, hash)` for timing-safe verification.
- No separate salt column — BCrypt embeds the salt in the hash string.
- Hash comparison happens in the service layer (`AuthService`), not in repository SQL queries.
- The `UserProfileDTO` (response record) never includes the `Password` field.

### Refresh token security
- Raw refresh tokens are **never stored**. Only SHA-256 hashes are persisted: `SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))`.
- Tokens are rotated on every refresh — old hash is replaced with new hash.
- On logout, refresh token hash and expiry are set to `null`.

### Redis caching services
- **`ProductCacheService`** — Cache-aside pattern for products. Inject `IConnectionMultiplexer`, get `IDatabase`. Keys: `product:{id}` (single, TTL 10min), `products:v{version}:...` (lists, TTL 5min). Version counter `products:version` bumped on any product write to invalidate all list caches.
- **`SubCategoryCacheService`** — Cache-aside pattern for subcategories. Keys: `subcategories:v{version}:...` (lists, TTL 5min). Version counter `subcategories:version` bumped on subcategory writes.

#### Caching rules
- All Redis operations **must** be wrapped in `try/catch (RedisException)`. On failure, log a warning and **fall back to the database** — the app must never crash due to cache failures.
- Cache services are injected into business services (e.g., `ProductService` uses `IProductCacheService`). The service checks cache first, calls repository on miss, and populates cache before returning.
- On write operations (create/update/delete), invalidate relevant cache entries:
  - Single entity: `KeyDeleteAsync(key)`.
  - All lists: `StringIncrementAsync(versionKey)` to bump the version counter.
- Cache key format for lists: `{entity}:v{version}:{position}:{skip}:{desc}:{filterIds}`.

### Kafka event publishing
- **`OrderEventPublisher`** — Publishes `OrderDetailsDTO` to the `orders` Kafka topic when an order is created. Registered as `AddSingleton`. Config via `KafkaSettings` (`BootstrapServers`, `Topic`).
- Startup probe: tests Kafka broker connectivity at construction time and logs the result.
- Uses `Acks.All` and idempotent producer for guaranteed delivery.
- `Flush()` on `Dispose()` to drain in-flight messages on shutdown.
- Called from `OrderService.AddOrderFromCartAsync()` after the order is persisted to the database.

### AI / Gemini services
- **`GeminiService`** — Generates AI prompts for products, subcategories, and basic sites using Google Gemini API. Injects `ISubCategoryRepository` and `IMainCategoryRepository` for context.
- **`GeminiChatService`** — Multi-turn chatbot with conversation history (in-memory). Manages chat sessions with system instructions.
- **`ChatBotService`** — Simplified chat interface wrapper.
- **`Gemini`** — Low-level Gemini API client. Config via `GeminiSettings` (ApiKey from User Secrets or env var).

### Prompt builder
- **`OrderPromptBuilder`** — Assembles the final order prompt from a Markdown template (`WebApiShop/Prompts/BasicPrompt.md`) + product data + site configuration. Called during order creation.

## Contract and compatibility rules

- Preserve existing API/service contracts whenever possible:
  - keep DTO shapes stable unless change is intentional and coordinated,
  - keep `I*Service` signatures aligned with implementations,
  - keep behavior backward-compatible unless explicitly asked to change.
- If DTO/entity shape changes:
  - update AutoMapper profile in `Services/Mapper.cs`,
  - update any affected service methods and tests in the same pass.

## Async and reliability expectations

- Follow existing async conventions consistently (`Task`, `await`, `*Async`).
- Avoid sync-over-async and blocking calls.
- Throw meaningful, deterministic exceptions for invalid business operations.
- Do not swallow unexpected exceptions silently; let middleware handle standardized error responses.
- For Kafka publishing: catch `ProduceException` and rethrow after logging — order creation should fail if the event can't be published.
- For Redis: catch `RedisException` and continue with DB fallback — never let cache failures break business operations.

## Validation and business-guard guidance

- Validate at service boundary before repository calls where possible.
- Keep validations explicit and easy to reason about.
- Prefer single, clear guard checks over deeply nested conditionals.
- Keep error messages actionable and consistent with existing project tone.

## DI registration patterns

| Service Type | DI Lifetime | Example |
|---|---|---|
| Standard business services | `AddScoped` | `IProductService` → `ProductService` |
| Cache services | `AddScoped` | `IProductCacheService` → `ProductCacheService` |
| Auth/JWT services | `AddScoped` | `IAuthService` → `AuthService`, `IJwtService` → `JwtService` |
| Redis connection | `AddSingleton` | `IConnectionMultiplexer` → `ConnectionMultiplexer` |
| Kafka producer | `AddSingleton` | `IOrderEventPublisher` → `OrderEventPublisher` |
| Configuration | `Configure<T>` | `JwtSettings`, `KafkaSettings`, `GeminiSettings` |

## Integration points checklist for service changes

When adding/changing service behavior, update all relevant pieces in one slice:

1. `I*Service` interface.
2. Concrete service implementation.
3. Any repository interface/implementation changes required for data access.
4. `Services/Mapper.cs` mappings.
5. DI registration in `WebApiShop/Program.cs` (for new types).
6. Cache invalidation (if the entity is cached — currently Products and SubCategories).
7. Kafka event publishing (if the operation triggers async workflows — currently order creation).
8. Unit tests and (when needed) integration tests.

## Testing expectations for service changes

- Add/adjust unit tests in `Tests/UnitTests` for business rules, validations, and orchestration paths.
- Add integration tests when behavior depends on DB-level effects, relational constraints, or query semantics.
- Cover negative paths (invalid inputs, missing records, forbidden operations) in addition to happy paths.
- For auth services: test token generation, refresh rotation, password hashing, and social login flows.
- For cache services: test cache hit/miss paths, invalidation, and Redis failure fallback.

## Style and scope

- Keep changes minimal and focused on the requested behavior.
- Follow existing naming/style patterns in this repo, including current quirks.
- Avoid broad refactors unless explicitly requested.
