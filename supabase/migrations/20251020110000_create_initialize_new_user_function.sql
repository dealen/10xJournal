-- ============================================================================
-- migration: create initialize_new_user function
-- description: creates a database function to initialize a new user's profile
--              and streak records. uses security definer to bypass RLS.
-- tables affected: public.profiles, public.user_streaks
-- dependencies: 20251011100000_create_profiles_table.sql,
--               20251011100200_create_user_streaks_table.sql
-- notes: 
--   - function runs with elevated privileges (security definer)
--   - bypasses RLS to create initial user records
--   - can be called immediately after user signup, even if email not confirmed
--   - used when manual profile creation is needed (triggers removed)
-- ============================================================================

-- create function to initialize a new user's profile and streak records
-- this function uses security definer to bypass RLS policies
create or replace function public.initialize_new_user(user_id uuid)
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
  values (user_id)
  on conflict (id) do nothing; -- idempotent: don't fail if profile already exists

  -- create initial streak record for the user
  insert into public.user_streaks (user_id, current_streak, longest_streak, last_entry_date)
  values (user_id, 0, 0, null)
  on conflict (user_id) do nothing; -- idempotent: don't fail if streak already exists

  -- return success result with user_id
  result := json_build_object(
    'success', true,
    'user_id', user_id,
    'message', 'User initialized successfully'
  );

  return result;

exception
  when others then
    -- return error result with details
    result := json_build_object(
      'success', false,
      'user_id', user_id,
      'error', SQLERRM
    );
    return result;
end;
$$;

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.initialize_new_user(uuid) is 
  'Initializes a new user by creating their profile and streak records. Uses security definer to bypass RLS.';
