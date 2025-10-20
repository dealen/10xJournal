# Account Deletion Function - Testing Guide

## Overview
This guide documents the testing steps for the `delete_my_account()` database function.

## Migration Applied
- **File**: `20251019110000_create_delete_my_account_function.sql`
- **Date**: October 19, 2025
- **Status**: Applied ✅

## Function Details
- **Name**: `public.delete_my_account()`
- **Returns**: JSON response with success status
- **Security**: `SECURITY DEFINER` (runs with elevated permissions)
- **Permissions**: Only `authenticated` users can execute

## JSON Response Structure
```json
{
  "success": true,
  "message": "account successfully deleted",
  "entries_deleted": 5,
  "deleted_at": "2025-10-19T11:00:00.000Z"
}
```

## Manual Testing Checklist

### Prerequisites
- [ ] Local Supabase instance running
- [ ] Migration applied via `supabase db reset`
- [ ] Test user created with some journal entries

### Test Scenarios

#### ✅ Scenario 1: Successful Account Deletion
1. **Setup**: 
   - Create test user account
   - Create 2-3 journal entries for the user
2. **Action**: 
   - Navigate to Settings → Delete Account
   - Enter correct password
   - Confirm deletion
3. **Expected Result**:
   - Success message displayed
   - User redirected to login/landing page
   - All journal entries deleted from database
   - User profile deleted from `public.profiles`
   - User deleted from `auth.users`
   - No orphaned records remain

#### ❌ Scenario 2: Unauthenticated Access Attempt
1. **Setup**: User is logged out
2. **Action**: Attempt to call RPC directly (via API)
3. **Expected Result**: 
   - Error: "not authenticated"
   - No data deleted

#### ❌ Scenario 3: Wrong Password Verification
1. **Setup**: User logged in
2. **Action**: 
   - Navigate to Delete Account
   - Enter incorrect password
   - Attempt to confirm
3. **Expected Result**:
   - Error message: "Nieprawidłowe hasło. Spróbuj ponownie."
   - Account NOT deleted
   - User remains logged in

#### ✅ Scenario 4: Account with No Entries
1. **Setup**: Create user with no journal entries
2. **Action**: Delete account
3. **Expected Result**:
   - Success message
   - `entries_deleted: 0` in response
   - Account deleted successfully

## Database Verification Queries

### Check if function exists
```sql
SELECT routine_name, routine_type 
FROM information_schema.routines 
WHERE routine_schema = 'public' 
  AND routine_name = 'delete_my_account';
```

### Check function permissions
```sql
SELECT grantee, privilege_type 
FROM information_schema.routine_privileges 
WHERE routine_schema = 'public' 
  AND routine_name = 'delete_my_account';
```

### Verify user deletion
```sql
-- Check profiles (should be empty after deletion)
SELECT id, email FROM public.profiles WHERE email = 'test@example.com';

-- Check journal entries (should be empty)
SELECT COUNT(*) FROM public.journal_entries WHERE user_id = '<user-id>';

-- Check auth users (should be empty)
SELECT id, email FROM auth.users WHERE email = 'test@example.com';
```

## Security Verification

- [ ] Anonymous users cannot execute function
- [ ] Authenticated users can only delete their own account
- [ ] Function properly deletes from `auth.users` (requires SECURITY DEFINER)
- [ ] No SQL injection vulnerabilities
- [ ] All deletions occur in single transaction

## Error Handling Verification

- [ ] Proper error JSON returned on failure
- [ ] PostgreSQL NOTICE logs written during execution
- [ ] PostgreSQL WARNING logs written on errors
- [ ] Exception handling prevents partial deletions

## Performance Considerations

- [ ] Function completes in < 1 second for accounts with < 100 entries
- [ ] No hanging transactions or locks
- [ ] Proper cleanup of all related data

## Rollback Plan

If issues are discovered:

```sql
-- Remove the function
DROP FUNCTION IF EXISTS public.delete_my_account();

-- Then rollback migration or apply fix
```

## Integration with UI

The function is called from:
- **Component**: `DeleteAccountModal.razor`
- **Location**: `/Features/Settings/DeleteAccount/`
- **RPC Call**: `SupabaseClient.Rpc("delete_my_account", null)`

## Notes

- The function uses explicit deletion order to respect foreign key constraints
- All operations occur within a single transaction (implicit in PostgreSQL functions)
- The function returns structured JSON matching the C# `DeleteAccountResponse` model
- Logging uses PostgreSQL NOTICE and WARNING for audit trail
