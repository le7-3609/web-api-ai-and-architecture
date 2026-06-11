---
applyTo: "Repositories/**/*.cs"
---

Repository-layer guidance for this solution:

## Role in architecture

- Repositories are **data-access only**. They are the only layer that should speak directly to `MyShopContext` and EF Core entities for persistence/querying.
- Required flow remains: `Controller -> Service -> Repository -> DbContext`.
- Repository code must not contain business policy decisions (pricing policy, ownership checks, workflow rules, status transitions, etc.).

## Functional responsibilities (what belongs here)

- Build EF Core queries (including `Include`, `ThenInclude`, joins/projections when needed).
- Execute CRUD operations and save changes.
- Return data in the shapes expected by services/interfaces (entities, tuples, primitive flags, paging tuples).
- Keep query logic composable and efficient (`AsQueryable()`, server-side filtering/sorting/paging).

## What must stay out of repositories

- Request validation and input normalization that belongs to service/API contracts.
- DTO mapping decisions (should remain in service layer via AutoMapper, unless repository intentionally returns a projection contract already used in the codebase).
- HTTP semantics (status codes, IActionResult concerns).
- Cross-aggregate orchestration and business workflows.
- Redis caching logic — caching is handled by cache services (`ProductCacheService`, `SubCategoryCacheService`) in the `Services/` layer.
- Kafka event publishing — handled by `OrderEventPublisher` in the `Services/` layer.
- Password hashing/verification — handled by `AuthService` using BCrypt in the `Services/` layer.

## Authentication-related repository methods

The `UserRepository` includes auth-specific data access methods:

- **`GetByEmailForAuthAsync(string email)`** — Returns the full `User` entity including password hash for login verification. Used only by `AuthService.LoginAsync()`.
- **`GetByEmailAsync(string email, long excludeId)`** — Checks for duplicate emails during registration, excluding a specific user ID.
- **`GetByRefreshTokenAsync(string tokenHash)`** — Finds a user by their SHA-256 hashed refresh token for token refresh flow.
- **`SaveRefreshTokenAsync(long userId, string? tokenHash, DateTime? expiry)`** — Updates the user's refresh token hash and expiry. Called on login/register (set) and logout (clear to `null`).
- **`RegisterAsync(User user)`** — Creates a new user and returns the created entity with its generated ID.

### Important auth data rules
- The `Password` field is a BCrypt hash — repositories **never** perform password comparison. They fetch the user by email only; comparison happens in `AuthService`.
- The `RefreshToken` field stores a SHA-256 hash of the raw token, not the raw token itself.
- The `RefreshTokenExpiry` field is checked by `AuthService` after retrieval — repositories just return the entity.
- The `Role` field (from `UserRole.cs` partial class) defaults to `"User"` and is set during registration.

## Implementation rules for this repo

- Keep interface/implementation aligned:
  - if method signature changes in `I*Repository`, update implementation in the same pass,
  - maintain nullability consistency between interface and concrete class.
- Prefer async EF APIs (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `SaveChangesAsync`).
- Preserve existing naming/style patterns in this repository (including known typos like `Reposetory` in file names).
- Keep changes minimal and focused; do not refactor unrelated repository methods.

## Query and performance guidance

- Apply filters before materialization.
- Use stable ordering before paging to avoid inconsistent pages.
- Avoid premature `ToList()`; compose query first and execute at the end.
- Only eager-load required navigation properties.
- Prefer `AnyAsync()` for existence checks over fetching full entities.
- For cached entities (Products, SubCategories): the cache check happens in the service/cache-service layer **before** calling the repository. Repositories are unaware of caching.

## Cross-domain data access patterns

Some repositories currently access data from multiple domain tables in a single operation. These patterns exist and should be preserved:

- **`ProductRepository`** — On product deletion, also deletes related `CartItem` entries and checks for `OrderItem` references. This cross-domain write is a known coupling.
- **`PlatformRepository`** — `ReassignPlatformReferencesAsync()` updates `CartItems`, `OrderItems`, and `BasicSites` when a platform is deleted and references need to be reassigned.
- **`CartRepository`** — References `Product`, `Platform`, and `BasicSite` entities via navigation properties for cart item operations.

These cross-domain patterns are candidates for future microservice decomposition but should be kept as-is in the current monolith.

## Error and null handling

- Return `null`/empty sets according to current contract behavior; do not invent new error semantics in repository layer.
- Let exceptions bubble unless there is a clear repository-level reason to translate EF exceptions into known data-access exceptions already used in the codebase.

## Database-first boundaries

- Treat `MyShopContext` and generated entity mapping as database-first generated territory.
- Avoid broad manual edits to generated configurations/entities unless explicitly requested.
- The one exception is `UserRole.cs` — a **partial class** that extends `User` with `Role`, `RefreshToken`, and `RefreshTokenExpiry` properties. This is safe because it's a partial class, not an edit to the generated file.
- If entity shape changes are needed, follow regeneration-safe patterns and update dependent repository queries minimally.

## MyShopContext notes

- `MyShopContext` is the single `DbContext` for the entire application.
- Contains `DbSet<T>` for all entities: `User`, `Product`, `MainCategory`, `SubCategory`, `Platform`, `SiteType`, `BasicSite`, `Cart`, `CartItem`, `Order`, `OrderItem`, `Review`, `Status`, `Rating`, `GeminiPrompt`.
- Extension methods in `MyShopContextExtensions.cs` provide query helpers.
- Registered as `AddDbContext<MyShopContext>` with SQL Server provider in `Program.cs`.

## Testing expectations for repository changes

- Update/add unit tests when repository behavior changes in ways currently unit-tested.
- Add/update integration tests in `Tests/IntegretionTests` when data behavior, relational constraints, includes, or query semantics change.
- Prioritize edge cases: empty results, null lookups, paging boundaries, and filter combinations.
- For auth-related queries: test email lookup, refresh token lookup, and token save/clear operations.
