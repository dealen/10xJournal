# E2E Test User Management

This guide explains how to set up and manage test users for E2E (End-to-End) testing.

## 🎯 Overview

E2E tests require actual user accounts in Supabase to verify authentication and authorization flows. This tool helps you:

- ✅ Create test users automatically
- ✅ Ensure users are confirmed and ready to use
- ✅ Clean up test users after testing
- ✅ Verify user setup before running tests

## 📋 Prerequisites

### 1. Get Your Service Role Key

The Service Role Key has **admin privileges** and can bypass Row Level Security (RLS).

**⚠️ WARNING: Keep this key SECRET! Never commit it to version control!**

To get your Service Role Key:

1. Go to your Supabase Dashboard: [https://supabase.com/dashboard](https://supabase.com/dashboard)
2. Select your project
3. Navigate to: **Settings** → **API**
4. Find the **`service_role`** key (NOT the `anon` key)
5. Click to reveal and copy it

### 2. Configure appsettings.test.json

Update the `ServiceRoleKey` in `/10xJournal.E2E.Tests/appsettings.test.json`:

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "your_anon_key",
    "ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "TestUrl": "https://your-project.supabase.co",
    "TestKey": "your_anon_key"
  },
  "TestUser": {
    "Email": "e2etest@testmail.com",
    "Password": "TestPassword123!"
  }
}
```

**Security Best Practices:**
- ✅ Add `appsettings.test.json` to `.gitignore` if it contains sensitive keys
- ✅ Use environment variables for CI/CD pipelines
- ✅ Never commit the service role key to version control

## 🚀 Quick Start

### Option 1: Use the Standalone Tool (Recommended)

From the project root:

```bash
# Create/recreate the test user
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj

# Or with explicit command
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj setup
```

From the E2E.Tests directory:

```bash
cd 10xJournal.E2E.Tests

# Create/recreate the test user
dotnet run

# Or with explicit command
dotnet run setup
```

### Option 2: Integrate into Test Setup

The `TestUserManager` class can be used directly in test fixtures to automatically set up users before tests run. See [Integration Examples](#integration-examples) below.

## 📖 Available Commands

### `setup` (default)

Creates or recreates the test user with the credentials specified in `appsettings.test.json`.

```bash
dotnet run setup
```

**What it does:**
1. Deletes existing test user if present
2. Creates a new test user
3. Automatically confirms the email
4. Verifies the user was created successfully

**Output example:**
```
🚀 10xJournal E2E Test User Setup
=================================

📝 Setting up test user: e2etest@testmail.com

ℹ️  User e2etest@testmail.com does not exist, skipping deletion.
✅ Successfully created test user: e2etest@testmail.com
   User ID: 123e4567-e89b-12d3-a456-426614174000

✅ User e2etest@testmail.com exists and is confirmed.

✅ Test user setup complete and verified!
   Email: e2etest@testmail.com
   Password: TestPassword123!
   User ID: 123e4567-e89b-12d3-a456-426614174000

🎯 You can now run E2E tests!
```

### `verify`

Checks if the test user exists and is properly confirmed.

```bash
dotnet run verify
```

**What it does:**
1. Searches for the test user by email
2. Checks if the email is confirmed
3. Reports the status

**Use this to:**
- Verify setup before running tests
- Debug authentication issues
- Confirm user state

### `cleanup`

Removes all test users (users with `test_user: true` metadata).

```bash
dotnet run cleanup
```

**What it does:**
1. Lists all users in Supabase
2. Identifies test users by metadata
3. Deletes all test users

**Use this to:**
- Clean up after test runs
- Reset test environment
- Remove orphaned test accounts

### `help`

Shows usage information and available commands.

```bash
dotnet run help
```

## 🔧 Integration Examples

### Example 1: Using in Test Fixture

You can integrate `TestUserManager` into your test fixtures for automatic setup:

```csharp
public class E2ETestFixture : IAsyncLifetime
{
    private TestUserManager? _userManager;
    
    public async Task InitializeAsync()
    {
        var config = LoadConfiguration();
        _userManager = new TestUserManager(config.SupabaseUrl, config.ServiceRoleKey);
        
        // Ensure test user exists before tests run
        await _userManager.CreateTestUserAsync(
            config.TestUserEmail, 
            config.TestUserPassword
        );
    }
    
    public async Task DisposeAsync()
    {
        // Optional: Clean up after tests
        await _userManager?.CleanupAllTestUsersAsync();
        _userManager?.Dispose();
    }
}
```

### Example 2: One-Time Setup Script

Create a script to run before your entire test suite:

```bash
#!/bin/bash
# setup-e2e-tests.sh

echo "Setting up E2E test environment..."

# Create test user
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj setup

# Run E2E tests
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj

# Clean up (optional)
# dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj cleanup
```

### Example 3: CI/CD Integration

In your CI/CD pipeline (e.g., GitHub Actions):

```yaml
- name: Setup E2E Test Users
  env:
    SUPABASE_SERVICE_KEY: ${{ secrets.SUPABASE_SERVICE_KEY }}
  run: |
    # Update appsettings.test.json with secret
    jq '.Supabase.ServiceRoleKey = env.SUPABASE_SERVICE_KEY' \
      10xJournal.E2E.Tests/appsettings.test.json > tmp.json
    mv tmp.json 10xJournal.E2E.Tests/appsettings.test.json
    
    # Create test user
    dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj setup

- name: Run E2E Tests
  run: dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj

- name: Cleanup Test Users
  if: always()
  run: dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj cleanup
```

## 🐛 Troubleshooting

### "Service Role Key is not configured!"

**Problem:** The service role key is missing or set to the placeholder value.

**Solution:**
1. Get your service role key from Supabase Dashboard → Settings → API
2. Update `appsettings.test.json` with the actual key
3. Ensure you're using the `service_role` key, NOT the `anon` key

### "Failed to create user: 401 Unauthorized"

**Problem:** The service role key is incorrect or invalid.

**Solution:**
1. Verify you copied the complete service role key
2. Check that you're using the correct Supabase project URL
3. Ensure the key hasn't been rotated or revoked

### "Failed to create user: 422 Unprocessable Entity"

**Problem:** The user might already exist or there's a validation error.

**Solution:**
1. Try the `cleanup` command first to remove existing test users
2. Check that the email format is valid
3. Ensure the password meets Supabase requirements (typically 6+ characters)

### "User exists but email is not confirmed"

**Problem:** User was created but email confirmation failed.

**Solution:**
1. Re-run `setup` to recreate the user with auto-confirmation
2. Manually confirm the user in Supabase Dashboard → Authentication → Users
3. Check Supabase project settings for email confirmation requirements

### Tests still fail with "Invalid email or password"

**Problem:** The user exists but credentials don't match.

**Solution:**
1. Verify the exact email and password in `appsettings.test.json`
2. Re-run `setup` to recreate the user with correct credentials
3. Check that the test code is using the same credentials
4. Ensure no extra spaces or special characters

## 📚 API Reference

### TestUserManager Class

The core helper class for managing test users.

#### Constructor

```csharp
public TestUserManager(string supabaseUrl, string serviceRoleKey)
```

Creates a new instance with the specified Supabase credentials.

#### Methods

##### CreateTestUserAsync

```csharp
public async Task<string?> CreateTestUserAsync(string email, string password)
```

Creates a test user with auto-confirmed email. Returns user ID on success, null on failure.

##### DeleteUserByEmailAsync

```csharp
public async Task<bool> DeleteUserByEmailAsync(string email)
```

Deletes a user by email address. Returns true on success.

##### VerifyUserExistsAsync

```csharp
public async Task<bool> VerifyUserExistsAsync(string email)
```

Checks if a user exists and is confirmed. Returns true if user is ready to use.

##### CleanupAllTestUsersAsync

```csharp
public async Task<int> CleanupAllTestUsersAsync()
```

Removes all users with `test_user: true` metadata. Returns count of deleted users.

## 🔒 Security Notes

1. **Never commit service role keys** to version control
2. **Use environment variables** for sensitive configuration in CI/CD
3. **Rotate keys regularly** in production environments
4. **Limit test user permissions** if possible (though they need normal user permissions for realistic testing)
5. **Clean up test users** after test runs to minimize security exposure

## 🎯 Best Practices

1. **Run setup before each test session** to ensure clean state
2. **Use unique test user emails** per environment (dev, staging, CI)
3. **Clean up after tests** to avoid accumulating stale test data
4. **Verify setup** before running full E2E test suite
5. **Document credentials** for team members (securely, not in repo)
6. **Automate setup** in CI/CD pipelines for consistent results

## 📞 Support

If you encounter issues not covered here:

1. Check Supabase project logs in the Dashboard
2. Verify Row Level Security policies aren't blocking operations
3. Ensure Supabase project is active and not paused
4. Review test output and error messages carefully
5. Check network connectivity to Supabase

## 🚀 Next Steps

After setting up test users:

1. ✅ Run the E2E tests: `dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj`
2. ✅ Integrate setup into your development workflow
3. ✅ Add setup to CI/CD pipeline for automated testing
4. ✅ Document team procedures for running E2E tests

---

**Ready to test?** Run `dotnet run setup` and start testing! 🎉
