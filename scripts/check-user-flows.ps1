#!/usr/bin/env pwsh
# Quick check of current user flows in Dev tenant

$tenantId = "496527a2-41f8-4297-a979-c916e7255a22"

Write-Host "Connecting to Dev tenant..." -ForegroundColor Cyan
Connect-MgGraph -TenantId $tenantId -Scopes "IdentityUserFlow.Read.All" -NoWelcome

Write-Host ""
Write-Host "Checking External ID user flows..." -ForegroundColor Cyan

try {
    $flows = Invoke-MgGraphRequest -Uri "https://graph.microsoft.com/beta/identity/authenticationEventsFlows" -Method GET

    if ($flows.value.Count -eq 0) {
        Write-Host "No External ID user flows found" -ForegroundColor Yellow
    } else {
        foreach ($flow in $flows.value) {
            Write-Host "─────────────────────────────────────" -ForegroundColor Gray
            Write-Host "Flow: $($flow.displayName)" -ForegroundColor Green
            Write-Host "ID: $($flow.id)" -ForegroundColor Gray
            Write-Host "Type: $($flow.'@odata.type')" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "Could not fetch External ID flows, trying B2C..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Checking B2C user flows..." -ForegroundColor Cyan

try {
    $b2cFlows = Invoke-MgGraphRequest -Uri "https://graph.microsoft.com/beta/identity/b2cUserFlows" -Method GET

    if ($b2cFlows.value.Count -eq 0) {
        Write-Host "No B2C user flows found" -ForegroundColor Yellow
    } else {
        foreach ($flow in $b2cFlows.value) {
            Write-Host "─────────────────────────────────────" -ForegroundColor Gray
            Write-Host "Flow: $($flow.id)" -ForegroundColor Green
            Write-Host "Type: $($flow.userFlowType) v$($flow.userFlowTypeVersion)" -ForegroundColor Gray

            if ($flow.identityProviders) {
                Write-Host "Identity Providers:" -ForegroundColor Cyan
                $flow.identityProviders | ForEach-Object { Write-Host "  - $($_.name)" }
            }
        }
    }
} catch {
    Write-Host "Could not fetch B2C flows either" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Disconnect-MgGraph | Out-Null
