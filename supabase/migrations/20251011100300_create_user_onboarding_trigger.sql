-- ============================================================================
-- migration: user onboarding automation
-- description: creates trigger to automatically set up profile and streak records for new users
-- tables affected: auth.users (trigger source), public.profiles, public.user_streaks
-- dependencies: public.profiles, public.user_streaks
-- notes: 
--   - trigger fires when a new user is created in auth.users
--   - automatically creates corresponding records in profiles and user_streaks
--   - ensures data consistency and removes manual setup burden from application
--   - uses security definer to bypass rls for initial user setup
-- ============================================================================

-- create function to handle new user onboarding
-- this function is called automatically when a new user registers
create or replace function public.handle_new_user()
returns trigger
language plpgsql
-- security definer allows this function to bypass rls policies
-- this is necessary because the new user doesn't have a profile yet
security definer
set search_path = public
as $$
begin
  -- create a profile record for the new user
  -- the id from auth.users is used as the primary key
  insert into public.profiles (id, created_at, updated_at)
  values (
    new.id,
    now(),
    now()
  );
  
  -- create a streak tracking record for the new user
  -- all streak values start at 0 and last_entry_date is null
  insert into public.user_streaks (user_id, current_streak, longest_streak, last_entry_date)
  values (
    new.id,
    0,
    0,
    null
  );
  
  -- return new to allow the insert into auth.users to proceed
  return new;
end;
$$;

-- create trigger on auth.users to automatically onboard new users
-- this trigger fires after a new user record is inserted in auth.users
-- important: this trigger is on the auth schema, not public
create trigger on_auth_user_created
  after insert on auth.users
  for each row
  execute function public.handle_new_user();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.handle_new_user is 'automatically creates profile and streak records when a new user registers';
