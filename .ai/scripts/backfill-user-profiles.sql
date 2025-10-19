-- ============================================================================
-- Backfill Script: Create Missing Profile and Streak Records
-- Purpose: For existing users created before the profile creation fix
-- Date: October 19, 2025
-- ============================================================================

-- STEP 1: Check how many users are missing profiles
-- Run this first to see if you need the backfill
SELECT 
    COUNT(*) as users_missing_profiles
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE p.id IS NULL;

-- STEP 2: Check how many users are missing streaks
SELECT 
    COUNT(*) as users_missing_streaks
FROM auth.users u
LEFT JOIN user_streaks s ON u.id = s.user_id
WHERE s.user_id IS NULL;

-- STEP 3: Create missing profile records
-- This inserts a profile for every user that doesn't have one
INSERT INTO profiles (id, created_at, updated_at)
SELECT 
    u.id, 
    u.created_at,  -- Use user's creation date
    NOW()           -- Set updated_at to now
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE p.id IS NULL;

-- STEP 4: Create missing streak records
-- This inserts a streak record for every user that doesn't have one
INSERT INTO user_streaks (user_id, current_streak, longest_streak, last_entry_date)
SELECT 
    u.id, 
    0,      -- Start with 0 current streak
    0,      -- Start with 0 longest streak
    NULL    -- No last entry date yet
FROM auth.users u
LEFT JOIN user_streaks s ON u.id = s.user_id
WHERE s.user_id IS NULL;

-- STEP 5: Verify all users now have profiles and streaks
SELECT 
    'Users' as entity,
    COUNT(*) as count
FROM auth.users
UNION ALL
SELECT 
    'Profiles' as entity,
    COUNT(*) as count
FROM profiles
UNION ALL
SELECT 
    'Streaks' as entity,
    COUNT(*) as count
FROM user_streaks;

-- Expected result: All three counts should be equal

-- ============================================================================
-- Optional: Check specific user's data
-- ============================================================================

-- Replace '<user_email>' with the actual email address
DO $$
DECLARE
    user_email TEXT := '<user_email>';  -- CHANGE THIS
    user_id UUID;
    has_profile BOOLEAN;
    has_streak BOOLEAN;
BEGIN
    -- Get user ID
    SELECT id INTO user_id FROM auth.users WHERE email = user_email;
    
    IF user_id IS NULL THEN
        RAISE NOTICE 'User with email % not found', user_email;
        RETURN;
    END IF;
    
    -- Check profile
    SELECT EXISTS(SELECT 1 FROM profiles WHERE id = user_id) INTO has_profile;
    
    -- Check streak
    SELECT EXISTS(SELECT 1 FROM user_streaks WHERE user_id = user_id) INTO has_streak;
    
    RAISE NOTICE 'User: % (ID: %)', user_email, user_id;
    RAISE NOTICE 'Has Profile: %', has_profile;
    RAISE NOTICE 'Has Streak: %', has_streak;
END $$;

-- ============================================================================
-- Troubleshooting: If backfill fails
-- ============================================================================

-- Check for duplicate profiles (shouldn't happen, but just in case)
SELECT 
    id, 
    COUNT(*) as count
FROM profiles
GROUP BY id
HAVING COUNT(*) > 1;

-- Check for duplicate streaks (shouldn't happen, but just in case)
SELECT 
    user_id, 
    COUNT(*) as count
FROM user_streaks
GROUP BY user_id
HAVING COUNT(*) > 1;

-- Check RLS policies on profiles table
SELECT 
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies
WHERE tablename = 'profiles';

-- ============================================================================
-- IMPORTANT NOTES
-- ============================================================================

-- 1. Run this script in the Supabase SQL Editor or via psql
-- 2. This script is idempotent - safe to run multiple times
-- 3. Only inserts missing records, doesn't modify existing ones
-- 4. After running, all new registrations will automatically create profiles
-- 5. This is a ONE-TIME backfill for existing users only
