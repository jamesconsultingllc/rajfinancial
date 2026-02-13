#!/usr/bin/env pwsh
# Add current signed-in user to SQL database

$ErrorActionPreference = "Stop"

Import-Module SqlServer

# Get access token
Write-Host "Getting access token..." -ForegroundColor Yellow
$token = az account get-access-token --resource "https://database.windows.net/" --query accessToken -o tsv

# Get current user info
Write-Host "Getting user info..." -ForegroundColor Yellow
$userJson = az ad signed-in-user show -o json
$user = $userJson | ConvertFrom-Json
$upn = $user.userPrincipalName
$displayName = $user.displayName

Write-Host "Adding user: $displayName ($upn)" -ForegroundColor Cyan

# SQL to add current user
$sql = @"
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'$upn')
BEGIN
    CREATE USER [$upn] FROM EXTERNAL PROVIDER;
    PRINT 'Created user: $upn';
END
ELSE
BEGIN
    PRINT 'User already exists: $upn';
END

ALTER ROLE db_datareader ADD MEMBER [$upn];
ALTER ROLE db_datawriter ADD MEMBER [$upn];
ALTER ROLE db_ddladmin ADD MEMBER [$upn];
PRINT 'Roles assigned successfully';
"@

Invoke-Sqlcmd -ServerInstance "rajfinancial-dev.database.windows.net" -Database "rajfinancial" -AccessToken $token -Query $sql -Verbose

Write-Host "`n✓ User added successfully!" -ForegroundColor Green
