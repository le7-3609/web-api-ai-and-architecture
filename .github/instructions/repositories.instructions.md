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

## Error and null handling

- Return `null`/empty sets according to current contract behavior; do not invent new error semantics in repository layer.
- Let exceptions bubble unless there is a clear repository-level reason to translate EF exceptions into known data-access exceptions already used in the codebase.

## Database-first boundaries

- Treat `MyShopContext` and generated entity mapping as database-first generated territory.
- Avoid broad manual edits to generated configurations/entities unless explicitly requested.
- If entity shape changes are needed, follow regeneration-safe patterns and update dependent repository queries minimally.

## Testing expectations for repository changes

- Update/add unit tests when repository behavior changes in ways currently unit-tested.
- Add/update integration tests in `Tests/IntegretionTests` when data behavior, relational constraints, includes, or query semantics change.
- Prioritize edge cases: empty results, null lookups, paging boundaries, and filter combinations.
