# Find SWA Environment Slot

[![James Consulting LLC](https://img.shields.io/badge/James%20Consulting%20LLC-0066CC?style=flat-square)](https://jamesconsulting.com)

A GitHub Action to find an Azure Static Web App environment slot by branch pattern.

## Overview

When managing Azure Static Web App preview environments, you need to find the actual slot name that was created for a feature branch. SWA adds a hash suffix to slot names, so this action queries Azure to find the matching environment.

## Usage

```yaml
- name: Find SWA Slot
  id: find-slot
  uses: jamesconsultingllc/find-swa-slot@v1
  with:
    swa-name: my-static-web-app
    resource-group: my-resource-group
    slot-pattern: ${{ steps.resolve-env.outputs.sanitized-branch }}

- name: Use the found slot
  if: steps.find-slot.outputs.found == 'true'
  run: |
    echo "Slot: ${{ steps.find-slot.outputs.slot }}"
    echo "Origin: ${{ steps.find-slot.outputs.origin }}"
```

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| `swa-name` | Yes | The name of the Static Web App |
| `resource-group` | Yes | The Azure resource group containing the SWA |
| `slot-pattern` | Yes | The slot pattern to search for (typically the sanitized branch name from `resolve-swa-environment-action`) |

## Outputs

| Output | Description |
|--------|-------------|
| `slot` | The name of the found slot (empty if not found) |
| `found` | Whether a matching slot was found (`true` or `false`) |
| `origin` | The full origin URL of the found slot (e.g., `https://feature-abc-123.example.azurestaticapps.net`) |

## Prerequisites

- Azure CLI must be available and authenticated in the runner
- The authenticated identity must have read access to the Static Web App

## Complete Workflow Example

```yaml
name: Close Preview Environment

on:
  pull_request:
    types: [closed]
    branches: [main, develop]

jobs:
  cleanup:
    if: github.event.pull_request.merged == true
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Resolve Source Environment
        id: source-env
        uses: jamesconsultingllc/resolve-swa-environment-action@v1
        with:
          branch: ${{ github.event.pull_request.head.ref }}

      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Find SWA Slot
        id: find-slot
        uses: jamesconsultingllc/find-swa-slot@v1
        with:
          swa-name: my-static-web-app
          resource-group: my-resource-group
          slot-pattern: ${{ steps.source-env.outputs.sanitized-branch }}

      - name: Delete Preview Environment
        if: steps.find-slot.outputs.found == 'true'
        run: |
          az staticwebapp environment delete \
            --name my-static-web-app \
            --resource-group my-resource-group \
            --environment-name "${{ steps.find-slot.outputs.slot }}" \
            --yes

      - name: Remove Entra Redirect URI
        if: steps.find-slot.outputs.found == 'true'
        uses: jamesconsultingllc/manage-entra-redirect-uris@v1
        with:
          action: remove
          app-object-id: ${{ secrets.ENTRA_APP_OBJECT_ID }}
          origin: ${{ steps.find-slot.outputs.origin }}
          tenant-id: ${{ secrets.ENTRA_TENANT_ID }}
```

## How It Works

1. Takes the `slot-pattern` (typically from `resolve-swa-environment-action`)
2. Queries Azure for SWA environments matching the first 10 characters of the pattern
3. Returns the full slot name, whether it was found, and the origin URL

## Related Actions

- [resolve-swa-environment-action](https://github.com/jamesconsultingllc/resolve-swa-environment-action) - Resolves branch names to SWA environment patterns
- [manage-entra-redirect-uris](https://github.com/jamesconsultingllc/manage-entra-redirect-uris) - Manages Entra app registration redirect URIs
- [sync-swa-settings-action](https://github.com/jamesconsultingllc/sync-swa-settings-action) - Syncs SWA environment settings

## License

MIT

