---
applyTo: "WebApiShop/Controllers/**/*.cs"
---

Controller-layer guidance for this solution:

## Role in architecture

- Controllers are the HTTP boundary only.
- Required flow remains: `Controller -> Service Interface -> Service -> Repository`.
- Controllers should delegate business behavior to services and avoid embedding domain rules.

## Functional responsibilities (what belongs here)

- Define routes, verbs, and action signatures.
- Accept request DTOs and query/path parameters.
- Perform lightweight request-shape checks only (for example: null body, obvious invalid route values when not already constrained).
- Call service methods and return appropriate HTTP responses/status codes.
- Keep responses aligned with existing API contracts and DTOs.

## What must stay out of controllers

- Business policy and workflow orchestration.
- Direct EF Core or `DbContext` usage.
- Repository calls from controllers.
- Complex mapping logic that belongs to services/AutoMapper.

## Status code and response guidance

- Keep status code behavior stable unless explicitly asked to change API contract.
- Use clear action result patterns already present in the project.
- Return meaningful responses for common outcomes (success, not found, bad request, conflict when applicable by current conventions).

## Return object guidance (action return types)

- Prefer typed results for data-returning endpoints:
	- `Task<ActionResult<TDto>>` for single objects,
	- `Task<ActionResult<IEnumerable<TDto>>>` for collections,
	- `Task<ActionResult<PaginatedResponse<TDto>>>` for paged endpoints.
- For status-only endpoints (update/delete without body), use `Task<ActionResult>`.
- Keep response body types consistent with current endpoint contract; do not swap DTO type or wrapper type unless intentionally changing API behavior.
- On create operations, return the created DTO/object with `CreatedAtAction(...)` and route values to the retrieval endpoint.
- For validation/business failures that intentionally return a message/object, keep message shape consistent with existing controller conventions.

## HTTP status mapping used in this project

- `200 OK`: successful read/update operation returning data or simple success.
- `201 Created`: successful create with response body and location (`CreatedAtAction`).
- `204 NoContent`: successful operation with no body, or empty result where current endpoint already follows this behavior.
- `400 BadRequest`: invalid input, mismatched ids, invalid request payload, or service-declared invalid operation.
- `401 Unauthorized`: authentication/login/token failure scenarios.
- `404 NotFound`: requested resource does not exist.
- `409 Conflict`: duplicate/resource state conflict (for example, item already exists).

## CRUD status expectations

### Create (`POST`)

- Success: `201 Created` + created object (`CreatedAtAction(...)`).
- Invalid payload/business validation failure: `400 BadRequest`.
- Duplicate/conflicting resource state: `409 Conflict`.
- Auth failure (when endpoint is protected): `401 Unauthorized`.

### Read (`GET`)

- Success with data: `200 OK` + DTO/list/paginated object.
- Missing single resource: `404 NotFound` (or keep existing endpoint behavior if already `204`).
- Empty list/query result: keep current controller convention (`204 NoContent` or `404 NotFound`).
- Invalid query/path input: `400 BadRequest`.
- Auth failure (when endpoint is protected): `401 Unauthorized`.

### Update (`PUT` / `PATCH`)

- Success with no response body: `204 NoContent`.
- Success with response body (existing endpoints that return updated DTO): `200 OK`.
- Route/body id mismatch or invalid payload: `400 BadRequest`.
- Target resource not found: `404 NotFound`.
- Business conflict (for unique constraints/state conflict): `409 Conflict`.
- Auth failure (when endpoint is protected): `401 Unauthorized`.

### Delete (`DELETE`)

- Success with no response body: `204 NoContent`.
- Success with explicit confirmation body (only where current endpoint already does this): `200 OK`.
- Invalid id/request: `400 BadRequest`.
- Target resource not found: `404 NotFound`.
- Auth failure (when endpoint is protected): `401 Unauthorized`.

## Status consistency rules

- Preserve each endpoint’s existing status semantics unless a contract change is explicitly requested.
- Do not return `200` with `null` body for missing resources when endpoint currently uses `404` or `204`.
- For list endpoints, keep the existing behavior (`204` or `404` for empty lists) per controller convention instead of introducing a new pattern ad hoc.
- If service throws domain exceptions already mapped by middleware, do not duplicate broad exception translation in controller actions.

## Validation and error handling

- Prefer service-layer validation and business guards.
- Let centralized middleware handle exception formatting and logging.
- Avoid catch-all handling in controllers unless needed for endpoint-specific behavior.

## Contract consistency checklist for controller changes

When changing controller behavior, verify the full slice:

1. Request/response DTOs in `DTO/` remain aligned.
2. Service interface (`I*Service`) and implementation support the action.
3. AutoMapper profile (`Services/Mapper.cs`) is updated if DTO/entity shape changed.
4. DI registrations in `WebApiShop/Program.cs` are present for new services.
5. Relevant tests are added/updated.

## Style and scope

- Keep actions focused and small.
- Follow existing naming/style patterns in this repo.
- Keep edits minimal and avoid unrelated refactors.
