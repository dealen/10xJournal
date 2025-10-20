-- ============================================================================
-- migration: cleanup conflicting triggers and functions
-- description: removes trigger-based onboarding in favor of rpc function approach
-- reason: client code uses initialize_new_user() rpc, trigger is redundant
-- date: 2025-10-20
-- ============================================================================

-- ============================================================================
-- ISSUE: conflicting initialization approaches
-- 
-- migration 20251019120000_restore_onboarding_trigger.sql created a trigger
-- that automatically creates profile/streak when user signs up.
--
-- migration 20251020110000_create_initialize_new_user_function.sql created
-- an rpc function that client code explicitly calls after Auth.SignUp().
--
-- the client code (SupabaseAuthService.cs) uses the RPC function approach.
-- having both creates potential race conditions and duplicate insert attempts.
-- 
-- SOLUTION: keep RPC function approach, remove trigger approach
-- ============================================================================

-- drop the trigger that was restored by 20251019120000
-- this trigger attempted to auto-create profile/streak on user signup
drop trigger if exists on_auth_user_created on auth.users;

-- drop the trigger function
-- note: this is different from initialize_new_user() which is the RPC function
drop function if exists public.handle_new_user();

-- ============================================================================
-- cleanup unused functions from previously removed triggers
-- ============================================================================

-- this function was for auto-creating welcome entries
-- trigger was removed in 20251012123000, but function remained
drop function if exists public.handle_new_profile();

-- this function was for auto-updating streaks on entry creation
-- trigger was removed in 20251012123000, but function remained
drop function if exists public.handle_streak_update();

-- ============================================================================
-- update documentation
-- ============================================================================

-- clarify that initialize_new_user is the official initialization method
comment on function public.initialize_new_user(uuid) is 
  'initializes new user profile and streak records. must be called from client after Auth.SignUp(). uses SECURITY DEFINER to bypass RLS during initial setup. this is the official user initialization method, replacing previous trigger-based approaches.';

-- ============================================================================
-- verification queries (run these after migration to verify)
-- ============================================================================

-- these are comments, not executed, but useful for manual verification:
-- 
-- -- verify trigger is gone:
-- SELECT * FROM pg_trigger WHERE tgname = 'on_auth_user_created';
-- -- should return 0 rows
--
-- -- verify RPC function exists:
-- SELECT proname FROM pg_proc WHERE proname = 'initialize_new_user';
-- -- should return 1 row
--
-- -- verify old trigger function is gone:
-- SELECT proname FROM pg_proc WHERE proname = 'handle_new_user';
-- -- should return 0 rows
--
-- -- verify unused functions are gone:
-- SELECT proname FROM pg_proc WHERE proname IN ('handle_new_profile', 'handle_streak_update');
-- -- should return 0 rows

-- ============================================================================
-- end of cleanup migration
-- ============================================================================
