-- ============================================================================
-- migration: remove automation triggers
-- description: drops onboarding, welcome entry, streak update, and updated_at
--              triggers plus related helper functions to simplify database
-- ============================================================================

-- drop triggers that run on auth.users and public tables
-- note: use "if exists" so migration stays idempotent when rerun locally

-- remove trigger that auto-created profile and streak records
drop trigger if exists on_auth_user_created on auth.users;

-- remove trigger that auto-created welcome entry
drop trigger if exists on_profile_created on public.profiles;

drop trigger if exists on_journal_entry_created on public.journal_entries;

drop trigger if exists set_updated_at on public.profiles;
drop trigger if exists set_updated_at on public.journal_entries;

drop function if exists public.handle_streak_update();
drop function if exists public.handle_new_profile();
drop function if exists public.handle_new_user();
drop function if exists public.handle_updated_at();
