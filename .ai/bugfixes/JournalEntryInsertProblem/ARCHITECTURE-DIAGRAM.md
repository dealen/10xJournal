# Visual Architecture Diagram - Bug Fixes

## 🎯 Problem → Solution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     ORIGINAL PROBLEM                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  User Registers                                                  │
│       ↓                                                          │
│  ✅ auth.users created                                          │
│       ↓                                                          │
│  ❌ NO profile created (trigger removed!)                       │
│       ↓                                                          │
│  User Logs In                                                    │
│       ↓                                                          │
│  ✅ Login succeeds                                              │
│       ↓                                                          │
│  ❌ Session NOT saved to localStorage                           │
│       ↓                                                          │
│  Component Re-renders                                            │
│       ↓                                                          │
│  ❌ Session LOST from memory                                    │
│       ↓                                                          │
│  User Types in Editor                                            │
│       ↓                                                          │
│  Auto-Save Triggers                                              │
│       ↓                                                          │
│  ❌ 401 Unauthorized (no JWT token!)                            │
│       ↓                                                          │
│  (Even if token existed)                                         │
│       ↓                                                          │
│  ❌ 409 Conflict (profile missing!)                             │
│       ↓                                                          │
│  💥 JOURNAL ENTRY CREATION FAILS                                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     FIXED SOLUTION                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  User Registers                                                  │
│       ↓                                                          │
│  ✅ auth.users created                                          │
│       ↓                                                          │
│  ✅ profiles created (NEW FIX #2!)                              │
│       ↓                                                          │
│  ✅ user_streaks created (NEW FIX #2!)                          │
│       ↓                                                          │
│  User Logs In                                                    │
│       ↓                                                          │
│  ✅ Login succeeds                                              │
│       ↓                                                          │
│  ✅ Session SAVED to localStorage (NEW FIX #1!)                 │
│       ↓                                                          │
│  Page Refresh / Component Re-render                              │
│       ↓                                                          │
│  ✅ Session RESTORED from localStorage (NEW FIX #1!)            │
│       ↓                                                          │
│  User Types in Editor                                            │
│       ↓                                                          │
│  Auto-Save Triggers                                              │
│       ↓                                                          │
│  ✅ JWT token present (session persisted!)                      │
│       ↓                                                          │
│  ✅ user_id valid (profile exists!)                             │
│       ↓                                                          │
│  ✅ Foreign key constraint passes                                │
│       ↓                                                          │
│  ✅ RLS policy passes (authenticated!)                          │
│       ↓                                                          │
│  🎉 JOURNAL ENTRY CREATED SUCCESSFULLY                          │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Relationship Diagram

```
┌─────────────────────────┐
│     auth.users          │  (Supabase Auth Schema)
│  ───────────────────    │
│  • id (PK)              │
│  • email                │
│  • created_at           │
└─────────┬───────────────┘
          │
          │ 1:1 relationship
          │ (profile required!)
          ↓
┌─────────────────────────┐
│      profiles           │  (Public Schema)
│  ───────────────────    │
│  • id (PK, FK)          │  ← References auth.users.id
│  • created_at           │
│  • updated_at           │
└─────────┬───────────────┘
          │
          │ 1:many relationship
          │ (foreign key constraint!)
          ↓
┌─────────────────────────┐
│   journal_entries       │  (Public Schema)
│  ───────────────────    │
│  • id (PK)              │
│  • user_id (FK)         │  ← References profiles.id
│  • content              │
│  • created_at           │
│  • updated_at           │
└─────────────────────────┘

┌─────────────────────────┐
│    user_streaks         │  (Public Schema)
│  ───────────────────    │
│  • user_id (PK, FK)     │  ← References profiles.id
│  • current_streak       │
│  • longest_streak       │
│  • last_entry_date      │
└─────────────────────────┘
```

**Key Constraint:**
```sql
journal_entries.user_id → profiles.id
```
❌ **Cannot insert journal entry if profile doesn't exist!**  
✅ **Fixed by creating profile during registration**

---

## 🔐 Authentication Flow

### Before Fix (BROKEN)
```
┌──────────────────┐
│   User Login     │
└────────┬─────────┘
         │
         ↓
┌──────────────────────────────┐
│  Supabase.Auth.SignIn()      │
│  Returns: Session with JWT   │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  Session stored in MEMORY    │  ❌ Lost on refresh!
│  (not persisted)             │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  Component Re-renders        │
│  Session LOST                │  💥 Problem!
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  API Call                    │
│  No JWT token in headers     │  ❌ 401 Unauthorized
└──────────────────────────────┘
```

### After Fix (WORKING)
```
┌──────────────────┐
│   User Login     │
└────────┬─────────┘
         │
         ↓
┌──────────────────────────────┐
│  Supabase.Auth.SignIn()      │
│  Returns: Session with JWT   │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  BlazorSessionPersistence    │  ✅ NEW!
│  SaveSession()               │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  localStorage                │  ✅ Persisted!
│  Key: supabase.auth.token    │
│  Value: {session JSON}       │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  Page Refresh / App Startup  │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  BlazorSessionPersistence    │  ✅ NEW!
│  LoadSessionAsync()          │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  Auth.SetSession()           │  ✅ Restored!
│  JWT tokens restored         │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  API Call                    │
│  JWT token in headers        │  ✅ Authenticated!
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  RLS Policy Check            │  ✅ auth.uid() valid
│  Foreign Key Check           │  ✅ profile exists
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  🎉 SUCCESS                  │
└──────────────────────────────┘
```

---

## 👤 User Registration Flow

### Before Fix (INCOMPLETE)
```
┌─────────────────────────┐
│  RegisterForm.razor     │
└──────────┬──────────────┘
           │
           ↓
┌─────────────────────────┐
│  SupabaseAuthService    │
│  RegisterAsync()        │
└──────────┬──────────────┘
           │
           ↓
┌─────────────────────────┐
│  Auth.SignUp()          │
└──────────┬──────────────┘
           │
           ↓
┌─────────────────────────┐
│  ✅ auth.users created  │
└─────────────────────────┘

❌ MISSING: profiles record
❌ MISSING: user_streaks record
💥 Result: Foreign key error later!
```

### After Fix (COMPLETE)
```
┌─────────────────────────┐
│  RegisterForm.razor     │
└──────────┬──────────────┘
           │
           ↓
┌─────────────────────────┐
│  SupabaseAuthService    │
│  RegisterAsync()        │  ✅ ENHANCED!
└──────────┬──────────────┘
           │
           ↓
┌─────────────────────────────────────┐
│  Step 1: Auth.SignUp()              │
│  → Creates user in auth.users       │
│  → Returns user ID                  │
└──────────┬──────────────────────────┘
           │
           ↓
┌─────────────────────────────────────┐
│  Step 2: Insert Profile             │  ✅ NEW!
│  → From<UserProfile>().Insert()     │
│  → Creates record in profiles       │
└──────────┬──────────────────────────┘
           │
           ↓
┌─────────────────────────────────────┐
│  Step 3: Insert Streak              │  ✅ NEW!
│  → From<UserStreak>().Insert()      │
│  → Creates record in user_streaks   │
└──────────┬──────────────────────────┘
           │
           ↓
┌─────────────────────────────────────┐
│  ✅ auth.users created              │
│  ✅ profiles created                │
│  ✅ user_streaks created            │
└─────────────────────────────────────┘

🎉 Result: All foreign keys satisfied!
```

---

## 🔄 Complete User Journey (Fixed)

```
1. REGISTRATION
   └→ SupabaseAuthService.RegisterAsync()
      ├→ Create auth.users
      ├→ Create profiles        ✅ NEW!
      └→ Create user_streaks    ✅ NEW!

2. LOGIN
   └→ Auth.SignIn()
      ├→ Returns session with JWT
      └→ BlazorSessionPersistence.SaveSession()  ✅ NEW!
         └→ localStorage['supabase.auth.token'] = session

3. APP STARTUP (page refresh)
   └→ Program.cs initialization
      └→ BlazorSessionPersistence.LoadSessionAsync()  ✅ NEW!
         ├→ Read from localStorage
         └→ Auth.SetSession(tokens)

4. CREATE JOURNAL ENTRY
   └→ EntryEditor.razor
      ├→ User types content
      ├→ 1 second debounce
      └→ AutoSaveAsync()
         ├→ Get CurrentUserId (from session)  ✅ Works!
         ├→ Build JournalEntry with UserId
         └→ From<JournalEntry>().Insert()
            ├→ JWT token in headers           ✅ Works!
            ├→ RLS policy check passes        ✅ Works!
            ├→ Foreign key check passes       ✅ Works!
            └→ Entry created                  🎉 SUCCESS!

5. PAGE REFRESH
   └→ Session restored from localStorage      ✅ Works!
      └→ User stays logged in                 ✅ Works!
```

---

## 📊 Error Code Quick Reference

| Code | Type | Meaning | Fix |
|------|------|---------|-----|
| `401` | HTTP | Unauthorized | Session lost → Persistence fix |
| `409` | HTTP | Conflict | Constraint violation → Profile fix |
| `23503` | PostgreSQL | Foreign Key | Missing parent record → Profile fix |
| `42501` | PostgreSQL | Insufficient Privilege | RLS policy failure → Session fix |

---

## ✅ Verification Checklist

```
After Registration:
  ☑ Record in auth.users
  ☑ Record in profiles (matching ID)
  ☑ Record in user_streaks (matching ID)

After Login:
  ☑ Session token in localStorage
  ☑ User redirected to journal

After Page Refresh:
  ☑ User still logged in
  ☑ Session token still in localStorage

After Creating Entry:
  ☑ Auto-save indicator shows "Saved"
  ☑ No console errors
  ☑ Entry appears in database
  ☑ user_id matches profile ID
```

---

**This diagram is part of the bug fix documentation.**  
**For detailed explanations, see:**
- `00-SUMMARY.md`
- `session-persistence-fix.md`
- `foreign-key-constraint-fix.md`
- `QUICK-START.md`
