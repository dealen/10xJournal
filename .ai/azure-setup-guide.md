# Azure Static Web Apps Setup Guide

Quick guide to deploy 10xJournal Blazor WASM app to Azure Static Web Apps and connect it with GitHub Actions.

---

## Prerequisites

- [ ] Azure account (free tier is sufficient)
- [ ] GitHub repository with your code
- [ ] Supabase production project created

---

## Part 1: Create Azure Static Web App

### Step 1: Sign in to Azure Portal

1. Go to https://portal.azure.com
2. Sign in with your Microsoft account

### Step 2: Create Static Web App Resource

1. Click **"Create a resource"** (top left)
2. Search for **"Static Web App"**
3. Click **"Create"**

### Step 3: Configure Basic Settings

Fill in the form:

```
Subscription: [Your Azure subscription]
Resource Group: Click "Create new" → Enter "rg-10xjournal"
Name: swa-10xjournal-prod
Plan type: Free
Region: West Europe (or closest to your users)
```

### Step 4: Configure Deployment Details

```
Source: GitHub
Organization: [Your GitHub username]
Repository: 10xJournal
Branch: main
```

**Important**: Azure will ask for GitHub authorization. Click **"Authorize Azure Static Web Apps"**

### Step 5: Configure Build Details

```
Build Presets: Custom
App location: /10xJournal.Client
Output location: wwwroot
```

**Note**: We'll use our own GitHub Actions workflow, but Azure will create a default one (you can delete it later)

### Step 6: Review and Create

1. Click **"Review + Create"**
2. Verify all settings
3. Click **"Create"**
4. Wait ~2 minutes for deployment

---

## Part 2: Get Deployment Token

### Step 1: Navigate to Your Static Web App

1. After creation, click **"Go to resource"**
2. You should see your Static Web App overview

### Step 2: Get the Deployment Token

1. In the left menu, click **"Overview"**
2. At the top, click **"Manage deployment token"**
3. Copy the token (it's a long string starting with `...`)
4. **Keep this safe** - you'll add it to GitHub Secrets

### Step 3: Note Your App URL

The URL format will be: `https://[some-generated-name].azurestaticapps.net`

You can find it in the **"URL"** field on the Overview page.

---

## Part 3: Configure GitHub Secrets

### Step 1: Go to GitHub Repository Settings

1. Navigate to your GitHub repository
2. Click **"Settings"** tab
3. In left sidebar, click **"Secrets and variables"** → **"Actions"**

### Step 2: Add Required Secrets

Click **"New repository secret"** for each of these:

#### Secret 1: AZURE_STATIC_WEB_APPS_API_TOKEN
```
Name: AZURE_STATIC_WEB_APPS_API_TOKEN
Secret: [Paste the deployment token from Step 2.2]
```

#### Secret 2: PROD_SUPABASE_URL
```
Name: PROD_SUPABASE_URL
Secret: https://your-project.supabase.co
```
*(Get this from your Supabase project settings)*

#### Secret 3: PROD_SUPABASE_ANON_KEY
```
Name: PROD_SUPABASE_ANON_KEY
Secret: eyJhbGc... (your anon/public key)
```
*(Get this from Supabase → Project Settings → API)*

#### Secret 4: DEV_SUPABASE_URL (for PR validation)
```
Name: DEV_SUPABASE_URL
Secret: https://your-dev-project.supabase.co
```

#### Secret 5: DEV_SUPABASE_ANON_KEY (for PR validation)
```
Name: DEV_SUPABASE_ANON_KEY
Secret: eyJhbGc... (your dev anon key)
```

**Result**: You should have 5 secrets configured.

---

## Part 4: Configure Static Web App

### Step 1: Create Configuration File

In your repository, create/update the file:  
`10xJournal.Client/wwwroot/staticwebapp.config.json`

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": [
      "/assets/*",
      "/*.{css,js,json,ico,png,jpg,jpeg,gif,svg,woff,woff2,ttf,eot}"
    ]
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "X-XSS-Protection": "1; mode=block",
    "Referrer-Policy": "strict-origin-when-cross-origin"
  },
  "mimeTypes": {
    ".json": "application/json",
    ".webmanifest": "application/manifest+json"
  }
}
```

**Why**: This configures Azure to properly handle Blazor routing and security headers.

### Step 2: Update appsettings.json Template

Update `10xJournal.Client/wwwroot/appsettings.json` to use placeholder values:

```json
{
  "Supabase": {
    "Url": "WILL_BE_REPLACED_BY_GITHUB_ACTIONS",
    "AnonKey": "WILL_BE_REPLACED_BY_GITHUB_ACTIONS"
  }
}
```

**Why**: The GitHub Actions workflow will replace these with actual production values during deployment.

---

## Part 5: Test the Setup

### Step 1: Delete Azure's Auto-Generated Workflow (Optional)

When you created the Static Web App, Azure auto-generated a workflow file in:  
`.github/workflows/azure-static-web-apps-*.yml`

You can delete this file since we're using our custom workflows:

```bash
git rm .github/workflows/azure-static-web-apps-*.yml
git commit -m "chore: remove auto-generated Azure workflow"
```

### Step 2: Create a Test Pull Request

```bash
# Create a test branch
git checkout -b test-deployment-setup

# Make a small change
echo "# Test Deployment" >> README.md

# Commit and push
git add .
git commit -m "test: deployment setup"
git push origin test-deployment-setup
```

### Step 3: Create PR on GitHub

1. Go to your GitHub repository
2. You should see a prompt to create PR
3. Create the pull request
4. Watch the **"PR Validation"** workflow run in the Actions tab

**Expected Result**: 
- ✅ Build succeeds
- ✅ Unit tests pass
- ✅ Integration tests pass
- ✅ E2E tests pass

### Step 4: Merge and Deploy

1. If PR validation passes, merge the PR
2. Watch the **"Deploy to Production"** workflow run
3. After ~2-3 minutes, visit your Azure Static Web App URL
4. You should see your deployed application!

---

## Part 6: Verify Deployment

### Checklist

- [ ] Visit your Azure Static Web App URL
- [ ] Homepage loads correctly
- [ ] Can register a new account
- [ ] Can create a journal entry
- [ ] Can logout/login
- [ ] Check browser console for errors (should be none)

### If Something Goes Wrong

1. **Check GitHub Actions logs**: Go to Actions tab → Click on the failed workflow
2. **Check Azure logs**: Azure Portal → Your Static Web App → "Logs" in left menu
3. **Verify secrets**: Make sure all 5 secrets are configured correctly
4. **Check Supabase**: Ensure your production Supabase project has all migrations applied

---

## Part 7: Optional - Custom Domain

If you want to use a custom domain (e.g., `10xjournal.app`) instead of the Azure-provided URL:

### Step 1: Purchase Domain

Purchase from: Namecheap, Google Domains, or Cloudflare Registrar

### Step 2: Add Custom Domain in Azure

1. In Azure Static Web App, click **"Custom domains"** in left menu
2. Click **"Add"** → **"Custom domain on other DNS"**
3. Enter your domain (e.g., `10xjournal.app`)
4. Azure will provide DNS records to add

### Step 3: Configure DNS

Add the records Azure provided to your DNS provider:
- CNAME record pointing to your Azure Static Web App URL
- TXT record for validation

### Step 4: Wait for SSL Certificate

Azure automatically provisions a free SSL certificate (5-30 minutes).

**Result**: Your app will be accessible at your custom domain with HTTPS! 🎉

---

## Summary

You've successfully:

✅ Created Azure Static Web App  
✅ Connected it with GitHub Actions  
✅ Configured GitHub Secrets  
✅ Set up automated PR validation  
✅ Set up automated deployment on merge to main  

**Workflow**:
1. Create feature branch
2. Make changes
3. Create PR → **PR Validation runs automatically**
4. Merge to main → **Deployment runs automatically**
5. App is live in ~3 minutes! 🚀

---

## Maintenance

### Update Deployment Token

If you need to rotate the deployment token:
1. Azure Portal → Static Web App → "Manage deployment token"
2. Click "Reset token"
3. Update the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret in GitHub

### Rollback Deployment

If deployment goes wrong:
1. Azure Portal → Static Web App → "Environments"
2. Find previous deployment
3. Click "Promote to production"

### Monitor Usage

Check if you're approaching free tier limits:
1. Azure Portal → Static Web App → "Usage"
2. Monitor bandwidth and storage

**Free tier limits**:
- 100 GB bandwidth/month
- 0.5 GB storage
- Should be sufficient for MVP with ~1000 users

---

## Need Help?

- **Azure Static Web Apps Docs**: https://learn.microsoft.com/en-us/azure/static-web-apps/
- **GitHub Actions Docs**: https://docs.github.com/en/actions
- **Blazor WASM Deployment**: https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly
