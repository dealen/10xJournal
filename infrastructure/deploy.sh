#!/bin/bash
# ============================================================================
# Azure Infrastructure Deployment Script for 10xJournal
# ============================================================================
# This script deploys the Bicep template to Azure
# Usage: ./deploy.sh [environment]
# Example: ./deploy.sh prod
# ============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# ============================================================================
# Configuration
# ============================================================================

ENVIRONMENT="${1:-prod}"
RESOURCE_GROUP="rg-10xjournal-${ENVIRONMENT}"
LOCATION="westeurope"
TEMPLATE_FILE="infrastructure/main.bicep"
PARAMETERS_FILE="infrastructure/main.parameters.json"

if [ "$ENVIRONMENT" == "dev" ]; then
    PARAMETERS_FILE="infrastructure/main.parameters.dev.json"
fi

# ============================================================================
# Functions
# ============================================================================

print_header() {
    echo -e "${GREEN}================================================${NC}"
    echo -e "${GREEN}$1${NC}"
    echo -e "${GREEN}================================================${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check if Azure CLI is installed
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed"
        echo "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    fi
    print_success "Azure CLI installed"
    
    # Check if logged in
    if ! az account show &> /dev/null; then
        print_error "Not logged in to Azure"
        echo "Run: az login"
        exit 1
    fi
    print_success "Logged in to Azure"
    
    # Display current subscription
    SUBSCRIPTION=$(az account show --query "name" -o tsv)
    print_info "Current subscription: ${SUBSCRIPTION}"
}

register_providers() {
    print_header "Registering Azure Resource Providers"
    
    PROVIDERS=("Microsoft.Web" "Microsoft.Insights" "Microsoft.OperationalInsights")
    
    for PROVIDER in "${PROVIDERS[@]}"; do
        print_info "Checking ${PROVIDER}..."
        STATE=$(az provider show --namespace "$PROVIDER" --query "registrationState" -o tsv 2>/dev/null || echo "NotRegistered")
        
        if [ "$STATE" != "Registered" ]; then
            print_info "Registering ${PROVIDER}..."
            az provider register --namespace "$PROVIDER" --wait --output none
            print_success "${PROVIDER} registered"
        else
            print_success "${PROVIDER} already registered"
        fi
    done
}

create_resource_group() {
    print_header "Creating Resource Group"
    
    if az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
        print_info "Resource group '${RESOURCE_GROUP}' already exists"
    else
        print_info "Creating resource group '${RESOURCE_GROUP}' in '${LOCATION}'"
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --output none
        print_success "Resource group created"
    fi
}

deploy_template() {
    print_header "Deploying Bicep Template"
    
    print_info "Environment: ${ENVIRONMENT}"
    print_info "Resource Group: ${RESOURCE_GROUP}"
    print_info "Template: ${TEMPLATE_FILE}"
    print_info "Parameters: ${PARAMETERS_FILE}"
    
    echo ""
    read -p "Proceed with deployment? (y/n) " -n 1 -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_error "Deployment cancelled"
        exit 1
    fi
    
    print_info "Starting deployment..."
    
    DEPLOYMENT_NAME="10xjournal-$(date +%Y%m%d-%H%M%S)"
    
    az deployment group create \
        --resource-group "$RESOURCE_GROUP" \
        --name "$DEPLOYMENT_NAME" \
        --template-file "$TEMPLATE_FILE" \
        --parameters "$PARAMETERS_FILE" \
        --output json > deployment_output.json
    
    print_success "Deployment completed"
}

display_outputs() {
    print_header "Deployment Outputs"
    
    # Extract outputs
    HOSTNAME=$(jq -r '.properties.outputs.staticWebAppDefaultHostname.value' deployment_output.json)
    SWA_NAME=$(jq -r '.properties.outputs.staticWebAppName.value' deployment_output.json)
    AI_KEY=$(jq -r '.properties.outputs.appInsightsInstrumentationKey.value' deployment_output.json)
    
    echo ""
    print_success "Static Web App URL: https://${HOSTNAME}"
    print_info "Static Web App Name: ${SWA_NAME}"
    print_info "Application Insights Key: ${AI_KEY}"
    echo ""
}

get_deployment_token() {
    print_header "Retrieving Deployment Token"
    
    SWA_NAME=$(jq -r '.properties.outputs.staticWebAppName.value' deployment_output.json)
    
    print_info "Fetching deployment token..."
    
    TOKEN=$(az staticwebapp secrets list \
        --name "$SWA_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.apiKey" \
        -o tsv)
    
    echo ""
    print_success "Deployment Token (add to GitHub Secrets as AZURE_STATIC_WEB_APPS_API_TOKEN):"
    echo ""
    echo "$TOKEN"
    echo ""
    
    # Save to file
    echo "$TOKEN" > .deployment-token.txt
    print_info "Token also saved to .deployment-token.txt (gitignored)"
}

next_steps() {
    print_header "Next Steps"
    
    echo "1. Add deployment token to GitHub Secrets:"
    echo "   - Go to GitHub → Settings → Secrets → Actions"
    echo "   - Add secret: AZURE_STATIC_WEB_APPS_API_TOKEN"
    echo "   - Value: (token displayed above)"
    echo ""
    echo "2. Add Supabase configuration to GitHub Secrets:"
    echo "   - PROD_SUPABASE_URL"
    echo "   - PROD_SUPABASE_ANON_KEY"
    echo ""
    echo "3. Push code to trigger deployment:"
    echo "   git push origin main"
    echo ""
    print_success "Infrastructure deployment complete! 🚀"
}

# ============================================================================
# Main
# ============================================================================

main() {
    print_header "10xJournal - Azure Infrastructure Deployment"
    
    check_prerequisites
    register_providers
    create_resource_group
    deploy_template
    display_outputs
    get_deployment_token
    next_steps
    
    # Cleanup
    rm -f deployment_output.json
}

main
