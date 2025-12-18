# .NET Version Strategy for RajFinancial

## Current Configuration - ALL .NET 8

| Project | .NET Version | Reason |
|---------|--------------|--------|
| **Client** (Blazor WASM) | **.NET 8 LTS** | Oryx build system limitation (see below) |
| **Api** (Azure Functions) | **.NET 8 LTS** | Azure Functions runtime limitation |
| **Shared** (Class Library) | **.NET 8** | Matches all projects |
| **UnitTests** | **.NET 8** | Matches all projects |
| **AcceptanceTests** | **.NET 8** | Matches all projects |

## Why .NET 8 for Everything?

### The Oryx Build System Limitation

Azure Static Web Apps uses **Oryx** to build your application. Oryx has limited .NET SDK support:

**Supported .NET Versions in Oryx (Dec 2024):**
- ✅ .NET 3.1 (EOL)
- ✅ .NET 6
- ✅ .NET 7 (EOL)
- ✅ **.NET 8** (LTS - **MAXIMUM**)
- ❌ .NET 9 (not available)
- ❌ .NET 10 (not available)

### Why Can't Client Use .NET 10?

**Common Misconception:** "Blazor WASM runs in the browser, so it doesn't need Azure runtime support"

**Reality:**
1. Blazor WASM *runs* in the browser ✅
2. But Azure SWA must *build* it first using Oryx ❌
3. Oryx doesn't have .NET 10 SDK installed
4. Build fails with: `Platform 'dotnet' version '10.0' is unsupported`

**Solution:** Use .NET 8 for Client until Oryx adds .NET 10 support

### Why Can't Api Use .NET 10?

**Two limitations:**
1. **Oryx Build**: Oryx must build the Functions project (same as Client issue)
2. **Azure Functions Runtime**: Even if built locally, Azure Functions runtime doesn't support .NET 10 yet

## When Will .NET 9/10 Be Supported?

### Oryx (Build System)
- **Status**: Tracks .NET releases with 2-6 month delay
- **Check**: https://github.com/microsoft/Oryx/releases
- Expected timeline:
  - .NET 9 support: Q1-Q2 2025
  - .NET 10 support: Q3-Q4 2025

### Azure Functions Runtime
- **Status**: Prioritizes LTS versions
- **Check**: https://github.com/Azure/static-web-apps/issues
- Expected timeline:
  - .NET 9 support: Q2 2025
  - .NET 10 support: Q1 2026 (after it becomes LTS)

### Client (Blazor WASM)
- Can upgrade when **Oryx** adds support (earlier timeline)
- Doesn't need Azure Functions runtime support

## Migration Path

### When Oryx Adds .NET 10 Support

You can upgrade Client immediately:

```xml
<!-- Client can upgrade to .NET 10 -->
<TargetFramework>net10.0</TargetFramework>
```

Api, Shared, and Tests must wait for Azure Functions support.

### When Azure Functions Adds .NET 10 Support

Upgrade all projects:

```xml
<!-- All projects can upgrade to .NET 10 -->
<TargetFramework>net10.0</TargetFramework>
```

## Alternative: Self-Hosted Build

If you need .NET 10 immediately:

1. Build locally with .NET 10 SDK
2. Deploy pre-built artifacts to Azure SWA
3. Bypasses Oryx limitations
4. More complex CI/CD setup

**Recommendation:** Wait for Oryx support. .NET 8 is LTS and supported until November 2026.

---

**Last Updated**: December 18, 2024  
**Current Limitation**: Oryx build system (Azure SWA)  
**All Projects**: .NET 8 LTS  
**Next Upgrade**: When Oryx adds .NET 9/10 support

