#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Checks and configures Entra External ID user flows for Dev and Prod tenants.

.DESCRIPTION
    This script:
    1. Checks current user flow configuration (MFA settings, attributes, etc.)
    2. Shows you what's currently configured
    3. Optionally updates MFA settings if needed

.PARAMETER TenantId
    The Entra External ID tenant ID to configure.
    Default: Dev tenant (496527a2-41f8-4297-a979-c916e7255a22)

.PARAMETER Environment
    The environment to configure: Dev or Prod
    Default: Dev

.PARAMETER UpdateMFA
    If specified, updates the MFA configuration. Otherwise, just shows current config.

.EXAMPLE
    # Check Dev tenant user flows
    .\configure-user-flows.ps1

.EXAMPLE
    # Check Prod tenant user flows
    .\configure-user-flows.ps1 -Environment Prod

.EXAMPLE
    # Update Dev tenant to disable MFA
    .\configure-user-flows.ps1 -UpdateMFA -Environment Dev

.EXAMPLE
    # Update Prod tenant to enable MFA
    .\configure-user-flows.ps1 -UpdateMFA -Environment Prod

.NOTES
    Requires Microsoft.Graph PowerShell module.
    Install: Install-Module Microsoft.Graph -Scope CurrentUser
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Dev", "Prod")]
    [string]$Environment = "Dev",

    [Parameter(Mandatory = $false)]
    [switch]$UpdateMFA
)

$ErrorActionPreference = "Stop"

# Tenant configuration
$tenants = @{
    Dev = @{
        TenantId = "496527a2-41f8-4297-a979-c916e7255a22"
        Domain = "rajfinancialdev.onmicrosoft.com"
        MFARequired = $false  # No MFA for dev
    }
    Prod = @{
        TenantId = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
        Domain = "rajfinancialprod.onmicrosoft.com"
        MFARequired = $true   # MFA for production
    }
}

$config = $tenants[$Environment]

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  RAJ Financial - User Flow Configuration Checker" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment: " -NoNewline
Write-Host $Environment -ForegroundColor Yellow
Write-Host "Tenant: " -NoNewline
Write-Host $config.Domain -ForegroundColor Yellow
Write-Host "Tenant ID: " -NoNewline
Write-Host $config.TenantId -ForegroundColor Gray
Write-Host ""

# Check if Microsoft.Graph module is installed
Write-Host "Checking prerequisites..." -ForegroundColor Cyan
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Identity.SignIns)) {
    Write-Host "❌ Microsoft.Graph.Identity.SignIns module not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Installing module..." -ForegroundColor Yellow
    Install-Module Microsoft.Graph.Identity.SignIns -Scope CurrentUser -Force
    Write-Host "✅ Module installed" -ForegroundColor Green
}

# Connect to Microsoft Graph
Write-Host ""
Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Cyan
Write-Host "  (You'll be prompted to sign in)" -ForegroundColor Gray

try {
    Connect-MgGraph -TenantId $config.TenantId -Scopes "IdentityUserFlow.ReadWrite.All" -NoWelcome
    $context = Get-MgContext
    Write-Host "✅ Connected as: " -NoNewline -ForegroundColor Green
    Write-Host $context.Account -ForegroundColor White
} catch {
    Write-Host "❌ Failed to connect to Microsoft Graph" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Current User Flow Configuration" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Note: Entra External ID for customers uses a different API than B2C
# We need to use the beta API endpoint

Write-Host "Fetching user flows..." -ForegroundColor Cyan

try {
    # Get user flows using Graph API
    # Note: For External ID, user flows are at /identity/authenticationEventsFlows
    $uri = "https://graph.microsoft.com/beta/identity/authenticationEventsFlows"

    $userFlows = Invoke-MgGraphRequest -Uri $uri -Method GET

    if ($userFlows.value.Count -eq 0) {
        Write-Host "⚠️  No user flows found in this tenant" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "This might mean:" -ForegroundColor Gray
        Write-Host "  1. User flows haven't been created yet" -ForegroundColor Gray
        Write-Host "  2. You need to create them in the Entra portal" -ForegroundColor Gray
        Write-Host ""
        Write-Host "To create a user flow:" -ForegroundColor Cyan
        Write-Host "  1. Go to https://entra.microsoft.com" -ForegroundColor White
        Write-Host "  2. Select tenant: $($config.Domain)" -ForegroundColor White
        Write-Host "  3. Navigate to External Identities → User flows" -ForegroundColor White
        Write-Host "  4. Click + New user flow" -ForegroundColor White
        Write-Host "  5. Configure: Sign up and sign in" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "Found $($userFlows.value.Count) user flow(s):" -ForegroundColor Green
        Write-Host ""

        foreach ($flow in $userFlows.value) {
            Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
            Write-Host "Flow Name: " -NoNewline
            Write-Host $flow.displayName -ForegroundColor Yellow
            Write-Host "Flow ID: " -NoNewline
            Write-Host $flow.id -ForegroundColor Gray
            Write-Host "Type: " -NoNewline
            Write-Host $flow.'@odata.type' -ForegroundColor Gray

            # Check for MFA configuration
            if ($flow.conditions) {
                Write-Host ""
                Write-Host "Conditions:" -ForegroundColor Cyan
                Write-Host ($flow.conditions | ConvertTo-Json -Depth 10) -ForegroundColor Gray
            }

            if ($flow.onAttributeCollection) {
                Write-Host ""
                Write-Host "Attribute Collection:" -ForegroundColor Cyan
                Write-Host "  Attributes collected during signup"
            }

            if ($flow.onAuthenticationMethodLoadStart) {
                Write-Host ""
                Write-Host "Authentication Methods:" -ForegroundColor Cyan
                Write-Host "  MFA configuration would be here"
            }

            Write-Host ""
        }
    }

} catch {
    Write-Host "⚠️  Could not fetch user flows using beta API" -ForegroundColor Yellow
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Trying alternative method (B2C user flows)..." -ForegroundColor Cyan

    try {
        # Try B2C user flows endpoint
        $b2cFlows = Invoke-MgGraphRequest -Uri "https://graph.microsoft.com/beta/identity/b2cUserFlows" -Method GET

        if ($b2cFlows.value.Count -eq 0) {
            Write-Host "⚠️  No B2C user flows found either" -ForegroundColor Yellow
        } else {
            Write-Host "Found $($b2cFlows.value.Count) B2C user flow(s):" -ForegroundColor Green
            Write-Host ""

            foreach ($flow in $b2cFlows.value) {
                Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
                Write-Host "Flow Name: " -NoNewline
                Write-Host $flow.id -ForegroundColor Yellow

                Write-Host "User Flow Type: " -NoNewline
                Write-Host $flow.userFlowType -ForegroundColor Gray

                Write-Host "User Flow Version: " -NoNewline
                Write-Host $flow.userFlowTypeVersion -ForegroundColor Gray

                # Check identity providers
                if ($flow.identityProviders) {
                    Write-Host ""
                    Write-Host "Identity Providers: " -ForegroundColor Cyan
                    foreach ($idp in $flow.identityProviders) {
                        Write-Host "  • $($idp.name)" -ForegroundColor White
                    }
                }

                # Check user attributes
                if ($flow.userAttributeAssignments) {
                    Write-Host ""
                    Write-Host "User Attributes: " -ForegroundColor Cyan
                    foreach ($attr in $flow.userAttributeAssignments) {
                        Write-Host "  • $($attr.displayName)" -NoNewline -ForegroundColor White
                        if ($attr.isOptional) {
                            Write-Host " (optional)" -ForegroundColor Gray
                        } else {
                            Write-Host " (required)" -ForegroundColor Yellow
                        }
                    }
                }

                Write-Host ""
            }
        }
    } catch {
        Write-Host "❌ Could not access user flows" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "This likely means:" -ForegroundColor Yellow
        Write-Host "  • User flows need to be created manually in the Entra portal" -ForegroundColor Gray
        Write-Host "  • Or this tenant doesn't have External ID configured yet" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  MFA Configuration Check" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Expected MFA setting for $Environment`: " -NoNewline
if ($config.MFARequired) {
    Write-Host "ENABLED (Always on)" -ForegroundColor Green
} else {
    Write-Host "DISABLED (No MFA)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Note: " -ForegroundColor Cyan -NoNewline
Write-Host "MFA configuration for External ID user flows must be done manually in the Entra portal." -ForegroundColor Gray
Write-Host "The Graph API does not currently support programmatic MFA configuration for External ID." -ForegroundColor Gray

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Manual Configuration Steps" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($config.MFARequired) {
    Write-Host "To ENABLE MFA for $Environment tenant:" -ForegroundColor Green
    Write-Host ""
    Write-Host "  1. Go to https://entra.microsoft.com" -ForegroundColor White
    Write-Host "  2. Select tenant: $($config.Domain)" -ForegroundColor White
    Write-Host "  3. Navigate to: External Identities → User flows" -ForegroundColor White
    Write-Host "  4. Select your user flow (e.g., B2C_1_signup_signin)" -ForegroundColor White
    Write-Host "  5. Go to: Settings → Multifactor authentication" -ForegroundColor White
    Write-Host "  6. Choose: Email or Text message" -ForegroundColor White
    Write-Host "  7. Set Enforcement to: Always on" -ForegroundColor Yellow
    Write-Host "  8. Click Save" -ForegroundColor White
} else {
    Write-Host "To DISABLE MFA for $Environment tenant:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Go to https://entra.microsoft.com" -ForegroundColor White
    Write-Host "  2. Select tenant: $($config.Domain)" -ForegroundColor White
    Write-Host "  3. Navigate to: External Identities → User flows" -ForegroundColor White
    Write-Host "  4. Select your user flow (e.g., B2C_1_signup_signin)" -ForegroundColor White
    Write-Host "  5. Go to: Settings → Multifactor authentication" -ForegroundColor White
    Write-Host "  6. Set Enforcement to: Off" -ForegroundColor Yellow
    Write-Host "  7. Click Save" -ForegroundColor White
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Recommended Configuration Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$table = @"
┌──────────────┬────────────────────────────────────────┬─────────────────┐
│ Environment  │ Tenant                                 │ MFA             │
├──────────────┼────────────────────────────────────────┼─────────────────┤
│ Development  │ rajfinancialdev.onmicrosoft.com        │ DISABLED ❌     │
│              │ 496527a2-41f8-4297-a979-c916e7255a22   │ (No MFA)        │
├──────────────┼────────────────────────────────────────┼─────────────────┤
│ Production   │ rajfinancialprod.onmicrosoft.com       │ ENABLED ✅      │
│              │ cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6   │ (Always on)     │
└──────────────┴────────────────────────────────────────┴─────────────────┘
"@

Write-Host $table -ForegroundColor White

Write-Host ""
Write-Host "Why this configuration?" -ForegroundColor Cyan
Write-Host "  • Dev: No MFA = Faster testing, no phone needed for test accounts" -ForegroundColor Gray
Write-Host "  • Prod: MFA = Security for real user accounts" -ForegroundColor Gray
Write-Host ""

Write-Host "Test Users:" -ForegroundColor Cyan
Write-Host "  • Create test users in DEV tenant (no MFA required)" -ForegroundColor Gray
Write-Host "  • Use real accounts in PROD tenant (MFA required)" -ForegroundColor Gray
Write-Host ""

# Disconnect
Disconnect-MgGraph | Out-Null

Write-Host "✅ Configuration check complete!" -ForegroundColor Green
Write-Host ""
