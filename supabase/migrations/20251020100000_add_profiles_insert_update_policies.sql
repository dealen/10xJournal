-- ============================================================================
-- migration: add insert and update rls policies for profiles table
-- description: adds missing row level security policies to allow authenticated
--              users to insert and update their own profile records
-- tables affected: public.profiles
-- dependencies: 20251011100000_create_profiles_table.sql
-- notes: 
--   - these policies were missing after the original table creation
--   - required for manual profile creation during user registration
--   - ensures users can only insert/update their own profile
-- ============================================================================

-- ============================================================================
-- row level security policies for profiles table
-- ============================================================================

-- policy: allow authenticated users to insert their own profile
-- rationale: when a user signs up, they need to create their profile record
-- role: authenticated users only
-- operation: insert
-- constraint: user can only insert a profile where the id matches their auth.uid()
create policy "authenticated users can insert their own profile"
  on public.profiles
  for insert
  to authenticated
  with check (auth.uid() = id);

-- policy: allow authenticated users to update their own profile
-- rationale: users should be able to modify their own profile data
-- role: authenticated users only
-- operation: update
-- constraint: user can only update their own profile
create policy "authenticated users can update their own profile"
  on public.profiles
  for update
  to authenticated
  using (auth.uid() = id)
  with check (auth.uid() = id);

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on policy "authenticated users can insert their own profile" on public.profiles is 
  'allows authenticated users to create their profile record during registration';
comment on policy "authenticated users can update their own profile" on public.profiles is 
  'allows authenticated users to update their own profile information';
