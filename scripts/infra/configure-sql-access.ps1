#!/usr/bin/env pwsh
# ============================================================================
# RAJ Financial - Configure SQL Database Access for Managed Identity
# ============================================================================
# This script grants the Azure Functions Managed Identity access to the
# SQL Database using T-SQL commands.
#
# Why is this needed?
#   Azure RBAC can't grant database-level permissions. SQL Server requires
#   T-SQL commands to create database users and assign roles.
#
# Usage:
#   .\configure-sql-access.ps1 -Environment dev
#   .\configure-sql-access.ps1 -Environment prod
#
# Prerequisites:
#   - Azure CLI logged in
#   - sqlcmd utility installed (comes with SQL Server tools)
#   - You must be a SQL Admin (in the Entra admin group)
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

# ============================================================================
# Configuration
# ============================================================================

$config = @{
    dev = @{
        ResourceGroup = "raj-financial-dev-rg"
        SqlServer = "rajfinancial-dev"
        SqlDatabase = "rajfinancial"
        FunctionAppName = "func-rajfinancial-dev"
    }
    prod = @{
        ResourceGroup = "raj-financial-prod-rg"
        SqlServer = "rajfinancial"
        SqlDatabase = "rajfinancial"
        FunctionAppName = "func-rajfinancial-prod"
    }
}

$envConfig = $config[$Environment]

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  RAJ Financial - Configure SQL Managed Identity Access         ║" -ForegroundColor Cyan
Write-Host "║  Environment: $Environment                                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Get SQL Server FQDN
# ============================================================================

Write-Host "→ Getting SQL Server details..." -ForegroundColor Yellow

$sqlServer = az sql server show `
    --resource-group $envConfig.ResourceGroup `
    --name $envConfig.SqlServer `
    --output json 2>$null | ConvertFrom-Json

if (-not $sqlServer) {
    Write-Error "SQL Server not found: $($envConfig.SqlServer)"
    exit 1
}

$sqlFqdn = $sqlServer.fullyQualifiedDomainName
Write-Host "  SQL Server: $sqlFqdn" -ForegroundColor Gray

# ============================================================================
# Get Access Token for SQL
# ============================================================================

Write-Host "→ Getting access token for SQL..." -ForegroundColor Yellow

$token = az account get-access-token `
    --resource "https://database.windows.net/" `
    --query accessToken `
    --output tsv

if (-not $token) {
    Write-Error "Failed to get access token. Ensure you're logged in with an account that has SQL Admin access."
    exit 1
}

Write-Host "  ✓ Access token obtained" -ForegroundColor Green

# ============================================================================
# Check for sqlcmd
# ============================================================================

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue

if (-not $sqlcmd) {
    Write-Host ""
    Write-Host "⚠ sqlcmd not found. Installing via winget..." -ForegroundColor Yellow
    winget install Microsoft.SqlServer.SqlCmd
    
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    
    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        Write-Error "sqlcmd still not found. Please install manually and try again."
        Write-Host "  Download: https://aka.ms/sqlcmd" -ForegroundColor Gray
        exit 1
    }
}

# ============================================================================
# Create SQL User for Function App Managed Identity
# ============================================================================

Write-Host "→ Creating database user for Function App..." -ForegroundColor Yellow

$sql = @"
-- Create user from the Function App's Managed Identity
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'$($envConfig.FunctionAppName)')
BEGIN
    CREATE USER [$($envConfig.FunctionAppName)] FROM EXTERNAL PROVIDER;
    PRINT 'Created user: $($envConfig.FunctionAppName)';
END
ELSE
BEGIN
    PRINT 'User already exists: $($envConfig.FunctionAppName)';
END

-- Grant read/write access
ALTER ROLE db_datareader ADD MEMBER [$($envConfig.FunctionAppName)];
ALTER ROLE db_datawriter ADD MEMBER [$($envConfig.FunctionAppName)];

-- Grant DDL rights for EF Core migrations (dev only)
-- In prod, migrations should be run separately with elevated privileges
$(if ($Environment -eq 'dev') { "ALTER ROLE db_ddladmin ADD MEMBER [$($envConfig.FunctionAppName)];" } else { "-- DDL admin skipped for production" })

PRINT 'Roles assigned successfully';
"@

# Save SQL to temp file
$sqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sql | Out-File -FilePath $sqlFile -Encoding UTF8

try {
    Write-Host "  Executing SQL commands..." -ForegroundColor Gray

    # Execute using Azure AD Default authentication (uses az login credentials)
    # Note: The new go-sqlcmd uses --authentication-method, older sqlcmd uses -G
    $result = sqlcmd -S "tcp:$sqlFqdn,1433" `
        -d $envConfig.SqlDatabase `
        --authentication-method=ActiveDirectoryDefault `
        -i $sqlFile `
        -b 2>&1

    if ($LASTEXITCODE -ne 0) {
        # Fallback: try legacy sqlcmd with -G flag
        Write-Host "  Trying legacy sqlcmd..." -ForegroundColor Gray
        $env:SQLCMDPASSWORD = $token
        sqlcmd -S "tcp:$sqlFqdn,1433" `
            -d $envConfig.SqlDatabase `
            -G `
            -U "dummy" `
            -i $sqlFile `
            -b
        $env:SQLCMDPASSWORD = $null

        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host "  ✓ Database user configured successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to configure SQL access: $_"
    Write-Host ""
    Write-Host "Manual alternative - run this in Azure Portal Query Editor:" -ForegroundColor Yellow
    Write-Host $sql -ForegroundColor Gray
    exit 1
}
finally {
    # Clean up temp file
    Remove-Item $sqlFile -ErrorAction SilentlyContinue
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✓ SQL Access Configured!                                      ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "The Function App '$($envConfig.FunctionAppName)' can now access" -ForegroundColor Gray
Write-Host "the database '$($envConfig.SqlDatabase)' using Managed Identity." -ForegroundColor Gray
Write-Host ""
Write-Host "Connection string (no secrets!):" -ForegroundColor Cyan
Write-Host "  Server=tcp:$sqlFqdn,1433;Database=$($envConfig.SqlDatabase);Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;" -ForegroundColor Gray
Write-Host ""
