-- ============================================================================
-- migration: create journal_entries table
-- description: creates the public.journal_entries table to store user journal entries
-- tables affected: public.journal_entries
-- dependencies: public.profiles
-- notes: 
--   - this table has a 1:many relationship with profiles (one user, many entries)
--   - includes composite index on user_id and created_at for optimal query performance
--   - uses text type for content with no artificial length limit
--   - includes automatic updated_at timestamp management
--   - row level security (rls) ensures users can only crud their own entries
-- ============================================================================

-- create the journal_entries table
-- this table stores all journal entries created by users
create table public.journal_entries (
  -- primary key using uuid, automatically generated
  id uuid primary key default gen_random_uuid(),
  
  -- foreign key referencing the user who owns this entry
  -- cascade delete ensures all entries are removed when user profile is deleted
  user_id uuid not null references public.profiles(id) on delete cascade,
  
  -- the actual content of the journal entry
  -- using text type without artificial length constraints
  content text not null,
  
  -- timestamp of when the entry was created
  -- automatically set to current time on insert
  created_at timestamptz not null default now(),
  
  -- timestamp of when the entry was last updated
  -- automatically set to current time on insert and update (via trigger)
  updated_at timestamptz not null default now()
);

-- ============================================================================
-- indexes for query performance optimization
-- ============================================================================

-- create composite index on user_id and created_at (descending)
-- this index optimizes the most common query: fetching a user's entries sorted by date
-- the descending order on created_at allows efficient retrieval of newest entries first
create index idx_journal_entries_user_id_created_at 
  on public.journal_entries (user_id, created_at desc);

-- enable row level security on the journal_entries table
-- this ensures that database-level security policies are enforced
alter table public.journal_entries enable row level security;

-- ============================================================================
-- row level security policies for journal_entries table
-- ============================================================================

-- policy: allow authenticated users to select (read) their own journal entries
-- rationale: users should only be able to view their own entries
-- role: authenticated users only
-- operation: select
create policy "authenticated users can view their own entries"
  on public.journal_entries
  for select
  to authenticated
  using (auth.uid() = user_id);

-- policy: allow authenticated users to insert their own journal entries
-- rationale: users should be able to create new entries for themselves
-- role: authenticated users only
-- operation: insert
create policy "authenticated users can insert their own entries"
  on public.journal_entries
  for insert
  to authenticated
  with check (auth.uid() = user_id);

-- policy: allow authenticated users to update their own journal entries
-- rationale: users should be able to edit their own entries
-- role: authenticated users only
-- operation: update
create policy "authenticated users can update their own entries"
  on public.journal_entries
  for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

-- policy: allow authenticated users to delete their own journal entries
-- rationale: users should be able to remove their own entries
-- role: authenticated users only
-- operation: delete
create policy "authenticated users can delete their own entries"
  on public.journal_entries
  for delete
  to authenticated
  using (auth.uid() = user_id);

-- policy: deny anonymous users from selecting journal entries
-- rationale: unauthenticated users should not have access to any journal data
-- role: anonymous users
-- operation: select
create policy "anonymous users cannot view entries"
  on public.journal_entries
  for select
  to anon
  using (false);

-- policy: deny anonymous users from inserting journal entries
-- rationale: unauthenticated users cannot create journal entries
-- role: anonymous users
-- operation: insert
create policy "anonymous users cannot insert entries"
  on public.journal_entries
  for insert
  to anon
  with check (false);

-- policy: deny anonymous users from updating journal entries
-- rationale: unauthenticated users cannot modify journal entries
-- role: anonymous users
-- operation: update
create policy "anonymous users cannot update entries"
  on public.journal_entries
  for update
  to anon
  using (false);

-- policy: deny anonymous users from deleting journal entries
-- rationale: unauthenticated users cannot delete journal entries
-- role: anonymous users
-- operation: delete
create policy "anonymous users cannot delete entries"
  on public.journal_entries
  for delete
  to anon
  using (false);

-- ============================================================================
-- trigger to automatically update the updated_at timestamp
-- ============================================================================

-- create trigger to automatically update updated_at on entry updates
-- this trigger fires before any update operation on the journal_entries table
-- uses the handle_updated_at function created in the profiles migration
create trigger set_updated_at
  before update on public.journal_entries
  for each row
  execute function public.handle_updated_at();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on table public.journal_entries is 'stores all journal entries created by users';
comment on column public.journal_entries.id is 'unique identifier for the journal entry';
comment on column public.journal_entries.user_id is 'references the user who owns this entry';
comment on column public.journal_entries.content is 'the actual text content of the journal entry';
comment on column public.journal_entries.created_at is 'timestamp when the entry was created';
comment on column public.journal_entries.updated_at is 'timestamp when the entry was last updated';
