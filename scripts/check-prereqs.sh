#!/usr/bin/env bash
# scripts/check-prereqs.sh — verify local-dev toolchain is installed AND
# at the required minimum version. Exits 0 only when every required tool
# is installed and meets its minimum.

set -uo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

PASS=0
FAIL=0
WARN=0

# version_ge $have $need
# Returns 0 iff $have >= $need using natural version-sort.
version_ge() {
    local have=$1 need=$2
    [[ -z "$have" || "$have" == "unknown" ]] && return 1
    # Strip leading 'v' (node prints "v22.x"), trailing '+', any commit suffix.
    have=${have#v}
    have=${have%%+*}
    have=${have%% *}
    # `sort -V` gives natural version ordering; if the smallest is $need,
    # then $have >= $need.
    [[ "$(printf '%s\n%s\n' "$need" "$have" | sort -V | head -n1)" == "$need" ]]
}

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
        dotnet)    ver=$(dotnet --version 2>/dev/null | head -n1) ;;
        dotnet-ef) ver=$(dotnet-ef --version 2>/dev/null | tail -n1 | awk '{print $NF}') ;;
        node)      ver=$(node --version 2>/dev/null | sed 's/^v//') ;;
        func)      ver=$(func --version 2>/dev/null | head -n1) ;;
        pwsh)      ver=$(pwsh --version 2>/dev/null | awk '{print $NF}') ;;
        az)        ver=$(az version --query '"azure-cli"' -o tsv 2>/dev/null) ;;
        gh)        ver=$(gh --version 2>/dev/null | head -n1 | awk '{print $3}') ;;
        docker)    ver=$(docker --version 2>/dev/null | awk '{print $3}' | sed 's/,//') ;;
        sqlcmd)
            # sqlcmd 18+: --version-only ; sqlcmd 17.x: -? prints version
            ver=$(sqlcmd '-?' 2>&1 | grep -Eo 'Version [0-9.]+' | awk '{print $2}' | head -n1)
            [[ -z "$ver" ]] && ver="present"
            ;;
        *)         ver=$($cmd --version 2>/dev/null | head -n1) ;;
    esac

    if [[ "$ver" == "present" ]]; then
        # Tool doesn't expose a parseable version — accept presence.
        printf "  ${GREEN}✓${NC} %-30s installed (version not parsed; min %s)\n" "$label" "$min"
        PASS=$((PASS+1))
        return
    fi

    if version_ge "$ver" "$min"; then
        printf "  ${GREEN}✓${NC} %-30s %s (≥ %s)\n" "$label" "$ver" "$min"
        PASS=$((PASS+1))
    else
        if [[ "$required" == "required" ]]; then
            printf "  ${RED}✗${NC} %-30s %s — below required %s\n" "$label" "$ver" "$min"
            FAIL=$((FAIL+1))
        else
            printf "  ${YELLOW}~${NC} %-30s %s — below recommended %s (optional)\n" "$label" "$ver" "$min"
            WARN=$((WARN+1))
        fi
    fi
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
check "sqlcmd"                            sqlcmd    "18.0"  optional

echo ""
if [[ $FAIL -eq 0 ]]; then
    printf "${GREEN}✓ All required prereqs present at minimum versions${NC} (%d ok, %d optional warnings)\n" "$PASS" "$WARN"
    exit 0
else
    printf "${RED}✗ %d required prereq(s) missing or below minimum${NC}\n" "$FAIL"
    echo "  See docs/local-development.md §Prerequisites for install instructions."
    exit 1
fi
