# 10xJournal Authentication & Database Flow Diagrams

## Registration Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          REGISTRATION FLOW                                │
└──────────────────────────────────────────────────────────────────────────┘

USER                    CLIENT                  SUPABASE AUTH        DATABASE
  │                       │                           │                  │
  │  1. Submit Form       │                           │                  │
  │  (email, password)    │                           │                  │
  ├──────────────────────>│                           │                  │
  │                       │                           │                  │
  │                       │  2. Auth.SignUp()        │                  │
  │                       ├─────────────────────────>│                  │
  │                       │                           │                  │
  │                       │                           │  3. Create User  │
  │                       │                           │  in auth.users   │
  │                       │                           ├─────────────────>│
  │                       │                           │                  │
  │                       │                           │  4. User ID      │
  │                       │                           │<─────────────────┤
  │                       │                           │                  │
  │                       │  5. Session + User ID     │                  │
  │                       │<─────────────────────────┤                  │
  │                       │                           │                  │
  │                       │  6. RPC: initialize_new_user(user_id)       │
  │                       ├────────────────────────────────────────────>│
  │                       │                           │                  │
  │                       │                           │   7. INSERT INTO │
  │                       │                           │   profiles (id)  │
  │                       │                           │   (BYPASSES RLS) │
  │                       │                           │<─────────────────┤
  │                       │                           │                  │
  │                       │                           │   8. INSERT INTO │
  │                       │                           │   user_streaks   │
  │                       │                           │   (BYPASSES RLS) │
  │                       │                           │<─────────────────┤
  │                       │                           │                  │
  │                       │  9. Success JSON          │                  │
  │                       │<────────────────────────────────────────────┤
  │                       │                           │                  │
  │                       │  10. SetSession()         │                  │
  │                       │  (if tokens available)    │                  │
  │                       ├─────────────────────────>│                  │
  │                       │                           │                  │
  │  11. Success          │                           │                  │
  │  Redirect to app      │                           │                  │
  │<──────────────────────┤                           │                  │
  │                       │                           │                  │

Notes:
- initialize_new_user() uses SECURITY DEFINER to bypass RLS
- Function is idempotent (ON CONFLICT DO NOTHING)
- If email confirmation required, session tokens may be null
```

## Login Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                             LOGIN FLOW                                    │
└──────────────────────────────────────────────────────────────────────────┘

USER                    CLIENT                  SUPABASE AUTH        DATABASE
  │                       │                           │                  │
  │  1. Submit Form       │                           │                  │
  │  (email, password)    │                           │                  │
  ├──────────────────────>│                           │                  │
  │                       │                           │                  │
  │                       │  2. Auth.SignIn()         │                  │
  │                       ├─────────────────────────>│                  │
  │                       │                           │                  │
  │                       │                           │  3. Validate     │
  │                       │                           │  Credentials     │
  │                       │                           ├─────────────────>│
  │                       │                           │                  │
  │                       │                           │  4. User Found   │
  │                       │                           │<─────────────────┤
  │                       │                           │                  │
  │                       │  5. Session + Tokens      │                  │
  │                       │  (access + refresh)       │                  │
  │                       │<─────────────────────────┤                  │
  │                       │                           │                  │
  │                       │  6. Store Session         │                  │
  │                       │  (via SessionPersistence) │                  │
  │                       │                           │                  │
  │  7. Success           │                           │                  │
  │  Redirect to app      │                           │                  │
  │<──────────────────────┤                           │                  │
  │                       │                           │                  │

Notes:
- Password validation handled by Supabase Auth (secure)
- Session automatically persisted in browser storage
- Tokens automatically refreshed by Supabase client
```

## Data Access Flow (After Login)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        DATA ACCESS WITH RLS                               │
└──────────────────────────────────────────────────────────────────────────┘

CLIENT                  SUPABASE                DATABASE (RLS ENABLED)
  │                       │                           │
  │  1. Query Entries     │                           │
  │  (authenticated)      │                           │
  ├──────────────────────>│                           │
  │                       │                           │
  │                       │  2. SELECT FROM           │
  │                       │  journal_entries          │
  │                       │  (with auth context)      │
  │                       ├─────────────────────────>│
  │                       │                           │
  │                       │                           │  3. RLS Check:
  │                       │                           │  WHERE auth.uid()
  │                       │                           │  = user_id
  │                       │                           │
  │                       │  4. Only User's Entries   │
  │                       │<─────────────────────────┤
  │                       │                           │
  │  5. Entries Data      │                           │
  │<──────────────────────┤                           │
  │                       │                           │

Notes:
- RLS automatically filters data based on auth.uid()
- User A can NEVER see User B's data
- No data leakage possible with correct RLS policies
```

## Database Schema ERD

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        DATABASE SCHEMA (ERD)                              │
└──────────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│   auth.users        │  (Supabase Managed)
│─────────────────────│
│ id (uuid) PK        │
│ email               │
│ encrypted_password  │
│ created_at          │
│ ...                 │
└─────────────────────┘
          │
          │ 1:1
          │ ON DELETE CASCADE
          ▼
┌─────────────────────┐
│  public.profiles    │
│─────────────────────│
│ id (uuid) PK, FK    │◄───────────────┐
│ created_at          │                │
│ updated_at          │                │ 1:1
└─────────────────────┘                │ ON DELETE CASCADE
          │                            │
          │ 1:many                     │
          │ ON DELETE CASCADE          │
          ▼                            │
┌─────────────────────┐      ┌────────────────────┐
│ journal_entries     │      │  user_streaks      │
│─────────────────────│      │────────────────────│
│ id (uuid) PK        │      │ user_id (uuid) PK  │
│ user_id (uuid) FK   │      │ current_streak     │
│ content (text)      │      │ longest_streak     │
│ created_at          │      │ last_entry_date    │
│ updated_at          │      └────────────────────┘
└─────────────────────┘

Indexes:
- journal_entries: (user_id, created_at DESC) [composite]

RLS Policies:
- All tables: Authenticated users can only access own data
- All tables: Anonymous users explicitly denied
```

## RLS Policy Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    RLS POLICY EVALUATION                                  │
└──────────────────────────────────────────────────────────────────────────┘

Query: SELECT * FROM journal_entries WHERE user_id = 'some-uuid'
                            │
                            ▼
           ┌────────────────────────────────┐
           │   Is RLS Enabled?              │
           │   (Yes, for all tables)        │
           └────────────────────────────────┘
                            │
                            ▼
           ┌────────────────────────────────┐
           │   Get User Role                │
           │   - authenticated?             │
           │   - anon?                      │
           └────────────────────────────────┘
                            │
        ┌───────────────────┴───────────────────┐
        ▼                                       ▼
┌──────────────────┐                  ┌──────────────────┐
│  authenticated   │                  │     anon         │
└──────────────────┘                  └──────────────────┘
        │                                       │
        ▼                                       ▼
Apply policy:                          Apply policy:
"authenticated users                   "anonymous users
can view their own                     cannot view
entries"                               entries"
        │                                       │
        ▼                                       ▼
USING (auth.uid()                      USING (false)
= user_id)                                     │
        │                                       ▼
        ▼                                  RETURN []
Check: auth.uid()                         (empty result)
matches user_id?                               
        │
    ┌───┴───┐
    ▼       ▼
  YES      NO
    │       │
    ▼       ▼
RETURN   RETURN []
rows    (empty)

Notes:
- RLS is checked for EVERY query automatically
- No way to bypass RLS except SECURITY DEFINER functions
- Even if client sends malicious query, RLS blocks it
```

## Account Deletion Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      ACCOUNT DELETION FLOW                                │
└──────────────────────────────────────────────────────────────────────────┘

USER                CLIENT             DATABASE (RPC)           AUTH
  │                   │                      │                    │
  │  1. Request       │                      │                    │
  │  Delete Account   │                      │                    │
  ├──────────────────>│                      │                    │
  │                   │                      │                    │
  │                   │  2. RPC:             │                    │
  │                   │  delete_my_account() │                    │
  │                   ├─────────────────────>│                    │
  │                   │                      │                    │
  │                   │                      │  3. Validate       │
  │                   │                      │  auth.uid()        │
  │                   │                      │                    │
  │                   │                      │  4. DELETE FROM    │
  │                   │                      │  journal_entries   │
  │                   │                      │                    │
  │                   │                      │  5. DELETE FROM    │
  │                   │                      │  user_streaks      │
  │                   │                      │                    │
  │                   │                      │  6. DELETE FROM    │
  │                   │                      │  profiles          │
  │                   │                      │                    │
  │                   │                      │  7. DELETE FROM    │
  │                   │                      │  auth.users        │
  │                   │                      ├───────────────────>│
  │                   │                      │                    │
  │                   │                      │  8. Cascade Delete │
  │                   │                      │  auth sessions     │
  │                   │                      │<───────────────────┤
  │                   │                      │                    │
  │                   │  9. Success JSON     │                    │
  │                   │<─────────────────────┤                    │
  │                   │                      │                    │
  │  10. Goodbye      │                      │                    │
  │<──────────────────┤                      │                    │
  │                   │                      │                    │

Notes:
- Uses SECURITY DEFINER to allow deletion from auth.users
- All related data deleted via cascade constraints
- Operation is atomic (transaction)
- Returns success/failure JSON
```

## Security Layers

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    SECURITY LAYERS (DEFENSE IN DEPTH)                     │
└──────────────────────────────────────────────────────────────────────────┘

LAYER 1: Network
├─ HTTPS/TLS encryption
└─ Secure WebSocket connections

LAYER 2: Authentication
├─ Supabase Auth (industry standard)
├─ JWT tokens (signed and verified)
├─ Automatic token refresh
└─ Session management

LAYER 3: Application
├─ Input validation (client-side)
├─ Error handling
├─ No sensitive data in logs
└─ AuthErrorMapper (friendly messages)

LAYER 4: Database RLS
├─ auth.uid() validation
├─ User can only access own data
├─ Anonymous explicitly denied
└─ Policies on SELECT, INSERT, UPDATE, DELETE

LAYER 5: Database Functions
├─ SECURITY DEFINER used minimally
├─ Input validation in functions
├─ Idempotent operations
└─ Error handling with EXCEPTION blocks

LAYER 6: Foreign Keys & Constraints
├─ ON DELETE CASCADE (data integrity)
├─ NOT NULL constraints
├─ Primary/Foreign key relationships
└─ Type safety (uuid, timestamptz, etc.)
```

---

**Generated:** October 20, 2025  
**Tool:** GitHub Copilot  
**Purpose:** Visual documentation of 10xJournal authentication and database flows
