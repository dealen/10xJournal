-- ============================================================================
-- migration: restore user onboarding trigger
-- description: restores the trigger to automatically create profile records when users sign up
-- tables affected: auth.users (trigger source), public.profiles
-- dependencies: public.profiles
-- notes: 
--   - this trigger was previously removed but is needed for automatic profile creation
--   - without this trigger, users must manually create their profile records
--   - restores the handle_new_user function that creates profile and streak records
-- ============================================================================

-- create function to handle new user onboarding
-- this function is called automatically when a new user signs up via Supabase Auth
create or replace function public.handle_new_user()
returns trigger
language plpgsql
-- security definer allows this function to bypass rls policies
-- this is necessary to create the profile on behalf of the user
security definer
set search_path = public
as $$
begin
  -- create a profile record for the new user
  -- this gives users a profile that matches their auth.users record
  insert into public.profiles (id, created_at, updated_at)
  values (
    new.id,
    now(),
    now()
  );
  
  -- create a streak record for the new user
  -- this initializes their writing streak tracking
  insert into public.user_streaks (user_id, current_streak, longest_streak)
  values (
    new.id,
    0,
    0
  );
  
  -- return new to allow the insert into auth.users to proceed
  return new;
end;
$$;

-- create trigger on auth.users to automatically create profile and streak
-- this trigger fires after a new user record is inserted into auth.users
create trigger on_auth_user_created
  after insert on auth.users
  for each row
  execute function public.handle_new_user();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.handle_new_user is 'automatically creates profile and streak records when a new user signs up';
