# E2E Tests - Quick Start Guide

## Running the Tests

### 1. Start the Blazor App
```bash
cd 10xJournal.Client
dotnet run
# Wait for: Now listening on: http://localhost:5212
```

### 2. Run E2E Tests (in a new terminal)
```bash
# Run all E2E tests
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj

# Run with detailed output
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj -v detailed
```

## Test Files

- **AuthenticationJourneyE2ETests.cs** - 3 critical authentication tests
- **E2ETestBase.cs** - Base class with browser setup and cleanup
- **TestDataCleanupHelper.cs** - Automatic test data cleanup

## What Gets Tested

1. ✅ New user registration flow
2. ✅ Login with existing credentials
3. ✅ Invalid credentials error handling

## Cleanup

All test data is automatically cleaned up after tests run:
- Test users deleted from Supabase Auth
- Profiles, streaks, and entries removed from database
- No manual cleanup needed

## Debugging

To see the browser during tests, edit `E2ETestBase.cs`:
```csharp
Browser = await Playwright.Chromium.LaunchAsync(new()
{
    Headless = false,  // Show browser
    SlowMo = 500       // Slow down actions
});
```

See `E2E_TESTS_SUMMARY.md` for complete documentation.
