#!/bin/bash
# ============================================================================
# Azure Infrastructure Cleanup Script for 10xJournal
# ============================================================================
# This script DELETES all Azure resources created by the deployment
# Usage: ./cleanup.sh [environment]
# Example: ./cleanup.sh prod
# ============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ============================================================================
# Configuration
# ============================================================================

ENVIRONMENT="${1:-prod}"
RESOURCE_GROUP="rg-10xjournal-${ENVIRONMENT}"

# ============================================================================
# Functions
# ============================================================================

print_header() {
    echo -e "${RED}================================================${NC}"
    echo -e "${RED}$1${NC}"
    echo -e "${RED}================================================${NC}"
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

print_warning() {
    echo -e "${RED}⚠️  WARNING: $1${NC}"
}

check_prerequisites() {
    # Check if Azure CLI is installed
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed"
        exit 1
    fi
    
    # Check if logged in
    if ! az account show &> /dev/null; then
        print_error "Not logged in to Azure"
        echo "Run: az login"
        exit 1
    fi
    
    SUBSCRIPTION=$(az account show --query "name" -o tsv)
    print_info "Current subscription: ${SUBSCRIPTION}"
}

check_resource_group_exists() {
    if ! az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
        print_info "Resource group '${RESOURCE_GROUP}' does not exist"
        print_success "Nothing to clean up!"
        exit 0
    fi
}

list_resources() {
    print_header "Resources to be DELETED"
    
    echo ""
    print_warning "The following resources will be PERMANENTLY DELETED:"
    echo ""
    
    az resource list \
        --resource-group "$RESOURCE_GROUP" \
        --query "[].{Name:name, Type:type, Location:location}" \
        --output table
    
    echo ""
}

export_resource_list() {
    print_info "Exporting resource list to cleanup-resources-${ENVIRONMENT}.json..."
    
    az resource list \
        --resource-group "$RESOURCE_GROUP" \
        --output json > "cleanup-resources-${ENVIRONMENT}.json"
    
    print_success "Resource list exported"
}

confirm_deletion() {
    echo ""
    print_warning "THIS ACTION CANNOT BE UNDONE!"
    print_warning "All data, configurations, and resources will be permanently deleted."
    echo ""
    
    read -p "Are you absolutely sure you want to delete resource group '${RESOURCE_GROUP}'? (yes/no): " -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        print_info "Cleanup cancelled"
        exit 0
    fi
    
    # Double confirmation for production
    if [ "$ENVIRONMENT" == "prod" ]; then
        echo ""
        print_warning "You are deleting the PRODUCTION environment!"
        read -p "Type 'DELETE-PRODUCTION' to confirm: " -r
        echo
        
        if [[ $REPLY != "DELETE-PRODUCTION" ]]; then
            print_info "Cleanup cancelled"
            exit 0
        fi
    fi
}

delete_resource_group() {
    print_header "Deleting Resource Group"
    
    print_info "Deleting '${RESOURCE_GROUP}'..."
    print_info "This may take several minutes..."
    
    az group delete \
        --name "$RESOURCE_GROUP" \
        --yes \
        --no-wait
    
    print_success "Deletion initiated (running in background)"
    print_info "Check status with: az group show --name ${RESOURCE_GROUP}"
}

wait_for_deletion() {
    echo ""
    read -p "Wait for deletion to complete? (y/n): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Waiting for deletion to complete..."
        
        while az group exists --name "$RESOURCE_GROUP" | grep -q "true"; do
            echo -n "."
            sleep 5
        done
        
        echo ""
        print_success "Resource group deleted successfully!"
    else
        print_info "Deletion is running in background"
        print_info "You can close this terminal"
    fi
}

cleanup_local_files() {
    print_header "Cleaning Up Local Files"
    
    # Remove deployment token file if it exists
    if [ -f ".deployment-token.txt" ]; then
        rm -f .deployment-token.txt
        print_success "Removed .deployment-token.txt"
    fi
    
    # Remove exported resource list
    if [ -f "cleanup-resources-${ENVIRONMENT}.json" ]; then
        rm -f "cleanup-resources-${ENVIRONMENT}.json"
        print_success "Removed cleanup-resources-${ENVIRONMENT}.json"
    fi
}

show_next_steps() {
    print_header "Cleanup Complete"
    
    echo ""
    print_success "All Azure resources for ${ENVIRONMENT} environment have been deleted"
    echo ""
    echo "What was deleted:"
    echo "  - Azure Static Web App"
    echo "  - Application Insights"
    echo "  - Resource Group: ${RESOURCE_GROUP}"
    echo ""
    echo "What was NOT deleted (manual cleanup needed if desired):"
    echo "  - GitHub repository"
    echo "  - GitHub Secrets"
    echo "  - Supabase project"
    echo "  - Custom domain (if configured)"
    echo ""
    echo "To redeploy, run: ./infrastructure/deploy.sh"
    echo ""
}

# ============================================================================
# Main
# ============================================================================

main() {
    print_header "⚠️  10xJournal - Azure Infrastructure CLEANUP ⚠️"
    
    echo ""
    print_warning "This script will DELETE all Azure resources for the ${ENVIRONMENT} environment"
    echo ""
    
    check_prerequisites
    check_resource_group_exists
    list_resources
    export_resource_list
    confirm_deletion
    delete_resource_group
    wait_for_deletion
    cleanup_local_files
    show_next_steps
}

main
