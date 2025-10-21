# 🎯 Visual Step-by-Step Guide

Follow these exact steps to get your E2E tests passing!

---

## 📸 Step 1: Get Your Service Role Key

### Navigate to Supabase Dashboard

1. Open your browser and go to: **https://supabase.com/dashboard**

2. You should see your project: **mudsjpiykmtxwoiyiimz**

3. Click on **Settings** in the left sidebar

4. Click on **API** in the settings menu

5. Scroll down past the "Project API keys" section

6. Find the section labeled **"Service role"** or **"service_role"**

7. Click the 👁️ (eye icon) to reveal the key

8. Click 📋 (copy icon) or select all and copy the entire key
   - It will start with: `eyJ...`
   - It's very long (several lines)
   - Copy the ENTIRE key including any `...` at the end

---

## 📝 Step 2: Update Your Configuration File

### Open the file:
```
/home/dealen/Dev/10xDevs/10xJournal/10xJournal.E2E.Tests/appsettings.test.json
```

### Find this line:
```json
"ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY_HERE",
```

### Replace with your actual key:
```json
"ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS[...YOUR ACTUAL KEY HERE...]",
```

⚠️ **Important**: 
- Keep the quotes around the key: `"ServiceRoleKey": "your-key-here"`
- Don't remove the comma at the end
- Make sure it's valid JSON

### Your file should look like:
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

### Save the file

---

## 🚀 Step 3: Create the Test User

### Open your terminal

### Navigate to the E2E.Tests directory:
```bash
cd /home/dealen/Dev/10xDevs/10xJournal/10xJournal.E2E.Tests
```

### Run the setup command:
```bash
dotnet run
```

Or if that doesn't work:
```bash
dotnet run setup
```

### What you should see:

```
🚀 10xJournal E2E Test User Setup
=================================

📝 Setting up test user: e2etest@testmail.com

🗑️  Successfully deleted test user: e2etest@testmail.com
✅ Successfully created test user: e2etest@testmail.com
   User ID: 123e4567-e89b-12d3-a456-426614174000

✅ User e2etest@testmail.com exists and is confirmed.

✅ Test user setup complete and verified!
   Email: e2etest@testmail.com
   Password: TestPassword123!
   User ID: 123e4567-e89b-12d3-a456-426614174000

🎯 You can now run E2E tests!
```

### ✅ Success Indicators:
- See ✅ green checkmarks
- See "Test user setup complete and verified!"
- See "You can now run E2E tests!"

### ❌ If you see errors:

**Error: "Service Role Key is not configured!"**
→ Go back to Step 2, make sure you updated the JSON file

**Error: "Failed to create user: 401"**
→ Check that you copied the ENTIRE service role key

**Error: "Failed to create user: 422"**
→ Try running `dotnet run cleanup` first, then `dotnet run setup` again

---

## 🧪 Step 4: Run Your E2E Tests

### Navigate back to the project root:
```bash
cd /home/dealen/Dev/10xDevs/10xJournal
```

### Run the E2E tests:
```bash
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj
```

### What you should see:

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 45s
```

### ✅ Success Indicators:
- All tests pass (Failed: 0)
- See "Passed: 3" (or however many E2E tests you have)
- No "Invalid email or password" errors

---

## 🎉 You're Done!

Your E2E tests should now be working perfectly!

### Quick Command Reference:

```bash
# Create/recreate test user (run this first time)
cd /home/dealen/Dev/10xDevs/10xJournal/10xJournal.E2E.Tests
dotnet run setup

# Run E2E tests
cd /home/dealen/Dev/10xDevs/10xJournal
dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj

# Verify test user exists (troubleshooting)
cd /home/dealen/Dev/10xDevs/10xJournal/10xJournal.E2E.Tests
dotnet run verify

# Clean up test users (optional)
dotnet run cleanup
```

---

## 🐛 Troubleshooting Quick Reference

| Problem | Solution |
|---------|----------|
| "Service Role Key is not configured!" | Update `appsettings.test.json` with your actual key |
| "Failed to create user: 401" | Check service role key is complete and correct |
| "Failed to create user: 422" | Run `dotnet run cleanup` then `dotnet run setup` |
| "Invalid email or password" in tests | Re-run `dotnet run setup` |
| Tests still fail after setup | Check that app is running on `http://localhost:5212/` |

---

## 📚 Full Documentation Available

- **QUICK_START.md** - Quick reference
- **README_TEST_USER_SETUP.md** - Complete documentation
- **SOLUTION_SUMMARY.md** - Technical overview

---

## 🎯 Expected Timeline

This should take about **5 minutes**:

- ⏱️ 2 minutes: Get service role key from Supabase
- ⏱️ 1 minute: Update appsettings.test.json
- ⏱️ 1 minute: Run setup command
- ⏱️ 1 minute: Run E2E tests

---

## ✨ Final Checklist

Before running tests, make sure:

- [ ] I have my Supabase service role key
- [ ] I updated `appsettings.test.json` with the service role key
- [ ] I saved the `appsettings.test.json` file
- [ ] I ran `dotnet run setup` successfully
- [ ] I saw "Test user setup complete and verified!" message
- [ ] The local app is running on `http://localhost:5212/` (if testing)

---

**Ready to go?** Follow the steps above and your E2E tests will pass! 🚀

Need help? Check the troubleshooting section or see the full documentation files.
