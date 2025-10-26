# 10xJournal Deployment Plan

**Version**: 1.0  
**Date**: October 25, 2025  
**Project**: 10xJournal MVP  
**Timeline**: 4 weeks  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Deployment Architecture](#2-deployment-architecture)
3. [Infrastructure Requirements](#3-infrastructure-requirements)
4. [Environment Configuration](#4-environment-configuration)
5. [Pre-Deployment Checklist](#5-pre-deployment-checklist)
6. [Deployment Workflows](#6-deployment-workflows)
7. [Supabase Database Deployment](#7-supabase-database-deployment)
8. [Azure Static Web Apps Setup](#8-azure-static-web-apps-setup)
9. [GitHub Actions Configuration](#9-github-actions-configuration)
10. [PWA Deployment Considerations](#10-pwa-deployment-considerations)
11. [Domain and SSL Configuration](#11-domain-and-ssl-configuration)
12. [Monitoring and Health Checks](#12-monitoring-and-health-checks)
13. [Rollback Procedures](#13-rollback-procedures)
14. [Security Hardening](#14-security-hardening)
15. [Step-by-Step Implementation](#15-step-by-step-implementation)
16. [Post-Deployment Validation](#16-post-deployment-validation)
17. [Maintenance and Updates](#17-maintenance-and-updates)

---

## 1. Overview

### 1.1. Deployment Goals

- **Automated Deployment**: Zero-touch deployment from git push to live production
- **High Availability**: 99.9% uptime target for MVP
- **Fast Deployment**: < 5 minutes from commit to live
- **Easy Rollback**: < 2 minutes to previous version
- **Cost-Effective**: Leverage free tiers for MVP phase
- **Secure**: HTTPS, secure headers, RLS policies enforced

### 1.2. Technology Stack

- **Frontend Hosting**: Azure Static Web Apps (Free Tier)
- **Backend**: Supabase (Free Tier)
- **CI/CD**: GitHub Actions
- **DNS**: Cloudflare (Free Tier) - optional
- **Monitoring**: Azure Application Insights (Free Tier)

### 1.3. Timeline Alignment

| Week | Focus | Deployment Activities |
|------|-------|----------------------|
| Week 1 | Core Features | Setup dev environment, Supabase test instance |
| Week 2 | Authentication & Entries | Configure Azure Static Web Apps, initial deployment |
| Week 3 | UI/UX Polish | Staging deployment testing, PWA configuration |
| Week 4 | Testing & Launch | Production deployment, monitoring setup, go-live |

---

## 2. Deployment Architecture

### 2.1. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Developer                            │
└────────────┬────────────────────────────────────────────────┘
             │ git push
             ▼
┌─────────────────────────────────────────────────────────────┐
│                      GitHub Repository                       │
│                    (Source Control)                          │
└────────────┬────────────────────────────────────────────────┘
             │ triggers
             ▼
┌─────────────────────────────────────────────────────────────┐
│                     GitHub Actions                           │
│              (Build, Test, Deploy)                           │
└────┬───────────────────────────────────────────┬────────────┘
     │                                            │
     │ deploy                                     │ deploy
     ▼                                            ▼
┌──────────────────────────┐         ┌──────────────────────────┐
│  Azure Static Web Apps   │◄────────┤    Supabase Backend      │
│  (Blazor WASM + PWA)     │  uses   │  (PostgreSQL + Auth)     │
└────────────┬─────────────┘         └──────────────────────────┘
             │
             │ serves
             ▼
┌─────────────────────────────────────────────────────────────┐
│                        End Users                             │
│                  (Web + Mobile PWA)                          │
└─────────────────────────────────────────────────────────────┘
```

### 2.2. Environment Strategy

For MVP, we'll use a **simplified two-environment approach**:

1. **Development**: Local machine + Supabase test instance
2. **Production**: Azure Static Web Apps + Supabase production instance

**Rationale**: Staging environment adds complexity and cost for single developer MVP. Production can be safely updated using:
- Feature flags (if needed)
- Atomic deployments (Azure SWA handles this)
- Quick rollback capability

---

## 3. Infrastructure Requirements

### 3.1. Azure Resources

| Resource | SKU/Tier | Cost (Monthly) | Purpose |
|----------|----------|----------------|---------|
| Azure Static Web Apps | Free | $0 | Host Blazor WASM app |
| Application Insights | Free (5GB) | $0 | Monitoring and logging |

**Total Azure Cost**: **$0/month** (within free tier limits)

### 3.2. Supabase Resources

| Resource | Tier | Limits | Cost |
|----------|------|--------|------|
| Database | Free | 500 MB, unlimited API requests | $0 |
| Auth | Free | 50,000 monthly active users | $0 |
| Storage | Free | 1 GB | $0 |
| Bandwidth | Free | 2 GB | $0 |

**Total Supabase Cost**: **$0/month** (within free tier)

### 3.3. Domain and DNS (Optional)

| Service | Purpose | Cost |
|---------|---------|------|
| Domain Registration | Custom domain (e.g., 10xjournal.app) | ~$12/year |
| Cloudflare | DNS + CDN + DDoS protection | $0 (Free tier) |

---

## 4. Environment Configuration

### 4.1. Environment Variables

#### Development (`appsettings.Development.json`)
```json
{
  "Supabase": {
    "Url": "https://xxxxx.supabase.co",
    "AnonKey": "eyJhbGc...(test key)"
  }
}
```

#### Production (`appsettings.json`)
```json
{
  "Supabase": {
    "Url": "https://yyyyy.supabase.co",
    "AnonKey": "eyJhbGc...(production key)"
  }
}
```

### 4.2. Supabase Projects Setup

#### Test/Development Project
- **Name**: `10xjournal-dev`
- **Region**: Closest to developer location
- **Purpose**: Integration tests, development
- **Data**: Test data, can be reset

#### Production Project
- **Name**: `10xjournal-prod`
- **Region**: Closest to target users (e.g., EU/US)
- **Purpose**: Live user data
- **Data**: Protected, backed up

### 4.3. GitHub Secrets Configuration

Add these secrets in **GitHub Repository Settings → Secrets and variables → Actions**:

```
# Supabase Configuration
DEV_SUPABASE_URL=https://xxxxx.supabase.co
DEV_SUPABASE_ANON_KEY=eyJhbGc...

PROD_SUPABASE_URL=https://yyyyy.supabase.co
PROD_SUPABASE_ANON_KEY=eyJhbGc...

# Azure Static Web Apps Deployment Tokens
AZURE_STATIC_WEB_APPS_API_TOKEN=xxx-xxx-xxx
```

---

## 5. Pre-Deployment Checklist

### 5.1. Code Readiness

- [ ] All features implemented and tested locally
- [ ] Unit tests passing (90%+ coverage for critical paths)
- [ ] Integration tests passing against test Supabase
- [ ] E2E tests created for critical user journeys
- [ ] No hardcoded secrets in code
- [ ] Error handling implemented for all API calls
- [ ] Logging configured (Serilog to Application Insights)

### 5.2. Database Readiness

- [ ] All migrations tested in development
- [ ] RLS policies created and tested
- [ ] Database indexes created for performance
- [ ] Supabase production project created
- [ ] Backup strategy configured (Supabase automatic backups enabled)

### 5.3. Security Readiness

- [ ] HTTPS enforced
- [ ] Security headers configured (CSP, HSTS, X-Frame-Options)
- [ ] Authentication flow tested
- [ ] Password reset flow tested
- [ ] RLS policies prevent unauthorized access
- [ ] CORS configured correctly

### 5.4. PWA Readiness

- [ ] `manifest.json` configured
- [ ] Service worker configured for offline support
- [ ] App icons created (192x192, 512x512)
- [ ] Splash screens configured
- [ ] Offline fallback page created

### 5.5. Infrastructure Readiness

- [ ] Azure subscription created
- [ ] Azure Static Web App resource provisioned
- [ ] Supabase production project created
- [ ] GitHub Actions workflows configured
- [ ] Domain purchased (optional)
- [ ] DNS configured (optional)

---

## 6. Deployment Workflows

### 6.1. Continuous Integration (CI)

**Trigger**: Every push and pull request to `main` and `develop` branches

**Steps**:
1. Checkout code
2. Setup .NET 9 SDK
3. Restore NuGet packages
4. Build solution (Release configuration)
5. Run unit tests
6. Run integration tests (against dev Supabase)
7. Upload test results
8. Publish build artifacts

**File**: `.github/workflows/ci.yml`

### 6.2. Continuous Deployment (CD) - Production

**Trigger**: Push to `main` branch (after CI passes)

**Steps**:
1. Download build artifacts from CI
2. Run E2E smoke tests
3. Build and publish Blazor WASM
4. Deploy to Azure Static Web Apps
5. Run post-deployment health checks
6. Send deployment notification

**File**: `.github/workflows/deploy-production.yml`

### 6.3. Manual Deployment Trigger

**Purpose**: Emergency deployments or rollbacks

**Access**: Repository administrators only

**File**: Uses `workflow_dispatch` trigger in deployment workflows

---

## 7. Supabase Database Deployment

### 7.1. Migration Strategy

#### Development to Production Flow

```
┌──────────────┐
│   Developer  │
└──────┬───────┘
       │ 1. Create migration SQL file
       ▼
┌──────────────────────┐
│  Local Supabase CLI  │
│  or SQL Editor       │
└──────┬───────────────┘
       │ 2. Test locally
       ▼
┌──────────────────────┐
│ supabase/migrations/ │
│   (Git repository)   │
└──────┬───────────────┘
       │ 3. Commit & push
       ▼
┌──────────────────────┐
│  Dev Supabase (Test) │
└──────┬───────────────┘
       │ 4. Validate integration tests pass
       ▼
┌──────────────────────┐
│ Prod Supabase (Live) │
│  (Manual application)│
└──────────────────────┘
```

### 7.2. Migration Checklist

**Before Applying Migration**:
- [ ] Migration file reviewed and approved
- [ ] Applied to dev Supabase successfully
- [ ] Integration tests pass against dev
- [ ] Rollback script prepared (if needed)
- [ ] Database backup confirmed (Supabase auto-backup)

**Applying Migration**:
1. Open Supabase Dashboard → SQL Editor
2. Copy migration SQL from repository
3. Review SQL one final time
4. Execute SQL
5. Verify tables/policies created correctly
6. Test with application immediately

**After Migration**:
- [ ] Application connects successfully
- [ ] RLS policies working as expected
- [ ] No errors in Supabase logs
- [ ] Quick smoke test of affected features

### 7.3. Critical Migrations for MVP

| Migration | Purpose | Risk Level |
|-----------|---------|------------|
| `create_profiles_table.sql` | User profiles | Low |
| `create_journal_entries_table.sql` | Core functionality | Medium |
| `create_streaks_table.sql` | Streak tracking | Low |
| `enable_rls_policies.sql` | Security | **HIGH** |

---

## 8. Azure Static Web Apps Setup

### 8.1. Create Azure Static Web App

#### Via Azure Portal

1. **Sign in** to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"** → Search for **"Static Web App"**
3. Click **"Create"**

**Configuration**:
```
Basics:
  Subscription: [Your subscription]
  Resource Group: Create new "rg-10xjournal-prod"
  Name: swa-10xjournal-prod
  Plan type: Free
  Region: West Europe (or closest to users)
  
Source:
  Source: GitHub
  Organization: [Your GitHub username]
  Repository: 10xJournal
  Branch: main
  
Build Details:
  Build Presets: Blazor
  App location: /10xJournal.Client
  Output location: wwwroot
```

4. Click **"Review + Create"** → **"Create"**

#### What Happens Automatically

- Azure creates the resource
- Azure generates a deployment token
- Azure commits a GitHub Actions workflow file to your repository
- First deployment triggers automatically

### 8.2. Retrieve Deployment Token

1. Go to Azure Static Web App → **"Overview"**
2. Click **"Manage deployment token"**
3. Copy the token
4. Add to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`

### 8.3. Azure Static Web App Configuration

Create or update `staticwebapp.config.json` in `10xJournal.Client/wwwroot/`:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": [
      "/assets/*",
      "/*.{css,js,json,ico,png,jpg,jpeg,gif,svg,woff,woff2,ttf,eot}"
    ]
  },
  "routes": [
    {
      "route": "/app/*",
      "allowedRoles": ["authenticated"]
    }
  ],
  "responseOverrides": {
    "401": {
      "rewrite": "/login",
      "statusCode": 302
    },
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "X-XSS-Protection": "1; mode=block",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Permissions-Policy": "geolocation=(), microphone=(), camera=()"
  },
  "mimeTypes": {
    ".json": "application/json",
    ".webmanifest": "application/manifest+json"
  }
}
```

### 8.4. Custom Domain Configuration (Optional)

If you purchased a custom domain:

1. Go to Azure Static Web App → **"Custom domains"**
2. Click **"Add"** → **"Custom domain on other DNS"**
3. Enter your domain (e.g., `10xjournal.app`)
4. Azure provides TXT and CNAME records
5. Add these records to your DNS provider (Cloudflare)
6. Wait for validation (5-30 minutes)
7. Azure automatically provisions SSL certificate

---

## 9. GitHub Actions Configuration

### 9.1. Workflow Files Structure

```
.github/
  workflows/
    ci.yml                    # Continuous Integration
    deploy-production.yml     # Production Deployment
    e2e-scheduled.yml         # Daily E2E tests
```

### 9.2. CI Workflow (`ci.yml`)

```yaml
name: CI - Build and Test

on:
  push:
    branches: [ main, develop, devCICD ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  build-and-test:
    name: Build and Test
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout repository
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build solution
      run: dotnet build --no-restore --configuration Release
    
    - name: Run unit tests
      run: |
        dotnet test 10xJournal.Client.Tests/10xJournal.Client.Tests.csproj \
          --no-build \
          --configuration Release \
          --logger "trx;LogFileName=test-results.trx" \
          --collect:"XPlat Code Coverage"
    
    - name: Run integration tests
      env:
        SUPABASE_URL: ${{ secrets.DEV_SUPABASE_URL }}
        SUPABASE_ANON_KEY: ${{ secrets.DEV_SUPABASE_ANON_KEY }}
      run: |
        dotnet test 10xJournal.Client.Tests/10xJournal.Client.Tests.csproj \
          --filter "Category=Integration" \
          --configuration Release \
          --logger "trx;LogFileName=integration-test-results.trx"
    
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results
        path: '**/TestResults/*.trx'
    
    - name: Publish build artifacts
      run: |
        dotnet publish 10xJournal.Client/10xJournal.Client.csproj \
          -c Release \
          -o ./publish
    
    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: blazor-wasm-app
        path: ./publish/wwwroot
        retention-days: 7
```

### 9.3. Production Deployment Workflow (`deploy-production.yml`)

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]
  workflow_dispatch:
    inputs:
      reason:
        description: 'Reason for manual deployment'
        required: false

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  deploy:
    name: Deploy to Azure Static Web Apps
    runs-on: ubuntu-latest
    environment: production
    
    steps:
    - name: Checkout repository
      uses: actions/checkout@v4
      with:
        submodules: true
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Build and publish
      run: |
        dotnet publish 10xJournal.Client/10xJournal.Client.csproj \
          -c Release \
          -o ./publish
    
    - name: Update appsettings for production
      run: |
        cat > ./publish/wwwroot/appsettings.json <<EOF
        {
          "Supabase": {
            "Url": "${{ secrets.PROD_SUPABASE_URL }}",
            "AnonKey": "${{ secrets.PROD_SUPABASE_ANON_KEY }}"
          }
        }
        EOF
    
    - name: Deploy to Azure Static Web Apps
      id: deploy
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        action: "upload"
        app_location: "./publish/wwwroot"
        skip_app_build: true
    
    - name: Run post-deployment health check
      run: |
        echo "Waiting 30 seconds for deployment to stabilize..."
        sleep 30
        
        # Health check: verify app is accessible
        HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" https://10xjournal.app)
        if [ $HTTP_CODE -eq 200 ]; then
          echo "✅ Health check passed: App is accessible"
        else
          echo "❌ Health check failed: HTTP $HTTP_CODE"
          exit 1
        fi
    
    - name: Create deployment summary
      run: |
        echo "## 🚀 Deployment Successful" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "**Environment**: Production" >> $GITHUB_STEP_SUMMARY
        echo "**Branch**: ${{ github.ref_name }}" >> $GITHUB_STEP_SUMMARY
        echo "**Commit**: ${{ github.sha }}" >> $GITHUB_STEP_SUMMARY
        echo "**Deployed by**: ${{ github.actor }}" >> $GITHUB_STEP_SUMMARY
        echo "**Time**: $(date -u)" >> $GITHUB_STEP_SUMMARY
```

### 9.4. Scheduled E2E Tests (`e2e-scheduled.yml`)

```yaml
name: E2E Tests (Scheduled)

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM UTC
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  e2e-tests:
    name: Run E2E Tests
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout repository
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Setup Playwright
      run: |
        dotnet build 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj
        pwsh 10xJournal.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install --with-deps
    
    - name: Run E2E tests
      env:
        SUPABASE_URL: ${{ secrets.PROD_SUPABASE_URL }}
        SUPABASE_ANON_KEY: ${{ secrets.PROD_SUPABASE_ANON_KEY }}
        BASE_URL: https://10xjournal.app
      run: |
        dotnet test 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj \
          --logger "trx;LogFileName=e2e-results.trx" \
          --logger "console;verbosity=detailed"
    
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: e2e-test-results
        path: '**/TestResults/*.trx'
    
    - name: Create issue on failure
      if: failure()
      uses: actions/github-script@v7
      with:
        script: |
          github.rest.issues.create({
            owner: context.repo.owner,
            repo: context.repo.repo,
            title: '❌ Scheduled E2E Tests Failed',
            body: `The scheduled E2E tests have failed.\n\n**Workflow Run**: ${context.serverUrl}/${context.repo.owner}/${context.repo.repo}/actions/runs/${context.runId}`,
            labels: ['bug', 'e2e-failure', 'automated']
          })
```

---

## 10. PWA Deployment Considerations

### 10.1. Service Worker Configuration

Ensure `service-worker.js` is properly configured for caching:

```javascript
// Key files to cache for offline functionality
const CACHE_NAME = '10xjournal-v1.0.0';
const urlsToCache = [
  '/',
  '/index.html',
  '/css/app.css',
  '/js/app.js',
  '/manifest.json',
  // Add critical Blazor framework files
];
```

### 10.2. Manifest File Validation

Verify `manifest.json` in `wwwroot/`:

```json
{
  "name": "10xJournal",
  "short_name": "10xJournal",
  "description": "Your personal, minimalist digital journal",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#000000",
  "icons": [
    {
      "src": "/icon-192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "/icon-512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any maskable"
    }
  ]
}
```

### 10.3. PWA Deployment Checklist

- [ ] Service worker registered in `index.html`
- [ ] Manifest file linked in `index.html`
- [ ] All icon sizes created (192x192, 512x512)
- [ ] Cache strategy implemented for offline support
- [ ] Background sync configured (optional for MVP)
- [ ] Install prompt tested on mobile devices
- [ ] Lighthouse PWA audit score > 90

---

## 11. Domain and SSL Configuration

### 11.1. Domain Registration (Optional)

**Recommended Registrars**:
- Namecheap
- Google Domains
- Cloudflare Registrar

**Cost**: ~$12/year for `.app` domain

### 11.2. DNS Configuration with Cloudflare

1. **Add site to Cloudflare**:
   - Sign up at cloudflare.com
   - Add your domain
   - Update nameservers at registrar

2. **Add DNS records**:

```
Type    Name    Content                                   Proxy Status
CNAME   @       [your-swa].azurestaticapps.net           Proxied
CNAME   www     [your-swa].azurestaticapps.net           Proxied
```

3. **SSL/TLS Configuration**:
   - Go to SSL/TLS → Overview
   - Set to "Full (strict)"
   - Enable "Always Use HTTPS"
   - Enable "Automatic HTTPS Rewrites"

### 11.3. Azure Custom Domain Setup

1. In Azure Static Web App → **Custom domains**
2. Add `10xjournal.app` and `www.10xjournal.app`
3. Follow validation steps
4. Wait for SSL certificate provisioning (automatic)

**Result**: Automatic HTTPS with free SSL certificate

---

## 12. Monitoring and Health Checks

### 12.1. Azure Application Insights Setup

1. Create Application Insights resource in Azure
2. Copy Instrumentation Key
3. Add to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

4. Install NuGet package:
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

### 12.2. Key Metrics to Monitor

| Metric | Threshold | Action |
|--------|-----------|--------|
| Page Load Time | > 3 seconds | Optimize assets |
| Error Rate | > 1% | Investigate logs |
| API Response Time | > 2 seconds | Check Supabase |
| Availability | < 99% | Check Azure status |

### 12.3. Logging Configuration

Configure Serilog to send logs to Application Insights:

```csharp
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddApplicationInsights();
```

### 12.4. Health Check Endpoint

Create a simple health check in `Program.cs`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

---

## 13. Rollback Procedures

### 13.1. Automatic Rollback Triggers

GitHub Actions can be configured to automatically roll back if:
- Post-deployment health check fails
- E2E tests fail after deployment
- Error rate exceeds threshold

### 13.2. Manual Rollback via Azure Portal

**Steps**:
1. Go to Azure Static Web App → **Environments**
2. Find the previous successful deployment
3. Click **"Promote to production"**
4. Confirm rollback

**Time**: ~2 minutes

### 13.3. Git-Based Rollback

```bash
# Find the previous good commit
git log --oneline

# Create a revert commit
git revert <bad-commit-sha>

# Push to trigger new deployment
git push origin main
```

**Time**: ~5 minutes (includes new deployment)

### 13.4. Database Rollback

**Important**: Database changes are **not** automatically rolled back.

**If migration caused issues**:
1. Create rollback migration SQL
2. Test in dev environment
3. Apply to production manually
4. Redeploy application if needed

---

## 14. Security Hardening

### 14.1. Security Headers

Already configured in `staticwebapp.config.json`:
- ✅ X-Content-Type-Options: nosniff
- ✅ X-Frame-Options: DENY
- ✅ X-XSS-Protection: 1; mode=block
- ✅ Referrer-Policy: strict-origin-when-cross-origin

### 14.2. Content Security Policy (CSP)

Add to `staticwebapp.config.json`:

```json
"globalHeaders": {
  "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https://*.supabase.co"
}
```

### 14.3. Supabase Security Checklist

- [ ] RLS policies enabled on all tables
- [ ] Service role key stored securely (never exposed to client)
- [ ] Anon key used in client (read-only via RLS)
- [ ] CORS configured to allow only your domain
- [ ] Email verification enabled for new accounts
- [ ] Rate limiting enabled (Supabase default)

### 14.4. GitHub Security

- [ ] Enable Dependabot alerts
- [ ] Enable Dependabot security updates
- [ ] Require code review for main branch
- [ ] Enable branch protection rules
- [ ] Require status checks before merge
- [ ] Secrets stored in GitHub Secrets (never in code)

---

## 15. Step-by-Step Implementation

### Phase 1: Infrastructure Setup (Day 1-2)

#### Step 1: Create Supabase Production Project
```bash
1. Go to https://app.supabase.com
2. Click "New Project"
3. Name: 10xjournal-prod
4. Region: [Closest to users]
5. Database Password: [Generate strong password, store in password manager]
6. Click "Create new project"
```

**Output**: Production Supabase URL and Anon Key

#### Step 2: Apply Database Migrations
```bash
1. Open Supabase Dashboard → SQL Editor
2. Open each migration file from supabase/migrations/ in order
3. Copy SQL content
4. Paste into SQL Editor
5. Click "Run"
6. Verify no errors
```

**Checklist**:
- [ ] All tables created
- [ ] RLS policies enabled
- [ ] Indexes created

#### Step 3: Create Azure Static Web App
```bash
1. Sign in to Azure Portal
2. Create Resource → Static Web App
3. Configure as per Section 8.1
4. Click "Review + Create"
5. Wait for deployment (~2 minutes)
```

**Output**: Azure Static Web App URL and Deployment Token

#### Step 4: Configure GitHub Secrets
```bash
1. Go to GitHub Repository → Settings → Secrets → Actions
2. Click "New repository secret"
3. Add each secret:
   - DEV_SUPABASE_URL
   - DEV_SUPABASE_ANON_KEY
   - PROD_SUPABASE_URL
   - PROD_SUPABASE_ANON_KEY
   - AZURE_STATIC_WEB_APPS_API_TOKEN
```

---

### Phase 2: CI/CD Configuration (Day 3-4)

#### Step 5: Create GitHub Actions Workflows
```bash
mkdir -p .github/workflows
```

Create files:
- `.github/workflows/ci.yml` (from Section 9.2)
- `.github/workflows/deploy-production.yml` (from Section 9.3)
- `.github/workflows/e2e-scheduled.yml` (from Section 9.4)

#### Step 6: Configure Static Web App
```bash
# Create staticwebapp.config.json
touch 10xJournal.Client/wwwroot/staticwebapp.config.json
```

Add content from Section 8.3

#### Step 7: Update appsettings.json
```bash
# Update production settings
cat > 10xJournal.Client/wwwroot/appsettings.json <<EOF
{
  "Supabase": {
    "Url": "WILL_BE_REPLACED_BY_GITHUB_ACTIONS",
    "AnonKey": "WILL_BE_REPLACED_BY_GITHUB_ACTIONS"
  }
}
EOF
```

**Note**: GitHub Actions will replace these values during deployment

---

### Phase 3: First Deployment (Day 5)

#### Step 8: Test CI Pipeline
```bash
# Create feature branch
git checkout -b test-ci-pipeline

# Make small change
echo "# CI/CD Test" >> README.md

# Commit and push
git add .
git commit -m "test: CI pipeline"
git push origin test-ci-pipeline

# Watch GitHub Actions run
# Go to GitHub → Actions tab
```

**Expected**: CI workflow runs and passes

#### Step 9: First Production Deployment
```bash
# Merge to main (via PR or directly)
git checkout main
git merge test-ci-pipeline
git push origin main

# Watch deployment
# Go to GitHub → Actions tab
# Wait for deploy-production.yml to complete
```

**Expected**:
- Deployment completes successfully
- App accessible at Azure URL
- Health check passes

#### Step 10: Verify Deployment
```bash
# Open browser
# Navigate to your Azure Static Web App URL
# Test:
1. Load homepage
2. Register new account
3. Create journal entry
4. Logout and login
5. Delete entry
6. Check browser console for errors
```

---

### Phase 4: Domain and SSL (Day 6) - Optional

#### Step 11: Configure Custom Domain
```bash
1. Purchase domain (e.g., 10xjournal.app)
2. Add to Cloudflare
3. Configure DNS records (Section 11.2)
4. Add custom domain in Azure (Section 11.3)
5. Wait for SSL certificate provisioning
```

#### Step 12: Update appsettings with Production Domain
```bash
# No code changes needed
# Azure handles routing automatically
```

---

### Phase 5: Monitoring Setup (Day 7)

#### Step 13: Configure Application Insights
```bash
1. Create Application Insights in Azure Portal
2. Copy Instrumentation Key
3. Add to GitHub Secrets as APP_INSIGHTS_KEY
4. Update deploy workflow to inject key
```

#### Step 14: Test Monitoring
```bash
1. Trigger an error in the app
2. Check Application Insights → Failures
3. Verify error logged
```

---

### Phase 6: Final Validation (Day 8)

#### Step 15: Run Full Test Suite
```bash
# Run all tests locally
dotnet test

# Run E2E tests against production
BASE_URL=https://10xjournal.app dotnet test 10xJournal.E2E.Tests/
```

#### Step 16: Load Testing (Optional)
```bash
# Install Artillery
npm install -g artillery

# Create simple load test
artillery quick --count 100 --num 10 https://10xjournal.app
```

#### Step 17: PWA Validation
```bash
1. Open Chrome DevTools
2. Go to Lighthouse tab
3. Select "Progressive Web App"
4. Click "Generate report"
5. Verify score > 90
```

---

## 16. Post-Deployment Validation

### 16.1. Functional Testing Checklist

- [ ] **Homepage loads** in < 2 seconds
- [ ] **Registration works** (new account created in Supabase)
- [ ] **Login works** (redirects to /app/entries)
- [ ] **Create entry** (entry appears in list)
- [ ] **Edit entry** (changes saved)
- [ ] **Delete entry** (confirmation shown, entry deleted)
- [ ] **Logout works** (redirects to homepage)
- [ ] **Password reset** (email received)
- [ ] **Mobile responsive** (test on phone/tablet)
- [ ] **PWA install** (install prompt appears)
- [ ] **Offline mode** (service worker caches assets)

### 16.2. Performance Validation

Run Lighthouse audit:
- Performance: > 90
- Accessibility: > 90
- Best Practices: > 90
- SEO: > 80
- PWA: > 90

### 16.3. Security Validation

```bash
# Test security headers
curl -I https://10xjournal.app

# Expected headers:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
```

### 16.4. Monitoring Validation

- [ ] Application Insights receiving telemetry
- [ ] Errors logged correctly
- [ ] Page views tracked
- [ ] Custom events tracked (entry created, etc.)

---

## 17. Maintenance and Updates

### 17.1. Regular Maintenance Tasks

| Task | Frequency | Owner |
|------|-----------|-------|
| Review Application Insights errors | Daily | Developer |
| Check Supabase database size | Weekly | Developer |
| Review and merge Dependabot PRs | Weekly | Developer |
| Run manual E2E tests | Weekly | Developer |
| Review user feedback | Daily | Developer |
| Backup validation | Monthly | Developer |

### 17.2. Updating the Application

```bash
# Standard update process
1. Create feature branch
2. Develop feature
3. Test locally
4. Create PR to main
5. CI runs automatically
6. Review and merge
7. Automatic deployment to production
8. Verify deployment
```

### 17.3. Database Schema Updates

```bash
# Migration process
1. Create migration SQL file in supabase/migrations/
2. Test in dev Supabase
3. Run integration tests
4. Apply to production Supabase
5. Deploy application code (if needed)
```

### 17.4. Emergency Procedures

**If production is down**:
1. Check Azure Status Dashboard
2. Check Supabase Status Dashboard
3. Review Application Insights for errors
4. Check recent deployments (rollback if needed)
5. Check GitHub Actions for failed deployments

**Contact Information**:
- Azure Support: portal.azure.com → Support
- Supabase Support: app.supabase.com → Support
- GitHub Support: support.github.com

---

## 18. Success Metrics

### 18.1. Deployment Success Criteria

- ✅ Deployment completes in < 5 minutes
- ✅ Zero-downtime deployment
- ✅ Automatic rollback on failure
- ✅ All tests pass before deployment
- ✅ Health checks pass post-deployment

### 18.2. Performance Criteria

- ✅ Page load time < 2 seconds (3G connection)
- ✅ Time to Interactive < 3 seconds
- ✅ First Contentful Paint < 1 second
- ✅ Lighthouse Performance score > 90

### 18.3. Reliability Criteria

- ✅ Uptime > 99.9% (< 43 minutes downtime/month)
- ✅ Error rate < 1%
- ✅ Successful rollback within 2 minutes if needed

---

## 19. Cost Tracking

### 19.1. Monthly Cost Estimate (MVP)

| Service | Tier | Cost |
|---------|------|------|
| Azure Static Web Apps | Free | $0 |
| Application Insights | Free (5GB) | $0 |
| Supabase | Free | $0 |
| GitHub Actions | Free (2000 min) | $0 |
| Domain (annual) | - | ~$1/month |
| **Total** | | **~$1/month** |

### 19.2. Scaling Considerations

**When to upgrade**:
- Supabase: > 500 MB database or > 2GB bandwidth/month → $25/month
- Azure SWA: > 100 GB bandwidth → $9/month
- Application Insights: > 5 GB logs → Pay-as-you-go

**Expected scaling point**: ~1000 active users

---

## 20. Conclusion

This deployment plan provides a comprehensive, step-by-step guide to deploying 10xJournal to production. The plan prioritizes:

1. **Simplicity**: Streamlined for single developer
2. **Cost-effectiveness**: Leveraging free tiers
3. **Automation**: Minimal manual intervention
4. **Reliability**: Health checks and rollback procedures
5. **Security**: Best practices baked in

**Timeline Summary**:
- Week 1-3: Development
- Week 4 Day 1-2: Infrastructure setup
- Week 4 Day 3-4: CI/CD configuration
- Week 4 Day 5: First deployment
- Week 4 Day 6-7: Domain + monitoring
- Week 4 Day 8: Final validation and go-live

**Next Steps**:
1. Review this plan with stakeholders
2. Begin Phase 1: Infrastructure Setup
3. Follow step-by-step implementation
4. Track progress against timeline
5. Adjust as needed based on learnings

Good luck with the deployment! 🚀
