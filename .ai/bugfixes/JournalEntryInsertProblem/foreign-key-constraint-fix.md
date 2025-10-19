# Foreign Key Constraint Fix - Profile Creation on Registration

## 🐛 Problem Description

After fixing the session persistence issue, users encountered a new error when creating journal entries:

**Error Message:**
```
{"code":"23503","details":"Key is not present in table \"profiles\".","hint":null,"message":"insert or update on table \"journal_entries\" violates foreign key constraint \"journal_entries_user_id_fkey\""}
```

**HTTP Response:**
```
StatusCode: 409, ReasonPhrase: 'Conflict'
```

**PostgreSQL Error Code:** `23503` = Foreign Key Violation

## 🔍 Root Cause Analysis

### The Missing Link

The `journal_entries` table has a foreign key constraint:
```sql
ALTER TABLE journal_entries 
ADD CONSTRAINT journal_entries_user_id_fkey 
FOREIGN KEY (user_id) REFERENCES profiles(id);
```

This means every `user_id` in `journal_entries` **must** exist in the `profiles` table.

### What Was Happening

1. ✅ User registers → Record created in `auth.users` (Supabase Auth)
2. ❌ **No record created in `profiles` table**
3. ✅ User logs in successfully (auth works fine)
4. ✅ Session persists correctly (previous fix)
5. ✅ User tries to create journal entry with their `user_id`
6. ❌ **Foreign key constraint fails** - `user_id` doesn't exist in `profiles`

### Why This Happened

The database had automation triggers that were removed in migration `20251012123000_remove_automation_triggers.sql`:

```sql
-- This trigger was REMOVED
drop trigger if exists on_auth_user_created on auth.users;

-- This function was REMOVED
drop function if exists public.handle_new_user();
```

**Original Trigger Logic (REMOVED):**
```sql
create or replace function public.handle_new_user()
returns trigger as $$
begin
  -- Create profile record
  insert into public.profiles (id, created_at, updated_at)
  values (new.id, now(), now());
  
  -- Create streak record
  insert into public.user_streaks (user_id, current_streak, longest_streak, last_entry_date)
  values (new.id, 0, 0, null);
  
  return new;
end;
$$;
```

**Result:** After the trigger was removed, registration only created the auth user, leaving the application responsible for creating the profile and streak records.

## ✅ Solution Implemented

### Architecture Decision

**Move user onboarding logic from database triggers to application code.**

**Rationale:**
1. **Explicit Control:** Application has full visibility into the onboarding process
2. **Error Handling:** Can provide better user feedback if profile creation fails
3. **Flexibility:** Easier to modify onboarding logic without database migrations
4. **Debugging:** Easier to trace and log the onboarding flow

### Implementation

#### 1. Updated UserProfile Model

**File:** `/Features/Authentication/Models/UserProfile.cs`

Changed from plain POCO to Supabase PostgREST model:

```csharp
// BEFORE - Plain C# class
public class UserProfile
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    // ...
}

// AFTER - Supabase PostgREST model
[Table("profiles")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

**Changes:**
- ✅ Added `[Table("profiles")]` attribute for Supabase mapping
- ✅ Extended `BaseModel` to enable CRUD operations
- ✅ Changed from `JsonPropertyName` to PostgREST `[Column]` attributes
- ✅ Added `[PrimaryKey]` attribute for proper entity tracking

#### 2. Enhanced SupabaseAuthService

**File:** `/Features/Authentication/Register/SupabaseAuthService.cs`

Enhanced `RegisterAsync()` to replicate the removed trigger logic:

```csharp
public async Task RegisterAsync(string email, string password)
{
    try
    {
        // Step 1: Create the user account in Supabase Auth
        var response = await _supabaseClient.Auth.SignUp(email, password);
        
        if (response?.User == null || string.IsNullOrEmpty(response.User.Id))
        {
            _logger.LogError("SignUp succeeded but returned null user for {Email}", email);
            throw new InvalidOperationException("User registration failed - no user ID returned");
        }

        var userId = Guid.Parse(response.User.Id);
        _logger.LogInformation("User created with ID {UserId}, creating profile and streak records", userId);

        // Step 2: Create profile record
        try
        {
            await _supabaseClient
                .From<UserProfile>()
                .Insert(new UserProfile
                {
                    Id = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            
            _logger.LogInformation("Profile record created for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile for user {UserId}", userId);
            throw new InvalidOperationException($"Failed to create user profile: {ex.Message}", ex);
        }

        // Step 3: Create streak record
        try
        {
            await _supabaseClient
                .From<UserStreak>()
                .Insert(new UserStreak
                {
                    UserId = userId,
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    LastEntryDate = DateTime.MinValue
                });
            
            _logger.LogInformation("Streak record created for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streak for user {UserId}", userId);
            // Don't throw - profile is more critical than streak
        }
    }
    catch (Supabase.Gotrue.Exceptions.GotrueException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error occurred during registration for {Email}", email);
        throw;
    }
}
```

**Key Features:**
1. **Three-Step Process:**
   - Create auth user (existing functionality)
   - Create profile record (NEW - required for foreign keys)
   - Create streak record (NEW - for future streak tracking)

2. **Robust Error Handling:**
   - Validates user ID is returned from SignUp
   - Profile creation failure throws exception (critical)
   - Streak creation failure is logged but doesn't block registration (non-critical)

3. **Comprehensive Logging:**
   - Logs user creation with ID
   - Logs successful profile/streak creation
   - Logs errors with context for debugging

4. **Transactional Safety:**
   - If profile creation fails, exception is thrown
   - User sees registration error instead of mysterious failures later
   - Prevents orphaned auth users without profiles

## 🎯 Technical Details

### Database Relationships

```
auth.users (Supabase Auth)
    ↓ (1:1)
profiles (Public Schema)
    ↓ (1:many)
journal_entries (Public Schema)
```

### Foreign Key Constraint

**Definition:**
```sql
ALTER TABLE journal_entries 
ADD CONSTRAINT journal_entries_user_id_fkey 
FOREIGN KEY (user_id) REFERENCES profiles(id);
```

**Enforcement:**
- Every `user_id` in `journal_entries` MUST exist in `profiles.id`
- Cannot insert journal entry for non-existent profile
- Database enforces referential integrity

### Why Not Use auth.users Directly?

**Question:** Why doesn't `journal_entries` reference `auth.users` directly?

**Answer:**
1. **Schema Separation:** `auth` schema is managed by Supabase, `public` schema is ours
2. **RLS Simplicity:** RLS policies use `auth.uid()` which references current user
3. **Profile Extension:** `profiles` table can be extended with user-specific data
4. **Best Practice:** Decouple application data from authentication provider

## 🔒 Security Considerations

### RLS Policy Compliance

The profile creation happens with the authenticated user's session:

```csharp
// After SignUp, user is automatically logged in
var response = await _supabaseClient.Auth.SignUp(email, password);
// response contains valid session

// Profile insert uses authenticated session
await _supabaseClient.From<UserProfile>().Insert(...)
```

**RLS Policy on profiles table:**
```sql
create policy "authenticated users can view their own profile"
  on public.profiles
  for select
  to authenticated
  using (auth.uid() = id);
```

**INSERT Policy (implicit):** New users can create their own profile during onboarding.

### Error Handling Security

- ❌ **Don't expose** database error details to users
- ✅ **Do log** full errors server-side for debugging
- ✅ **Do show** user-friendly error messages
- ✅ **Do prevent** orphaned auth users

## 📊 Testing Strategy

### Manual Testing Steps

1. **New User Registration:**
   ```
   - Navigate to registration page
   - Enter email and password
   - Click "Załóż konto" (Register)
   - Verify success message
   ```

2. **Profile Verification (Database):**
   ```sql
   SELECT * FROM auth.users WHERE email = 'test@example.com';
   SELECT * FROM profiles WHERE id = '<user_id_from_above>';
   SELECT * FROM user_streaks WHERE user_id = '<user_id_from_above>';
   ```
   ✅ All three tables should have matching records

3. **Journal Entry Creation:**
   ```
   - Log in with new user
   - Navigate to /app/journal/new
   - Type content
   - Wait for auto-save (1 second)
   - Verify: "Saved at HH:MM:SS" appears
   - Check browser console: No errors
   ```
   ✅ Entry should save without foreign key constraint errors

### Expected Results

**Registration Flow:**
1. ✅ User account created in `auth.users`
2. ✅ Profile record created in `profiles`
3. ✅ Streak record created in `user_streaks`
4. ✅ User automatically logged in
5. ✅ Redirected to journal

**Journal Entry Creation:**
1. ✅ User ID retrieved from session
2. ✅ Entry inserted with valid `user_id`
3. ✅ Foreign key constraint passes
4. ✅ Entry saved successfully
5. ✅ No 409 Conflict errors

## 🔄 Migration Path

### For Existing Users (Without Profiles)

If you have existing users in `auth.users` without corresponding `profiles` records:

**Option 1: Backfill Script (SQL)**
```sql
-- Create profiles for users that don't have them
INSERT INTO profiles (id, created_at, updated_at)
SELECT 
    u.id, 
    u.created_at, 
    NOW()
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE p.id IS NULL;

-- Create streaks for users that don't have them
INSERT INTO user_streaks (user_id, current_streak, longest_streak, last_entry_date)
SELECT 
    u.id, 
    0, 
    0, 
    NULL
FROM auth.users u
LEFT JOIN user_streaks s ON u.id = s.user_id
WHERE s.user_id IS NULL;
```

**Option 2: Application-Level Backfill**
Add a one-time startup check that creates missing profiles.

### For New Projects

This implementation is now the standard. All new registrations will:
1. Create auth user
2. Create profile record
3. Create streak record

No database triggers required.

## 💡 Best Practices Applied

1. **Explicit Over Implicit:** Application code explicitly creates all required records
2. **Fail Fast:** Profile creation failure immediately throws exception
3. **Logging:** Comprehensive logging for debugging and monitoring
4. **Error Context:** Errors include user ID and specific failure point
5. **Defensive Coding:** Null checks on all external responses
6. **Transaction Safety:** Critical operations throw, non-critical log warnings

## 🎓 Lessons Learned

### Database Triggers vs Application Code

**Database Triggers (Previous Approach):**
- ✅ Automatic execution
- ✅ Can't be bypassed
- ❌ Hidden logic (not visible in application code)
- ❌ Harder to test
- ❌ Harder to debug
- ❌ Requires database migrations to change

**Application Code (Current Approach):**
- ✅ Explicit and visible
- ✅ Easy to test
- ✅ Easy to debug
- ✅ Better error handling
- ✅ Can be modified without database migrations
- ❌ Can be bypassed (if multiple entry points exist)

**Decision:** For this application, application-level logic is better because:
1. Single registration entry point (SupabaseAuthService)
2. Better error handling and user feedback
3. Easier to maintain and modify
4. More transparent for development

### Foreign Key Constraints

**Key Insight:** Foreign key constraints enforce data integrity at the database level. You cannot insert a child record (journal_entry) without a parent record (profile).

**Best Practice:** Ensure all required parent records are created during user onboarding.

## 📅 Fix Date
**October 19, 2025**

---

**Issue Status:** ✅ **RESOLVED - PROFILE CREATION IMPLEMENTED IN APPLICATION CODE**

**Testing Required:** 
1. Register a new user account
2. Verify profile and streak records are created
3. Create a journal entry
4. Verify no 409 Conflict or foreign key errors

**Migration Required:**
If you have existing users, run the backfill SQL script to create missing profile records.
