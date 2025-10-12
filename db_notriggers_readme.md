# Database State Without Automation Triggers

## What Changes
- No automatic onboarding: inserting into `auth.users` no longer seeds `public.profiles` or `public.user_streaks`. The client or a backend job must create those rows explicitly after signup.
- No auto-generated welcome entry: the application decides whether to insert a starter entry.
- No automatic streak maintenance: `public.user_streaks` updates now require explicit writes from application logic or scheduled jobs.
- No automatic `updated_at` maintenance: columns on `public.profiles` and `public.journal_entries` remain static unless updated by application code.

## Simplification Benefits
- Registration can succeed without cross-table triggers that roll back on failure.
- Easier local debugging: all writes are initiated from the application, avoiding security definer complexity.
- Reduced PL/pgSQL surface area removes need for function deployment/tests.

## Responsibilities Shifted to App Layer
- After signup, create `profiles`, `user_streaks`, and optional welcome entry using Supabase client.
- Maintain `updated_at` fields when updating rows.
- Recalculate streak metrics on entry creation/edit/delete if the feature remains in scope.

## Follow-Up Considerations
- Decide whether streak tracking remains in MVP; if yes, design client-side update flow or lightweight Supabase RPC.
- Provide migration or script to backfill data for existing users if triggers have been previously used.
- Update onboarding documentation so developers know which Supabase calls are required when creating accounts manually.
