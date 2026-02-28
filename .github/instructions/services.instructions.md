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

## Validation and business-guard guidance

- Validate at service boundary before repository calls where possible.
- Keep validations explicit and easy to reason about.
- Prefer single, clear guard checks over deeply nested conditionals.
- Keep error messages actionable and consistent with existing project tone.

## Integration points checklist for service changes

When adding/changing service behavior, update all relevant pieces in one slice:

1. `I*Service` interface.
2. Concrete service implementation.
3. Any repository interface/implementation changes required for data access.
4. `Services/Mapper.cs` mappings.
5. DI registration in `WebApiShop/Program.cs` (for new types).
6. Unit tests and (when needed) integration tests.

## Testing expectations for service changes

- Add/adjust unit tests in `Tests/UnitTests` for business rules, validations, and orchestration paths.
- Add integration tests when behavior depends on DB-level effects, relational constraints, or query semantics.
- Cover negative paths (invalid inputs, missing records, forbidden operations) in addition to happy paths.

## Style and scope

- Keep changes minimal and focused on the requested behavior.
- Follow existing naming/style patterns in this repo, including current quirks.
- Avoid broad refactors unless explicitly requested.
