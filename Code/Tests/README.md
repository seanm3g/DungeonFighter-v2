# Test Suite Organization

This directory contains all test files organized by type and purpose.

## Directory Structure

### `Unit/`
Unit tests for specific components, grouped by system (`Data/`, `Combat/`, `UI/`, `Game/`, etc.).

### `Runners/`
System and filtered runners used by CLI and the Settings Testing panel:

- `ComprehensiveTestRunner` — full suite
- `FilteredTestRunner` — substring / `game-system:` / `data:` filters
- `*SystemTestRunner` — per-domain suites (Combat, Data, UI, …)

### `Integration/`
Reserved for multi-system integration tests.

## Running Tests

From `Code/`:

```bash
dotnet run -- --run-tests
dotnet run -- --run-game-system-tests
dotnet run -- --run-data-tests
dotnet run -- --list-test-suites
dotnet run -- --run-test-filter <pattern>
```

### From Code — comprehensive suite

```csharp
using RPGGame.Tests.Runners;

ComprehensiveTestRunner.RunAllTests();
DataSystemTestRunner.RunAllTests();
CombatSystemTestRunner.RunAllTests();
// … other *SystemTestRunner classes
```

### From Code — individual unit classes

```csharp
using RPGGame.Tests.Unit.Data;
ActionLoaderTests.RunAllTests();
```

## Test Guidelines

### Unit Tests
- Test individual components in isolation
- Mock dependencies when necessary
- Focus on single responsibility
- Include edge cases and error conditions

### Integration Tests
- Test interaction between multiple components
- Use realistic data and scenarios
- Verify end-to-end workflows

## Adding New Tests

1. **Unit Tests**: Add under `Unit/` (or a subdomain folder)
   - Name: `[ComponentName]Tests.cs`
   - Namespace: `RPGGame.Tests.Unit` (or subdomain)

2. **Wire into a runner**: Register in the matching `*SystemTestRunner` and/or ensure `FilteredTestRunner` can discover it via the suite name.

3. **Build**: Prefer `Scripts\DF.bat` / `dotnet build` from `Code/` after changes.
