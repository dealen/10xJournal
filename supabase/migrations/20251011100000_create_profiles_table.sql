-- ============================================================================
-- migration: create profiles table
-- description: creates the public.profiles table to store user profile information
-- tables affected: public.profiles
-- dependencies: auth.users (supabase auth)
-- notes: 
--   - this table has a 1:1 relationship with auth.users
--   - uses uuid from auth.users as primary key
--   - includes automatic updated_at timestamp management
--   - row level security (rls) is enabled to ensure users can only access their own profile
-- ============================================================================

-- create the profiles table
-- this table stores public profile information for each authenticated user
create table public.profiles (
  -- primary key that references the supabase auth.users table
  -- cascade delete ensures profile is removed when user account is deleted
  id uuid primary key references auth.users(id) on delete cascade,
  
  -- timestamp of when the profile was created
  -- automatically set to current time on insert
  created_at timestamptz not null default now(),
  
  -- timestamp of when the profile was last updated
  -- automatically set to current time on insert and update (via trigger)
  updated_at timestamptz not null default now()
);

-- enable row level security on the profiles table
-- this ensures that database-level security policies are enforced
alter table public.profiles enable row level security;

-- ============================================================================
-- row level security policies for profiles table
-- ============================================================================

-- policy: allow authenticated users to select (read) their own profile
-- rationale: users should only be able to view their own profile data
-- role: authenticated users only
-- operation: select
create policy "authenticated users can view their own profile"
  on public.profiles
  for select
  to authenticated
  using (auth.uid() = id);

-- policy: deny anonymous users from selecting profiles
-- rationale: unauthenticated users should not have access to any profile data
-- role: anonymous users
-- operation: select
create policy "anonymous users cannot view profiles"
  on public.profiles
  for select
  to anon
  using (false);

-- ============================================================================
-- trigger function to automatically update the updated_at timestamp
-- ============================================================================

-- create a reusable function to update the updated_at column
-- this function will be called by triggers on tables that need automatic timestamp updates
create or replace function public.handle_updated_at()
returns trigger
language plpgsql
security definer
as $$
begin
  -- set the updated_at column to the current timestamp
  new.updated_at = now();
  return new;
end;
$$;

-- create trigger to automatically update updated_at on profile updates
-- this trigger fires before any update operation on the profiles table
create trigger set_updated_at
  before update on public.profiles
  for each row
  execute function public.handle_updated_at();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on table public.profiles is 'stores public profile information for authenticated users';
comment on column public.profiles.id is 'user id from auth.users, serves as primary key';
comment on column public.profiles.created_at is 'timestamp when the profile was created';
comment on column public.profiles.updated_at is 'timestamp when the profile was last updated';
