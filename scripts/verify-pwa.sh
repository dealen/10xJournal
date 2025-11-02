#!/bin/bash
# PWA Verification Script for 10xJournal
# This script verifies that all PWA components are properly configured

echo "🔍 PWA Implementation Verification"
echo "=================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check counter
checks_passed=0
total_checks=0

# Function to check file exists
check_file() {
    total_checks=$((total_checks + 1))
    if [ -f "$1" ]; then
        echo -e "${GREEN}✓${NC} $2"
        checks_passed=$((checks_passed + 1))
    else
        echo -e "${RED}✗${NC} $2 - MISSING: $1"
    fi
}

# Function to check file contains text
check_content() {
    total_checks=$((total_checks + 1))
    if grep -q "$2" "$1"; then
        echo -e "${GREEN}✓${NC} $3"
        checks_passed=$((checks_passed + 1))
    else
        echo -e "${RED}✗${NC} $3 - NOT FOUND in $1"
    fi
}

echo "📁 Checking PWA Files..."
echo ""

# Check manifest
check_file "10xJournal.Client/wwwroot/manifest.json" "Web App Manifest exists"
check_content "10xJournal.Client/wwwroot/manifest.json" "icon-512.png" "Manifest includes 512x512 icon"
check_content "10xJournal.Client/wwwroot/manifest.json" "icon-192.png" "Manifest includes 192x192 icon"
check_content "10xJournal.Client/wwwroot/manifest.json" "standalone" "Manifest has standalone display mode"

echo ""
echo "🔧 Checking Service Worker..."
echo ""

# Check service worker
check_file "10xJournal.Client/wwwroot/service-worker.js" "Service Worker exists"
check_content "10xJournal.Client/wwwroot/service-worker.js" "CACHE_VERSION" "Service Worker has versioning"
check_content "10xJournal.Client/wwwroot/service-worker.js" "stale-while-revalidate" "Service Worker uses stale-while-revalidate strategy"
check_content "10xJournal.Client/wwwroot/service-worker.js" "CACHE_BLACKLIST" "Service Worker has cache blacklist"

echo ""
echo "🎨 Checking Icons..."
echo ""

# Check icons
check_file "10xJournal.Client/wwwroot/icon-192.png" "192x192 icon exists"
check_file "10xJournal.Client/wwwroot/icon-512.png" "512x512 icon exists"
check_file "10xJournal.Client/wwwroot/favicon.png" "Favicon exists"

echo ""
echo "📄 Checking HTML Configuration..."
echo ""

# Check index.html
check_file "10xJournal.Client/wwwroot/index.html" "index.html exists"
check_content "10xJournal.Client/wwwroot/index.html" "manifest.json" "HTML links to manifest"
check_content "10xJournal.Client/wwwroot/index.html" "serviceWorker.register" "HTML registers service worker"
check_content "10xJournal.Client/wwwroot/index.html" "apple-mobile-web-app-capable" "HTML has iOS meta tags"
check_content "10xJournal.Client/wwwroot/index.html" "theme-color" "HTML has theme color"
check_content "10xJournal.Client/wwwroot/index.html" "apple-touch-icon" "HTML has Apple touch icon"

echo ""
echo "⚙️  Checking Project Configuration..."
echo ""

# Check project file
check_file "10xJournal.Client/10xJournal.Client.csproj" "Project file exists"
check_content "10xJournal.Client/10xJournal.Client.csproj" "ServiceWorkerAssetsManifest" "Project has ServiceWorkerAssetsManifest"
check_content "10xJournal.Client/10xJournal.Client.csproj" "ServiceWorker" "Project includes ServiceWorker item"

echo ""
echo "📚 Checking Documentation..."
echo ""

# Check documentation
check_file "docs/PWA_IMPLEMENTATION.md" "Implementation guide exists"
check_file "docs/PWA_QUICK_START.md" "Quick start guide exists"
check_file "docs/PWA_SUMMARY.md" "Summary document exists"

echo ""
echo "🏗️  Building Project..."
echo ""

# Build the project
if dotnet build --verbosity quiet > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} Project builds successfully"
    checks_passed=$((checks_passed + 1))
    total_checks=$((total_checks + 1))
else
    echo -e "${RED}✗${NC} Project build failed"
    total_checks=$((total_checks + 1))
fi

echo ""
echo "📊 Verification Results"
echo "=================================="
echo ""

percentage=$((checks_passed * 100 / total_checks))

if [ $checks_passed -eq $total_checks ]; then
    echo -e "${GREEN}✓ All checks passed! ($checks_passed/$total_checks)${NC}"
    echo ""
    echo "🎉 PWA implementation is complete and ready to use!"
    echo ""
    echo "Next steps:"
    echo "1. Run: dotnet run --project 10xJournal.Client"
    echo "2. Open browser to https://localhost:5001"
    echo "3. Open DevTools (F12) and check Console"
    echo "4. Look for install icon in address bar"
    echo "5. Test offline mode in DevTools → Network → Offline"
    echo ""
    exit 0
else
    echo -e "${YELLOW}⚠ $checks_passed/$total_checks checks passed ($percentage%)${NC}"
    echo ""
    echo "Some issues need attention. Please review the output above."
    echo ""
    exit 1
fi
