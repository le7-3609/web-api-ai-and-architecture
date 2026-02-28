# Prompt: Auto-generate and fix tests for every function (happy & unhappy paths)

Agent role
- Act as a focused coding agent: explore the repo, suggest and implement minimal, safe code changes, add or fix tests, and produce clear, buildable commits/PRs that follow the project's conventions and CI-friendly practices.

Goal
- Make the repository's full test suite pass (unit + integration).
- For every non-trivial public/internal function in the repository projects, generate at least:
  - One happy-path test (expected/normal inputs).
  - One unhappy-path test (invalid inputs, nulls, exceptions, boundary conditions).
- If tests fail due to missing or incorrect production code, implement minimal correct production changes rather than weakening tests.

Context
- Repo: ASP.NET Core 9 Web API (C#) with EF Core (database-first), AutoMapper, NLog.
- Tests use xUnit. Use dotnet CLI on Windows (PowerShell).
- Fast validation: iterate by building/running the WebApiShop project only; final validation must run `dotnet test` across solution.

Step-by-step instructions
1. Inspect repository
   - List projects and open key files: `README.md`, `.editorconfig`, `Program.cs`, `WebApiShop/Controllers/`, `Services/`, `Repositories/`, `Entities/`, `DTO/`, and `Tests/`.
   - Search for `TODO`, `HACK`, `FIXME`, `XXX` to note risky areas.

2. Baseline run
   - Restore/build/tests:
     ```
     dotnet restore
     dotnet build
     dotnet test --logger:"console;verbosity=detailed" > test-output.txt
     ```
   - Capture compile errors first, then failing tests.

3. Identify functions to test
   - Enumerate all public/internal methods in Services, Repositories, Controllers, and any domain helpers.
   - For each method, document expected behavior, edge cases, and failure modes (null args, empty lists, DB not found, exceptions, invalid DTOs).

4. Test design rules (apply to every function)
   - Happy path: normal inputs, verify expected return, side-effects, DB changes, and logging where relevant.
   - Unhappy paths: invalid args (null, empty), boundary values, DB exceptions (simulate via mocks), mapping errors, and authorization failures if applicable.
   - Use parameterized tests (Theory / InlineData) when multiple similar cases exist.
   - Prefer behavior-first tests: assert observable behavior/state, not implementation details.

5. Unit vs Integration guidance
   - Unit tests: mock external dependencies (Moq or existing framework). Add Moq if missing:
     ```
     dotnet add Tests\<UnitProject>.csproj package Moq
     ```
     Use EF Core InMemory for simple repository logic if appropriate:
     ```
     dotnet add Tests\<UnitProject>.csproj package Microsoft.EntityFrameworkCore.InMemory
     ```
   - Integration tests: use SQLite in-memory to emulate relational behaviors:
     ```
     dotnet add Tests\<IntegrationProject>.csproj package Microsoft.Data.Sqlite
     dotnet add Tests\<IntegrationProject>.csproj package Microsoft.EntityFrameworkCore.Sqlite
     ```
     Use WebApplicationFactory<TEntryPoint> and configure the test host to replace DB registration with SQLite in-memory.

6. Implement missing production code minimally
   - For failing compile/errors, implement small, correct code in production project (Services/Repositories/DTOs/Mappings).
   - Keep controllers thin; prefer adding service methods.
   - Register any new services in `Program.cs` with proper lifetimes.

7. Edge-case strategies
   - Null or invalid DTOs: tests expect ArgumentNullException or validation result; implement Guard clauses if project convention uses them.
   - NotFound: tests should assert returned NotFoundResult or service-specific exception handled by middleware.
   - Exceptions from lower layers: mock repository to throw and assert service/controller returns proper error handling (500 or domain-specific).
   - Concurrency/unique constraints: for integration tests seed DB and assert constraint behavior.

8. Iteration and validation
   - After each change:
     ```
     dotnet build
     dotnet test
     ```
   - Fix failures and repeat. Use one-project builds to speed iteration:
     ```
     dotnet build WebApiShop\WebApiShop.csproj
     dotnet test Tests\<Project>.csproj
     ```

9. Commit and PR
   - Create branch `tests/add-generated-tests`.
   - Commit incremental, focused changes with clear messages.
   - Push and open PR when `dotnet test` passes across the solution.

Helpful commands
- Build one project: `dotnet build WebApiShop\WebApiShop.csproj`
- Run web project locally: `dotnet run --project WebApiShop\WebApiShop.csproj`
- Run tests: `dotnet test`
- Add NuGet package: `dotnet add <PROJECT>.csproj package <PackageName>`

Safeguards
- Do not commit secrets or real production connection strings. Use in-memory/SQLite for tests.
- If full suite has flaky or external-resource tests, document and isolate them but ensure CI-friendly replacements for acceptance.
- Keep changes minimal and reversible; prefer adding tests that illustrate desired behavior before broad refactors.

Output expected
- All tests passing (`dotnet test` returns 0).
- Branch `tests/add-generated-tests` with production fixes and new tests.
- Short commit messages and PR ready for review.

End.