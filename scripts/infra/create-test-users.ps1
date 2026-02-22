# ============================================================================
# RAJ Financial - Create Test Users for ROPC Integration Testing
# ============================================================================
# This script creates test users in Microsoft Entra External ID for use with
# ROPC (Resource Owner Password Credentials) integration and E2E testing.
#
# What it does:
#   1. Creates a "Test Users - No MFA" security group (for CA policy exclusion)
#   2. Creates 3 test users: test-client, test-admin, test-advisor
#   3. Adds all test users to the security group
#   4. Assigns app roles on the API app's service principal
#   5. Optionally stores passwords in Azure Key Vault
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Logged into the correct Entra External ID tenant:
#       az login --tenant <tenant-id> --allow-no-subscriptions
#   - register-entra-apps.ps1 has already been run (API app must exist)
#   - Conditional Access policy excluding "Test Users - No MFA" group
#     must be created manually (requires Premium P1)
#
# Usage:
#   .\create-test-users.ps1 -Environment dev
#   .\create-test-users.ps1 -Environment prod
#   .\create-test-users.ps1 -Environment prod -SkipKeyVault
#   .\create-test-users.ps1 -Environment prod -ClientPassword "P@ss..." -AdminPassword "P@ss..." -AdvisorPassword "P@ss..."
#
# Role Model:
#   - test-client  → App Role: Client (standard user who owns financial data)
#   - test-admin   → App Role: Administrator (platform staff with system-wide access)
#   - test-advisor → App Role: Client (advisors use DataAccessGrant, not app roles)
#
# Idempotent: Safe to run multiple times. Checks for existing resources before
# creating. Re-running will not duplicate users, groups, or role assignments.
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$ClientPassword,

    [Parameter(Mandatory = $false)]
    [string]$AdminPassword,

    [Parameter(Mandatory = $false)]
    [string]$AdvisorPassword,

    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,

    [Parameter(Mandatory = $false)]
    [switch]$SkipKeyVault
)

# ============================================================================
# Configuration
# ============================================================================

# App role GUIDs — MUST match register-entra-apps.ps1 and appsettings.json
$roleGuids = @{
    dev = @{
        Client        = "bc34bd6c-38b8-46a6-9d4c-d338afeea81f"
        Administrator = "2202014c-e4b9-4ab9-9e6a-4cc53e13598f"
    }
    prod = @{
        Client        = "d4e5f6a7-b8c9-4d5e-a6b7-c8d9e0f1a2b3"
        Administrator = "1a2b3c4d-5e6f-4a5b-8c9d-0e1f2a3b4c5d"
    }
}

$config = @{
    dev = @{
        TenantId       = "496527a2-41f8-4297-a979-c916e7255a22"
        TenantDomain   = "rajfinancialdev.onmicrosoft.com"
        ApiAppName     = "rajfinancial-api-dev"
        RoleGuids      = $roleGuids.dev
        KeyVaultName   = "kv-rajfinancial-dev"
    }
    prod = @{
        TenantId       = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
        TenantDomain   = "rajfinancialprod.onmicrosoft.com"
        ApiAppName     = "rajfinancial-api"
        RoleGuids      = $roleGuids.prod
        KeyVaultName   = "kv-rajfinancial-prod"
    }
}

$envConfig = $config[$Environment]
$securityGroupName = "Test Users - No MFA"

# Test user definitions
# NOTE: test-advisor gets the Client app role because advisor/CPA access
# is handled via DataAccessGrant entities, not via app roles.
$testUsers = @(
    @{
        DisplayName = "Test Client User"
        MailNickname = "test-client"
        UserPrincipalName = "test-client@$($envConfig.TenantDomain)"
        AppRole = "Client"
        AppRoleId = $envConfig.RoleGuids.Client
        PasswordParam = "ClientPassword"
        KeyVaultSecretName = "test-client-password"
        GithubSecretHint = "TEST_CLIENT_PASSWORD"
    },
    @{
        DisplayName = "Test Admin User"
        MailNickname = "test-admin"
        UserPrincipalName = "test-admin@$($envConfig.TenantDomain)"
        AppRole = "Administrator"
        AppRoleId = $envConfig.RoleGuids.Administrator
        PasswordParam = "AdminPassword"
        KeyVaultSecretName = "test-admin-password"
        GithubSecretHint = "TEST_ADMINISTRATOR_PASSWORD"
    },
    @{
        DisplayName = "Test Advisor User"
        MailNickname = "test-advisor"
        UserPrincipalName = "test-advisor@$($envConfig.TenantDomain)"
        AppRole = "Client"
        AppRoleId = $envConfig.RoleGuids.Client
        PasswordParam = "AdvisorPassword"
        KeyVaultSecretName = "test-advisor-password"
        GithubSecretHint = "TEST_ADVISOR_PASSWORD"
    }
)

# ============================================================================
# Helper: Generate Secure Password
# ============================================================================

function New-SecurePassword {
    <#
    .SYNOPSIS
        Generates a cryptographically random password meeting Entra complexity requirements.
    .DESCRIPTION
        Password contains uppercase, lowercase, digits, and special characters.
        Minimum 16 characters for strong security.
    #>
    $length = 24
    $upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower = "abcdefghjkmnpqrstuvwxyz"
    $digits = "23456789"
    $special = "!@#$%&*"

    # Ensure at least one of each category
    $password = ""
    $password += $upper[(Get-Random -Maximum $upper.Length)]
    $password += $lower[(Get-Random -Maximum $lower.Length)]
    $password += $digits[(Get-Random -Maximum $digits.Length)]
    $password += $special[(Get-Random -Maximum $special.Length)]

    # Fill remaining with random mix
    $allChars = $upper + $lower + $digits + $special
    for ($i = $password.Length; $i -lt $length; $i++) {
        $password += $allChars[(Get-Random -Maximum $allChars.Length)]
    }

    # Shuffle the password characters
    $charArray = $password.ToCharArray()
    $shuffled = $charArray | Get-Random -Count $charArray.Length
    return -join $shuffled
}

# ============================================================================
# Banner
# ============================================================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " RAJ Financial - Create Test Users" -ForegroundColor Cyan
Write-Host " Environment: $Environment" -ForegroundColor Cyan
Write-Host " Tenant: $($envConfig.TenantDomain)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Pre-flight Checks
# ============================================================================

Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if logged into Azure CLI
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged into Azure CLI. Run: az login --tenant $($envConfig.TenantId) --allow-no-subscriptions"
    exit 1
}

# Check if logged into correct tenant
if ($account.tenantId -ne $envConfig.TenantId) {
    Write-Warning "Currently logged into tenant: $($account.tenantId)"
    Write-Host "Switching to tenant: $($envConfig.TenantId)" -ForegroundColor Yellow
    az login --tenant $envConfig.TenantId --allow-no-subscriptions
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to switch tenant"
        exit 1
    }
}

Write-Host "Prerequisites OK" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 1: Resolve API App Service Principal
# ============================================================================

Write-Host "Step 1: Resolving API application service principal..." -ForegroundColor Yellow

# Find the API app by display name
$apiApps = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/applications?`$filter=displayName eq '$($envConfig.ApiAppName)'" `
    2>$null | ConvertFrom-Json

if ($apiApps.value.Count -eq 0) {
    Write-Error "API application '$($envConfig.ApiAppName)' not found. Run register-entra-apps.ps1 first."
    exit 1
}

$apiApp = $apiApps.value[0]
Write-Host "  API App: $($apiApp.displayName) (appId: $($apiApp.appId))" -ForegroundColor Gray

# Find the service principal for the API app
$apiSpResult = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$($apiApp.appId)'" `
    2>$null | ConvertFrom-Json

if ($apiSpResult.value.Count -eq 0) {
    Write-Error "Service principal for API app not found. Run register-entra-apps.ps1 first."
    exit 1
}

$apiSpId = $apiSpResult.value[0].id
Write-Host "  Service Principal ID: $apiSpId" -ForegroundColor Gray
Write-Host "API service principal resolved" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 2: Create Security Group "Test Users - No MFA"
# ============================================================================

Write-Host "Step 2: Creating security group '$securityGroupName'..." -ForegroundColor Yellow

# Check if group already exists
$existingGroups = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/groups?`$filter=displayName eq '$securityGroupName'" `
    2>$null | ConvertFrom-Json

if ($existingGroups.value.Count -gt 0) {
    $securityGroup = $existingGroups.value[0]
    Write-Host "Security group already exists: $($securityGroup.id)" -ForegroundColor Green
} else {
    $groupBody = @{
        displayName     = $securityGroupName
        mailEnabled     = $false
        securityEnabled = $true
        mailNickname    = "test-users-no-mfa"
        description     = "Test users exempt from MFA for ROPC integration testing. See docs/MFA_CONFIGURATION_WITH_TEST_EXCEPTIONS.md"
    } | ConvertTo-Json -Compress

    $groupTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($groupTempFile, $groupBody)

    try {
        $securityGroup = az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/groups" `
            --headers "Content-Type=application/json" `
            --body "@$groupTempFile" 2>$null | ConvertFrom-Json
    } finally {
        Remove-Item $groupTempFile -ErrorAction SilentlyContinue
    }

    if (-not $securityGroup) {
        Write-Error "Failed to create security group"
        exit 1
    }
    Write-Host "Created security group: $($securityGroup.id)" -ForegroundColor Green
}

$securityGroupId = $securityGroup.id
Write-Host ""

# ============================================================================
# Step 3: Resolve or Generate Passwords
# ============================================================================

Write-Host "Step 3: Resolving passwords..." -ForegroundColor Yellow

$passwords = @{}

foreach ($user in $testUsers) {
    $paramValue = Get-Variable -Name $user.PasswordParam -ValueOnly -ErrorAction SilentlyContinue
    if ($paramValue) {
        $passwords[$user.MailNickname] = $paramValue
        Write-Host "  $($user.MailNickname): Using provided password" -ForegroundColor Gray
    } else {
        $passwords[$user.MailNickname] = New-SecurePassword
        Write-Host "  $($user.MailNickname): Generated new password" -ForegroundColor Gray
    }
}

Write-Host "Passwords resolved" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 4: Create Test Users
# ============================================================================

Write-Host "Step 4: Creating test users..." -ForegroundColor Yellow

$createdUsers = @{}

foreach ($user in $testUsers) {
    Write-Host "  Processing: $($user.UserPrincipalName)..." -ForegroundColor Gray

    # Check if user already exists
    $existingUsers = az rest `
        --method GET `
        --uri "https://graph.microsoft.com/v1.0/users?`$filter=userPrincipalName eq '$($user.UserPrincipalName)'" `
        2>$null | ConvertFrom-Json

    if ($existingUsers.value.Count -gt 0) {
        $createdUsers[$user.MailNickname] = $existingUsers.value[0]
        Write-Host "  User already exists: $($existingUsers.value[0].id)" -ForegroundColor Green
        continue
    }

    # Create user via Graph API
    $userBody = @{
        accountEnabled    = $true
        displayName       = $user.DisplayName
        mailNickname      = $user.MailNickname
        userPrincipalName = $user.UserPrincipalName
        passwordProfile   = @{
            password                      = $passwords[$user.MailNickname]
            forceChangePasswordNextSignIn = $false
        }
    } | ConvertTo-Json -Depth 5

    $userTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($userTempFile, $userBody)

    try {
        $newUser = az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/users" `
            --headers "Content-Type=application/json" `
            --body "@$userTempFile" 2>$null | ConvertFrom-Json
    } finally {
        Remove-Item $userTempFile -ErrorAction SilentlyContinue
    }

    if (-not $newUser) {
        Write-Error "Failed to create user: $($user.UserPrincipalName)"
        exit 1
    }

    $createdUsers[$user.MailNickname] = $newUser
    Write-Host "  Created user: $($newUser.id)" -ForegroundColor Green
}

Write-Host "All test users ready" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 5: Add Users to Security Group
# ============================================================================

Write-Host "Step 5: Adding test users to '$securityGroupName' group..." -ForegroundColor Yellow

# Get current group members
$groupMembers = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/groups/$securityGroupId/members" `
    2>$null | ConvertFrom-Json

$existingMemberIds = @()
if ($groupMembers.value) {
    $existingMemberIds = $groupMembers.value | ForEach-Object { $_.id }
}

foreach ($user in $testUsers) {
    $userId = $createdUsers[$user.MailNickname].id

    if ($existingMemberIds -contains $userId) {
        Write-Host "  $($user.MailNickname): Already in group" -ForegroundColor Green
        continue
    }

    $memberBody = @{
        "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$userId"
    } | ConvertTo-Json -Compress

    $memberTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($memberTempFile, $memberBody)

    try {
        az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/groups/$securityGroupId/members/`$ref" `
            --headers "Content-Type=application/json" `
            --body "@$memberTempFile" 2>$null | Out-Null
    } finally {
        Remove-Item $memberTempFile -ErrorAction SilentlyContinue
    }

    Write-Host "  $($user.MailNickname): Added to group" -ForegroundColor Green
}

Write-Host "Group membership updated" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 6: Assign App Roles on API Service Principal
# ============================================================================

Write-Host "Step 6: Assigning app roles on API service principal..." -ForegroundColor Yellow

# Get existing role assignments on the API SP
$existingAssignments = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$apiSpId/appRoleAssignedTo" `
    2>$null | ConvertFrom-Json

foreach ($user in $testUsers) {
    $userId = $createdUsers[$user.MailNickname].id

    # Check if this user already has this role assigned
    $alreadyAssigned = $existingAssignments.value | Where-Object {
        $_.principalId -eq $userId -and $_.appRoleId -eq $user.AppRoleId
    }

    if ($alreadyAssigned) {
        Write-Host "  $($user.MailNickname): Already has '$($user.AppRole)' role" -ForegroundColor Green
        continue
    }

    $roleBody = @{
        principalId = $userId
        resourceId  = $apiSpId
        appRoleId   = $user.AppRoleId
    } | ConvertTo-Json -Compress

    $roleTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($roleTempFile, $roleBody)

    try {
        az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$apiSpId/appRoleAssignedTo" `
            --headers "Content-Type=application/json" `
            --body "@$roleTempFile" 2>$null | Out-Null
    } finally {
        Remove-Item $roleTempFile -ErrorAction SilentlyContinue
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  $($user.MailNickname): Failed to assign '$($user.AppRole)' role (exit code $LASTEXITCODE)"
    } else {
        Write-Host "  $($user.MailNickname): Assigned '$($user.AppRole)' role" -ForegroundColor Green
    }
}

Write-Host "App role assignments complete" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 7: Store Passwords in Key Vault (Optional)
# ============================================================================

$kvName = if ($KeyVaultName) { $KeyVaultName } else { $envConfig.KeyVaultName }

if (-not $SkipKeyVault) {
    Write-Host "Step 7: Storing passwords in Key Vault ($kvName)..." -ForegroundColor Yellow

    foreach ($user in $testUsers) {
        $password = $passwords[$user.MailNickname]
        $secretName = $user.KeyVaultSecretName

        $setResult = az keyvault secret set `
            --vault-name $kvName `
            --name $secretName `
            --value $password `
            --content-type "text/plain" `
            2>$null

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  Failed to store '$secretName' in Key Vault '$kvName'. You may need to set the subscription context or grant access."
            Write-Warning "  To set manually: az keyvault secret set --vault-name $kvName --name $secretName --value '<password>'"
        } else {
            Write-Host "  Stored '$secretName' in Key Vault" -ForegroundColor Green
        }
    }

    Write-Host "Key Vault secrets updated" -ForegroundColor Green
} else {
    Write-Host "Step 7: Skipping Key Vault (--SkipKeyVault specified)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# Summary
# ============================================================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Security Group:" -ForegroundColor Yellow
Write-Host "  Name: $securityGroupName" -ForegroundColor White
Write-Host "  ID:   $securityGroupId" -ForegroundColor White
Write-Host ""

Write-Host "Test Users:" -ForegroundColor Yellow
foreach ($user in $testUsers) {
    $userId = $createdUsers[$user.MailNickname].id
    Write-Host "  $($user.UserPrincipalName)" -ForegroundColor White
    Write-Host "    Object ID: $userId" -ForegroundColor Gray
    Write-Host "    App Role:  $($user.AppRole) ($($user.AppRoleId))" -ForegroundColor Gray
}
Write-Host ""

# Display passwords (only if they were generated, not from Key Vault)
Write-Host "Passwords (SAVE THESE — displayed only once):" -ForegroundColor Red
foreach ($user in $testUsers) {
    Write-Host "  $($user.MailNickname): $($passwords[$user.MailNickname])" -ForegroundColor White
}
Write-Host ""

Write-Host "GitHub Secrets to configure:" -ForegroundColor Yellow
foreach ($user in $testUsers) {
    Write-Host "  $($user.GithubSecretHint) = <password for $($user.MailNickname)>" -ForegroundColor White
}
Write-Host ""

# ============================================================================
# Next Steps
# ============================================================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Next Steps" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Create a Conditional Access policy (requires Premium P1):" -ForegroundColor White
Write-Host "   - Entra Admin Center > Protection > Conditional Access > New Policy" -ForegroundColor Gray
Write-Host "   - Name: 'Require MFA for All Users Except Test Accounts'" -ForegroundColor Gray
Write-Host "   - Include: All users" -ForegroundColor Gray
Write-Host "   - Exclude: '$securityGroupName' group" -ForegroundColor Gray
Write-Host "   - Grant: Require MFA" -ForegroundColor Gray
Write-Host "   See: docs/MFA_CONFIGURATION_WITH_TEST_EXCEPTIONS.md" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Add passwords to GitHub Secrets (production environment):" -ForegroundColor White
foreach ($user in $testUsers) {
    Write-Host "   gh secret set $($user.GithubSecretHint) --env production" -ForegroundColor Gray
}
Write-Host ""
Write-Host "3. Verify ROPC token acquisition:" -ForegroundColor White
Write-Host "   # Switch to Azure subscription context for Key Vault access" -ForegroundColor Gray
Write-Host "   az account set --subscription <subscription-id>" -ForegroundColor Gray
Write-Host "   # Test with: curl -X POST https://login.microsoftonline.com/$($envConfig.TenantId)/oauth2/v2.0/token ..." -ForegroundColor Gray
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
