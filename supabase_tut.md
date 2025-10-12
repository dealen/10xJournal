# Supabase CLI & DB Quick Tutorial

This file collects the most frequently used Supabase CLI and PostgreSQL commands for maintaining your project's database and applying migrations. Tailored for the 10xJournal project (Supabase-backed, no intermediate API).

IMPORTANT: Always run migrations against a local/test Supabase instance first. Keep backups or use a dedicated Supabase project for testing.

## Prerequisites
- Install Supabase CLI: https://supabase.com/docs/guides/cli
- Install psql (PostgreSQL client) or use "supabase db" commands which wrap psql.
- Configure Supabase CLI: `supabase login` and `supabase link` to your project (for remote ops).

---

## 1. Development/local workflow

Start local Supabase (Docker-based) for development:

```bash
# start local supabase (runs Postgres, rest, auth)
supabase start

# stop local supabase when done
supabase stop
```

Apply migrations locally (recommended):

```bash
# run all pending migrations against local db
supabase db push

# run a single SQL migration file manually (when debugging)
psql "$(supabase gen connection-string)" -f ./supabase/migrations/20251012123000_remove_automation_triggers.sql
```

Inspect database schema locally using psql:

```bash
# open psql shell connected to local supabase
psql "$(supabase gen connection-string)"

# list tables
\dt

# describe a table
\d+ public.journal_entries
```

Check RLS policies and functions:

```sql
-- list functions
\df+

-- show policies for a table
select * from pg_policies where schemaname = 'public' and tablename = 'journal_entries';
```

---

## 2. Applying migrations to remote (Supabase hosted)

CAUTION: Make backups or run on a staging project before production.

```bash
# push local migrations to your linked supabase project (remote)
supabase db push --project-ref <your-project-ref>

# alternatively, apply a single SQL file remotely using psql and the remote connection string
psql "$(supabase gen connection-string --project-ref <your-project-ref>)" -f ./supabase/migrations/20251012123000_remove_automation_triggers.sql
```

If you need to run a one-off SQL command remotely (example: drop a trigger):

```bash
psql "$(supabase gen connection-string --project-ref <your-project-ref>)" -c "DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;"
```

---

## 3. Managing functions and triggers

List triggers on a table:

```sql
-- in psql
select event_object_schema, event_object_table, trigger_name from information_schema.triggers where event_object_table = 'profiles';
```

Drop a function (safely):

```sql
-- ensure no dependencies exist first
DROP FUNCTION IF EXISTS public.handle_streak_update();
```

If a function has dependent objects, Postgres will fail the drop; use `DROP ... CASCADE` only if you understand the impact.

---

## 4. Rollbacks and backups

Take a logical dump (recommended before production migration):

```bash
# use pg_dump via the generated connection string
pg_dump --format=custom --file=backup_$(date +%F).dump "$(supabase gen connection-string --project-ref <your-project-ref>)"

# restore to a database (careful: will overwrite data)
pg_restore --clean --if-exists --dbname="$(supabase gen connection-string --project-ref <target-ref>)" backup_2025-10-12.dump
```

---

## 5. Common troubleshooting
- "permission denied": ensure you're using `supabase login` with the right account and the project link is correct.
- "trigger function" errors during user creation: triggers run inside the same transaction as `auth.users` insert — failing triggers will roll back user creation. Temporarily drop triggers (or fix functions) to unblock signup.
- "psql: could not connect": verify `supabase start` is running locally, or that the remote connection string is correct.

---

## 6. Quick commands checklist
- Start local dev stack: `supabase start`
- Push migrations locally: `supabase db push`
- Apply single SQL migration: `psql "$(supabase gen connection-string)" -f ./supabase/migrations/<file>.sql`
- Run single SQL on remote: `psql "$(supabase gen connection-string --project-ref <ref>)" -c "<sql>"`
- Backup DB: `pg_dump --format=custom -f backup.dump "$(supabase gen connection-string --project-ref <ref>)"`

---

## 7. Next steps for 10xJournal
- After applying the migration that removes triggers, update the client onboarding flow so new users trigger explicit `profiles` and `user_streaks` creation.
- Add a small SQL seed script (or client-side code) to create a test user + profile so you can continue development while the triggers are disabled.

If you'd like, I can also produce a small SQL seed script and a sample C# client snippet (Blazor) that performs the required post-signup steps. Let me know which you'd prefer.