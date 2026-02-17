#!/usr/bin/env pwsh
# Add a user or service principal to SQL database with appropriate roles

param(
    [Parameter()]
    [string]$PrincipalName,

    [Parameter()]
    [ValidateSet("User", "ServicePrincipal")]
    [string]$PrincipalType = "User",

    [Parameter()]
    [string]$Server = "rajfinancial-dev.database.windows.net",

    [Parameter()]
    [string]$Database = "rajfinancial",

    [Parameter()]
    [string[]]$Roles = @("db_datareader", "db_datawriter", "db_ddladmin")
)

$ErrorActionPreference = "Stop"

Import-Module SqlServer

# Get access token
Write-Host "Getting access token..." -ForegroundColor Yellow
$token = az account get-access-token --resource "https://database.windows.net/" --query accessToken -o tsv

# Resolve the principal name
if (-not $PrincipalName) {
    # Default: current signed-in user
    Write-Host "Getting signed-in user info..." -ForegroundColor Yellow
    $userJson = az ad signed-in-user show -o json
    $user = $userJson | ConvertFrom-Json
    $PrincipalName = $user.userPrincipalName
    $displayName = $user.displayName
}
elseif ($PrincipalType -eq "ServicePrincipal") {
    # Look up SP display name by app ID or name
    Write-Host "Looking up service principal: $PrincipalName" -ForegroundColor Yellow
    $spJson = az ad sp show --id $PrincipalName -o json 2>$null
    if ($spJson) {
        $sp = $spJson | ConvertFrom-Json
        $displayName = $sp.displayName
        $PrincipalName = $sp.displayName
    }
    else {
        $displayName = $PrincipalName
    }
}
else {
    $displayName = $PrincipalName
}

Write-Host "Adding principal: $displayName ($PrincipalName)" -ForegroundColor Cyan
Write-Host "Server: $Server / Database: $Database" -ForegroundColor Cyan
Write-Host "Roles: $($Roles -join ', ')" -ForegroundColor Cyan

# Build role assignment SQL
$rolesSql = ($Roles | ForEach-Object { "ALTER ROLE $_ ADD MEMBER [$PrincipalName];" }) -join "`n"

$sql = @"
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'$PrincipalName')
BEGIN
    CREATE USER [$PrincipalName] FROM EXTERNAL PROVIDER;
    PRINT 'Created user: $PrincipalName';
END
ELSE
BEGIN
    PRINT 'User already exists: $PrincipalName';
END

$rolesSql
PRINT 'Roles assigned successfully';
"@

Invoke-Sqlcmd -ServerInstance $Server -Database $Database -AccessToken $token -Query $sql -Verbose

Write-Host "`n✓ Principal added successfully!" -ForegroundColor Green
