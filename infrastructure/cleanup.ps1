# ============================================================================
# Azure Infrastructure Cleanup Script for 10xJournal (PowerShell)
# ============================================================================
# This script DELETES all Azure resources created by the deployment
# Usage: .\cleanup.ps1 [-Environment prod]
# Example: .\cleanup.ps1 -Environment dev
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

# ============================================================================
# Functions
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host "================================================" -ForegroundColor Red
    Write-Host $Message -ForegroundColor Red
    Write-Host "================================================" -ForegroundColor Red
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

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  WARNING: $Message" -ForegroundColor Red
}

function Test-Prerequisites {
    # Check if Azure CLI is installed
    try {
        $null = az --version
    }
    catch {
        Write-Error "Azure CLI is not installed"
        exit 1
    }
    
    # Check if logged in
    try {
        $account = az account show | ConvertFrom-Json
        Write-Info "Current subscription: $($account.name)"
    }
    catch {
        Write-Error "Not logged in to Azure"
        Write-Host "Run: az login"
        exit 1
    }
}

function Test-ResourceGroupExists {
    $exists = az group exists --name $ResourceGroup
    
    if ($exists -eq 'false') {
        Write-Info "Resource group '$ResourceGroup' does not exist"
        Write-Success "Nothing to clean up!"
        exit 0
    }
}

function Show-Resources {
    Write-Header "Resources to be DELETED"
    
    Write-Host ""
    Write-Warning "The following resources will be PERMANENTLY DELETED:"
    Write-Host ""
    
    az resource list `
        --resource-group $ResourceGroup `
        --query "[].{Name:name, Type:type, Location:location}" `
        --output table
    
    Write-Host ""
}

function Export-ResourceList {
    Write-Info "Exporting resource list to cleanup-resources-$Environment.json..."
    
    $resources = az resource list `
        --resource-group $ResourceGroup `
        --output json
    
    $resources | Out-File -FilePath "cleanup-resources-$Environment.json"
    
    Write-Success "Resource list exported"
}

function Confirm-Deletion {
    Write-Host ""
    Write-Warning "THIS ACTION CANNOT BE UNDONE!"
    Write-Warning "All data, configurations, and resources will be permanently deleted."
    Write-Host ""
    
    $confirmation = Read-Host "Are you absolutely sure you want to delete resource group '$ResourceGroup'? (yes/no)"
    
    if ($confirmation -ne 'yes') {
        Write-Info "Cleanup cancelled"
        exit 0
    }
    
    # Double confirmation for production
    if ($Environment -eq 'prod') {
        Write-Host ""
        Write-Warning "You are deleting the PRODUCTION environment!"
        $prodConfirmation = Read-Host "Type 'DELETE-PRODUCTION' to confirm"
        
        if ($prodConfirmation -ne 'DELETE-PRODUCTION') {
            Write-Info "Cleanup cancelled"
            exit 0
        }
    }
}

function Remove-ResourceGroup {
    Write-Header "Deleting Resource Group"
    
    Write-Info "Deleting '$ResourceGroup'..."
    Write-Info "This may take several minutes..."
    
    az group delete `
        --name $ResourceGroup `
        --yes `
        --no-wait
    
    Write-Success "Deletion initiated (running in background)"
    Write-Info "Check status with: az group show --name $ResourceGroup"
}

function Wait-ForDeletion {
    Write-Host ""
    $wait = Read-Host "Wait for deletion to complete? (y/n)"
    
    if ($wait -eq 'y') {
        Write-Info "Waiting for deletion to complete..."
        
        $exists = 'true'
        while ($exists -eq 'true') {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 5
            $exists = az group exists --name $ResourceGroup
        }
        
        Write-Host ""
        Write-Success "Resource group deleted successfully!"
    }
    else {
        Write-Info "Deletion is running in background"
        Write-Info "You can close this terminal"
    }
}

function Remove-LocalFiles {
    Write-Header "Cleaning Up Local Files"
    
    # Remove deployment token file if it exists
    if (Test-Path ".deployment-token.txt") {
        Remove-Item ".deployment-token.txt" -Force
        Write-Success "Removed .deployment-token.txt"
    }
    
    # Remove exported resource list
    if (Test-Path "cleanup-resources-$Environment.json") {
        Remove-Item "cleanup-resources-$Environment.json" -Force
        Write-Success "Removed cleanup-resources-$Environment.json"
    }
}

function Show-NextSteps {
    Write-Header "Cleanup Complete"
    
    Write-Host ""
    Write-Success "All Azure resources for $Environment environment have been deleted"
    Write-Host ""
    Write-Host "What was deleted:"
    Write-Host "  - Azure Static Web App"
    Write-Host "  - Application Insights"
    Write-Host "  - Resource Group: $ResourceGroup"
    Write-Host ""
    Write-Host "What was NOT deleted (manual cleanup needed if desired):"
    Write-Host "  - GitHub repository"
    Write-Host "  - GitHub Secrets"
    Write-Host "  - Supabase project"
    Write-Host "  - Custom domain (if configured)"
    Write-Host ""
    Write-Host "To redeploy, run: .\infrastructure\deploy.ps1"
    Write-Host ""
}

# ============================================================================
# Main
# ============================================================================

Write-Header "⚠️  10xJournal - Azure Infrastructure CLEANUP ⚠️"

Write-Host ""
Write-Warning "This script will DELETE all Azure resources for the $Environment environment"
Write-Host ""

Test-Prerequisites
Test-ResourceGroupExists
Show-Resources
Export-ResourceList
Confirm-Deletion
Remove-ResourceGroup
Wait-ForDeletion
Remove-LocalFiles
Show-NextSteps
