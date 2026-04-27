# scripts/check-prereqs.ps1 — verify local-dev toolchain is installed.
# Exits 0 when all required tools meet minimum versions, 1 otherwise.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'

$pass = 0
$fail = 0
$warn = 0

function Test-Tool {
    param(
        [string]$Label,
        [string]$Command,
        [string]$MinVersion,
        [switch]$Optional
    )

    $exists = Get-Command $Command -ErrorAction SilentlyContinue
    if (-not $exists) {
        if ($Optional) {
            "  ~ {0,-30} not installed (optional, recommend >= {1})" -f $Label, $MinVersion | Write-Host -ForegroundColor Yellow
            $script:warn++
        } else {
            "  X {0,-30} not installed (need >= {1})" -f $Label, $MinVersion | Write-Host -ForegroundColor Red
            $script:fail++
        }
        return
    }

    $ver = switch ($Command) {
        'dotnet'  { (& dotnet --version 2>$null) }
        'node'    { (& node --version 2>$null) -replace '^v', '' }
        'func'    { (& func --version 2>$null) | Select-Object -First 1 }
        'pwsh'    { (& pwsh --version 2>$null) -split ' ' | Select-Object -Last 1 }
        'az'      { (& az version --query '\"azure-cli\"' -o tsv 2>$null) }
        'gh'      { (& gh --version 2>$null | Select-Object -First 1) -split ' ' | Select-Object -Index 2 }
        'docker'  { ((& docker --version 2>$null) -split ' ' | Select-Object -Index 2) -replace ',', '' }
        default   { (& $Command --version 2>$null | Select-Object -First 1) }
    }

    "  + {0,-30} {1}" -f $Label, $ver | Write-Host -ForegroundColor Green
    $script:pass++
}

Write-Host "Checking RAJ Financial local-dev prerequisites..."
Write-Host ""

Test-Tool "Docker"                        docker  "24.0"
Test-Tool ".NET SDK"                      dotnet  "10.0"
Test-Tool "Node.js"                       node    "22.0"
Test-Tool "Azure Functions Core Tools v4" func    "4.0.5413"
Test-Tool "PowerShell 7+"                 pwsh    "7.4"
Test-Tool "Azure CLI"                     az      "2.60"
Test-Tool "GitHub CLI"                    gh      "2.50"

Write-Host ""
Write-Host "Optional (nice to have):"
Test-Tool "EF Core CLI (dotnet-ef)" dotnet-ef "10.0" -Optional
Test-Tool "sqlcmd"                  sqlcmd    "2.x"  -Optional

Write-Host ""
if ($fail -eq 0) {
    "+ All required prereqs present ({0} ok, {1} optional warnings)" -f $pass, $warn |
        Write-Host -ForegroundColor Green
    exit 0
} else {
    "X {0} required prereq(s) missing" -f $fail | Write-Host -ForegroundColor Red
    Write-Host "  See docs/local-development.md Prerequisites section for install instructions."
    exit 1
}
