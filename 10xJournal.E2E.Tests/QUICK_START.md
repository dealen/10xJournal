# Quick Start: E2E Test User Setup

## 🚀 Step-by-Step Guide

### Step 1: Get Your Service Role Key

1. Go to: https://supabase.com/dashboard
2. Select your project: **mudsjpiykmtxwoiyiimz**
3. Navigate to: **Settings** → **API**
4. Find the **service_role** key (NOT the anon key)
5. Click to reveal and copy it

⚠️ **WARNING**: This key has admin privileges. Keep it SECRET!

### Step 2: Update Configuration

Edit: `/10xJournal.E2E.Tests/appsettings.test.json`

Replace `YOUR_SERVICE_ROLE_KEY_HERE` with your actual service role key:

```json
{
  "Supabase": {
    "Url": "https://mudsjpiykmtxwoiyiimz.supabase.co",
    "AnonKey": "sb_publishable_Ad7la4eK0g5ztL80AFv2ng_b_5Ecze1",
    "ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "TestUrl": "https://mudsjpiykmtxwoiyiimz.supabase.co",
    "TestKey": "sb_publishable_Ad7la4eK0g5ztL80AFv2ng_b_5Ecze1"
  },
  "TestUser": {
    "Email": "e2etest@testmail.com",
    "Password": "TestPassword123!"
  }
}
```

### Step 3: Create Test User

From the project root:

```bash
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj
```

Or from the E2E.Tests directory:

```bash
cd 10xJournal.E2E.Tests
dotnet run
```

You should see:
```
✅ Test user setup complete and verified!
   Email: e2etest@testmail.com
   Password: TestPassword123!
   User ID: ...

🎯 You can now run E2E tests!
```

### Step 4: Run E2E Tests

```bash
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj
```

## 📋 Available Commands

```bash
# Create/recreate test user
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj setup

# Verify test user exists
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj verify

# Clean up all test users
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj cleanup

# Show help
dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj help
```

## 🐛 Common Issues

### "Service Role Key is not configured!"
→ You need to add your actual service role key to `appsettings.test.json`

### "Failed to create user: 401"
→ Check that you copied the complete service role key correctly

### "User exists but email is not confirmed"
→ Re-run the setup: `dotnet run setup`

## 🎯 Workflow

```bash
# Before running E2E tests for the first time
cd 10xJournal.E2E.Tests
dotnet run setup

# Run your E2E tests
dotnet test

# Optional: Clean up after testing
dotnet run cleanup
```

## 📚 Full Documentation

For complete documentation, see: `README_TEST_USER_SETUP.md`

---

**Ready?** Get your service role key and run `dotnet run` to get started! 🚀
