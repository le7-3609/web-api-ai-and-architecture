# ASP.NET Core Test Suite Execution & Fix Plan

## Current State
- ✅ Build succeeds (0 errors, 18 warnings)
- ✅ 113+ tests created across 11 repositories
- ⏳ **Tests not yet executed** — blocking issues identified
- ⏳ No coverage data yet

---

## Phase 1: Diagnose Root Causes (Execute & Analyze)

### Step 1.1: Run Full Test Suite
```bash
dotnet test --collect:"XPlat Code Coverage" 2>&1 | Tee-Object -FilePath test-results.txt
```
**Goal:** Identify all failing tests and categorize by root cause

**Expected Issues:**
1. InMemory EF async failures (IAsyncQueryProvider) — Unit tests
2. SQLite getdate() failures — Integration tests  
3. Test logic/assertion bugs — Both

### Step 1.2: Parse Test Output
Extract:
- Total tests run
- Passed count
- Failed count
- Failures by file/test
- Error types (async, SQL, assertion, FK constraint, etc.)

---

## Phase 2: Fix InMemory EF Async Issues

### Problem
Unit tests use `UseInMemoryDatabase()` which doesn't support `async` LINQ queries.
```csharp
// ❌ Fails on InMemory
var count = await context.SubCategories.CountAsync();  // IAsyncQueryProvider missing
```

### Solution A: Switch Unit Tests to SQLite (Recommended)
Replace all `UseInMemoryDatabase(dbName)` with `UseSqlite(":memory:")` in:
- CartRepositoryUnitTests.cs
- PlatformRepositoryUnitTests.cs
- MainCategoryRepositoryUnitTests.cs
- BasicSiteRepositoryUnitTests.cs
- SiteTypeRepositoryUnitTests.cs
- GeminiPromptsRepositoryUnitTests.cs
- RatingRepositoryUnitTests.cs
- ProductReposetoryUnitTests.cs (if exists)
- UserRepositoryUnitTests.cs (if exists)

**Change Template:**
```csharp
// Before
var options = new DbContextOptionsBuilder<MyShopContext>()
    .UseInMemoryDatabase(dbName)
    .Options;

// After
var connection = new SqliteConnection(":memory:");
connection.Open();
var options = new DbContextOptionsBuilder<MyShopContext>()
    .UseSqlite(connection)
    .Options;
```

**Files to Modify:**
```
Tests/UnitTests/
├── CartRepositoryUnitTests.cs
├── PlatformRepositoryUnitTests.cs
├── MainCategoryRepositoryUnitTests.cs
├── BasicSiteRepositoryUnitTests.cs
├── SiteTypeRepositoryUnitTests.cs
├── GeminiPromptsRepositoryUnitTests.cs
└── RatingRepositoryUnitTests.cs
```

### Solution B: Use Moq (Alternative)
If Solution A fails, mock DbSet<T> directly:
```csharp
var mockDbSet = new Mock<DbSet<SubCategory>>();
var mockContext = new Mock<MyShopContext>();
mockContext.Setup(c => c.SubCategories).Returns(mockDbSet.Object);
var repository = new SubCategoryRepository(mockContext.Object);
```

---

## Phase 3: Fix SQLite getdate() Issue

### Problem
EF model defines: `entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())")`
SQLite doesn't have `getdate()` function.

### Current Mitigation
DatabaseFixture.cs has `TestMyShopContext` that overrides with NULL:
```csharp
modelBuilder.Entity<Entities.Order>(entity =>
{
    entity.Property(e => e.OrderDate).HasDefaultValueSql(null);
});
```

Plus manual schema creation with proper column types.

### Validation Steps
1. Verify `TestMyShopContext` is being used (check fixture initialization)
2. If still failing: Check if schema creation SQL has `OrderDate TEXT DEFAULT CURRENT_TIMESTAMP`
3. Test Order insertion without OrderDate value: should use DB default

### Fallback Solutions
If getdate() persists:

**Option 1:** Manually set OrderDate in tests
```csharp
var order = new Order { 
    OrderId = 1, 
    UserId = 1, 
    OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),  // ✅ Set explicitly
    Status = 1 
};
```

**Option 2:** Disable default value in test context completely
```csharp
// In TestMyShopContext.OnModelCreating
modelBuilder.Entity<Order>(entity =>
{
    entity.Property(e => e.OrderDate).HasDefaultValue(null);  // No SQL function
});
```

---

## Phase 4: Fix Test Logic & Assertion Bugs

### CartRepositoryUnitTests.ClearCartItemsAsync_WithNoItems_ReturnsFalse

**Issue:**
```csharp
[Fact]
public async Task ClearCartItemsAsync_WithNoItems_ReturnsFalse()
{
    // ...
    var result = await _repository.ClearCartItemsAsync(nonexistentCartId);
    Assert.False(result);  // ❌ Expects False but gets True
}
```

**Fix:** Check repository logic
```csharp
// What does ClearCartItemsAsync actually return?
// If it returns success (True) when clearing empty set, fix assertion:
Assert.True(result);  // ✅ Correct for "clearing empty cart succeeded"
```

OR check if method should return False for non-existent cart:
```csharp
// If non-existent cart should fail:
var result = await _repository.ClearCartItemsAsync(999999);
Assert.False(result);  // ✅ Correct - cart doesn't exist
```

### Review Other Unhappy Path Tests
- Verify correct assertion direction (True/False)
- Verify test data setup (FK constraints satisfied)
- Verify exception vs return value expectations

---

## Phase 5: Data Seeding & FK Constraint Fixes

### Common FK Constraint Issues
```
SQLite Error 19: 'FOREIGN KEY constraint failed'
```

### Solutions

1. **Seed Parent Entities First**
   ```csharp
   // Add Status records (parent)
   Context.Set<Status>().Add(new Status { StatusId = 1, StatusName = "Pending" });
   await Context.SaveChangesAsync();
   
   // Then add Order records (child)
   Context.Set<Order>().Add(new Order { Status = 1, ... });
   await Context.SaveChangesAsync();
   ```

2. **Use Fixture Pre-Seeded Data**
   - DatabaseFixture creates schema but should also seed static lookup tables
   - Add Status, Platform, SiteType records on fixture init

3. **Disable FK in Test Context (Last Resort)**
   ```csharp
   Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
   // Do test operations
   Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
   ```

---

## Phase 6: Test Execution & Validation

### Step 6.1: Run Tests in Isolation
```bash
# Test one file at a time
dotnet test Tests/UnitTests/CartRepositoryUnitTests.cs --verbosity detailed
dotnet test Tests/IntegretionTests/OrderRepositoryIntegrationTests.cs --verbosity detailed
```

### Step 6.2: Fix & Repeat
- Fix identified issue
- Re-run single test file
- Verify fix works
- Move to next failing test

### Step 6.3: Batch-Run by Category
```bash
# All unit tests
dotnet test Tests/UnitTests/ --verbosity detailed

# All integration tests
dotnet test Tests/IntegretionTests/ --verbosity detailed

# All tests
dotnet test
```

### Step 6.4: Full Test Suite with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --verbosity detailed
```

---

## Phase 7: Coverage Analysis & Reporting

### Extract Coverage Metrics
```powershell
# Find coverage report (usually in TestResults/)
$reportPath = Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1
# Parse XML and extract: lines covered, methods covered, by-file breakdown
```

### Display Report
```
======= CODE COVERAGE REPORT =======
Total Tests: 113
Passed: XXX
Failed: 0

Line Coverage: XX%
Method Coverage: XX%
Branch Coverage: XX%

By Repository:
- OrderRepository: XX% coverage
- ProductRepository: XX% coverage
...
```

---

## Execution Priority

1. **CRITICAL:** Phase 1 (run tests to see real failures)
2. **HIGH:** Phase 2 (fix InMemory async — blocks half the tests)
3. **HIGH:** Phase 3 (fix getdate() — blocks integration tests)
4. **MEDIUM:** Phase 4 (fix logic bugs)
5. **MEDIUM:** Phase 5 (fix FK constraints if needed)
6. **FINAL:** Phase 6 & 7 (verify all pass, report coverage)

---

## Success Criteria

- ✅ All 113+ tests execute without errors
- ✅ All tests pass (100% pass rate)
- ✅ Code coverage ≥ 70% for repository layer
- ✅ No SQL errors or async exceptions
- ✅ Coverage report generated and displayed

---

## Files That Will Need Changes

**Unit Tests (May need SQLite switch):**
- Tests/UnitTests/CartRepositoryUnitTests.cs
- Tests/UnitTests/PlatformRepositoryUnitTests.cs
- Tests/UnitTests/MainCategoryRepositoryUnitTests.cs
- Tests/UnitTests/BasicSiteRepositoryUnitTests.cs
- Tests/UnitTests/SiteTypeRepositoryUnitTests.cs
- Tests/UnitTests/GeminiPromptsRepositoryUnitTests.cs
- Tests/UnitTests/RatingRepositoryUnitTests.cs

**Integration Tests (Verify getdate() fix works):**
- Tests/IntegretionTests/OrderRepositoryIntegrationTests.cs
- Tests/IntegretionTests/ProductReposetoryIntegrationTests.cs
- Tests/IntegretionTests/UserRepositoryIntegrationTests.cs
- Tests/IntegretionTests/SubCategoryRepositoryIntegrationTests.cs

**Fixtures & Config:**
- Tests/IntegretionTests/DatabaseFixture.cs (verify TestMyShopContext active)
- Tests/IntegretionTests/DatabaseFixtureCollection.cs (verify collection registration)

---

## Command Reference

```bash
# Run tests with diagnostics
dotnet test --verbosity detailed --logger "console;verbosity=detailed"

# Run single test file
dotnet test Tests/UnitTests/CartRepositoryUnitTests.cs

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Check test results
Get-ChildItem TestResults -Recurse -Include "*.xml"
```
