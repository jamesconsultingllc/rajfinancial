# scripts/check-prereqs.ps1 — verify local-dev toolchain is installed AND
# at the required minimum version. Exits 0 only when every required tool
# is installed and meets its minimum.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'

$pass = 0
$fail = 0
$warn = 0

# Compare two version strings using [System.Version] when possible,
# falling back to a numeric-tuple comparison so values like "10.0.107",
# "22.22.2" and "4.0.5413" all sort naturally.
function Test-VersionGe {
    param([string]$Have, [string]$Need)

    if ([string]::IsNullOrWhiteSpace($Have) -or $Have -eq 'unknown') { return $false }

    # Strip leading 'v' (node), trailing '+', and trailing ' (...)' annotations.
    $clean = $Have -replace '^v', ''
    $clean = ($clean -split '[ +-]')[0]

    # Parseable as System.Version? Pad to 4 components for clean comparison.
    $hv = $null; $nv = $null
    if ([System.Version]::TryParse($clean, [ref]$hv) -and
        [System.Version]::TryParse($Need, [ref]$nv)) {
        return $hv -ge $nv
    }

    # Fallback: split on '.', compare numeric components left to right.
    $haveParts = ($clean -split '\.')   | ForEach-Object { [int64]($_ -replace '\D','0') }
    $needParts = ($Need  -split '\.')   | ForEach-Object { [int64]($_ -replace '\D','0') }
    $max = [Math]::Max($haveParts.Count, $needParts.Count)
    for ($i = 0; $i -lt $max; $i++) {
        $h = if ($i -lt $haveParts.Count) { $haveParts[$i] } else { 0 }
        $n = if ($i -lt $needParts.Count) { $needParts[$i] } else { 0 }
        if ($h -gt $n) { return $true }
        if ($h -lt $n) { return $false }
    }
    return $true   # equal
}

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
        'dotnet'    { (& dotnet --version 2>$null) }
        'dotnet-ef' { ((& dotnet-ef --version 2>$null) | Select-Object -Last 1) -split ' ' | Select-Object -Last 1 }
        'node'      { (& node --version 2>$null) -replace '^v', '' }
        'func'      { (& func --version 2>$null) | Select-Object -First 1 }
        'pwsh'      { (& pwsh --version 2>$null) -split ' ' | Select-Object -Last 1 }
        # JMESPath: hyphenated keys MUST be wrapped in literal double-quotes
        # inside the query string. Use a single-quoted PS string so PowerShell
        # passes the bytes `"azure-cli"` straight through to az.
        'az'        { (& az version --query '"azure-cli"' -o tsv 2>$null) }
        'gh'        { (& gh --version 2>$null | Select-Object -First 1) -split ' ' | Select-Object -Index 2 }
        'docker'    { ((& docker --version 2>$null) -split ' ' | Select-Object -Index 2) -replace ',', '' }
        'sqlcmd'    {
            $raw = (& sqlcmd '-?' 2>&1) -join "`n"
            if ($raw -match 'Version\s+([\d\.]+)') { $matches[1] } else { 'present' }
        }
        default     { (& $Command --version 2>$null | Select-Object -First 1) }
    }

    if ($ver -eq 'present') {
        # Tool doesn't expose a parseable version — accept presence.
        "  + {0,-30} installed (version not parsed; min {1})" -f $Label, $MinVersion |
            Write-Host -ForegroundColor Green
        $script:pass++
        return
    }

    if (Test-VersionGe -Have $ver -Need $MinVersion) {
        "  + {0,-30} {1} (>= {2})" -f $Label, $ver, $MinVersion | Write-Host -ForegroundColor Green
        $script:pass++
    } else {
        if ($Optional) {
            "  ~ {0,-30} {1} -- below recommended {2} (optional)" -f $Label, $ver, $MinVersion |
                Write-Host -ForegroundColor Yellow
            $script:warn++
        } else {
            "  X {0,-30} {1} -- below required {2}" -f $Label, $ver, $MinVersion |
                Write-Host -ForegroundColor Red
            $script:fail++
        }
    }
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
Write-Host "Required tooling (continued):"
Test-Tool "EF Core CLI (dotnet-ef)" dotnet-ef "10.0"

Write-Host ""
Write-Host "Optional (nice to have):"
Test-Tool "sqlcmd"                  sqlcmd    "18.0" -Optional

Write-Host ""
if ($fail -eq 0) {
    "+ All required prereqs present at minimum versions ({0} ok, {1} optional warnings)" -f $pass, $warn |
        Write-Host -ForegroundColor Green
    exit 0
} else {
    "X {0} required prereq(s) missing or below minimum" -f $fail | Write-Host -ForegroundColor Red
    Write-Host "  See docs/local-development.md Prerequisites section for install instructions."
    exit 1
}
