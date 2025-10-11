-- ============================================================================
-- migration: welcome entry automation
-- description: creates trigger to automatically create a welcome journal entry for new users
-- tables affected: public.profiles (trigger source), public.journal_entries
-- dependencies: public.profiles, public.journal_entries
-- notes: 
--   - trigger fires when a new profile is created
--   - automatically creates a welcoming first journal entry
--   - provides users with an example entry and warm greeting
--   - uses security definer to bypass rls for initial entry creation
-- ============================================================================

-- create function to handle welcome entry creation
-- this function is called automatically when a new profile is created
create or replace function public.handle_new_profile()
returns trigger
language plpgsql
-- security definer allows this function to bypass rls policies
-- this is necessary to create the entry on behalf of the user
security definer
set search_path = public
as $$
begin
  -- create a welcome journal entry for the new user
  -- this gives users a friendly starting point and example of how entries work
  insert into public.journal_entries (user_id, content, created_at, updated_at)
  values (
    new.id,
    'Witaj w 10xJournal! 👋

To jest Twój pierwszy wpis w dzienniku. Możesz go edytować lub usunąć w dowolnym momencie.

10xJournal to Twoja prywatna przestrzeń do zapisywania myśli, refleksji i postępów. Pisz regularnie, aby budować nawyk, który pomoże Ci w rozwoju osobistym i zawodowym.

Powodzenia w Twojej podróży! 🚀',
    now(),
    now()
  );
  
  -- return new to allow the insert into profiles to proceed
  return new;
end;
$$;

-- create trigger on public.profiles to automatically create welcome entry
-- this trigger fires after a new profile record is inserted
-- note: this trigger runs after the profile is created (which happens during user onboarding)
create trigger on_profile_created
  after insert on public.profiles
  for each row
  execute function public.handle_new_profile();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.handle_new_profile is 'automatically creates a welcome journal entry when a new profile is created';
