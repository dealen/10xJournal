# Azure Infrastructure with Bicep

This directory contains Infrastructure as Code (IaC) definitions using Azure Bicep for deploying 10xJournal to Azure.

## 📁 Files

- `main.bicep` - Main Bicep template defining all Azure resources
- `main.parameters.json` - Parameters for production environment
- `main.parameters.dev.json` - Parameters for development environment (optional)
- `deploy.sh` - Deployment script for Linux/macOS
- `deploy.ps1` - Deployment script for Windows PowerShell

## 🏗️ Resources Created

The Bicep template creates:

1. **Azure Static Web App** - Hosts the Blazor WASM application
2. **Application Insights** - Monitoring and logging

## 📋 Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- Azure subscription
- Contributor or Owner role on the subscription

### Install Azure CLI

**Linux/macOS:**
```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

**Windows:**
```powershell
winget install -e --id Microsoft.AzureCLI
```

**Verify installation:**
```bash
az --version
```

## 🚀 Deployment

### Step 1: Login to Azure

```bash
az login
```

### Step 2: Set Active Subscription

```bash
# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"

# Verify
az account show
```

### Step 3: Update Parameters File

Edit `main.parameters.json` and update:

```json
{
  "repositoryUrl": {
    "value": "https://github.com/YOUR_USERNAME/10xJournal"  // ← Change this
  },
  "tags": {
    "value": {
      "Owner": "YOUR_NAME"  // ← Change this
    }
  }
}
```

### Step 4: Deploy

#### Option A: Using Azure CLI (Recommended)

```bash
# Create resource group
az group create \
  --name rg-10xjournal-prod \
  --location westeurope

# Deploy Bicep template
az deployment group create \
  --resource-group rg-10xjournal-prod \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/main.parameters.json
```

#### Option B: Using Deployment Script

**Linux/macOS:**
```bash
chmod +x infrastructure/deploy.sh
./infrastructure/deploy.sh
```

**Windows PowerShell:**
```powershell
.\infrastructure\deploy.ps1
```

### Step 5: Get Deployment Token

After deployment, retrieve the deployment token for GitHub Actions:

```bash
# Get the Static Web App name (output from deployment)
STATIC_WEB_APP_NAME="10xjournal-swa-prod"

# Retrieve deployment token
az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group rg-10xjournal-prod \
  --query "properties.apiKey" \
  -o tsv
```

**Copy this token** - you'll add it to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`

### Step 6: Get Application Insights Key (Optional)

```bash
# Get Application Insights instrumentation key
az monitor app-insights component show \
  --app 10xjournal-ai-prod \
  --resource-group rg-10xjournal-prod \
  --query "instrumentationKey" \
  -o tsv
```

## 🔄 Update/Redeploy

To update infrastructure:

```bash
az deployment group create \
  --resource-group rg-10xjournal-prod \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/main.parameters.json \
  --mode Incremental
```

**Note**: Bicep deployments are idempotent - safe to run multiple times.

## 🧹 Cleanup/Delete Resources

### Using Cleanup Script (Recommended)

**Linux/macOS:**
```bash
./infrastructure/cleanup.sh prod
```

**Windows PowerShell:**
```powershell
.\infrastructure\cleanup.ps1 -Environment prod
```

The script will:
- ✅ Show all resources that will be deleted
- ✅ Export resource list to JSON (backup)
- ✅ Ask for confirmation (double confirmation for prod)
- ✅ Delete the resource group and all resources
- ✅ Clean up local files

### Manual Cleanup

```bash
# Delete resource group (deletes all resources within)
az group delete \
  --name rg-10xjournal-prod \
  --yes \
  --no-wait
```

## 🎯 What You Get

After deployment, you'll have:

### Outputs

The deployment provides these outputs:

```bash
# View deployment outputs
az deployment group show \
  --resource-group rg-10xjournal-prod \
  --name main \
  --query "properties.outputs"
```

**Outputs include:**
- `staticWebAppDefaultHostname` - Your app URL (e.g., `happy-sand-123.azurestaticapps.net`)
- `staticWebAppName` - Resource name
- `appInsightsInstrumentationKey` - For monitoring
- `deploymentTokenNote` - Command to retrieve deployment token

### Access Your App

Your app will be available at:
```
https://[staticWebAppDefaultHostname]
```

## 🔐 GitHub Integration

### Manual Token Setup (Current Approach)

1. Get deployment token (Step 5 above)
2. Add to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`
3. GitHub Actions workflows will use this token for deployments

### Automated GitHub Integration (Optional)

If you want full automation, you can:

1. Create a GitHub Personal Access Token (PAT)
2. Pass it to the `repositoryToken` parameter during deployment
3. Azure will automatically configure GitHub Actions

**Create GitHub PAT:**
1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token with `repo` and `workflow` scopes
3. Copy the token

**Deploy with PAT:**
```bash
az deployment group create \
  --resource-group rg-10xjournal-prod \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/main.parameters.json \
  --parameters repositoryToken="YOUR_GITHUB_PAT"
```

**Security Note**: Never commit the PAT to git. Pass it as a command-line parameter or store in Azure Key Vault.

## 📊 Monitoring

Application Insights is automatically configured. View telemetry:

```bash
# Open in Azure Portal
az monitor app-insights component show \
  --app 10xjournal-ai-prod \
  --resource-group rg-10xjournal-prod \
  --query "appId" \
  -o tsv | xargs -I {} open "https://portal.azure.com/#@/resource/subscriptions/YOUR_SUB/resourceGroups/rg-10xjournal-prod/providers/Microsoft.Insights/components/10xjournal-ai-prod"
```

Or visit: Azure Portal → Application Insights → 10xjournal-ai-prod

## 🌍 Multiple Environments

To deploy development environment:

```bash
# Create dev resource group
az group create \
  --name rg-10xjournal-dev \
  --location westeurope

# Deploy with dev parameters
az deployment group create \
  --resource-group rg-10xjournal-dev \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/main.parameters.dev.json
```

## 🔧 Troubleshooting

### Deployment Fails

```bash
# View deployment logs
az deployment group show \
  --resource-group rg-10xjournal-prod \
  --name main \
  --query "properties.error"
```

### Can't Find Resource

```bash
# List all resources in resource group
az resource list \
  --resource-group rg-10xjournal-prod \
  --output table
```

### Permission Denied

Ensure you have Contributor or Owner role:

```bash
az role assignment list \
  --assignee YOUR_EMAIL \
  --resource-group rg-10xjournal-prod
```

## 📚 Learn More

- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

## 🔄 CI/CD Integration (Future)

You can automate infrastructure deployment using GitHub Actions:

```yaml
# .github/workflows/infrastructure.yml
name: Deploy Infrastructure

on:
  workflow_dispatch:
  push:
    branches: [main]
    paths:
      - 'infrastructure/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy Bicep
        uses: azure/arm-deploy@v1
        with:
          resourceGroupName: rg-10xjournal-prod
          template: ./infrastructure/main.bicep
          parameters: ./infrastructure/main.parameters.json
```

## 💰 Cost Estimate

With Free tier:
- Static Web Apps: **$0/month**
- Application Insights: **$0/month** (5GB free)

**Total: $0/month** ✨
