# RAJ Financial - Blazor Hybrid Desktop Architecture

## Comparison: Blazor Static Web App vs. Blazor Hybrid Desktop

This document outlines what would change if RAJ Financial were built as a **Blazor Hybrid desktop application** (MAUI/WPF) instead of the current **Blazor WebAssembly + Azure Static Web Apps + Azure Functions** architecture.

---

## Executive Summary

| Aspect | Blazor WASM (Current Plan) | Blazor Hybrid Desktop |
|--------|---------------------------|----------------------|
| **Deployment** | Azure Static Web Apps | Windows installer (MSIX/ClickOnce) |
| **Backend** | Azure Functions (serverless) | Local services + optional cloud sync |
| **Data Storage** | Azure SQL (cloud) | SQLite (local) + optional cloud sync |
| **Offline Support** | Limited (PWA caching) | Full native offline |
| **Authentication** | Entra External ID (browser) | Entra External ID (embedded WebView) |
| **Updates** | Instant (refresh browser) | App store / auto-update |
| **Performance** | WebAssembly (slower startup) | Native .NET (faster startup) |
| **Distribution** | URL (universal access) | Windows Store / direct download |
| **Plaid Integration** | JS interop in browser | Embedded WebView for Link |
| **Target Users** | Any device with browser | Windows desktop users |

---

## Part 1: Architecture Changes

### Current Architecture (Blazor WASM + Azure)

```
┌─────────────────────────────────────────────────────────────────┐
│                        User's Browser                            │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Blazor WebAssembly Application                 │ │
│  │  (Downloaded from Azure Static Web Apps)                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                        HTTPS (Internet)
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Static Web Apps                          │
│              (Hosts Blazor WASM + Managed Functions)             │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Functions (.NET 9)                       │
│                  (Isolated Worker - API Layer)                   │
└─────────────────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Azure SQL   │     │  Azure Key Vault │     │  Azure Redis    │
└───────────────┘     └─────────────────┘     └─────────────────┘
```

### Hybrid Architecture (Blazor MAUI/WPF Desktop)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Desktop Application                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Blazor Hybrid (MAUI or WPF)                    │ │
│  │         Razor Components rendered in BlazorWebView          │ │
│  └─────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Local Services (.NET 9)                        │ │
│  │   AccountService │ AssetService │ AnalysisService │ etc.   │ │
│  └─────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Local Data Store                               │ │
│  │         SQLite + EF Core (encrypted at rest)                │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                        HTTPS (Optional Sync)
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Cloud Sync Services (Optional)                 │
│       Azure Functions for: Plaid webhooks, AI insights,         │
│       Cross-device sync, Professional portal access             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 2: Solution Structure Changes

### Current Structure (Web)

```
RAJFinancial/
├── src/
│   ├── RAJFinancial.Client/           # Blazor WASM
│   ├── RAJFinancial.Api/              # Azure Functions
│   ├── RAJFinancial.Core/             # Domain
│   ├── RAJFinancial.Application/      # Services
│   ├── RAJFinancial.Infrastructure/   # Data + External
│   └── RAJFinancial.Shared/           # DTOs
└── tests/
```

### Hybrid Structure (Desktop)

```
RAJFinancial/
├── src/
│   ├── RAJFinancial.Desktop/          # MAUI or WPF host
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml            # Hosts BlazorWebView
│   │   ├── MauiProgram.cs             # DI, services registration
│   │   └── Platforms/
│   │       └── Windows/
│   │
│   ├── RAJFinancial.UI/               # Shared Razor components
│   │   ├── Components/                # (same as current Client)
│   │   ├── Pages/
│   │   ├── State/                     # Fluxor state
│   │   └── wwwroot/                   # Static assets
│   │
│   ├── RAJFinancial.Core/             # Domain (unchanged)
│   ├── RAJFinancial.Application/      # Services (unchanged)
│   │
│   ├── RAJFinancial.Infrastructure/   # Data + External
│   │   ├── Data/
│   │   │   ├── LocalDbContext.cs      # SQLite context
│   │   │   └── Migrations/
│   │   ├── External/
│   │   │   ├── PlaidService.cs        # Direct API calls
│   │   │   └── ClaudeAIService.cs     # Direct API calls
│   │   └── Sync/
│   │       └── CloudSyncService.cs    # Optional cloud sync
│   │
│   ├── RAJFinancial.Shared/           # DTOs (unchanged)
│   │
│   └── RAJFinancial.CloudApi/         # Optional cloud backend
│       └── Functions/                 # Reduced scope
│           ├── PlaidWebhook.cs
│           ├── SyncEndpoints.cs
│           └── ProfessionalPortal.cs
│
└── tests/
```

---

## Part 3: Component-by-Component Changes

### 3.1 UI Layer (Razor Components)

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **Location** | `RAJFinancial.Client/` | `RAJFinancial.UI/` (shared library) |
| **Host** | Browser DOM | BlazorWebView in MAUI/WPF |
| **Styling** | CSS (same) | CSS (same - 95% reuse) |
| **JS Interop** | `IJSRuntime` for Plaid Link | `IJSRuntime` works in WebView |
| **Navigation** | Browser URL routing | WebView URL routing |
| **Window Controls** | N/A | Native title bar, minimize/maximize |

**What Changes:**
- Extract Razor components to shared `RAJFinancial.UI` class library
- Add native window chrome (optional custom title bar)
- Handle WebView-specific quirks (file dialogs, downloads)

**What Stays the Same:**
- All Razor components (`.razor` files)
- All CSS/design system
- Fluxor state management
- Syncfusion components (Blazor versions work in Hybrid)

### 3.2 Data Layer

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **Primary DB** | Azure SQL | SQLite (local file) |
| **Connection** | HTTP to Azure Functions | Direct EF Core access |
| **Encryption** | Azure-managed | SQLite encryption (SQLCipher) |
| **Backup** | Azure automated | Local + optional cloud sync |
| **Multi-device** | Automatic (cloud-first) | Requires sync implementation |

**What Changes:**
```csharp
// Web: DbContext talks to Azure SQL via Functions
public class CloudDbContext : DbContext
{
    // Azure SQL connection via Managed Identity
}

// Hybrid: DbContext talks to local SQLite
public class LocalDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RAJFinancial", "data.db");
        
        options.UseSqlite($"Data Source={dbPath}");
    }
}
```

### 3.3 Service Layer

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **Service Location** | Azure Functions (cloud) | In-process (.NET services) |
| **API Calls** | HttpClient to Functions | Direct method calls |
| **Authentication** | Bearer token to API | Local token storage + validation |
| **Secrets** | Azure Key Vault | Windows Credential Manager or DPAPI |

**What Changes:**
```csharp
// Web: Services injected via HttpClient
builder.Services.AddScoped<IAssetService>(sp => 
    new ApiAssetService(sp.GetRequiredService<HttpClient>()));

// Hybrid: Services injected directly
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddDbContext<LocalDbContext>();
```

### 3.4 Authentication

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **Provider** | Entra External ID | Entra External ID (same) |
| **Flow** | MSAL.js redirect/popup | MSAL.NET with embedded browser |
| **Token Storage** | Browser localStorage | Windows Credential Manager |
| **Refresh** | MSAL.js automatic | MSAL.NET automatic |

**What Changes:**
```csharp
// Web: MSAL.js via Blazor WASM
builder.Services.AddMsalAuthentication(options => { ... });

// Hybrid: MSAL.NET with WAM (Windows broker)
builder.Services.AddMsalAuthentication(options =>
{
    options.UseOperatingSystemAccount = true; // Windows SSO
    options.UseBroker = true; // Web Account Manager
});
```

### 3.5 Plaid Integration

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **Link UI** | Plaid Link JS in browser | Plaid Link in embedded WebView |
| **Token Exchange** | Via Azure Functions | Direct HTTPS to Plaid API |
| **Webhooks** | Azure Functions endpoint | Cloud function → push notification to app |
| **Secrets** | Key Vault | Encrypted local config or cloud fetch |

**What Changes:**
- Plaid Link still works (runs in WebView)
- Token exchange can be done directly from app
- Webhooks require a cloud endpoint to receive, then push to app
- Consider: keep thin cloud layer for webhooks

### 3.6 AI Integration (Claude)

| Aspect | Web (Current) | Hybrid (Desktop) |
|--------|--------------|------------------|
| **API Calls** | Via Azure Functions | Direct HTTPS to Anthropic |
| **Rate Limiting** | Server-side | Client-side (honor limits) |
| **Caching** | Azure Redis | Local SQLite cache table |
| **API Key** | Key Vault | Encrypted local storage or fetch at runtime |

**What Changes:**
```csharp
// Web: Calls go through Functions
var insights = await _httpClient.GetAsync("/api/analysis/insights");

// Hybrid: Direct API call
var insights = await _claudeService.GenerateInsightsAsync(userId);
```

**Security Consideration:** Storing API keys locally is less secure than Key Vault. Options:
1. Fetch API key from cloud at app startup (requires internet)
2. Use a proxy cloud function for AI calls
3. Accept local storage risk with encryption

---

## Part 4: What You Gain with Hybrid

### 4.1 Performance Benefits

| Metric | Web | Hybrid | Improvement |
|--------|-----|--------|-------------|
| **Cold Start** | 2-4 seconds (WASM download + parse) | <500ms (native) | 4-8x faster |
| **Data Access** | HTTP latency per request | Local SQLite (sub-ms) | 50-100x faster |
| **Memory** | Browser memory limits | Full system memory | No limits |
| **CPU** | Single-threaded WASM | Multi-threaded .NET | Full parallelism |

### 4.2 Offline Capabilities

```
Web (PWA):
- Service worker caching of static assets
- Limited offline data (IndexedDB)
- Requires internet for most operations

Hybrid:
- Full offline functionality
- All data stored locally
- Sync when online (optional)
- No internet required for core features
```

### 4.3 Native Integration

| Feature | Web | Hybrid |
|---------|-----|--------|
| File System | Limited (File API) | Full access |
| Notifications | Browser push | Windows native toast |
| System Tray | Not possible | Minimize to tray |
| Keyboard Shortcuts | Limited | Global hotkeys |
| Printing | Browser print dialog | Native print API |
| Clipboard | Restricted | Full access |

### 4.4 Distribution & Updates

```
Web:
- No installation needed
- Instant updates (browser refresh)
- Works on any device with browser
- No app store approval

Hybrid:
- Requires installation
- Auto-update via MSIX/Squirrel
- Windows only (or cross-platform with MAUI)
- Microsoft Store (optional) or direct download
- Can work without internet after install
```

---

## Part 5: What You Lose with Hybrid

### 5.1 Cross-Platform Reach

| Platform | Web | Hybrid (MAUI) | Hybrid (WPF) |
|----------|-----|---------------|--------------|
| Windows | ✅ | ✅ | ✅ |
| macOS | ✅ | ✅ | ❌ |
| Linux | ✅ | ❌ | ❌ |
| iOS | ✅ | ✅ | ❌ |
| Android | ✅ | ✅ | ❌ |
| Chromebook | ✅ | ❌ | ❌ |

### 5.2 Deployment Simplicity

```
Web:
- git push → CI/CD → live
- No installation friction
- No version fragmentation
- Easy A/B testing

Hybrid:
- Build → Sign → Publish → User downloads → Install
- Users on different versions
- Support burden for old versions
- Rollback is painful
```

### 5.3 Professional Portal Challenges

The current plan includes **professional access** (advisors, attorneys, accountants) to client data. With Hybrid:

| Scenario | Web | Hybrid |
|----------|-----|--------|
| Advisor views client dashboard | Easy - same app, different role | Need separate web portal OR sync |
| Client grants access | Server-side permission | Sync data to cloud for sharing |
| Multi-device access | Login from any browser | Install on each device + sync |

**Recommendation:** If professional portal is important, keep a cloud backend even with Hybrid.

---

## Part 6: Hybrid Implementation Options

### Option A: Pure Hybrid (No Cloud Backend)

```
Pros:
- Simplest architecture
- No cloud costs for personal use
- Complete privacy (data stays local)

Cons:
- No multi-device sync
- No professional portal
- Plaid webhooks won't work (need cloud endpoint)
- AI API keys stored locally (security concern)

Best For: Single-user, single-device, privacy-focused users
```

### Option B: Hybrid + Minimal Cloud Sync

```
┌─────────────────────────────────────────────────────────────────┐
│                    Desktop App (Primary)                         │
│     Local SQLite + All Features + Optional Cloud Sync           │
└─────────────────────────────────────────────────────────────────┘
                                │
                         Periodic Sync
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Minimal Cloud Backend                          │
│  - Plaid webhooks → push to devices                             │
│  - Sync endpoint (encrypted blobs)                              │
│  - AI proxy (optional, for key security)                        │
└─────────────────────────────────────────────────────────────────┘

Pros:
- Desktop-first with cloud backup
- Multi-device sync
- Plaid webhooks work

Cons:
- Still need some cloud infrastructure
- Sync conflict resolution complexity

Best For: Power users who want local-first + backup
```

### Option C: Hybrid + Full Cloud Backend (Current Plan + Desktop Client)

```
┌──────────────────┐     ┌──────────────────┐
│   Desktop App    │     │    Web App       │
│  (Blazor Hybrid) │     │  (Blazor WASM)   │
└────────┬─────────┘     └────────┬─────────┘
         │                        │
         └────────────┬───────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │   Azure Functions API  │
         │   (Full Backend)       │
         └────────────────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │      Azure SQL         │
         └────────────────────────┘

Pros:
- Best of both worlds
- Same backend for web and desktop
- Desktop gets offline cache + faster UI
- Web for quick access / professional portal

Cons:
- Two client apps to maintain
- Desktop is "just" a faster shell for the same backend
- Most complexity

Best For: Enterprise users who want desktop experience + web flexibility
```

---

## Part 7: Recommended Approach

### For RAJ Financial MVP

**Stick with Web (Blazor WASM + Azure Functions)** for MVP because:

1. **Faster time to market** - No installer/update infrastructure
2. **Professional portal** - Advisors/attorneys need web access
3. **Plaid webhooks** - Require cloud endpoint anyway
4. **Cross-device** - Users expect access from phone/tablet
5. **Lower complexity** - Single deployment target

### Future Hybrid Consideration

Consider adding a Hybrid desktop client **after MVP** when:

- Users request offline access
- Performance becomes a pain point
- You want premium "desktop app" experience
- Revenue justifies dual-client maintenance

### If You Choose Hybrid Now

**Go with Option B (Hybrid + Minimal Cloud)**:

1. Use **MAUI Blazor Hybrid** for cross-platform potential
2. Keep **thin Azure Functions layer** for:
   - Plaid webhooks
   - AI API proxy (key security)
   - Sync endpoint
3. Store **primary data locally** in encrypted SQLite
4. Build **professional portal** as separate minimal web app later

---

## Part 8: Migration Path (Web → Hybrid)

If you build Web first and want to add Hybrid later:

### Phase 1: Prepare for Extraction
```
Current:
  RAJFinancial.Client/
    ├── Components/
    ├── Pages/
    └── wwwroot/

Refactor to:
  RAJFinancial.UI/           # Shared Razor Class Library
    ├── Components/
    ├── Pages/
    └── wwwroot/
  
  RAJFinancial.Client/       # Web host (thin)
    └── Program.cs           # Just bootstraps UI library
```

### Phase 2: Add Desktop Host
```
Add:
  RAJFinancial.Desktop/      # MAUI host
    ├── MauiProgram.cs       # Bootstraps same UI library
    └── MainWindow.xaml
```

### Phase 3: Add Local Data Layer
```
Add:
  RAJFinancial.Infrastructure/
    └── Data/
        ├── LocalDbContext.cs
        └── SyncService.cs
```

### Shared Code Percentage

| Layer | Web | Hybrid | Shared |
|-------|-----|--------|--------|
| Razor Components | 100% | 100% | ✅ 100% |
| CSS/Design | 100% | 100% | ✅ 100% |
| Fluxor State | 100% | 100% | ✅ 100% |
| DTOs (Shared) | 100% | 100% | ✅ 100% |
| Core Domain | 100% | 100% | ✅ 100% |
| Application Services | 100% | ~80% | ⚠️ Interface same, impl differs |
| Infrastructure | 100% | ~30% | ⚠️ Different data layer |
| Host/Bootstrap | 100% | 0% | ❌ Different hosts |

**Total Reuse: ~85%** - Worth the investment if you plan to go Hybrid later.

---

## Summary

| Decision Factor | Web Wins | Hybrid Wins |
|-----------------|----------|-------------|
| Time to MVP | ✅ | |
| Cross-platform reach | ✅ | |
| Professional portal | ✅ | |
| No installation friction | ✅ | |
| Offline-first | | ✅ |
| Performance | | ✅ |
| Native integrations | | ✅ |
| Data privacy (local-only) | | ✅ |
| Single-device power user | | ✅ |

**Recommendation:** Build Web first (current plan), architect for future Hybrid extraction by keeping UI in a shared library. Add Hybrid desktop client post-MVP if user demand justifies it.
