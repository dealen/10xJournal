# Encryption Feature Implementation Plan

## Overview

This document outlines the implementation plan for adding end-to-end encryption to journal entries for premium/paid users. The feature enables users to encrypt their journal content client-side before it's sent to Supabase, ensuring zero-knowledge architecture where even the database cannot read the plaintext content.

## User Story

**As a** premium journal user  
**I want** my journal entries to be encrypted so that only I can read them with my password  
**So that** my private thoughts remain completely confidential, even from the database/server

## Technical Architecture

### Encryption Model: Client-Side Zero-Knowledge

```
User Password (Login)
    ↓
PBKDF2-SHA256 (600k iterations) + Salt
    ↓
256-bit Encryption Key
    ↓
AES-256-GCM Encryption
    ↓
Base64 Encoded Ciphertext → Stored in Database
```

**Key Principles:**
1. **Client-Side Only**: All encryption/decryption happens in the browser
2. **Zero-Knowledge**: Server never sees plaintext or encryption key
3. **Password-Derived Keys**: Encryption key derived from user's login password
4. **Authenticated Encryption**: AES-GCM provides both encryption and tampering detection
5. **Session-Based**: Encryption password stored in memory during active session only

### Data Format

Encrypted entries are stored as Base64-encoded strings containing:
```
[Salt (16 bytes)] + [Nonce (12 bytes)] + [Ciphertext (variable)] + [Auth Tag (16 bytes)]
```

## Database Schema Changes

### 1. Add Encryption Column to Profiles Table

```sql
-- Migration: Add encryption support to user profiles
ALTER TABLE public.profiles 
ADD COLUMN encryption_enabled boolean NOT NULL DEFAULT false;

-- Create index for efficient querying
CREATE INDEX idx_profiles_encryption_enabled 
ON public.profiles(encryption_enabled) 
WHERE encryption_enabled = true;

-- Add comment for documentation
COMMENT ON COLUMN public.profiles.encryption_enabled IS 
'Indicates if user has encryption enabled for journal entries. Premium feature only.';
```

### 2. Update UserProfile Model

```csharp
// File: 10xJournal.Client/Shared/Models/UserProfile.cs

/// <summary>
/// Indicates if the user has encryption enabled for their entries.
/// Only premium/paid users can encrypt their journal entries.
/// </summary>
[Column("encryption_enabled")]
public bool EncryptionEnabled { get; set; } = false;
```

## Implementation Steps

### Step 1: Create Encryption Service (New Feature Slice)

**File:** `10xJournal.Client/Features/Encryption/EncryptionService.cs`

**Purpose:** Handles all encryption and decryption operations using AES-256-GCM.

**Key Methods:**
- `string Encrypt(string plaintext, string userPassword)` - Encrypts content using password-derived key
- `string Decrypt(string encryptedContent, string userPassword)` - Decrypts content and validates authentication tag
- `byte[] DeriveKey(string password, byte[] salt)` - Derives encryption key using PBKDF2-SHA256

**Algorithm Details:**
- **Cipher**: AES-256-GCM (Galois/Counter Mode)
- **Key Derivation**: PBKDF2-SHA256 with 600,000 iterations (OWASP 2023 recommendation)
- **Key Size**: 256 bits
- **Nonce**: 96 bits (random, unique per encryption)
- **Authentication Tag**: 128 bits

**Error Handling:**
- Throw `CryptographicException` if decryption fails (wrong password or tampered data)
- Validate all inputs for null/empty values
- Clear sensitive data from memory after use

### Step 2: Create Password Storage Service

**File:** `10xJournal.Client/Features/Encryption/PasswordStorageService.cs`

**Purpose:** Manages secure in-memory storage of the encryption password during user session.

**Key Methods:**
- `void StorePassword(string password)` - Stores password after successful login
- `string? GetPassword()` - Retrieves stored password for encryption operations
- `void ClearPassword()` - Clears password from memory on logout
- `bool HasPassword()` - Checks if password is available

**Security Considerations:**
- Password stored in managed memory (C# string) - not persisted to disk
- Cleared on logout/session end
- No password recovery mechanism (by design)

### Step 3: Register Services in DI Container

**File:** `10xJournal.Client/Program.cs`

```csharp
// Add encryption services
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<PasswordStorageService>();
```

### Step 4: Update Login Flow to Store Password

**File:** `10xJournal.Client/Features/Authentication/Login/Login.razor`

**Changes:**
1. Inject `PasswordStorageService`
2. After successful authentication, call `PasswordStorage.StorePassword(request.Password)`
3. Ensure password is stored BEFORE navigation to journal

**Critical:** Password must be stored immediately after Supabase authentication succeeds, as it's needed for decrypting existing entries.

### Step 5: Update Logout Flow to Clear Password

**File:** `10xJournal.Client/Features/Authentication/Logout/LogoutHandler.cs`

**Changes:**
1. Inject `PasswordStorageService`
2. Call `PasswordStorage.ClearPassword()` before clearing session
3. Ensure password is cleared even if logout fails

### Step 6: Update EntryEditor Component

**File:** `10xJournal.Client/Features/JournalEntries/EditEntry/EntryEditor.razor`

**New Dependencies:**
- `@inject EncryptionService EncryptionService`
- `@inject PasswordStorageService PasswordStorage`

**New State Fields:**
```csharp
private bool _encryptionEnabled = false; // User's encryption setting
private bool _isEncrypted = false;       // Current entry encryption status
```

**Changes to `OnInitializedAsync()`:**
1. Fetch current user's profile to check `EncryptionEnabled` flag
2. Store in `_encryptionEnabled` field
3. Use this to determine whether to encrypt new entries

**Changes to `LoadEntryAsync()`:**
1. Check if loaded content is Base64-encoded (encrypted)
2. If encrypted and user has encryption enabled:
   - Get password from `PasswordStorage`
   - If password available: decrypt content
   - If password missing: show error "Encryption password required. Please re-login."
3. Handle `CryptographicException` (wrong password) gracefully

**Changes to `AutoSaveAsync()`:**
1. Before saving, check if `_encryptionEnabled` is true
2. If enabled:
   - Get password from `PasswordStorage`
   - If no password: set error status and abort
   - Encrypt content using `EncryptionService.Encrypt()`
   - Save encrypted content to database
3. If disabled: save plaintext as before

**Helper Method:**
```csharp
private bool IsBase64String(string value)
{
    // Check if string is valid Base64
    // Used to detect encrypted entries
}
```

### Step 7: Update ListEntries Component (Optional Enhancement)

**File:** `10xJournal.Client/Features/JournalEntries/ListEntries/ListEntries.razor`

**Considerations:**
- Encrypted entries cannot show preview text (it's gibberish)
- Option 1: Show placeholder text "🔒 Encrypted Entry" for encrypted entries
- Option 2: Decrypt previews client-side (performance cost)
- Recommendation: Start with Option 1, add Option 2 later if needed

**Detection Logic:**
```csharp
private bool IsEntryEncrypted(string content)
{
    return IsBase64String(content);
}
```

### Step 8: Add User Settings UI for Encryption

**File:** `10xJournal.Client/Features/Settings/Settings.razor`

**New Section:** "Privacy & Encryption"

**UI Elements:**
1. Toggle switch: "Enable Encryption" (bound to user profile)
2. Warning message about password reset consequences
3. Save button to persist setting

**Warning Text:**
```
⚠️ Important: If you enable encryption and forget your password, 
your encrypted entries will be permanently unrecoverable. 
Password reset will NOT restore access to encrypted content.
```

**Save Logic:**
```csharp
private async Task SaveEncryptionSettingAsync()
{
    await SupabaseClient
        .From<UserProfile>()
        .Where(x => x.Id == currentUserId)
        .Set(x => x.EncryptionEnabled, _encryptionEnabled)
        .Update();
}
```

### Step 9: Migration Path for Existing Users

**Considerations:**
- Existing unencrypted entries remain unencrypted
- When user enables encryption, only NEW entries are encrypted
- User must manually re-save old entries to encrypt them (future enhancement)

**Future Enhancement:** Bulk encryption tool in settings to encrypt all existing entries

## Security Model & Guarantees

### What IS Protected:
✅ Content is encrypted before leaving the browser  
✅ Database stores only ciphertext  
✅ Server/Supabase cannot read journal content  
✅ Tampering with ciphertext is detected (authenticated encryption)  
✅ Each entry has unique encryption (random salt + nonce)  

### What IS NOT Protected:
❌ Metadata (entry ID, user ID, timestamps, entry count)  
❌ User forgets password = permanent data loss  
❌ Browser memory dumps (encryption key exists in memory during session)  
❌ Malicious browser extensions with full page access  
❌ XSS attacks (can access in-memory password)  

### Threat Model:
- **Protected Against:** Database breach, curious administrators, subpoena of database
- **NOT Protected Against:** Compromised client device, browser vulnerabilities, active session attacks

## Testing Requirements

### Unit Tests

**File:** `10xJournal.Client.Tests/Features/Encryption/EncryptionServiceTests.cs`

Test cases:
1. ✅ Encrypt and decrypt returns original plaintext
2. ✅ Wrong password throws CryptographicException
3. ✅ Tampered ciphertext throws CryptographicException
4. ✅ Empty plaintext throws ArgumentException
5. ✅ Null password throws ArgumentException
6. ✅ Different passwords produce different ciphertexts
7. ✅ Same content produces different ciphertexts (unique nonce/salt)
8. ✅ Large content (10KB+) encrypts/decrypts correctly

**File:** `10xJournal.Client.Tests/Features/Encryption/PasswordStorageServiceTests.cs`

Test cases:
1. ✅ Store and retrieve password
2. ✅ Clear password removes it from memory
3. ✅ HasPassword returns correct status
4. ✅ GetPassword returns null when not set

### Integration Tests

**File:** `10xJournal.Client.Tests/Features/JournalEntries/EncryptedEntryIntegrationTests.cs`

Test cases:
1. ✅ Create encrypted entry and retrieve (same user)
2. ✅ User A cannot decrypt User B's entries (RLS + wrong password)
3. ✅ Entry remains encrypted in database (verify ciphertext is Base64)
4. ✅ Disable encryption allows reading old encrypted entries
5. ✅ Enable encryption encrypts new entries
6. ✅ Session timeout clears password, prevents decryption

### E2E Tests

**File:** `10xJournal.E2E.Tests/Features/Encryption/EncryptedJournalFlowTests.cs`

Test scenarios:
1. ✅ User enables encryption → Creates entry → Logs out → Logs in → Reads entry
2. ✅ User with encryption creates entry → Entry content is encrypted in database
3. ✅ User without encryption sees normal plaintext entries
4. ✅ Password prompt appears when password not in memory
5. ✅ Wrong password shows decryption error

## Known Limitations & Trade-offs

### 1. Password Reset = Data Loss
**Impact:** If user forgets password, encrypted entries are permanently unrecoverable  
**Mitigation:** Clear warning in UI when enabling encryption  
**Alternative:** Could implement key escrow (conflicts with zero-knowledge principle)

### 2. No Server-Side Search
**Impact:** Encrypted entries cannot be searched via database queries  
**Mitigation:** Client-side search after decrypting all entries  
**Performance:** Acceptable for MVP with limited entries

### 3. Session-Only Decryption
**Impact:** Browser close = must re-login to decrypt  
**Mitigation:** Consider IndexedDB storage with browser encryption (future)  
**Security Trade-off:** Longer persistence = higher risk

### 4. No Password Change Support (V1)
**Impact:** Cannot change encryption password without re-encrypting all entries  
**Mitigation:** Future enhancement with bulk re-encryption tool  
**Complexity:** Must decrypt all entries with old password, encrypt with new

### 5. Performance Overhead
**Impact:** Encryption/decryption adds computational cost  
**Measured Impact:** ~1-5ms per entry (acceptable for text content)  
**Mitigation:** Consider Web Workers for large batch operations (future)

### 6. Mobile Browser Considerations
**Impact:** Mobile browsers may clear memory more aggressively  
**Mitigation:** Detect session loss, prompt for password  
**Testing:** Extensive mobile browser testing required

## Future Enhancements

### Phase 2 (Post-MVP):
1. **Bulk Re-encryption Tool**: Encrypt all existing plaintext entries
2. **Password Change Flow**: Re-encrypt all entries with new password
3. **Client-Side Search**: Decrypt and search entries in browser
4. **IndexedDB Caching**: Persist encrypted content locally for offline access
5. **Key Recovery Options**: Optional recovery key or trusted contact recovery
6. **Encryption Indicators**: Visual indicators in UI for encrypted vs plaintext entries

### Phase 3 (Advanced):
1. **Multiple Device Support**: Sync encryption key across devices via QR code
2. **Export Encrypted Backup**: Download encrypted backup file
3. **Selective Encryption**: Choose which entries to encrypt
4. **Performance Optimization**: Web Workers for batch encryption/decryption

## Migration & Rollout Plan

### Phase 1: Foundation (Week 1)
- [ ] Database migration: Add `encryption_enabled` column
- [ ] Implement `EncryptionService` with full test coverage
- [ ] Implement `PasswordStorageService` with tests
- [ ] Register services in DI container

### Phase 2: Core Integration (Week 2)
- [ ] Update Login flow to store password
- [ ] Update Logout flow to clear password
- [ ] Update EntryEditor for encryption support
- [ ] Add encryption detection logic

### Phase 3: UI & Settings (Week 3)
- [ ] Add encryption toggle to Settings page
- [ ] Add warning messages and documentation
- [ ] Update ListEntries to handle encrypted previews
- [ ] Add encryption status indicators

### Phase 4: Testing & Validation (Week 4)
- [ ] Complete unit test coverage
- [ ] Integration tests for encrypted entries
- [ ] E2E tests for full user flow
- [ ] Security review and penetration testing
- [ ] Performance testing with large entry counts

### Phase 5: Documentation & Launch (Week 5)
- [ ] User documentation on encryption feature
- [ ] FAQ on password loss and recovery
- [ ] Admin documentation
- [ ] Soft launch to beta users
- [ ] Monitor for issues and gather feedback

## Success Metrics

1. **Security:** Zero plaintext entries in database for encryption-enabled users
2. **Reliability:** <0.1% decryption failures (excluding wrong password)
3. **Performance:** <5ms encryption overhead per entry
4. **User Experience:** <1% of users report password storage issues
5. **Adoption:** Track % of premium users enabling encryption

## Documentation Requirements

1. **User Guide:** "How to Enable Encryption and Protect Your Journal"
2. **Security Whitepaper:** Technical details of encryption implementation
3. **FAQ:** Common questions about encryption, password loss, etc.
4. **Developer Docs:** Encryption service API documentation
5. **Migration Guide:** For users upgrading to encryption

## Conclusion

This implementation provides strong client-side encryption for journal entries while maintaining the project's vertical slice architecture and simplicity principles. The zero-knowledge design ensures maximum privacy for users willing to accept the trade-off of permanent data loss if they forget their password.

The phased rollout allows for thorough testing and validation before full release, and the defined extension points enable future enhancements without architectural changes.
