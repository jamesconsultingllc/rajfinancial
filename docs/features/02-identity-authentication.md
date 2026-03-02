# 02 — Identity & Authentication

> Entra External ID, MSAL React, user flows, MFA, JWT validation, multi-tenant strategy.

**ADO Tracking:** [Epic #288 — 02 - Identity & Authentication](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/288)

| # | Feature | State |
|---|---------|-------|
| [291](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/291) | JWT Validation Middleware | Done |
| [485](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/485) | Auth Functions & UserProfileService API | Done |
| [266](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/266) | Test Users & Security Policies | New |
| [267](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/267) | Entra User Flows Configuration | New |
| [502](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/502) | ROPC-Based Auth for Integration & E2E Tests | In Progress |

---

## Overview

RAJ Financial uses **Microsoft Entra External ID (CIAM)** for all authentication. Users sign up and sign in through Entra user flows. The React SPA authenticates via **MSAL React** and sends bearer tokens to the Azure Functions API, which validates JWTs server-side.

---

## Entra Tenant Configuration

RAJ Financial maintains **separate Entra tenants** per environment:

| Property | Development | Production |
|----------|------------|------------|
| **Tenant Domain** | `rajfinancialdev.onmicrosoft.com` | `rajfinancialprod.onmicrosoft.com` |
| **Tenant ID** | `496527a2-41f8-4297-a979-c916e7255a22` | `cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6` |
| **Tenant Type** | External (CIAM) | External (CIAM) |
| **Custom Domain** | `rajfinancialdev.ciamlogin.com` | `rajfinancialprod.ciamlogin.com` |

### App Registrations

| Registration | Purpose | Redirect URIs |
|-------------|---------|---------------|
| **SPA App** | React client authentication | `http://localhost:3000`, `https://stapp-rajfinancial-dev.azurestaticapps.net`, preview URLs |
| **API App** | Azure Functions token validation | — (API, no redirect) |

> The SPA app registration exposes no API permissions of its own. The API app registration defines **App Roles** (see [03-authorization-data-access.md](03-authorization-data-access.md)).

---

## MSAL React Configuration

### Auth Config

```typescript
// src/lib/auth-config.ts
import { Configuration, LogLevel } from '@azure/msal-browser';

/**
 * MSAL configuration for Entra External ID.
 * Uses environment variables set at build time.
 */
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_AZURE_AD_CLIENT_ID,
    authority: `https://${import.meta.env.VITE_AZURE_AD_DOMAIN}/${import.meta.env.VITE_AZURE_AD_TENANT_ID}`,
    knownAuthorities: [import.meta.env.VITE_AZURE_AD_DOMAIN],
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: (level, message) => {
        if (level === LogLevel.Error) {
          console.error(message);
        }
      },
    },
  },
};

/** Scopes requested when calling the API */
export const apiScopes = {
  access: [`api://${import.meta.env.VITE_AZURE_AD_API_CLIENT_ID}/access_as_user`],
};
```

### Auth Provider Setup

```tsx
// src/main.tsx
import { PublicClientApplication, EventType } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { msalConfig } from '@/lib/auth-config';

const msalInstance = new PublicClientApplication(msalConfig);

// Set active account after login
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const account = (event.payload as any).account;
    msalInstance.setActiveAccount(account);
  }
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  </StrictMode>
);
```

### Protected Route Component

```tsx
// src/components/auth/ProtectedRoute.tsx
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import { Navigate, Outlet } from 'react-router-dom';

/**
 * Route guard that redirects unauthenticated users to sign-in.
 * Shows loading state during MSAL interaction.
 */
export function ProtectedRoute() {
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  if (inProgress !== InteractionStatus.None) {
    return <LoadingSpinner />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
```

### API Client with Token Acquisition

```typescript
// src/services/api-client.ts
import { msalInstance } from '@/main';
import { apiScopes } from '@/lib/auth-config';

/**
 * Makes authenticated API calls by acquiring tokens silently.
 * Falls back to interactive login if silent acquisition fails.
 */
export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const account = msalInstance.getActiveAccount();
  if (!account) {
    throw new Error('No active account');
  }

  const tokenResponse = await msalInstance.acquireTokenSilent({
    scopes: apiScopes.access,
    account,
  });

  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${tokenResponse.accessToken}`,
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error = await response.json();
    throw new ApiError(error.code, error.message, response.status);
  }

  if (response.status === 204) return undefined as T;
  return response.json();
}
```

---

## User Flows

Entra External ID user flows handle:

| Flow | Description |
|------|-------------|
| **Sign up + Sign in** | Combined flow — email/password with optional social providers |
| **Password Reset** | Self-service password reset via email verification |
| **Profile Edit** | Update display name, profile attributes |

### User Flow Configuration

- **Identity Providers**: Email/password (MVP), Google (future), Microsoft Account (future)
- **User Attributes Collected**: Display Name, Email
- **Custom Attributes**: `UserTier` (free/premium), `CompanyName` (optional)
- **Branding**: Custom CSS, RF logo, gold accent colors per brand identity

---

## MFA Configuration

| Setting | Value |
|---------|-------|
| **MFA Method** | Email OTP (SMS optional for premium) |
| **Enforcement** | Required for all users |
| **Conditional Access** | Risk-based step-up for sensitive operations |

> **Test Exception**: Development tenant has MFA disabled for automated test accounts only. Production always enforces MFA.

---

## JWT Validation (API)

### Azure Functions Middleware

```csharp
// RAJFinancial.Api/Middleware/AuthenticationMiddleware.cs
namespace RAJFinancial.Api.Middleware;

/// <summary>
/// Validates JWT bearer tokens from Entra External ID.
/// Extracts user ID and roles from claims.
/// </summary>
public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TokenValidationParameters _tokenValidation;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest is null)
        {
            await next(context);
            return;
        }

        // Skip auth for anonymous endpoints
        var targetMethod = context.GetTargetFunctionMethod();
        if (targetMethod?.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
        {
            await next(context);
            return;
        }

        var authHeader = httpRequest.Headers
            .GetValues("Authorization")
            .FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            await WriteUnauthorized(context, "AUTH_REQUIRED");
            return;
        }

        var token = authHeader["Bearer ".Length..];
        var principal = await ValidateTokenAsync(token);

        if (principal is null)
        {
            await WriteUnauthorized(context, "AUTH_INVALID_TOKEN");
            return;
        }

        // Store claims in FunctionContext for downstream use
        context.Items["UserId"] = principal.FindFirst("oid")?.Value
            ?? principal.FindFirst("sub")?.Value;
        context.Items["UserRoles"] = principal.FindAll("roles")
            .Select(c => c.Value).ToList();

        await next(context);
    }
}
```

### Token Validation Configuration

```csharp
// Program.cs
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new TokenValidationParameters
    {
        ValidAudience = config["AzureAd:ApiClientId"],
        ValidIssuer = $"https://{config["AzureAd:Domain"]}/{config["AzureAd:TenantId"]}/v2.0",
        IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
        {
            // Fetch signing keys from Entra OIDC metadata
            var client = new HttpClient();
            var json = client.GetStringAsync(
                $"https://{config["AzureAd:Domain"]}/{config["AzureAd:TenantId"]}/discovery/v2.0/keys"
            ).Result;
            return new JsonWebKeySet(json).GetSigningKeys();
        },
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});
```

---

## Environment Variables (Client)

The React SPA reads its configuration from Vite env vars (all prefixed with `VITE_`).
For local development these live in `.env.development`. In Azure Static Web Apps
(and other hosted environments) these same keys must be configured as app settings
(they are **not** committed in a `.env.production` file).

```env
# .env.development (only 3 vars are read by the client)
VITE_AZURE_AD_AUTHORITY=https://YOUR_TENANT_NAME.ciamlogin.com/YOUR_TENANT_NAME.onmicrosoft.com
VITE_AZURE_AD_CLIENT_ID=YOUR_AZURE_AD_CLIENT_ID
VITE_MSAL_CACHE_LOCATION=sessionStorage

# Production values are injected by CI via .env.local (see GitHub workflow).
# The same 3 vars are required:
#   VITE_AZURE_AD_AUTHORITY=https://<tenant>.ciamlogin.com/<tenant>.onmicrosoft.com
#   VITE_AZURE_AD_CLIENT_ID=<spa-app-client-id>
#   VITE_MSAL_CACHE_LOCATION=sessionStorage
```

---

## OIDC Federated Credentials (CI/CD)

GitHub Actions authenticates to Azure via OIDC — no stored secrets:

```powershell
# scripts/setup-entra-oidc.ps1
# Creates federated identity credentials for GitHub Actions workflows
az ad app federated-credential create `
    --id <app-object-id> `
    --parameters '{
        "name": "github-actions-deploy",
        "issuer": "https://token.actions.githubusercontent.com",
        "subject": "repo:jamesconsultingllc/rajfinancial:ref:refs/heads/develop",
        "audiences": ["api://AzureADTokenExchange"]
    }'
```

---

## Entra Branding

Custom branding assets are stored in `infra/entra-branding/`:

| Asset | Description |
|-------|-------------|
| `banner-logo.png` | RF logo for sign-in page header |
| `sign-in-background.png` | Background image for sign-in page |
| `custom.css` | Gold accent styling per brand guidelines |

---

## Cross-References

- App Roles & RBAC: [03-authorization-data-access.md](03-authorization-data-access.md)
- Entra tenant infrastructure: [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Tier-gated features: [10-user-profile-settings.md](10-user-profile-settings.md)

---

*Last Updated: February 2026*
