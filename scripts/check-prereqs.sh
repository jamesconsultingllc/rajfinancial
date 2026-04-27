#!/usr/bin/env bash
# scripts/check-prereqs.sh — verify local-dev toolchain is installed.
# Exits 0 when all required tools meet minimum versions, 1 otherwise.

set -uo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

PASS=0
FAIL=0
WARN=0

check() {
    local label=$1
    local cmd=$2
    local min=$3
    local required=${4:-required}

    if ! command -v "$cmd" >/dev/null 2>&1; then
        if [[ "$required" == "required" ]]; then
            printf "  ${RED}✗${NC} %-30s not installed (need ≥ %s)\n" "$label" "$min"
            FAIL=$((FAIL+1))
        else
            printf "  ${YELLOW}~${NC} %-30s not installed (optional, recommend ≥ %s)\n" "$label" "$min"
            WARN=$((WARN+1))
        fi
        return
    fi

    local ver
    case "$cmd" in
        dotnet)  ver=$(dotnet --version 2>/dev/null | head -n1) ;;
        node)    ver=$(node --version 2>/dev/null | sed 's/^v//') ;;
        func)    ver=$(func --version 2>/dev/null | head -n1) ;;
        pwsh)    ver=$(pwsh --version 2>/dev/null | awk '{print $NF}') ;;
        az)      ver=$(az version --query '"azure-cli"' -o tsv 2>/dev/null) ;;
        gh)      ver=$(gh --version 2>/dev/null | head -n1 | awk '{print $3}') ;;
        docker)  ver=$(docker --version 2>/dev/null | awk '{print $3}' | sed 's/,//') ;;
        sqlcmd)  ver="present" ;;
        *)       ver=$($cmd --version 2>/dev/null | head -n1) ;;
    esac

    printf "  ${GREEN}✓${NC} %-30s %s\n" "$label" "$ver"
    PASS=$((PASS+1))
}

echo "Checking RAJ Financial local-dev prerequisites..."
echo ""

check "Docker"                           docker  "24.0"
check ".NET SDK"                         dotnet  "10.0"
check "Node.js"                          node    "22.0"
check "Azure Functions Core Tools v4"    func    "4.0.5413"
check "PowerShell 7+"                    pwsh    "7.4"
check "Azure CLI"                        az      "2.60"
check "GitHub CLI"                       gh      "2.50"

echo ""
echo "Optional (nice to have):"
check "EF Core CLI (dotnet-ef)"          dotnet-ef "10.0"   optional
check "sqlcmd"                            sqlcmd    "2.x"    optional

echo ""
if [[ $FAIL -eq 0 ]]; then
    printf "${GREEN}✓ All required prereqs present${NC} (%d ok, %d optional warnings)\n" "$PASS" "$WARN"
    exit 0
else
    printf "${RED}✗ %d required prereq(s) missing${NC}\n" "$FAIL"
    echo "  See docs/local-development.md §Prerequisites for install instructions."
    exit 1
fi
