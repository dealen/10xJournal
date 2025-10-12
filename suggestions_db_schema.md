# Schema Suggestions Aligned With PRD

- **Clarify onboarding responsibilities:** PRD requires a welcome entry and streak tracking, so document the post-signup workflow in the app (create profile → create optional welcome entry → initialise streak row) to avoid regressions after removing triggers.
- **Consider simplifying streak tracking for MVP:** If real-time streak updates prove heavy, store only `current_streak` and recompute `longest_streak` lazily (e.g., nightly function) or defer streak feature until after core CRUD stabilises.
- **Add soft-constraints for journal content:** Enforce `check (length(trim(content)) > 0)` to block empty entries and match the expectation that an entry contains text.
- **Track deletion intent for export prompt:** PRD mentions offering export before account deletion—adding a nullable `pending_delete` flag to `profiles` can help coordinate that flow if handled client-side.
- **Index streak lookups by recency:** When streak badge is displayed on load, consider `create index on public.user_streaks (last_entry_date desc)` to keep reads fast once data grows.
