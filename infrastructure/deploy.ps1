# ============================================================================
# Azure Infrastructure Deployment Script for 10xJournal (PowerShell)
# ============================================================================
# This script deploys the Bicep template to Azure
# Usage: .\deploy.ps1 [-Environment prod]
# Example: .\deploy.ps1 -Environment dev
# ============================================================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('dev','prod')]
    [string]$Environment = 'prod'
)

$ErrorActionPreference = "Stop"

# ============================================================================
# Configuration
# ============================================================================

$ResourceGroup = "rg-10xjournal-$Environment"
$Location = "westeurope"
$TemplateFile = "infrastructure/main.bicep"
$ParametersFile = "infrastructure/main.parameters.json"

if ($Environment -eq 'dev') {
    $ParametersFile = "infrastructure/main.parameters.dev.json"
}

# ============================================================================
# Functions
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host "================================================" -ForegroundColor Green
    Write-Host $Message -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check if Azure CLI is installed
    try {
        $null = az --version
        Write-Success "Azure CLI installed"
    }
    catch {
        Write-Error "Azure CLI is not installed"
        Write-Host "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    }
    
    # Check if logged in
    try {
        $account = az account show | ConvertFrom-Json
        Write-Success "Logged in to Azure"
        Write-Info "Current subscription: $($account.name)"
    }
    catch {
        Write-Error "Not logged in to Azure"
        Write-Host "Run: az login"
        exit 1
    }
}

function Register-AzureProviders {
    Write-Header "Registering Azure Resource Providers"
    
    $providers = @(
        "Microsoft.Web",
        "Microsoft.Insights",
        "Microsoft.OperationalInsights"
    )
    
    foreach ($provider in $providers) {
        Write-Info "Checking $provider..."
        
        try {
            $state = az provider show --namespace $provider --query "registrationState" -o tsv 2>$null
        }
        catch {
            $state = "NotRegistered"
        }
        
        if ($state -ne "Registered") {
            Write-Info "Registering $provider..."
            az provider register --namespace $provider --wait --output none
            Write-Success "$provider registered"
        }
        else {
            Write-Success "$provider already registered"
        }
    }
}

function New-ResourceGroupIfNotExists {
    Write-Header "Creating Resource Group"
    
    $exists = az group exists --name $ResourceGroup
    
    if ($exists -eq 'true') {
        Write-Info "Resource group '$ResourceGroup' already exists"
    }
    else {
        Write-Info "Creating resource group '$ResourceGroup' in '$Location'"
        az group create `
            --name $ResourceGroup `
            --location $Location `
            --output none
        Write-Success "Resource group created"
    }
}

function Deploy-BicepTemplate {
    Write-Header "Deploying Bicep Template"
    
    Write-Info "Environment: $Environment"
    Write-Info "Resource Group: $ResourceGroup"
    Write-Info "Template: $TemplateFile"
    Write-Info "Parameters: $ParametersFile"
    
    Write-Host ""
    $confirmation = Read-Host "Proceed with deployment? (y/n)"
    
    if ($confirmation -ne 'y') {
        Write-Error "Deployment cancelled"
        exit 1
    }
    
    Write-Info "Starting deployment..."
    
    $deploymentName = "10xjournal-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    $deployment = az deployment group create `
        --resource-group $ResourceGroup `
        --name $deploymentName `
        --template-file $TemplateFile `
        --parameters $ParametersFile `
        --output json | ConvertFrom-Json
    
    Write-Success "Deployment completed"
    
    return $deployment
}

function Show-Outputs {
    param($Deployment)
    
    Write-Header "Deployment Outputs"
    
    $hostname = $Deployment.properties.outputs.staticWebAppDefaultHostname.value
    $swaName = $Deployment.properties.outputs.staticWebAppName.value
    $aiKey = $Deployment.properties.outputs.appInsightsInstrumentationKey.value
    
    Write-Host ""
    Write-Success "Static Web App URL: https://$hostname"
    Write-Info "Static Web App Name: $swaName"
    Write-Info "Application Insights Key: $aiKey"
    Write-Host ""
    
    return $swaName
}

function Get-DeploymentToken {
    param([string]$StaticWebAppName)
    
    Write-Header "Retrieving Deployment Token"
    
    Write-Info "Fetching deployment token..."
    
    $token = az staticwebapp secrets list `
        --name $StaticWebAppName `
        --resource-group $ResourceGroup `
        --query "properties.apiKey" `
        -o tsv
    
    Write-Host ""
    Write-Success "Deployment Token (add to GitHub Secrets as AZURE_STATIC_WEB_APPS_API_TOKEN):"
    Write-Host ""
    Write-Host $token -ForegroundColor Cyan
    Write-Host ""
    
    # Save to file
    $token | Out-File -FilePath ".deployment-token.txt" -NoNewline
    Write-Info "Token also saved to .deployment-token.txt (gitignored)"
}

function Show-NextSteps {
    Write-Header "Next Steps"
    
    Write-Host "1. Add deployment token to GitHub Secrets:"
    Write-Host "   - Go to GitHub → Settings → Secrets → Actions"
    Write-Host "   - Add secret: AZURE_STATIC_WEB_APPS_API_TOKEN"
    Write-Host "   - Value: (token displayed above)"
    Write-Host ""
    Write-Host "2. Add Supabase configuration to GitHub Secrets:"
    Write-Host "   - PROD_SUPABASE_URL"
    Write-Host "   - PROD_SUPABASE_ANON_KEY"
    Write-Host ""
    Write-Host "3. Push code to trigger deployment:"
    Write-Host "   git push origin main"
    Write-Host ""
    Write-Success "Infrastructure deployment complete! 🚀"
}

# ============================================================================
# Main
# ============================================================================

Write-Header "10xJournal - Azure Infrastructure Deployment"

Test-Prerequisites
Register-AzureProviders
New-ResourceGroupIfNotExists
$deployment = Deploy-BicepTemplate
$swaName = Show-Outputs -Deployment $deployment
Get-DeploymentToken -StaticWebAppName $swaName
Show-NextSteps
