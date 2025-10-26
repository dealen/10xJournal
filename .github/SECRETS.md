# GitHub Secrets Configuration

This document lists all required GitHub Secrets for CI/CD workflows.

## 📋 Required Secrets

### For PR Validation (Test Environment)

These secrets are used by the `pr-validation.yml` workflow to run tests.

| Secret Name | Description | Where to Get It | Example Value |
|-------------|-------------|-----------------|---------------|
| `DEV_SUPABASE_URL` | Development Supabase project URL | Supabase Dashboard → Settings → API | `https://xxxxx.supabase.co` |
| `DEV_SUPABASE_ANON_KEY` | Development Supabase anon/public key | Supabase Dashboard → Settings → API | `eyJhbGc...` |
| `DEV_SUPABASE_SERVICE_ROLE_KEY` | Development Supabase service role key (⚠️ **Never expose to client!**) | Supabase Dashboard → Settings → API | `eyJhbGc...` |
| `TEST_USER_EMAIL` | Test user email for integration tests | Create a test user in your dev Supabase | `test@10xjournal.app` |
| `TEST_USER_PASSWORD` | Test user password for integration tests | Password used when creating test user | `SecurePassword123!` |

### For Production Deployment

These secrets are used by the `deploy-production.yml` workflow.

| Secret Name | Description | Where to Get It | Example Value |
|-------------|-------------|-----------------|---------------|
| `PROD_SUPABASE_URL` | Production Supabase project URL | Supabase Dashboard → Settings → API | `https://yyyyy.supabase.co` |
| `PROD_SUPABASE_ANON_KEY` | Production Supabase anon/public key | Supabase Dashboard → Settings → API | `eyJhbGc...` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Azure Static Web App deployment token | Run: `az staticwebapp secrets list --name <swa-name> --resource-group <rg-name> --query "properties.apiKey" -o tsv` | Long token string |

---

## 🔒 Security Best Practices

### ✅ DO
- ✅ Use different Supabase projects for dev/test and production
- ✅ Rotate service role keys periodically
- ✅ Use strong passwords for test users
- ✅ Keep service role keys secret (never expose to client-side code)
- ✅ Use environment-specific secrets (`DEV_*` vs `PROD_*`)

### ❌ DON'T
- ❌ Never commit secrets to git
- ❌ Never use production credentials in tests
- ❌ Never expose service role keys to the client
- ❌ Don't share the same test user across different environments
- ❌ Don't use weak passwords for test accounts

---

## 📍 How to Add Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the **Name** and **Value**
5. Click **Add secret**

---

## 🧪 Current Configuration Status

Based on the screenshots provided, you have configured:

### ✅ Test Environment Secrets (5 secrets)
- DEV_SUPABASE_ANON_KEY
- DEV_SUPABASE_SERVICE_ROLE_KEY
- DEV_SUPABASE_URL
- TEST_USER_EMAIL
- TEST_USER_PASSWORD

### ✅ Production Environment Secrets (3 secrets)
You'll need to add these after Azure deployment:
- PROD_SUPABASE_URL
- PROD_SUPABASE_ANON_KEY
- AZURE_STATIC_WEB_APPS_API_TOKEN

---

## 🔄 Workflows Using These Secrets

### 1. PR Validation (`pr-validation.yml`)
**Uses:** All `DEV_*` and `TEST_*` secrets

**When:** Every pull request to `main` branch

**Steps:**
- Unit tests (uses Supabase config)
- Integration tests (uses Supabase config + test user)
- E2E tests (uses Supabase config + test user)

### 2. Production Deployment (`deploy-production.yml`)
**Uses:** All `PROD_*` and `AZURE_*` secrets

**When:** Code is merged to `main` branch

**Steps:**
- Build Blazor WASM app
- Inject production Supabase config
- Deploy to Azure Static Web Apps

---

## 🐛 Troubleshooting

### Tests Fail with "Invalid URI: The hostname could not be parsed"

**Cause:** Supabase configuration secrets are not set or have incorrect values.

**Solution:**
1. Verify all `DEV_*` secrets are added to GitHub
2. Check that secret values don't contain extra spaces or quotes
3. Ensure the Supabase URL format is: `https://xxxxx.supabase.co`

### Deployment Fails with "Unauthorized"

**Cause:** `AZURE_STATIC_WEB_APPS_API_TOKEN` is missing or incorrect.

**Solution:**
1. Run the Azure deployment script: `./infrastructure/deploy.sh`
2. Copy the deployment token from the output
3. Add it to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`

### Integration Tests Fail with "User not found"

**Cause:** Test user doesn't exist in the dev Supabase project.

**Solution:**
1. Go to your dev Supabase dashboard
2. Create a test user with the email/password from secrets
3. Or update the `TEST_USER_EMAIL` and `TEST_USER_PASSWORD` secrets

---

## 📚 Related Documentation

- [GitHub Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Supabase API Keys](https://supabase.com/docs/guides/api/api-keys)
- [Azure Static Web Apps Deployment](https://learn.microsoft.com/en-us/azure/static-web-apps/github-actions-workflow)

---

## ✅ Verification Checklist

Before pushing changes, verify:

- [ ] All 5 test environment secrets added
- [ ] Test user created in dev Supabase
- [ ] Dev Supabase project has all migrations applied
- [ ] Production Supabase project created (for later)
- [ ] Azure deployment completed (for later)
- [ ] Azure deployment token retrieved (for later)

---

**Last Updated:** October 26, 2025
