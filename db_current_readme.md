# Current Database State

## Core Tables
- `public.profiles`: 1:1 with `auth.users`; columns `id`, `created_at`, `updated_at`; `updated_at` maintained by trigger `set_updated_at` that calls `public.handle_updated_at`.
- `public.journal_entries`: Stores journal content; columns `id`, `user_id`, `content`, `created_at`, `updated_at`; composite index on `(user_id, created_at desc)`; trigger `set_updated_at` keeps `updated_at` fresh via `public.handle_updated_at`.
- `public.user_streaks`: 1:1 with `public.profiles`; tracks `current_streak`, `longest_streak`, `last_entry_date`.

## Row Level Security
- Profiles: `authenticated` role can read own record, `anon` denied.
- Journal entries: `authenticated` role can CRUD own entries, `anon` denied for all operations.
- User streaks: `authenticated` role can read own streak, `anon` denied.

## Database Functions
- `public.handle_updated_at()`: Shared helper that overwrites `updated_at` before updates (used by both tables above).
- `public.handle_new_user()`: Security definer; invoked by `auth.users` trigger to seed `public.profiles` and `public.user_streaks`.
- `public.handle_new_profile()`: Security definer; invoked by profile trigger to insert a welcome entry into `public.journal_entries`.
- `public.handle_streak_update()`: Security definer; invoked by journal entry trigger to maintain streak metrics. Includes defensive creation of a `user_streaks` row when missing.

## Triggers
- `set_updated_at` on `public.profiles` and `public.journal_entries` (before update) calling `public.handle_updated_at()`.
- `on_auth_user_created` on `auth.users` (after insert) calling `public.handle_new_user()`.
- `on_profile_created` on `public.profiles` (after insert) calling `public.handle_new_profile()`.
- `on_journal_entry_created` on `public.journal_entries` (after insert) calling `public.handle_streak_update()`.

## Observed Behavior
- User registration relies on chained triggers: inserting into `auth.users` cascades to `public.profiles`, `public.user_streaks`, and a welcome entry. Errors in any step roll back the full transaction, blocking new users.
- Streak totals are maintained automatically on each entry insert; welcome entry ensures a first record exists.
