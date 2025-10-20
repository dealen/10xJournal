-- ============================================================================
-- migration: fix initialize_new_user function ambiguous column reference
-- description: fixes the "column reference user_id is ambiguous" error by
--              using a different parameter name (p_user_id)
-- tables affected: public.profiles, public.user_streaks
-- dependencies: 20251020110000_create_initialize_new_user_function.sql
-- notes: 
--   - drops and recreates the function with clearer parameter naming
--   - uses p_ prefix for parameters to avoid ambiguity with column names
-- ============================================================================

-- drop the existing function first (required to change parameter name)
drop function if exists public.initialize_new_user(uuid);

-- recreate the function with unambiguous parameter name
create or replace function public.initialize_new_user(p_user_id uuid)
returns json
language plpgsql
security definer -- runs with the privileges of the function creator, bypassing RLS
set search_path = public
as $$
declare
  result json;
begin
  -- create profile record for the user
  insert into public.profiles (id)
  values (p_user_id)
  on conflict (id) do nothing; -- idempotent: don't fail if profile already exists

  -- create initial streak record for the user
  insert into public.user_streaks (user_id, current_streak, longest_streak, last_entry_date)
  values (p_user_id, 0, 0, null)
  on conflict (user_id) do nothing; -- idempotent: don't fail if streak already exists

  -- return success result with user_id
  result := json_build_object(
    'success', true,
    'user_id', p_user_id,
    'message', 'User initialized successfully'
  );

  return result;

exception
  when others then
    -- return error result with details
    result := json_build_object(
      'success', false,
      'user_id', p_user_id,
      'error', SQLERRM
    );
    return result;
end;
$$;

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.initialize_new_user(uuid) is 
  'Initializes a new user by creating their profile and streak records. Uses security definer to bypass RLS. Parameter p_user_id avoids ambiguity with column names.';

-- ============================================================================
-- verification
-- ============================================================================

-- Test the function works (commented out, for manual testing):
-- SELECT initialize_new_user('00000000-0000-0000-0000-000000000001'::uuid);
