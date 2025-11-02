# Verify Production Database Configuration

## Step 1: Check What Database Production App Is Using

Open your browser DevTools on the deployed app and run:

```javascript
// In the browser console at yellow-hill-083907d03.3.azurestaticapps.net
fetch('/appsettings.json')
  .then(r => r.json())
  .then(config => {
    console.log('Production Supabase URL:', config.Supabase.Url);
    console.log('Production Anon Key (first 20 chars):', config.Supabase.AnonKey.substring(0, 20));
  });
```

This will show you which Supabase instance the deployed app is actually connected to.

## Step 2: Compare with Your GitHub Secrets

Your GitHub secrets (from screenshot):
- `PROD_SUPABASE_URL` - updated 15 minutes ago
- `PROD_SUPABASE_ANON_KEY` - updated 14 minutes ago

**If the URLs don't match**, it means:
- Production was deployed BEFORE you updated the secrets
- You need to redeploy to use the new secrets

## Step 3: Check Last Production Deployment Time

Go to: https://github.com/dealen/10xJournal/actions/workflows/deploy-production.yml

Check when the last successful deployment ran. If it was BEFORE you updated the secrets, that explains why you're seeing old data.

## Step 4: Trigger New Production Deployment

### Option A: Merge devPWA to main (Recommended)

```bash
# Switch to main and merge devPWA
git checkout main
git pull origin main
git merge devPWA
git push origin main
# This will trigger automatic production deployment
```

### Option B: Manual Deployment from main

Go to: https://github.com/dealen/10xJournal/actions/workflows/deploy-production.yml
- Click "Run workflow"
- Select branch: `main`
- Enter reason: "Redeploy with updated Supabase credentials"
- Click "Run workflow"

This will deploy with your updated production secrets.

## Step 5: Verify After Deployment

1. Wait for deployment to complete
2. Clear browser cache (DevTools → Application → Clear site data)
3. Reload the production app
4. Check which database it connects to (repeat Step 1)
5. Verify the entries shown match your production database

---

## Understanding the Data Flow

```
Your Production App Flow:
┌─────────────────────────────────────────────────┐
│ 1. Push to 'main' branch                       │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ 2. GitHub Actions triggers deployment          │
│    - Builds the app                             │
│    - Injects secrets into appsettings.json:     │
│      * PROD_SUPABASE_URL → Supabase.Url        │
│      * PROD_SUPABASE_ANON_KEY → Supabase.AnonKey│
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ 3. Deploys to Azure Static Web Apps            │
│    URL: yellow-hill-083907d03.3...              │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ 4. App uses injected configuration              │
│    Connects to database specified in secrets    │
└─────────────────────────────────────────────────┘
```

**Important**: Changes to GitHub secrets don't automatically update deployed apps. You must redeploy for secrets to take effect.

---

## Why Service Worker Caching Might Be Confusing

The service worker caches:
- Static assets (HTML, CSS, JS, icons)
- API responses from Supabase

Even after clearing cache, if the production app is still connected to an old database, you'll see entries from that database.

**The fix**: Redeploy with updated secrets to connect to the correct database.
