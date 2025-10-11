-- ============================================================================
-- migration: create user_streaks table
-- description: creates the public.user_streaks table to track writing habit streaks
-- tables affected: public.user_streaks
-- dependencies: public.profiles
-- notes: 
--   - this table has a 1:1 relationship with profiles (one user, one streak record)
--   - tracks current consecutive days, longest streak, and last entry date
--   - modifications to this table should primarily be done via database functions
--   - row level security (rls) allows users to view their own streak data
-- ============================================================================

-- create the user_streaks table
-- this table tracks each user's writing habit streak information
create table public.user_streaks (
  -- primary key that references the user's profile
  -- cascade delete ensures streak record is removed when user profile is deleted
  user_id uuid primary key references public.profiles(id) on delete cascade,
  
  -- number of consecutive days with at least one journal entry
  -- defaults to 0 for new users
  current_streak integer not null default 0,
  
  -- the longest historical streak the user has achieved
  -- defaults to 0 for new users
  longest_streak integer not null default 0,
  
  -- the date of the last journal entry that was counted toward the streak
  -- nullable because new users may not have any entries yet
  -- type is date (not timestamptz) because we only care about the day, not the specific time
  last_entry_date date
);

-- enable row level security on the user_streaks table
-- this ensures that database-level security policies are enforced
alter table public.user_streaks enable row level security;

-- ============================================================================
-- row level security policies for user_streaks table
-- ============================================================================

-- policy: allow authenticated users to select (read) their own streak data
-- rationale: users should be able to view their own progress and achievements
-- role: authenticated users only
-- operation: select
create policy "authenticated users can view their own streaks"
  on public.user_streaks
  for select
  to authenticated
  using (auth.uid() = user_id);

-- policy: deny anonymous users from selecting streak data
-- rationale: unauthenticated users should not have access to any streak data
-- role: anonymous users
-- operation: select
create policy "anonymous users cannot view streaks"
  on public.user_streaks
  for select
  to anon
  using (false);

-- note: insert, update, and delete operations on this table will be performed
-- exclusively by database functions with security definer privileges
-- therefore, no additional rls policies for these operations are needed
-- users should not directly modify their streak data

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on table public.user_streaks is 'tracks writing habit streak data for each user';
comment on column public.user_streaks.user_id is 'references the user whose streak is being tracked';
comment on column public.user_streaks.current_streak is 'number of consecutive days with at least one entry';
comment on column public.user_streaks.longest_streak is 'the longest historical streak achieved by the user';
comment on column public.user_streaks.last_entry_date is 'date of the last entry that was counted toward the streak';
