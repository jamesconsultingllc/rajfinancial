# Manage Entra Redirect URIs

[![James Consulting LLC](https://img.shields.io/badge/James%20Consulting%20LLC-0066CC?style=flat-square)](https://jamesconsulting.com)

A GitHub Action to automatically manage Microsoft Entra (Azure AD) app registration redirect URIs for Azure Static Web Apps preview environments.

## Overview

When using Azure Static Web Apps with custom authentication (Microsoft Entra ID / Azure AD B2C / Entra External ID), each preview environment needs its callback URL registered as a redirect URI in the app registration. This action automates adding and removing these redirect URIs.

## Usage

### Add redirect URI (on deployment)

```yaml
- name: Add Entra Redirect URI for Preview Environment
  uses: jamesconsultingllc/manage-entra-redirect-uris@v1
  with:
    action: add
    app-object-id: ${{ secrets.ENTRA_APP_OBJECT_ID }}
    origin: ${{ steps.builddeploy.outputs.static_web_app_url }}
    tenant-id: ${{ secrets.ENTRA_TENANT_ID }}
```

### Remove redirect URI (on cleanup)

```yaml
- name: Remove Entra Redirect URI for Preview Environment
  uses: jamesconsultingllc/manage-entra-redirect-uris@v1
  with:
    action: remove
    app-object-id: ${{ secrets.ENTRA_APP_OBJECT_ID }}
    origin: https://${{ steps.find-slot.outputs.slot }}.example.azurestaticapps.net
    tenant-id: ${{ secrets.ENTRA_TENANT_ID }}
```

## Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `action` | Yes | - | Action to perform: `add` or `remove` |
| `app-object-id` | Yes | - | The **Object ID** of the Entra app registration (not the Client/Application ID) |
| `origin` | Yes | - | The origin URL (e.g., `https://example.azurestaticapps.net`) |
| `callback-path` | No | `/.auth/login/aad/callback` | The callback path appended to the origin |
| `uri-type` | No | `web` | Type of redirect URI: `web` or `spa` |
| `tenant-id` | Yes | - | The Microsoft Entra tenant ID where the app registration exists |

## Finding the App Object ID

The `app-object-id` is the **Object ID**, not the Application (Client) ID:

1. Go to Azure Portal → Microsoft Entra ID → App registrations
2. Select your app
3. Copy the **Object ID** (not the Application/Client ID)

Or use Azure CLI:
```bash
az ad app list --display-name "your-app-name" --query "[].id" -o tsv
```

## Callback Paths

| Auth Provider | Default Callback Path |
|--------------|----------------------|
| Microsoft Entra ID (standard) | `/.auth/login/aad/callback` |
| Custom OpenID Connect | `/.auth/login/{provider-name}/callback` |
| Azure AD B2C | `/.auth/login/aad/callback` |

For custom OpenID Connect providers (like Entra External ID), specify the callback path:

```yaml
callback-path: '/.auth/login/entraExternalId/callback'
```

## Prerequisites

- Azure CLI must be available in the runner
- The workflow must have permission to authenticate to the Entra tenant
- The authenticated identity must have `Application.ReadWrite.All` or equivalent permissions

## Complete Workflow Example

```yaml
name: Azure Static Web Apps CI/CD

on:
  push:
    branches: [main, develop, 'feature/**']
  pull_request:
    types: [closed]
    branches: [main, develop]

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: "/"
          output_location: "dist"

      - name: Add Entra Redirect URI
        if: github.ref != 'refs/heads/main'
        uses: jamesconsultingllc/manage-entra-redirect-uris@v1
        with:
          action: add
          app-object-id: ${{ secrets.ENTRA_APP_OBJECT_ID }}
          origin: ${{ steps.builddeploy.outputs.static_web_app_url }}
          tenant-id: ${{ secrets.ENTRA_TENANT_ID }}

  cleanup:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Remove Entra Redirect URI
        uses: jamesconsultingllc/manage-entra-redirect-uris@v1
        with:
          action: remove
          app-object-id: ${{ secrets.ENTRA_APP_OBJECT_ID }}
          origin: https://${{ github.event.pull_request.number }}.example.azurestaticapps.net
          tenant-id: ${{ secrets.ENTRA_TENANT_ID }}
```

## License

MIT

