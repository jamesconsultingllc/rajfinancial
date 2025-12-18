# RAJ Financial Software - Complete UI Documentation
## Blazor WebAssembly + Syncfusion + Premium Design System

---

## Brand Identity

```
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║     ██████╗  █████╗      ██╗    ███████╗██╗███╗   ██╗       ║
║     ██╔══██╗██╔══██╗     ██║    ██╔════╝██║████╗  ██║       ║
║     ██████╔╝███████║     ██║    █████╗  ██║██╔██╗ ██║       ║
║     ██╔══██╗██╔══██║██   ██║    ██╔══╝  ██║██║╚██╗██║       ║
║     ██║  ██║██║  ██║╚█████╔╝    ██║     ██║██║ ╚████║       ║
║     ╚═╝  ╚═╝╚═╝  ╚═╝ ╚════╝     ╚═╝     ╚═╝╚═╝  ╚═══╝       ║
║                                                              ║
║                    Your Financial Future                     ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

**Name**: RAJ Financial Software  
**Logo**: RF monogram with wing motif (gold gradient)  
**Font**: Nexa XBold (display), Inter (body)  
**Colors**:
- `#fffbcc` - Lemon Chiffon (lightest backgrounds)
- `#fff7b3` - Light cream
- `#f5e99a` - Soft gold
- `#eed688` - Flax (secondary elements)
- `#e8c94d` - Bright gold
- `#ebbb10` - Spanish Yellow (PRIMARY)
- `#d4a80e` - Rich gold
- `#c3922e` - UC Gold (accent/depth)
- `#a67c26` - Deep gold
- `#8a661f` - Darkest gold

---

## Part 1: Solution Structure

```
RAJFinancial/
├── src/
│   ├── RAJFinancial.Client/                  # Blazor WebAssembly
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   ├── DesktopSidebar.razor
│   │   │   │   └── MobileBottomNav.razor
│   │   │   ├── Common/
│   │   │   │   ├── GlassCard.razor
│   │   │   │   ├── EmptyState.razor
│   │   │   │   ├── AnimatedNumber.razor
│   │   │   │   ├── CelebrationModal.razor
│   │   │   │   ├── TrendBadge.razor
│   │   │   │   └── DynamicIcon.razor
│   │   │   ├── Dashboard/
│   │   │   │   ├── NetWorthHero.razor
│   │   │   │   ├── QuickStatCard.razor
│   │   │   │   ├── InsightsPanel.razor
│   │   │   │   ├── InsightCard.razor
│   │   │   │   ├── HealthScoreCard.razor
│   │   │   │   ├── NetWorthChartCard.razor
│   │   │   │   ├── AssetAllocationCard.razor
│   │   │   │   ├── QuickActionsCard.razor
│   │   │   │   └── RecentActivityCard.razor
│   │   │   ├── Accounts/
│   │   │   │   ├── PlaidLinkModal.razor
│   │   │   │   ├── AccountCard.razor
│   │   │   │   └── ConnectionStatusBadge.razor
│   │   │   ├── Assets/
│   │   │   │   ├── AssetForm.razor
│   │   │   │   ├── AssetCard.razor
│   │   │   │   └── AssetTypeIcon.razor
│   │   │   ├── Beneficiaries/
│   │   │   │   ├── BeneficiaryCard.razor
│   │   │   │   └── AssignmentDialog.razor
│   │   │   ├── DebtPayoff/
│   │   │   │   ├── StrategyCard.razor
│   │   │   │   ├── DebtListItem.razor
│   │   │   │   ├── DebtForm.razor
│   │   │   │   └── PayoffScheduleTable.razor
│   │   │   └── Insurance/
│   │   │       ├── CoverageGauge.razor
│   │   │       └── BreakdownItem.razor
│   │   ├── Pages/
│   │   │   ├── Index.razor
│   │   │   ├── Dashboard.razor
│   │   │   ├── Accounts.razor
│   │   │   ├── Assets.razor
│   │   │   ├── Beneficiaries.razor
│   │   │   ├── Settings.razor
│   │   │   └── Tools/
│   │   │       ├── Index.razor
│   │   │       ├── DebtPayoff.razor
│   │   │       ├── InsuranceCalculator.razor
│   │   │       └── EstateChecklist.razor
│   │   ├── Services/
│   │   │   ├── ApiClient.cs
│   │   │   └── PlaidLinkService.cs
│   │   ├── State/
│   │   │   ├── AppState.cs
│   │   │   ├── DashboardState/
│   │   │   ├── AccountState/
│   │   │   ├── AssetState/
│   │   │   └── DebtPayoffState/
│   │   ├── wwwroot/
│   │   │   ├── index.html
│   │   │   ├── css/
│   │   │   │   └── raj-theme.css
│   │   │   ├── images/
│   │   │   │   ├── logo.svg
│   │   │   │   ├── logo_only.svg
│   │   │   │   └── logo_horizontal.svg
│   │   │   └── fonts/
│   │   │       └── Nexa-XBold.woff2
│   │   └── Program.cs
```

---

## Part 2: NuGet Packages

### RAJFinancial.Client

```xml
<ItemGroup>
    <!-- Blazor -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    
    <!-- Syncfusion Blazor -->
    <PackageReference Include="Syncfusion.Blazor.Grid" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Charts" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Inputs" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Buttons" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Navigations" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Popups" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Calendars" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.DropDowns" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Notifications" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Cards" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.ProgressBar" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Spinner" Version="24.2.9" />
    <PackageReference Include="Syncfusion.Blazor.Themes" Version="24.2.9" />
    
    <!-- State Management -->
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.9.1" />
    
    <!-- Serialization -->
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    
    <!-- Localization -->
    <PackageReference Include="Microsoft.Extensions.Localization" Version="8.0.0" />
</ItemGroup>
```

---

## Part 3: Syncfusion Components Used

| Component | Use Case |
|-----------|----------|
| `SfGrid` | Assets list, Transactions, Beneficiaries |
| `SfChart` | Net worth trends, Debt payoff timeline |
| `SfAccumulationChart` | Asset allocation pie, Insurance breakdown |
| `SfSparkline` | Quick stat cards |
| `SfDialog` | Add/Edit forms, Confirmations |
| `SfSidebar` | Mobile menu drawer |
| `SfNumericTextBox` | Currency inputs |
| `SfSlider` | Range selectors (income years) |
| `SfSwitch` | Toggle options |
| `SfDropDownButton` | Action menus |
| `SfTab` | Filter navigation |
| `SfCard` | Dashboard widgets |
| `SfToast` | Notifications |
| `SfProgressBar` | Loading states |
| `SfSkeleton` | Loading placeholders |
| `SfChip` | Status badges |

---

## Part 4: CSS Design System

```css
/* RAJFinancial.Client/wwwroot/css/raj-theme.css */

@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

:root {
  /* ═══════════════════════════════════════════════════════════
     RAJ FINANCIAL BRAND COLORS
     Premium gold palette conveying wealth, trust, and excellence
     ═══════════════════════════════════════════════════════════ */
  
  /* Primary Gold Palette */
  --gold-50: #fffbcc;
  --gold-100: #fff7b3;
  --gold-200: #f5e99a;
  --gold-300: #eed688;
  --gold-400: #e8c94d;
  --gold-500: #ebbb10;
  --gold-600: #d4a80e;
  --gold-700: #c3922e;
  --gold-800: #a67c26;
  --gold-900: #8a661f;
  
  /* Neutral Palette - Warm grays to complement gold */
  --neutral-50: #fafaf9;
  --neutral-100: #f5f5f4;
  --neutral-200: #e7e5e4;
  --neutral-300: #d6d3d1;
  --neutral-400: #a8a29e;
  --neutral-500: #78716c;
  --neutral-600: #57534e;
  --neutral-700: #44403c;
  --neutral-800: #292524;
  --neutral-900: #1c1917;
  
  /* Semantic Colors */
  --success-50: #f0fdf4;
  --success-100: #dcfce7;
  --success-500: #22c55e;
  --success-600: #16a34a;
  --success-700: #15803d;
  
  --warning-50: #fffbeb;
  --warning-100: #fef3c7;
  --warning-500: #f59e0b;
  --warning-600: #d97706;
  
  --error-50: #fef2f2;
  --error-100: #fee2e2;
  --error-500: #ef4444;
  --error-600: #dc2626;
  
  --info-50: #eff6ff;
  --info-100: #dbeafe;
  --info-500: #3b82f6;
  --info-600: #2563eb;
  
  /* Typography */
  --font-display: 'Nexa', 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-body: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', monospace;
  
  /* Font Sizes */
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  --text-3xl: 1.875rem;
  --text-4xl: 2.25rem;
  --text-5xl: 3rem;
  --text-6xl: 3.75rem;
  
  /* Spacing Scale */
  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
  --space-5: 1.25rem;
  --space-6: 1.5rem;
  --space-8: 2rem;
  --space-10: 2.5rem;
  --space-12: 3rem;
  --space-16: 4rem;
  --space-20: 5rem;
  
  /* Shadows - Warm-tinted for gold theme */
  --shadow-sm: 0 1px 2px 0 rgb(166 124 38 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(166 124 38 / 0.1), 0 2px 4px -2px rgb(166 124 38 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(166 124 38 / 0.1), 0 4px 6px -4px rgb(166 124 38 / 0.1);
  --shadow-xl: 0 20px 25px -5px rgb(166 124 38 / 0.15), 0 8px 10px -6px rgb(166 124 38 / 0.1);
  --shadow-gold: 0 4px 14px 0 rgb(235 187 16 / 0.25);
  --shadow-gold-lg: 0 10px 40px 0 rgb(235 187 16 / 0.3);
  
  /* Border Radius */
  --radius-sm: 0.375rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
  --radius-xl: 1rem;
  --radius-2xl: 1.5rem;
  --radius-full: 9999px;
  
  /* Transitions */
  --transition-fast: 150ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-base: 200ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-slow: 300ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-bounce: 500ms cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

/* ═══════════════════════════════════════════════════════════
   ANIMATIONS
   ═══════════════════════════════════════════════════════════ */

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes slideDown {
  from { opacity: 0; transform: translateY(-20px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes slideInRight {
  from { opacity: 0; transform: translateX(30px); }
  to { opacity: 1; transform: translateX(0); }
}

@keyframes slideInLeft {
  from { opacity: 0; transform: translateX(-30px); }
  to { opacity: 1; transform: translateX(0); }
}

@keyframes scaleIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

@keyframes goldShine {
  0% { background-position: -100% 0; }
  100% { background-position: 200% 0; }
}

@keyframes celebrate {
  0% { transform: scale(1); }
  25% { transform: scale(1.1); }
  50% { transform: scale(1); }
  75% { transform: scale(1.05); }
  100% { transform: scale(1); }
}

@keyframes countUp {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes progressFill {
  from { width: 0; }
}

@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

@keyframes confetti-fall {
  0% { transform: translateY(-100vh) rotate(0deg); opacity: 1; }
  100% { transform: translateY(100vh) rotate(720deg); opacity: 0; }
}

/* Animation Classes */
.animate-fade-in { animation: fadeIn var(--transition-base) ease-out; }
.animate-slide-up { animation: slideUp var(--transition-slow) ease-out; }
.animate-slide-down { animation: slideDown var(--transition-slow) ease-out; }
.animate-slide-in-right { animation: slideInRight var(--transition-slow) ease-out; }
.animate-slide-in-left { animation: slideInLeft var(--transition-slow) ease-out; }
.animate-scale-in { animation: scaleIn var(--transition-base) ease-out; }
.animate-pulse { animation: pulse 2s ease-in-out infinite; }
.animate-celebrate { animation: celebrate 0.6s ease-in-out; }
.animate-float { animation: float 3s ease-in-out infinite; }
.animate-spin { animation: spin 1s linear infinite; }

/* Staggered Animations */
.stagger-1 { animation-delay: 50ms; }
.stagger-2 { animation-delay: 100ms; }
.stagger-3 { animation-delay: 150ms; }
.stagger-4 { animation-delay: 200ms; }
.stagger-5 { animation-delay: 250ms; }

/* ═══════════════════════════════════════════════════════════
   GRADIENT UTILITIES
   ═══════════════════════════════════════════════════════════ */

/* Primary gold gradient - used for hero sections and CTAs */
.gradient-gold {
  background: linear-gradient(135deg, var(--gold-500) 0%, var(--gold-700) 100%);
}

/* Premium gold gradient with shine effect */
.gradient-gold-shine {
  background: linear-gradient(
    120deg,
    var(--gold-600) 0%,
    var(--gold-400) 25%,
    var(--gold-500) 50%,
    var(--gold-400) 75%,
    var(--gold-600) 100%
  );
  background-size: 200% 100%;
  animation: goldShine 3s ease-in-out infinite;
}

/* Subtle gold for cards */
.gradient-gold-subtle {
  background: linear-gradient(180deg, var(--gold-50) 0%, white 100%);
}

/* Dark premium gradient for headers */
.gradient-dark-gold {
  background: linear-gradient(135deg, var(--neutral-900) 0%, var(--neutral-800) 50%, var(--gold-900) 100%);
}

/* Mesh gradient background */
.gradient-mesh {
  background: 
    radial-gradient(at 40% 20%, var(--gold-100) 0px, transparent 50%),
    radial-gradient(at 80% 0%, var(--gold-50) 0px, transparent 50%),
    radial-gradient(at 0% 50%, var(--gold-100) 0px, transparent 50%),
    radial-gradient(at 80% 50%, var(--gold-50) 0px, transparent 50%),
    radial-gradient(at 0% 100%, var(--gold-200) 0px, transparent 50%),
    white;
}

/* ═══════════════════════════════════════════════════════════
   COMPONENT UTILITIES
   ═══════════════════════════════════════════════════════════ */

/* Glass effect card */
.glass {
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid rgba(235, 187, 16, 0.1);
}

/* Premium card with gold accent */
.card-premium {
  background: white;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  border: 1px solid var(--gold-100);
  transition: all var(--transition-base);
}

.card-premium:hover {
  box-shadow: var(--shadow-xl);
  border-color: var(--gold-300);
  transform: translateY(-2px);
}

/* Gold accent border */
.border-gold-accent {
  border-left: 4px solid var(--gold-500);
}

/* Skeleton loading with gold tint */
.skeleton {
  background: linear-gradient(
    90deg,
    var(--neutral-200) 25%,
    var(--gold-50) 50%,
    var(--neutral-200) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s ease-in-out infinite;
  border-radius: var(--radius-md);
}

/* Button styles */
.btn-gold {
  background: var(--gold-500);
  color: var(--neutral-900);
  font-weight: 600;
  padding: var(--space-3) var(--space-6);
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
  box-shadow: var(--shadow-gold);
}

.btn-gold:hover {
  background: var(--gold-600);
  box-shadow: var(--shadow-gold-lg);
  transform: translateY(-1px);
}

.btn-gold:active {
  transform: translateY(0);
}

.btn-gold-outline {
  background: transparent;
  color: var(--gold-700);
  border: 2px solid var(--gold-500);
  font-weight: 600;
  padding: var(--space-3) var(--space-6);
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
}

.btn-gold-outline:hover {
  background: var(--gold-50);
  border-color: var(--gold-600);
}

/* Focus states - accessible gold */
.focus-gold:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(235, 187, 16, 0.4);
}

/* Text utilities */
.text-gold { color: var(--gold-500); }
.text-gold-dark { color: var(--gold-700); }
.text-gold-light { color: var(--gold-300); }

/* Background utilities */
.bg-gold-50 { background-color: var(--gold-50); }
.bg-gold-100 { background-color: var(--gold-100); }
.bg-gold-500 { background-color: var(--gold-500); }

/* Safe area for mobile */
.safe-area-bottom {
  padding-bottom: env(safe-area-inset-bottom);
}
```

---

## Part 5: Program.cs Configuration

```csharp
// RAJFinancial.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Fluxor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Syncfusion license (get from Syncfusion account)
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");

// Configure HttpClient with base address
builder.Services.AddScoped(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config.GetValue<string>("Api:BaseUrl") ?? "https://localhost:7071/api/";
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});

// Register API client
builder.Services.AddScoped<IApiClient, ApiClient>();

// Syncfusion Blazor
builder.Services.AddSyncfusionBlazor();

// Fluxor state management
builder.Services.AddFluxor(options => options
    .ScanAssemblies(typeof(Program).Assembly)
    .UseReduxDevTools());

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

await builder.Build().RunAsync();
```

---

## Part 6: Dashboard Page

```razor
@* RAJFinancial.Client/Pages/Dashboard.razor *@
@page "/dashboard"
@attribute [Authorize]
@inject IStringLocalizer<Dashboard> L
@inject IState<DashboardState> State
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

@* Skip to main content for accessibility *@
<a href="#main-content" class="sr-only focus:not-sr-only">
    @L["SkipToContent"]
</a>

<div class="min-h-screen bg-neutral-50">
    @* Animated Background Mesh *@
    <div class="fixed inset-0 gradient-mesh opacity-60 pointer-events-none"></div>
    
    @* Mobile Header *@
    <header class="lg:hidden sticky top-0 z-40 bg-white border-b px-4 py-3">
        <div class="flex items-center justify-between">
            <button @onclick="ToggleMobileMenu" 
                    class="p-2 -ml-2"
                    aria-label="@L["Menu"]">
                <SfIcon Name="IconName.Menu" Size="IconSize.Medium" />
            </button>
            <img src="/images/logo.svg" alt="RAJ Financial" class="h-8" />
            <button class="p-2 -mr-2 relative" aria-label="@L["Notifications"]">
                <SfIcon Name="IconName.Notification" Size="IconSize.Medium" />
                @if (State.Value.UnreadNotifications > 0)
                {
                    <span class="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
                }
            </button>
        </div>
    </header>

    <div class="lg:flex">
        @* Desktop Sidebar *@
        <aside class="hidden lg:flex lg:flex-col lg:w-64 lg:fixed lg:inset-y-0 
                      bg-neutral-900 text-white">
            <DesktopSidebar />
        </aside>

        @* Main Content *@
        <main id="main-content" 
              class="flex-1 lg:pl-64 pb-20 lg:pb-0"
              role="main"
              aria-label="@L["MainContent"]">
            
            <div class="relative">
                @* Hero Section - Net Worth *@
                <section class="relative overflow-hidden" aria-labelledby="net-worth-heading">
                    <div class="absolute inset-0 gradient-dark-gold"></div>
                    
                    @* Decorative Elements *@
                    <div class="absolute inset-0 overflow-hidden" aria-hidden="true">
                        <div class="absolute -top-40 -right-40 w-80 h-80 rounded-full bg-white/5"></div>
                        <div class="absolute -bottom-20 -left-20 w-60 h-60 rounded-full bg-white/5"></div>
                    </div>
                    
                    <div class="relative px-4 pt-8 pb-32 lg:px-8 lg:pt-12 lg:pb-40">
                        <div class="max-w-7xl mx-auto">
                            @* Greeting *@
                            <div class="animate-fade-in">
                                <p class="text-white/70 text-sm font-medium">@GetGreeting()</p>
                                <h1 class="text-white text-2xl lg:text-3xl font-bold mt-1">
                                    @L["Welcome", State.Value.UserFirstName]
                                </h1>
                            </div>
                            
                            @* Net Worth Display *@
                            <div class="mt-8 animate-slide-up" style="animation-delay: 100ms;">
                                <h2 id="net-worth-heading" class="text-white/70 text-sm font-medium uppercase tracking-wide">
                                    @L["NetWorth.Title"]
                                </h2>
                                
                                @if (State.Value.IsLoading)
                                {
                                    <div class="h-12 w-64 skeleton rounded-lg mt-2"></div>
                                }
                                else
                                {
                                    <div class="flex items-baseline gap-4 mt-2">
                                        <span class="text-white text-5xl lg:text-6xl font-bold tracking-tight">
                                            <AnimatedNumber Value="@State.Value.NetWorth" Format="C0" />
                                        </span>
                                        <TrendBadge Value="@State.Value.NetWorthChangePercent" />
                                    </div>
                                    <p class="text-white/60 text-sm mt-2">
                                        @L["NetWorth.AsOf", State.Value.LastUpdated.ToString("MMM d, yyyy")]
                                    </p>
                                }
                            </div>
                        </div>
                    </div>
                </section>
                
                @* Main Content - Overlapping Cards *@
                <div class="relative -mt-24 lg:-mt-32 px-4 pb-24 lg:px-8">
                    <div class="max-w-7xl mx-auto">
                        
                        @* Quick Stats Cards - Horizontal scroll on mobile *@
                        <section aria-label="@L["QuickStats.Title"]" class="mb-6">
                            <div class="flex gap-4 overflow-x-auto pb-2 -mx-4 px-4 lg:mx-0 lg:px-0 
                                        lg:grid lg:grid-cols-4 snap-x snap-mandatory">
                                <QuickStatCard 
                                    Title="@L["Stats.Assets"]"
                                    Value="@State.Value.TotalAssets"
                                    Icon="trending-up"
                                    Color="success"
                                    Delay="200" />
                                
                                <QuickStatCard 
                                    Title="@L["Stats.Liabilities"]"
                                    Value="@State.Value.TotalLiabilities"
                                    Icon="credit-card"
                                    Color="neutral"
                                    IsNegative="true"
                                    Delay="250" />
                                
                                <QuickStatCard 
                                    Title="@L["Stats.LinkedAccounts"]"
                                    Value="@State.Value.LinkedAccountCount"
                                    ValueFormat="N0"
                                    Suffix="accounts"
                                    Icon="link"
                                    Color="accent"
                                    Delay="300" />
                                
                                <QuickStatCard 
                                    Title="@L["Stats.InsuranceCoverage"]"
                                    Value="@(State.Value.InsuranceCoverageRatio * 100)"
                                    ValueFormat="N0"
                                    Suffix="%"
                                    Icon="shield"
                                    Color="@GetCoverageColor()"
                                    ShowProgress="true"
                                    ProgressValue="@State.Value.InsuranceCoverageRatio"
                                    Delay="350" />
                            </div>
                        </section>
                        
                        @* Two Column Layout *@
                        <div class="grid lg:grid-cols-3 gap-6">
                            @* Left Column - 2/3 width *@
                            <div class="lg:col-span-2 space-y-6">
                                <InsightsPanel 
                                    Insights="@State.Value.Insights"
                                    IsLoading="@State.Value.IsLoadingInsights"
                                    OnInsightClick="HandleInsightClick"
                                    OnRefresh="RefreshInsights" />
                                
                                <NetWorthChartCard 
                                    Data="@State.Value.NetWorthHistory" />
                                
                                <AssetAllocationCard 
                                    Allocations="@State.Value.AssetAllocations" />
                            </div>
                            
                            @* Right Column - 1/3 width *@
                            <div class="space-y-6">
                                <HealthScoreCard 
                                    Score="@State.Value.HealthScore"
                                    Breakdown="@State.Value.HealthScoreBreakdown" />
                                
                                <QuickActionsCard OnLinkAccount="OpenPlaidLink" />
                                
                                <RecentActivityCard 
                                    Activities="@State.Value.RecentActivity" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </main>
    </div>

    @* Mobile Bottom Navigation *@
    <MobileBottomNav />
</div>

@* Mobile Menu Drawer *@
<SfSidebar @ref="MobileSidebar"
           Type="SidebarType.Over"
           Position="SidebarPosition.Left"
           EnableGestures="true"
           CloseOnDocumentClick="true">
    <ChildContent>
        <DesktopSidebar OnNavigate="CloseMobileMenu" />
    </ChildContent>
</SfSidebar>

@* Plaid Link Modal *@
<PlaidLinkModal @ref="PlaidLinkModal" OnSuccess="HandlePlaidSuccess" />

@code {
    private SfSidebar? MobileSidebar;
    private PlaidLinkModal? PlaidLinkModal;
    
    protected override void OnInitialized()
    {
        Dispatcher.Dispatch(new LoadDashboardAction());
        Dispatcher.Dispatch(new LoadInsightsAction());
    }
    
    private string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        return hour switch
        {
            < 12 => L["Greeting.Morning"],
            < 17 => L["Greeting.Afternoon"],
            _ => L["Greeting.Evening"]
        };
    }
    
    private string GetCoverageColor() => State.Value.InsuranceCoverageRatio switch
    {
        >= 0.8m => "success",
        >= 0.5m => "warning",
        _ => "error"
    };
    
    private void ToggleMobileMenu() => MobileSidebar?.Toggle();
    private void CloseMobileMenu() => MobileSidebar?.Hide();
    
    private async Task OpenPlaidLink() => await PlaidLinkModal!.OpenAsync();
    
    private async Task HandlePlaidSuccess(PlaidLinkSuccessEvent e)
    {
        Dispatcher.Dispatch(new ExchangePlaidTokenAction(e.PublicToken, e.Metadata));
    }
    
    private void HandleInsightClick(AIInsightDto insight)
    {
        NavigationManager.NavigateTo(insight.ActionUrl);
    }
    
    private void RefreshInsights()
    {
        Dispatcher.Dispatch(new LoadInsightsAction(forceRefresh: true));
    }
}


```

---

## Part 7: Assets Page

```razor
@* RAJFinancial.Client/Pages/Assets.razor *@
@page "/assets"
@attribute [Authorize]
@inject IStringLocalizer<Assets> L
@inject IState<AssetState> AssetState
@inject IDispatcher Dispatcher
@inject NavigationManager NavigationManager

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

<div class="p-4 lg:p-8 max-w-7xl mx-auto">
    @* Header *@
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <h1 class="text-2xl font-bold text-neutral-900">@L["Title"]</h1>
        <SfButton CssClass="e-primary w-full sm:w-auto" 
                  @onclick="OpenAddAssetDialog"
                  aria-label="@L["AddAsset"]">
            <SfIcon Name="IconName.Plus" CssClass="mr-2" />
            @L["AddAsset"]
        </SfButton>
    </div>

    @* Filter Tabs *@
    <SfTab CssClass="mb-6" @bind-SelectedItem="SelectedTabIndex">
        <TabItems>
            <TabItem><HeaderTemplate>@L["Filter.All"]</HeaderTemplate></TabItem>
            <TabItem><HeaderTemplate>@L["Filter.RealEstate"]</HeaderTemplate></TabItem>
            <TabItem><HeaderTemplate>@L["Filter.Vehicles"]</HeaderTemplate></TabItem>
            <TabItem><HeaderTemplate>@L["Filter.Business"]</HeaderTemplate></TabItem>
            <TabItem><HeaderTemplate>@L["Filter.Other"]</HeaderTemplate></TabItem>
        </TabItems>
    </SfTab>

    @* Desktop: Data Grid *@
    <div class="hidden lg:block">
        <SfGrid DataSource="@FilteredAssets" AllowPaging="true" AllowSorting="true" EnableAltRow="true">
            <GridPageSettings PageSize="10" />
            <GridColumns>
                <GridColumn Field="@nameof(AssetDto.Name)" HeaderText="@L["Grid.Name"]" Width="200">
                    <Template>
                        @{
                            var asset = context as AssetDto;
                            <div class="flex items-center gap-3">
                                <div class="w-10 h-10 rounded-lg bg-gold-100 flex items-center justify-center">
                                    <AssetTypeIcon Type="@asset!.AssetType" />
                                </div>
                                <div>
                                    <p class="font-medium text-neutral-900">@asset.Name</p>
                                    <p class="text-sm text-neutral-500">@L[$"AssetType.{asset.AssetType}"]</p>
                                </div>
                            </div>
                        }
                    </Template>
                </GridColumn>
                <GridColumn Field="@nameof(AssetDto.CurrentValue)" HeaderText="@L["Grid.Value"]" Format="C0" TextAlign="TextAlign.Right" Width="150" />
                <GridColumn Field="@nameof(AssetDto.OwnershipPercentage)" HeaderText="@L["Grid.Ownership"]" Format="P0" TextAlign="TextAlign.Right" Width="120" />
                <GridColumn Field="@nameof(AssetDto.UserShareValue)" HeaderText="@L["Grid.YourShare"]" Format="C0" TextAlign="TextAlign.Right" Width="150" />
                <GridColumn Field="@nameof(AssetDto.BeneficiaryCount)" HeaderText="@L["Grid.Beneficiaries"]" TextAlign="TextAlign.Center" Width="120">
                    <Template>
                        @{
                            var asset = context as AssetDto;
                            <div class="flex items-center justify-center gap-1">
                                @if (asset!.BeneficiaryCount > 0)
                                {
                                    <SfIcon Name="IconName.Users" CssClass="text-success-600" Size="IconSize.Small" />
                                    <span>@asset.BeneficiaryCount</span>
                                }
                                else
                                {
                                    <SfChip CssClass="e-warning" Text="@L["NoBeneficiaries"]" />
                                }
                            </div>
                        }
                    </Template>
                </GridColumn>
                <GridColumn HeaderText="" Width="80" TextAlign="TextAlign.Center">
                    <Template>
                        @{
                            var asset = context as AssetDto;
                            <SfDropDownButton CssClass="e-flat" IconCss="e-icons e-more-vertical" aria-label="@L["Actions"]">
                                <DropDownMenuItems>
                                    <DropDownMenuItem Text="@L["Action.Edit"]" @onclick="() => EditAsset(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.UpdateValue"]" @onclick="() => UpdateValue(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.ManageBeneficiaries"]" @onclick="() => ManageBeneficiaries(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.Delete"]" CssClass="text-error-600" @onclick="() => ConfirmDelete(asset!)" />
                                </DropDownMenuItems>
                            </SfDropDownButton>
                        }
                    </Template>
                </GridColumn>
            </GridColumns>
        </SfGrid>
    </div>

    @* Mobile: Card Layout *@
    <div class="lg:hidden space-y-4">
        @if (AssetState.Value.IsLoading)
        {
            @for (int i = 0; i < 3; i++)
            {
                <SfCard><div class="p-4"><SfSkeleton Width="60%" Height="24px" /><SfSkeleton Width="40%" Height="20px" CssClass="mt-2" /></div></SfCard>
            }
        }
        else if (!FilteredAssets.Any())
        {
            <EmptyState Icon="package" Title="@L["Empty.Title"]" Description="@L["Empty.Description"]" ActionText="@L["AddAsset"]" OnAction="OpenAddAssetDialog" />
        }
        else
        {
            @foreach (var asset in FilteredAssets)
            {
                <SfCard @onclick="() => ViewAsset(asset.Id)" CssClass="cursor-pointer hover:shadow-lg transition-shadow">
                    <div class="p-4">
                        <div class="flex items-start justify-between">
                            <div class="flex items-center gap-3">
                                <div class="w-12 h-12 rounded-lg bg-gold-100 flex items-center justify-center">
                                    <AssetTypeIcon Type="@asset.AssetType" />
                                </div>
                                <div>
                                    <h3 class="font-semibold text-neutral-900">@asset.Name</h3>
                                    <p class="text-sm text-neutral-500">@L[$"AssetType.{asset.AssetType}"]</p>
                                </div>
                            </div>
                            @if (asset.BeneficiaryCount == 0) { <SfChip CssClass="e-warning e-small" Text="@L["NoBeneficiaries"]" /> }
                        </div>
                        <div class="mt-4 flex items-end justify-between">
                            <div>
                                <p class="text-2xl font-bold text-neutral-900">@asset.CurrentValue.ToString("C0")</p>
                                @if (asset.OwnershipPercentage < 100) { <p class="text-sm text-neutral-500">@L["YourShare"]: @asset.UserShareValue.ToString("C0") (@asset.OwnershipPercentage.ToString("P0"))</p> }
                            </div>
                            <SfIcon Name="IconName.ChevronRight" CssClass="text-neutral-400" />
                        </div>
                    </div>
                </SfCard>
            }
        }
    </div>
</div>

@* Add/Edit Asset Dialog *@
<SfDialog @bind-Visible="IsAddDialogVisible" Header="@(EditingAsset == null ? L["AddAsset"] : L["EditAsset"])" Width="600px" IsModal="true" ShowCloseIcon="true" CloseOnEscape="true">
    <DialogTemplates><Content><AssetForm Asset="@EditingAsset" OnSubmit="HandleAssetSubmit" OnCancel="CloseAddDialog" /></Content></DialogTemplates>
</SfDialog>

@* Delete Confirmation Dialog *@
<SfDialog @bind-Visible="IsDeleteDialogVisible" Header="@L["Delete.Title"]" Width="400px" IsModal="true" ShowCloseIcon="true">
    <DialogTemplates><Content><p class="text-neutral-600">@L["Delete.Confirmation", DeletingAsset?.Name ?? ""]</p></Content></DialogTemplates>
    <DialogButtons>
        <DialogButton Content="@L["Cancel"]" CssClass="e-flat" OnClick="() => IsDeleteDialogVisible = false" />
        <DialogButton Content="@L["Delete"]" CssClass="e-danger" IsPrimary="true" OnClick="ExecuteDelete" />
    </DialogButtons>
</SfDialog>

@code {
    private int SelectedTabIndex = 0;
    private bool IsAddDialogVisible = false;
    private bool IsDeleteDialogVisible = false;
    private AssetDto? EditingAsset;
    private AssetDto? DeletingAsset;
    
    private IEnumerable<AssetDto> FilteredAssets => SelectedTabIndex switch
    {
        0 => AssetState.Value.Assets,
        1 => AssetState.Value.Assets.Where(a => a.AssetType == AssetType.RealEstate),
        2 => AssetState.Value.Assets.Where(a => a.AssetType == AssetType.Vehicle),
        3 => AssetState.Value.Assets.Where(a => a.AssetType == AssetType.BusinessInterest),
        _ => AssetState.Value.Assets.Where(a => a.AssetType != AssetType.RealEstate && a.AssetType != AssetType.Vehicle && a.AssetType != AssetType.BusinessInterest)
    };
    
    protected override void OnInitialized() => Dispatcher.Dispatch(new LoadAssetsAction());
    private void OpenAddAssetDialog() { EditingAsset = null; IsAddDialogVisible = true; }
    private void CloseAddDialog() { IsAddDialogVisible = false; EditingAsset = null; }
    private void EditAsset(Guid id) { EditingAsset = AssetState.Value.Assets.FirstOrDefault(a => a.Id == id); IsAddDialogVisible = true; }
    private void ViewAsset(Guid id) => NavigationManager.NavigateTo($"/assets/{id}");
    private void UpdateValue(Guid id) { }
    private void ManageBeneficiaries(Guid id) => NavigationManager.NavigateTo($"/beneficiaries?asset={id}");
    private void ConfirmDelete(AssetDto asset) { DeletingAsset = asset; IsDeleteDialogVisible = true; }
    private async Task ExecuteDelete() { if (DeletingAsset != null) Dispatcher.Dispatch(new DeleteAssetAction(DeletingAsset.Id)); IsDeleteDialogVisible = false; DeletingAsset = null; }
    private async Task HandleAssetSubmit(CreateAssetRequest request) { if (EditingAsset == null) Dispatcher.Dispatch(new CreateAssetAction(request)); else Dispatcher.Dispatch(new UpdateAssetAction(EditingAsset.Id, request)); CloseAddDialog(); }
}
```

---

## Part 8: Debt Payoff Analyzer Page

```razor
@* RAJFinancial.Client/Pages/Tools/DebtPayoff.razor *@
@page "/tools/debt-payoff"
@attribute [Authorize]
@inject IStringLocalizer<DebtPayoff> L
@inject IState<DebtPayoffState> State
@inject IDispatcher Dispatcher
@inject NavigationManager Nav

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

<div class="min-h-screen bg-neutral-50">
    <div class="fixed inset-0 gradient-mesh opacity-60 pointer-events-none"></div>
    
    <div class="relative">
        @* Hero Header *@
        <header class="relative overflow-hidden">
            <div class="absolute inset-0 gradient-dark-gold"></div>
            <div class="relative px-4 pt-8 pb-24 lg:px-8 lg:pt-12 lg:pb-32">
                <div class="max-w-7xl mx-auto">
                    <nav class="mb-6">
                        <a href="/tools" class="text-white/60 hover:text-white transition-colors text-sm">
                            ← @L["BackToTools"]
                        </a>
                    </nav>
                    <div class="animate-fade-in">
                        <h1 class="text-white text-3xl lg:text-4xl font-bold">@L["Title"]</h1>
                        <p class="text-white/70 mt-2 max-w-2xl">@L["Subtitle"]</p>
                    </div>
                </div>
            </div>
        </header>
        
        @* Main Content *@
        <main class="relative -mt-16 px-4 py-8 lg:px-8">
            <div class="max-w-7xl mx-auto">
                <div class="grid lg:grid-cols-3 gap-6">
                    @* Left Column - Debt List *@
                    <div class="lg:col-span-1">
                        <GlassCard>
                            <div class="flex items-center justify-between mb-4">
                                <h2 class="text-lg font-semibold text-neutral-900">@L["YourDebts"]</h2>
                                <SfButton CssClass="e-primary e-small" @onclick="OpenAddDebtDialog">
                                    <SfIcon Name="IconName.Plus" CssClass="mr-1" /> Add
                                </SfButton>
                            </div>
                            
                            @if (!State.Value.Debts.Any())
                            {
                                <EmptyState Icon="credit-card" Title="@L["NoDebts.Title"]" Description="@L["NoDebts.Description"]" 
                                           ActionText="@L["AddDebt"]" OnAction="OpenAddDebtDialog" Variant="default" />
                            }
                            else
                            {
                                <div class="space-y-2">
                                    @foreach (var debt in State.Value.Debts.OrderByDescending(d => d.InterestRate))
                                    {
                                        <DebtListItem Debt="@debt" IsSelected="@(SelectedDebtId == debt.Id)"
                                                     OnSelect="() => SelectDebt(debt.Id)"
                                                     OnEdit="() => EditDebt(debt)"
                                                     OnDelete="() => ConfirmDeleteDebt(debt)" />
                                    }
                                </div>
                                
                                <div class="mt-6 pt-4 border-t border-neutral-200">
                                    <div class="flex justify-between text-sm">
                                        <span class="text-neutral-500">Total Debt</span>
                                        <span class="font-bold text-neutral-900">@State.Value.TotalDebt.ToString("C0")</span>
                                    </div>
                                </div>
                            }
                        </GlassCard>
                        
                        @* Extra Payment Input *@
                        <GlassCard CssClass="mt-4">
                            <h3 class="text-sm font-medium text-neutral-700 mb-2">@L["ExtraPayment.Title"]</h3>
                            <SfNumericTextBox @bind-Value="ExtraPayment" Format="C0" Min="0" Step="50"
                                             Placeholder="@L["ExtraPayment.Placeholder"]" CssClass="w-full" />
                            <p class="text-xs text-neutral-500 mt-2">@L["ExtraPayment.Help"]</p>
                        </GlassCard>
                    </div>
                    
                    @* Right Column - Strategy Comparison *@
                    <div class="lg:col-span-2 space-y-6">
                        @* Strategy Cards *@
                        <div class="grid md:grid-cols-2 gap-4">
                            <CascadingValue Value="@State.Value.Strategies">
                                @foreach (var (strategy, index) in State.Value.Strategies.Select((s, i) => (s, i)))
                                {
                                    <StrategyCard Strategy="@strategy" IsRecommended="@(strategy.Name == "Avalanche")"
                                                 IsSelected="@(SelectedStrategy == strategy.Name)"
                                                 OnSelect="() => SelectStrategy(strategy.Name)"
                                                 AnimationDelay="@(index * 100)" />
                                }
                            </CascadingValue>
                        </div>
                        
                        @* Payoff Chart *@
                        <GlassCard>
                            <h3 class="text-lg font-semibold text-neutral-900 mb-4">@L["PayoffTimeline"]</h3>
                            <SfChart @ref="PayoffChart" Height="300px">
                                <ChartPrimaryXAxis ValueType="Syncfusion.Blazor.Charts.ValueType.DateTime" 
                                                  LabelFormat="MMM yyyy" />
                                <ChartPrimaryYAxis LabelFormat="C0" />
                                <ChartSeriesCollection>
                                    <ChartSeries DataSource="@GetSelectedStrategyData()" XName="Date" YName="Balance"
                                                Type="ChartSeriesType.Area" Fill="url(#goldGradient)" />
                                </ChartSeriesCollection>
                            </SfChart>
                        </GlassCard>
                        
                        @* Analysis Summary *@
                        @if (GetSelectedStrategy() != null)
                        {
                            <GlassCard CssClass="border-gold-accent">
                                <div class="flex items-start gap-3">
                                    <div class="w-10 h-10 rounded-lg bg-gold-100 flex items-center justify-center">
                                        <SfIcon Name="IconName.Lightbulb" CssClass="text-gold-600" />
                                    </div>
                                    <div>
                                        <h4 class="font-semibold text-neutral-900">@L["Analysis.Title"]</h4>
                                        <p class="text-neutral-600 mt-1">@GetAnalysisSummary()</p>
                                    </div>
                                </div>
                            </GlassCard>
                        }
                        
                        @* Disclaimer *@
                        <p class="text-xs text-neutral-400 text-center">@L["Disclaimer"]</p>
                    </div>
                </div>
            </div>
        </main>
    </div>
</div>

@* Add/Edit Debt Drawer *@
<SfSidebar @bind-IsOpen="IsDebtDrawerOpen" Type="SidebarType.Over" Position="SidebarPosition.Right"
           Width="400px" EnableGestures="true" CloseOnDocumentClick="true">
    <ChildContent>
        <DebtForm Debt="@EditingDebt" OnSave="HandleDebtSave" OnCancel="() => IsDebtDrawerOpen = false" />
    </ChildContent>
</SfSidebar>

@* Delete Confirmation *@
<SfDialog @bind-Visible="IsDeleteDialogOpen" Width="400px" IsModal="true" ShowCloseIcon="true">
    <DialogTemplates>
        <Header><span class="font-semibold">Delete Debt</span></Header>
        <Content><p class="text-neutral-600">Are you sure you want to delete <strong>@DeletingDebt?.Name</strong>?</p></Content>
    </DialogTemplates>
    <DialogButtons>
        <DialogButton Content="Cancel" CssClass="e-flat" OnClick="() => IsDeleteDialogOpen = false" />
        <DialogButton Content="Delete" CssClass="e-danger" IsPrimary="true" OnClick="ExecuteDeleteDebt" />
    </DialogButtons>
</SfDialog>

<CelebrationModal @ref="CelebrationModal" />

@code {
    private SfChart? PayoffChart;
    private CelebrationModal? CelebrationModal;
    private decimal ExtraPayment { get; set; } = 0;
    private string SelectedStrategy { get; set; } = "Avalanche";
    private Guid? SelectedDebtId { get; set; }
    private bool IsDebtDrawerOpen { get; set; }
    private bool IsDeleteDialogOpen { get; set; }
    private DebtDto? EditingDebt { get; set; }
    private DebtDto? DeletingDebt { get; set; }
    
    protected override void OnInitialized() => Dispatcher.Dispatch(new LoadDebtsAction());
    
    protected override void OnParametersSet()
    {
        if (State.Value.Debts.Any()) Dispatcher.Dispatch(new CalculateStrategiesAction(ExtraPayment));
    }
    
    private PayoffStrategyDto? GetSelectedStrategy() => State.Value.Strategies.FirstOrDefault(s => s.Name == SelectedStrategy);
    private List<PayoffDataPoint> GetSelectedStrategyData() => GetSelectedStrategy()?.MonthlyProjection ?? new();
    private void SelectStrategy(string name) { SelectedStrategy = name; PayoffChart?.Refresh(); }
    private void SelectDebt(Guid id) => SelectedDebtId = SelectedDebtId == id ? null : id;
    private void OpenAddDebtDialog() { EditingDebt = null; IsDebtDrawerOpen = true; }
    private void EditDebt(DebtDto debt) { EditingDebt = debt; IsDebtDrawerOpen = true; }
    private void ConfirmDeleteDebt(DebtDto debt) { DeletingDebt = debt; IsDeleteDialogOpen = true; }
    
    private async Task ExecuteDeleteDebt()
    {
        if (DeletingDebt != null) Dispatcher.Dispatch(new DeleteDebtAction(DeletingDebt.Id));
        IsDeleteDialogOpen = false; DeletingDebt = null;
    }
    
    private async Task HandleDebtSave(DebtDto debt)
    {
        if (EditingDebt == null) Dispatcher.Dispatch(new AddDebtAction(debt));
        else Dispatcher.Dispatch(new UpdateDebtAction(debt));
        IsDebtDrawerOpen = false; EditingDebt = null;
    }
    
    private string GetAnalysisSummary()
    {
        var strategy = GetSelectedStrategy();
        if (strategy == null) return "";
        var debtFreeDate = DateTime.Now.AddMonths(strategy.MonthsToPayoff);
        return string.Format(L["Summary.Text"], strategy.Name, debtFreeDate.ToString("MMMM yyyy"), strategy.TotalInterestPaid.ToString("C0"));
    }
}
```

---

## Part 9: Insurance Calculator Page

```razor
@* RAJFinancial.Client/Pages/Tools/InsuranceCalculator.razor *@
@page "/tools/insurance"
@attribute [Authorize]
@inject IStringLocalizer<InsuranceCalculator> L
@inject IState<InsuranceState> State
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

<div class="min-h-screen bg-neutral-50">
    <div class="fixed inset-0 gradient-mesh opacity-60 pointer-events-none"></div>
    
    <div class="relative">
        <header class="relative overflow-hidden">
            <div class="absolute inset-0 gradient-dark-gold"></div>
            <div class="relative px-4 pt-8 pb-24 lg:px-8 lg:pt-12 lg:pb-32">
                <div class="max-w-7xl mx-auto">
                    <nav class="mb-6">
                        <a href="/tools" class="text-white/60 hover:text-white text-sm">← @L["BackToTools"]</a>
                    </nav>
                    <h1 class="text-white text-3xl lg:text-4xl font-bold animate-fade-in">@L["Title"]</h1>
                    <p class="text-white/70 mt-2 max-w-2xl">@L["Subtitle"]</p>
                </div>
            </div>
        </header>
        
        <main class="relative -mt-16 px-4 py-8 lg:px-8">
            <div class="max-w-7xl mx-auto">
                <div class="grid lg:grid-cols-2 gap-8">
                    @* Input Section *@
                    <div class="space-y-6">
                        <GlassCard>
                            <h2 class="text-lg font-semibold text-neutral-900 mb-4">@L["Inputs.Title"]</h2>
                            
                            <div class="space-y-4">
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.AnnualIncome"]</label>
                                    <SfNumericTextBox @bind-Value="AnnualIncome" Format="C0" Min="0" Step="5000" CssClass="w-full" />
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.IncomeYears"]</label>
                                    <SfSlider @bind-Value="IncomeYears" Min="1" Max="30" Step="1" Type="SliderType.Default">
                                        <SliderTooltip IsVisible="true" Placement="TooltipPlacement.Before" ShowOn="TooltipShowOn.Always" />
                                    </SfSlider>
                                    <p class="text-xs text-neutral-500 mt-1">@IncomeYears years of income replacement</p>
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.Dependents"]</label>
                                    <SfNumericTextBox @bind-Value="Dependents" Format="N0" Min="0" Max="10" CssClass="w-full" />
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.MortgageBalance"]</label>
                                    <SfNumericTextBox @bind-Value="MortgageBalance" Format="C0" Min="0" CssClass="w-full" />
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.OtherDebts"]</label>
                                    <SfNumericTextBox @bind-Value="OtherDebts" Format="C0" Min="0" CssClass="w-full" />
                                </div>
                                
                                <div class="flex items-center justify-between">
                                    <label class="text-sm font-medium text-neutral-700">@L["Input.IncludeEducation"]</label>
                                    <SfSwitch @bind-Checked="IncludeEducation" />
                                </div>
                            </div>
                        </GlassCard>
                        
                        <GlassCard>
                            <h2 class="text-lg font-semibold text-neutral-900 mb-4">@L["CurrentCoverage.Title"]</h2>
                            <div>
                                <label class="block text-sm font-medium text-neutral-700 mb-1">@L["Input.CurrentCoverage"]</label>
                                <SfNumericTextBox @bind-Value="CurrentCoverage" Format="C0" Min="0" CssClass="w-full" />
                            </div>
                        </GlassCard>
                    </div>
                    
                    @* Results Section *@
                    <div class="space-y-6">
                        @* Coverage Gauge *@
                        <GlassCard CssClass="text-center">
                            <h2 class="text-lg font-semibold text-neutral-900 mb-6">@L["Results.CoverageStatus"]</h2>
                            <CoverageGauge CalculatedNeed="@CalculatedNeed" CurrentCoverage="@CurrentCoverage" CoverageRatio="@CoverageRatio" />
                            
                            <div class="mt-6 grid grid-cols-2 gap-4 text-center">
                                <div>
                                    <p class="text-sm text-neutral-500">@L["Results.Recommended"]</p>
                                    <p class="text-2xl font-bold text-neutral-900">@CalculatedNeed.ToString("C0")</p>
                                </div>
                                <div>
                                    <p class="text-sm text-neutral-500">@L["Results.Current"]</p>
                                    <p class="text-2xl font-bold text-neutral-900">@CurrentCoverage.ToString("C0")</p>
                                </div>
                            </div>
                            
                            @if (CoverageGap > 0)
                            {
                                <div class="mt-4 p-4 rounded-xl bg-warning-50 border border-warning-200">
                                    <p class="text-warning-700 font-medium">@L["Results.Gap"]: @CoverageGap.ToString("C0")</p>
                                </div>
                            }
                        </GlassCard>
                        
                        @* Breakdown *@
                        <GlassCard>
                            <h2 class="text-lg font-semibold text-neutral-900 mb-4">@L["Breakdown.Title"]</h2>
                            <div class="space-y-3">
                                @foreach (var item in GetBreakdownData())
                                {
                                    <div class="flex items-center justify-between p-3 rounded-lg bg-neutral-50">
                                        <div>
                                            <p class="font-medium text-neutral-900">@item.Category</p>
                                            <p class="text-xs text-neutral-500">@item.Description</p>
                                        </div>
                                        <p class="font-bold text-neutral-900">@item.Amount.ToString("C0")</p>
                                    </div>
                                }
                            </div>
                        </GlassCard>
                        
                        <p class="text-xs text-neutral-400 text-center">@L["Disclaimer"]</p>
                    </div>
                </div>
            </div>
        </main>
    </div>
</div>

@code {
    private decimal AnnualIncome { get; set; } = 75000;
    private int IncomeYears { get; set; } = 10;
    private int Dependents { get; set; } = 2;
    private decimal MortgageBalance { get; set; } = 250000;
    private decimal OtherDebts { get; set; } = 25000;
    private bool IncludeEducation { get; set; } = true;
    private decimal CurrentCoverage { get; set; } = 250000;
    
    private decimal IncomeReplacement => AnnualIncome * IncomeYears;
    private decimal DebtPayoff => MortgageBalance + OtherDebts;
    private decimal EducationFunding => IncludeEducation ? Dependents * 50000 : 0;
    private decimal FinalExpenses => 25000;
    private decimal EmergencyFund => AnnualIncome * 0.5m;
    
    private decimal CalculatedNeed => IncomeReplacement + DebtPayoff + EducationFunding + FinalExpenses + EmergencyFund;
    private decimal CoverageGap => Math.Max(0, CalculatedNeed - CurrentCoverage);
    private decimal CoverageRatio => CalculatedNeed > 0 ? CurrentCoverage / CalculatedNeed : 1;
    
    private List<BreakdownDataPoint> GetBreakdownData() => new()
    {
        new("Income Replacement", IncomeReplacement, $"{IncomeYears} years × {AnnualIncome:C0}"),
        new("Debt Payoff", DebtPayoff, "Mortgage + other debts"),
        new("Education Funding", EducationFunding, $"{Dependents} dependents × $50,000"),
        new("Final Expenses", FinalExpenses, "Funeral, medical, legal"),
        new("Emergency Fund", EmergencyFund, "6 months income")
    };
    
    private record BreakdownDataPoint(string Category, decimal Amount, string Description);
}
```

---

## Part 10: Shared Components

### GlassCard Component

```razor
@* RAJFinancial.Client/Components/Shared/GlassCard.razor *@
<div class="glass-card @Class">
    @ChildContent
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;
}
```

```css
/* Glass Card Styling */
.glass-card {
    @apply bg-white/80 backdrop-blur-xl rounded-2xl p-6 shadow-premium border border-white/40;
    background: linear-gradient(
        145deg,
        rgba(255, 255, 255, 0.95) 0%,
        rgba(255, 255, 255, 0.85) 100%
    );
}

.glass-card-dark {
    @apply bg-neutral-900/80 backdrop-blur-xl rounded-2xl p-6 border border-white/10;
    background: linear-gradient(
        145deg,
        rgba(24, 24, 27, 0.95) 0%,
        rgba(24, 24, 27, 0.85) 100%
    );
}
```

---

### EmptyState Component

```razor
@* RAJFinancial.Client/Components/Shared/EmptyState.razor *@
<div class="text-center py-12">
    <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-neutral-100 mb-4">
        <span class="text-2xl">@Icon</span>
    </div>
    <h3 class="text-lg font-semibold text-neutral-900 mb-2">@Title</h3>
    <p class="text-neutral-500 mb-6 max-w-sm mx-auto">@Description</p>
    @if (ShowAction)
    {
        <button @onclick="OnAction" class="btn-primary">
            @ActionText
        </button>
    }
</div>

@code {
    [Parameter] public string Icon { get; set; } = "📭";
    [Parameter] public string Title { get; set; } = "No items found";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public bool ShowAction { get; set; } = false;
    [Parameter] public string ActionText { get; set; } = "Add Item";
    [Parameter] public EventCallback OnAction { get; set; }
}
```

---

### AnimatedNumber Component

```razor
@* RAJFinancial.Client/Components/Shared/AnimatedNumber.razor *@
@implements IAsyncDisposable
@inject IJSRuntime JS

<span class="@Class">@DisplayValue</span>

@code {
    [Parameter] public decimal Value { get; set; }
    [Parameter] public string Format { get; set; } = "C0";
    [Parameter] public string Class { get; set; } = "";
    [Parameter] public int DurationMs { get; set; } = 800;
    
    private decimal _currentValue;
    private string DisplayValue => _currentValue.ToString(Format);
    private CancellationTokenSource? _cts;
    
    protected override async Task OnParametersSetAsync()
    {
        if (_currentValue != Value)
        {
            await AnimateToValueAsync(Value);
        }
    }
    
    private async Task AnimateToValueAsync(decimal targetValue)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        var startValue = _currentValue;
        var diff = targetValue - startValue;
        var steps = 30;
        var stepDelay = DurationMs / steps;
        
        try
        {
            for (int i = 1; i <= steps; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;
                
                var progress = EaseOutCubic((double)i / steps);
                _currentValue = startValue + (decimal)(progress * (double)diff);
                StateHasChanged();
                
                await Task.Delay(stepDelay, _cts.Token);
            }
            _currentValue = targetValue;
            StateHasChanged();
        }
        catch (TaskCanceledException) { }
    }
    
    private double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
    
    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

---

### CelebrationModal Component

```razor
@* RAJFinancial.Client/Components/Shared/CelebrationModal.razor *@
@inject IJSRuntime JS

<SfDialog @bind-Visible="@IsVisible" Width="400px" IsModal="true" ShowCloseIcon="true" CssClass="celebration-dialog">
    <DialogTemplates>
        <Content>
            <div class="text-center py-8">
                <div class="celebration-burst mb-6">
                    <span class="text-6xl">@Icon</span>
                </div>
                <h2 class="text-2xl font-bold text-neutral-900 mb-2">@Title</h2>
                <p class="text-neutral-600 mb-6">@Message</p>
                @if (ShowConfetti)
                {
                    <div class="confetti-container" @ref="_confettiRef"></div>
                }
                <button @onclick="Close" class="btn-primary w-full">
                    @ButtonText
                </button>
            </div>
        </Content>
    </DialogTemplates>
</SfDialog>

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public string Icon { get; set; } = "🎉";
    [Parameter] public string Title { get; set; } = "Congratulations!";
    [Parameter] public string Message { get; set; } = "";
    [Parameter] public string ButtonText { get; set; } = "Continue";
    [Parameter] public bool ShowConfetti { get; set; } = true;
    
    private ElementReference _confettiRef;
    
    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && ShowConfetti)
        {
            await JS.InvokeVoidAsync("triggerConfetti", _confettiRef);
        }
    }
    
    private async Task Close()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
    }
}
```

```css
/* Celebration Burst Animation */
.celebration-burst {
    animation: celebration-burst 0.6s ease-out;
}

@keyframes celebration-burst {
    0% { transform: scale(0) rotate(-15deg); opacity: 0; }
    50% { transform: scale(1.2) rotate(5deg); }
    100% { transform: scale(1) rotate(0deg); opacity: 1; }
}
```

---

## Part 11: AI Insights Components

### InsightsPanel Component

```razor
@* RAJFinancial.Client/Components/Insights/InsightsPanel.razor *@
@inject IState<InsightsState> State
@inject IDispatcher Dispatcher
@inject IStringLocalizer<InsightsPanel> L

<div class="@ContainerClass">
    <div class="flex items-center justify-between mb-4">
        <h2 class="text-lg font-semibold text-neutral-900">@L["Title"]</h2>
        <button @onclick="RefreshInsights" disabled="@State.Value.IsLoading" class="text-primary-600 hover:text-primary-700 text-sm font-medium disabled:opacity-50">
            @if (State.Value.IsLoading)
            {
                <span class="inline-flex items-center">
                    <svg class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    @L["Loading"]
                </span>
            }
            else
            {
                <span>@L["Refresh"]</span>
            }
        </button>
    </div>
    
    @if (State.Value.Insights.Any())
    {
        <div class="space-y-3">
            @foreach (var insight in State.Value.Insights.Take(MaxItems))
            {
                <InsightCard Insight="@insight" OnDismiss="@(() => DismissInsight(insight.Id))" OnAction="@(() => HandleAction(insight))" />
            }
        </div>
        
        @if (State.Value.Insights.Count > MaxItems)
        {
            <button @onclick="ShowAllInsights" class="w-full mt-4 text-center text-sm text-primary-600 hover:text-primary-700 font-medium">
                @L["ViewAll", State.Value.Insights.Count - MaxItems]
            </button>
        }
    }
    else if (!State.Value.IsLoading)
    {
        <EmptyState Icon="💡" Title="@L["Empty.Title"]" Description="@L["Empty.Description"]" />
    }
</div>

@code {
    [Parameter] public string ContainerClass { get; set; } = "";
    [Parameter] public int MaxItems { get; set; } = 3;
    
    private void RefreshInsights() => Dispatcher.Dispatch(new FetchInsightsAction());
    
    private void DismissInsight(Guid id) => Dispatcher.Dispatch(new DismissInsightAction(id));
    
    private void HandleAction(Insight insight)
    {
        Dispatcher.Dispatch(new InsightActionClickedAction(insight.Id, insight.ActionUrl));
    }
    
    private void ShowAllInsights() => Dispatcher.Dispatch(new NavigateToInsightsAction());
}
```

---

### InsightCard Component

```razor
@* RAJFinancial.Client/Components/Insights/InsightCard.razor *@

<div class="insight-card @GetCategoryClass() animate-slide-up group">
    <div class="flex items-start gap-3">
        <div class="insight-icon @GetIconBgClass()">
            <span>@GetCategoryIcon()</span>
        </div>
        <div class="flex-1 min-w-0">
            <div class="flex items-start justify-between gap-2">
                <p class="font-medium text-neutral-900 text-sm">@Insight.Title</p>
                <button @onclick="OnDismiss" class="opacity-0 group-hover:opacity-100 transition-opacity text-neutral-400 hover:text-neutral-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                </button>
            </div>
            <p class="text-neutral-600 text-sm mt-1 line-clamp-2">@Insight.Description</p>
            
            @if (Insight.ImpactAmount.HasValue)
            {
                <div class="flex items-center gap-2 mt-2">
                    <span class="text-xs font-medium @GetImpactClass()">
                        @(Insight.ImpactAmount > 0 ? "+" : "")@Insight.ImpactAmount?.ToString("C0")
                    </span>
                    <span class="text-xs text-neutral-400">@Insight.ImpactDescription</span>
                </div>
            }
            
            @if (!string.IsNullOrEmpty(Insight.ActionText))
            {
                <button @onclick="OnAction" class="mt-3 text-sm font-medium text-primary-600 hover:text-primary-700 inline-flex items-center gap-1">
                    @Insight.ActionText
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                    </svg>
                </button>
            }
        </div>
    </div>
    
    @if (Insight.IsNew)
    {
        <div class="absolute top-0 right-0 -mt-1 -mr-1">
            <span class="flex h-3 w-3">
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-primary-400 opacity-75"></span>
                <span class="relative inline-flex rounded-full h-3 w-3 bg-primary-500"></span>
            </span>
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public Insight Insight { get; set; } = default!;
    [Parameter] public EventCallback OnDismiss { get; set; }
    [Parameter] public EventCallback OnAction { get; set; }
    
    private string GetCategoryClass() => Insight.Category switch
    {
        InsightCategory.Saving => "border-l-4 border-l-success-500",
        InsightCategory.Spending => "border-l-4 border-l-warning-500",
        InsightCategory.Opportunity => "border-l-4 border-l-primary-500",
        InsightCategory.Alert => "border-l-4 border-l-error-500",
        _ => "border-l-4 border-l-neutral-300"
    };
    
    private string GetCategoryIcon() => Insight.Category switch
    {
        InsightCategory.Saving => "💰",
        InsightCategory.Spending => "📊",
        InsightCategory.Opportunity => "✨",
        InsightCategory.Alert => "⚠️",
        _ => "💡"
    };
    
    private string GetIconBgClass() => Insight.Category switch
    {
        InsightCategory.Saving => "bg-success-100",
        InsightCategory.Spending => "bg-warning-100",
        InsightCategory.Opportunity => "bg-primary-100",
        InsightCategory.Alert => "bg-error-100",
        _ => "bg-neutral-100"
    };
    
    private string GetImpactClass() => Insight.ImpactAmount > 0 
        ? "text-success-600" 
        : "text-error-600";
}
```

```css
/* Insight Card Styling */
.insight-card {
    @apply relative p-4 bg-white rounded-xl shadow-soft border border-neutral-100 transition-all duration-200;
}

.insight-card:hover {
    @apply shadow-medium border-neutral-200;
    transform: translateX(4px);
}

.insight-icon {
    @apply w-10 h-10 rounded-xl flex items-center justify-center text-lg flex-shrink-0;
}

/* Line clamp utility */
.line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
```

---

## Part 12: Health Score & Plaid Components

### HealthScoreCard Component

```razor
@* RAJFinancial.Client/Components/Dashboard/HealthScoreCard.razor *@
@inject IStringLocalizer<HealthScoreCard> L

<div class="health-score-card @ContainerClass">
    <div class="flex items-center justify-between mb-4">
        <h3 class="font-semibold text-neutral-900">@L["Title"]</h3>
        <span class="text-xs text-neutral-400">@L["UpdatedAt", LastUpdated.ToRelativeTime()]</span>
    </div>
    
    <div class="relative flex items-center justify-center mb-6">
        <SfCircularGauge Width="200px" Height="200px" Background="transparent">
            <CircularGaugeAxes>
                <CircularGaugeAxis StartAngle="220" EndAngle="140" Minimum="0" Maximum="100">
                    <CircularGaugeAxisLineStyle Width="0" />
                    <CircularGaugeAxisMajorTicks Height="0" />
                    <CircularGaugeAxisMinorTicks Height="0" />
                    <CircularGaugeAxisLabelStyle>
                        <CircularGaugeAxisLabelFont Size="0" />
                    </CircularGaugeAxisLabelStyle>
                    <CircularGaugeRanges>
                        <CircularGaugeRange Start="0" End="40" Color="#ef4444" StartWidth="12" EndWidth="12" />
                        <CircularGaugeRange Start="40" End="70" Color="#f59e0b" StartWidth="12" EndWidth="12" />
                        <CircularGaugeRange Start="70" End="100" Color="#22c55e" StartWidth="12" EndWidth="12" />
                    </CircularGaugeRanges>
                    <CircularGaugePointers>
                        <CircularGaugePointer Value="@Score" Radius="80%" PointerWidth="8" Color="#18181b">
                            <CircularGaugePointerAnimation Enable="true" Duration="1000" />
                            <CircularGaugeCap Radius="10" Color="#18181b" />
                        </CircularGaugePointer>
                    </CircularGaugePointers>
                    <CircularGaugeAnnotations>
                        <CircularGaugeAnnotation ZIndex="1" Angle="0" Radius="0%">
                            <ContentTemplate>
                                <div class="text-center">
                                    <span class="text-4xl font-bold @GetScoreColor()">@Score</span>
                                    <p class="text-xs text-neutral-500 mt-1">@GetScoreLabel()</p>
                                </div>
                            </ContentTemplate>
                        </CircularGaugeAnnotation>
                    </CircularGaugeAnnotations>
                </CircularGaugeAxis>
            </CircularGaugeAxes>
        </SfCircularGauge>
    </div>
    
    <div class="space-y-3">
        @foreach (var factor in Factors)
        {
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                    <div class="w-2 h-2 rounded-full @GetFactorColor(factor.Score)"></div>
                    <span class="text-sm text-neutral-600">@factor.Name</span>
                </div>
                <span class="text-sm font-medium text-neutral-900">@factor.Score/100</span>
            </div>
        }
    </div>
    
    @if (!string.IsNullOrEmpty(RecommendationText))
    {
        <div class="mt-4 p-3 rounded-xl @GetRecommendationBg()">
            <p class="text-sm @GetRecommendationTextColor()">@RecommendationText</p>
        </div>
    }
</div>

@code {
    [Parameter] public int Score { get; set; } = 75;
    [Parameter] public DateTime LastUpdated { get; set; } = DateTime.Now;
    [Parameter] public List<HealthFactor> Factors { get; set; } = new();
    [Parameter] public string? RecommendationText { get; set; }
    [Parameter] public string ContainerClass { get; set; } = "";
    
    private string GetScoreColor() => Score switch
    {
        >= 70 => "text-success-600",
        >= 40 => "text-warning-600",
        _ => "text-error-600"
    };
    
    private string GetScoreLabel() => Score switch
    {
        >= 80 => "Excellent",
        >= 70 => "Good",
        >= 50 => "Fair",
        _ => "Needs Work"
    };
    
    private string GetFactorColor(int score) => score switch
    {
        >= 70 => "bg-success-500",
        >= 40 => "bg-warning-500",
        _ => "bg-error-500"
    };
    
    private string GetRecommendationBg() => Score switch
    {
        >= 70 => "bg-success-50",
        >= 40 => "bg-warning-50",
        _ => "bg-error-50"
    };
    
    private string GetRecommendationTextColor() => Score switch
    {
        >= 70 => "text-success-700",
        >= 40 => "text-warning-700",
        _ => "text-error-700"
    };
    
    public record HealthFactor(string Name, int Score);
}
```

---

### PlaidLinkModal Component

```razor
@* RAJFinancial.Client/Components/Plaid/PlaidLinkModal.razor *@
@inject IPlaidService PlaidService
@inject IJSRuntime JS
@inject IDispatcher Dispatcher
@inject IStringLocalizer<PlaidLinkModal> L

<SfDialog @bind-Visible="@IsVisible" Width="500px" IsModal="true" ShowCloseIcon="true" CssClass="plaid-dialog">
    <DialogTemplates>
        <Header>
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-primary-100 flex items-center justify-center">
                    <span class="text-xl">🏦</span>
                </div>
                <div>
                    <h2 class="text-lg font-semibold text-neutral-900">@L["Title"]</h2>
                    <p class="text-sm text-neutral-500">@L["Subtitle"]</p>
                </div>
            </div>
        </Header>
        <Content>
            <div class="py-4">
                @switch (CurrentStep)
                {
                    case PlaidLinkStep.Intro:
                        <div class="text-center">
                            <div class="space-y-4 mb-6">
                                <div class="flex items-center gap-3 p-3 rounded-xl bg-neutral-50">
                                    <span class="text-2xl">🔒</span>
                                    <p class="text-sm text-neutral-600 text-left">@L["Security.Encrypted"]</p>
                                </div>
                                <div class="flex items-center gap-3 p-3 rounded-xl bg-neutral-50">
                                    <span class="text-2xl">👁️</span>
                                    <p class="text-sm text-neutral-600 text-left">@L["Security.ReadOnly"]</p>
                                </div>
                                <div class="flex items-center gap-3 p-3 rounded-xl bg-neutral-50">
                                    <span class="text-2xl">🗑️</span>
                                    <p class="text-sm text-neutral-600 text-left">@L["Security.DeleteAnytime"]</p>
                                </div>
                            </div>
                            <button @onclick="InitiatePlaidLink" class="btn-primary w-full">
                                @L["ConnectBank"]
                            </button>
                        </div>
                        break;
                        
                    case PlaidLinkStep.Linking:
                        <div class="text-center py-8">
                            <div class="animate-pulse mb-4">
                                <span class="text-5xl">🔗</span>
                            </div>
                            <p class="text-neutral-600">@L["Linking.Message"]</p>
                        </div>
                        break;
                        
                    case PlaidLinkStep.Success:
                        <div class="text-center py-8">
                            <div class="celebration-burst mb-4">
                                <span class="text-5xl">✅</span>
                            </div>
                            <h3 class="text-lg font-semibold text-neutral-900 mb-2">@L["Success.Title"]</h3>
                            <p class="text-neutral-600 mb-6">@L["Success.Message", LinkedAccountCount]</p>
                            <button @onclick="Close" class="btn-primary w-full">
                                @L["Success.Continue"]
                            </button>
                        </div>
                        break;
                        
                    case PlaidLinkStep.Error:
                        <div class="text-center py-8">
                            <span class="text-5xl mb-4 block">❌</span>
                            <h3 class="text-lg font-semibold text-neutral-900 mb-2">@L["Error.Title"]</h3>
                            <p class="text-neutral-600 mb-6">@ErrorMessage</p>
                            <button @onclick="RetryLink" class="btn-primary w-full mb-2">
                                @L["Error.Retry"]
                            </button>
                            <button @onclick="Close" class="btn-secondary w-full">
                                @L["Error.Cancel"]
                            </button>
                        </div>
                        break;
                }
            </div>
        </Content>
    </DialogTemplates>
</SfDialog>

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<PlaidLinkResult> OnLinkComplete { get; set; }
    
    private PlaidLinkStep CurrentStep { get; set; } = PlaidLinkStep.Intro;
    private string? ErrorMessage { get; set; }
    private int LinkedAccountCount { get; set; }
    
    private async Task InitiatePlaidLink()
    {
        CurrentStep = PlaidLinkStep.Linking;
        
        try
        {
            var linkToken = await PlaidService.CreateLinkTokenAsync();
            await JS.InvokeVoidAsync("plaidLink.open", linkToken, DotNetObjectReference.Create(this));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CurrentStep = PlaidLinkStep.Error;
        }
    }
    
    [JSInvokable]
    public async Task OnPlaidSuccess(string publicToken, string[] accountIds)
    {
        try
        {
            var result = await PlaidService.ExchangePublicTokenAsync(publicToken);
            LinkedAccountCount = accountIds.Length;
            CurrentStep = PlaidLinkStep.Success;
            
            await OnLinkComplete.InvokeAsync(new PlaidLinkResult(true, LinkedAccountCount));
            Dispatcher.Dispatch(new RefreshAccountsAction());
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CurrentStep = PlaidLinkStep.Error;
        }
    }
    
    [JSInvokable]
    public void OnPlaidExit(string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            ErrorMessage = error;
            CurrentStep = PlaidLinkStep.Error;
        }
        else
        {
            CurrentStep = PlaidLinkStep.Intro;
        }
    }
    
    private void RetryLink()
    {
        ErrorMessage = null;
        CurrentStep = PlaidLinkStep.Intro;
    }
    
    private async Task Close()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
        CurrentStep = PlaidLinkStep.Intro;
    }
    
    private enum PlaidLinkStep { Intro, Linking, Success, Error }
}

public record PlaidLinkResult(bool Success, int AccountCount);
```

---

## Part 13: Navigation Components

### MobileBottomNav Component

```razor
@* RAJFinancial.Client/Components/Layout/MobileBottomNav.razor *@
@inject NavigationManager Navigation
@inject IStringLocalizer<MobileBottomNav> L

<nav class="lg:hidden fixed bottom-0 left-0 right-0 z-50 safe-area-bottom">
    <div class="glass-nav border-t border-neutral-200/50">
        <div class="flex items-center justify-around px-2 py-2">
            @foreach (var item in NavItems)
            {
                <NavLink href="@item.Href" Match="@item.Match" class="nav-item-mobile" ActiveClass="nav-item-mobile-active">
                    <span class="nav-icon">@item.Icon</span>
                    <span class="nav-label">@L[item.LabelKey]</span>
                </NavLink>
            }
        </div>
    </div>
</nav>

@code {
    private List<NavItem> NavItems => new()
    {
        new("", "🏠", "Home", NavLinkMatch.All),
        new("assets", "💰", "Assets", NavLinkMatch.Prefix),
        new("debts", "💳", "Debts", NavLinkMatch.Prefix),
        new("tools", "🔧", "Tools", NavLinkMatch.Prefix),
        new("settings", "⚙️", "Settings", NavLinkMatch.Prefix)
    };
    
    private record NavItem(string Href, string Icon, string LabelKey, NavLinkMatch Match);
}
```

```css
/* Mobile Bottom Nav Styling */
.glass-nav {
    background: rgba(255, 255, 255, 0.85);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
}

.nav-item-mobile {
    @apply flex flex-col items-center justify-center py-2 px-3 rounded-xl transition-all duration-200;
    min-width: 64px;
}

.nav-item-mobile .nav-icon {
    @apply text-xl mb-0.5 transition-transform duration-200;
}

.nav-item-mobile .nav-label {
    @apply text-xs text-neutral-500 font-medium;
}

.nav-item-mobile:active .nav-icon {
    transform: scale(0.9);
}

.nav-item-mobile-active {
    @apply bg-primary-50;
}

.nav-item-mobile-active .nav-label {
    @apply text-primary-600;
}

.nav-item-mobile-active .nav-icon {
    transform: scale(1.1);
}

/* Safe area for devices with home indicators */
.safe-area-bottom {
    padding-bottom: env(safe-area-inset-bottom, 0);
}
```

---

### DesktopSidebar Component

```razor
@* RAJFinancial.Client/Components/Layout/DesktopSidebar.razor *@
@inject NavigationManager Navigation
@inject IState<UserState> UserState
@inject IStringLocalizer<DesktopSidebar> L

<aside class="hidden lg:flex lg:flex-col lg:fixed lg:inset-y-0 lg:w-64 lg:z-50">
    <div class="flex flex-col flex-1 min-h-0 bg-neutral-900">
        @* Logo *@
        <div class="flex items-center h-16 px-4 border-b border-neutral-800">
            <a href="/" class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-gradient-gold flex items-center justify-center">
                    <span class="text-lg font-bold text-neutral-900">R</span>
                </div>
                <span class="text-xl font-bold text-white">RAJ Financial</span>
            </a>
        </div>
        
        @* Navigation *@
        <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
            @foreach (var section in NavSections)
            {
                @if (!string.IsNullOrEmpty(section.Title))
                {
                    <p class="px-3 pt-4 pb-2 text-xs font-semibold text-neutral-500 uppercase tracking-wider">@L[section.Title]</p>
                }
                @foreach (var item in section.Items)
                {
                    <NavLink href="@item.Href" Match="@item.Match" class="nav-item-desktop group" ActiveClass="nav-item-desktop-active">
                        <span class="nav-icon-desktop">@item.Icon</span>
                        <span class="flex-1">@L[item.LabelKey]</span>
                        @if (item.Badge > 0)
                        {
                            <span class="badge-desktop">@item.Badge</span>
                        }
                    </NavLink>
                }
            }
        </nav>
        
        @* User Profile *@
        <div class="flex-shrink-0 p-4 border-t border-neutral-800">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-full bg-gradient-gold flex items-center justify-center">
                    <span class="text-sm font-bold text-neutral-900">@GetInitials()</span>
                </div>
                <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-white truncate">@UserState.Value.FullName</p>
                    <p class="text-xs text-neutral-400 truncate">@UserState.Value.Email</p>
                </div>
                <button @onclick="OpenSettings" class="p-2 rounded-lg hover:bg-neutral-800 transition-colors">
                    <svg class="w-5 h-5 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                </button>
            </div>
        </div>
    </div>
</aside>

@code {
    private List<NavSection> NavSections => new()
    {
        new("", new List<NavItemDesktop>
        {
            new("", "🏠", "Dashboard", NavLinkMatch.All, 0),
        }),
        new("Finances", new List<NavItemDesktop>
        {
            new("assets", "💰", "Assets", NavLinkMatch.Prefix, 0),
            new("debts", "💳", "Debts", NavLinkMatch.Prefix, 0),
            new("accounts", "🏦", "Accounts", NavLinkMatch.Prefix, UserState.Value.PendingAccountCount),
        }),
        new("Tools", new List<NavItemDesktop>
        {
            new("tools/debt-payoff", "📊", "DebtAnalyzer", NavLinkMatch.Prefix, 0),
            new("tools/insurance", "🛡️", "Insurance", NavLinkMatch.Prefix, 0),
            new("tools/goals", "🎯", "Goals", NavLinkMatch.Prefix, 0),
        }),
        new("Settings", new List<NavItemDesktop>
        {
            new("settings", "⚙️", "Settings", NavLinkMatch.Prefix, 0),
            new("help", "❓", "Help", NavLinkMatch.Prefix, 0),
        }),
    };
    
    private string GetInitials() => string.Join("", UserState.Value.FullName.Split(' ').Take(2).Select(n => n.FirstOrDefault()));
    
    private void OpenSettings() => Navigation.NavigateTo("/settings");
    
    private record NavSection(string Title, List<NavItemDesktop> Items);
    private record NavItemDesktop(string Href, string Icon, string LabelKey, NavLinkMatch Match, int Badge);
}
```

```css
/* Desktop Sidebar Styling */
.nav-item-desktop {
    @apply flex items-center gap-3 px-3 py-2.5 rounded-xl text-neutral-300 transition-all duration-200;
}

.nav-item-desktop:hover {
    @apply bg-neutral-800 text-white;
}

.nav-icon-desktop {
    @apply text-lg w-6 text-center;
}

.nav-item-desktop-active {
    @apply bg-primary-500/20 text-primary-400;
}

.nav-item-desktop-active:hover {
    @apply bg-primary-500/30 text-primary-300;
}

.badge-desktop {
    @apply px-2 py-0.5 text-xs font-medium rounded-full bg-primary-500 text-white;
}
```

## Part 14: Configuration & JavaScript Interop

### appsettings.json

\`\`\`\`\`\`json
{
  "ApiSettings": {
    "BaseUrl": "https://rajfinancial-api.azurewebsites.net/api",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  },
  "PlaidSettings": {
    "Environment": "sandbox"
  },
  "FeatureFlags": {
    "EnableAIInsights": true,
    "EnablePlaidLink": true,
    "EnableOfflineMode": false,
    "EnableBiometricAuth": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "RAJFinancial": "Information"
    }
  }
}
\`\`\`\`\`\`

---

### JavaScript Interop (wwwroot/js/app.js)

\`\`\`\`\`\`javascript
// Plaid Link Integration
window.plaidLink = {
    handler: null,
    
    open: function (linkToken, dotNetHelper) {
        this.handler = Plaid.create({
            token: linkToken,
            onSuccess: async (publicToken, metadata) => {
                const accountIds = metadata.accounts.map(a => a.id);
                await dotNetHelper.invokeMethodAsync('OnPlaidSuccess', publicToken, accountIds);
            },
            onExit: async (err, metadata) => {
                const errorMsg = err ? err.display_message || err.error_message : null;
                await dotNetHelper.invokeMethodAsync('OnPlaidExit', errorMsg);
            },
            onEvent: (eventName, metadata) => {
                console.log('Plaid Event:', eventName, metadata);
            }
        });
        this.handler.open();
    },
    
    destroy: function () {
        if (this.handler) {
            this.handler.destroy();
            this.handler = null;
        }
    }
};

// Confetti Effect
window.triggerConfetti = function (element) {
    if (typeof confetti === 'function') {
        const rect = element.getBoundingClientRect();
        confetti({
            particleCount: 100,
            spread: 70,
            origin: {
                x: (rect.left + rect.width / 2) / window.innerWidth,
                y: (rect.top + rect.height / 2) / window.innerHeight
            },
            colors: ['#ebbb10', '#d4a50e', '#f5d76e', '#18181b']
        });
    }
};

// Haptic Feedback (for mobile)
window.hapticFeedback = function (type) {
    if ('vibrate' in navigator) {
        switch (type) {
            case 'light':
                navigator.vibrate(10);
                break;
            case 'medium':
                navigator.vibrate(20);
                break;
            case 'heavy':
                navigator.vibrate([30, 10, 30]);
                break;
            case 'success':
                navigator.vibrate([10, 50, 10, 50, 10]);
                break;
        }
    }
};

// Scroll to element
window.scrollToElement = function (elementId, behavior = 'smooth') {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: behavior, block: 'start' });
    }
};

// LocalStorage wrapper for offline data
window.localData = {
    set: function (key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch (e) {
            console.error('LocalStorage error:', e);
            return false;
        }
    },
    
    get: function (key) {
        try {
            const item = localStorage.getItem(key);
            return item ? JSON.parse(item) : null;
        } catch (e) {
            console.error('LocalStorage error:', e);
            return null;
        }
    },
    
    remove: function (key) {
        localStorage.removeItem(key);
    },
    
    clear: function () {
        localStorage.clear();
    }
};
\`\`\`\`\`\`

---

### index.html Structure

\`\`\`\`\`\`html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
    <meta name="theme-color" content="#18181b" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    
    <title>RAJ Financial</title>
    
    <base href="/" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <link rel="apple-touch-icon" href="icon-192.png" />
    <link rel="manifest" href="manifest.webmanifest" />
    
    <!-- Fonts -->
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    
    <!-- Syncfusion -->
    <link href="_content/Syncfusion.Blazor.Themes/tailwind.css" rel="stylesheet" />
    
    <!-- App Styles -->
    <link href="css/app.css" rel="stylesheet" />
    <link href="RAJFinancial.Client.styles.css" rel="stylesheet" />
</head>
<body class="antialiased">
    <div id="app">
        <div class="loading-screen">
            <div class="loading-logo animate-pulse">
                <div class="w-16 h-16 rounded-2xl bg-gradient-gold flex items-center justify-center">
                    <span class="text-2xl font-bold text-neutral-900">R</span>
                </div>
            </div>
            <p class="loading-text">Loading RAJ Financial...</p>
        </div>
    </div>
    
    <div id="blazor-error-ui" class="hidden">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
    </div>
    
    <!-- Plaid Link SDK -->
    <script src="https://cdn.plaid.com/link/v2/stable/link-initialize.js"></script>
    
    <!-- Confetti -->
    <script src="https://cdn.jsdelivr.net/npm/canvas-confetti@1.6.0/dist/confetti.browser.min.js"></script>
    
    <!-- App Scripts -->
    <script src="js/app.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
\`\`\`\`\`\`

---

## Summary

This **RAJ_FINANCIAL_UI.md** document provides the complete frontend specification including:

---

## Part 15: Strategy Sources (Uploads + URLs)

This feature allows users to add their own reference material (uploaded documents and/or reference websites) containing strategies they want to follow, then request suggestions grounded in:
- their current financial profile, and
- the selected strategy sources.

### Minimal UX (MVP)

User actions:
- Upload a document
- Add a URL
- View ingestion status (Pending/Ready/Failed)
- Delete a source

Suggested placement:
- Add a “Strategy Sources” section under Settings (simple and discoverable)

Suggested components:

| Component/Page | Purpose |
|----------------|---------|
| StrategySources.razor (Page/Section) | List sources + actions |
| UploadSourceDialog.razor | File upload + validation |
| AddUrlDialog.razor | Add URL + validation |
| SourceStatusBadge.razor | Pending/Ready/Failed |

Safety notes:
- Show citations and short excerpts only.
- Avoid displaying large verbatim portions of uploaded copyrighted text.

| Section | Contents |
|---------|----------|
| **Part 1** | Solution structure and project organization |
| **Part 2** | NuGet package dependencies |
| **Part 3** | Syncfusion component reference table |
| **Part 4** | Complete CSS design system with gold theme |
| **Part 5** | Program.cs configuration and DI setup |
| **Part 6** | Dashboard.razor complete implementation |
| **Part 7** | Assets.razor with grid, mobile cards, dialogs |
| **Part 8** | DebtPayoff.razor analyzer with strategy cards |
| **Part 9** | Insurance Calculator page |
| **Part 10** | Shared components (GlassCard, EmptyState, AnimatedNumber, CelebrationModal) |
| **Part 11** | AI Insights components (InsightsPanel, InsightCard) |
| **Part 12** | Health Score and Plaid Link components |
| **Part 13** | Navigation (MobileBottomNav, DesktopSidebar) |
| **Part 14** | Configuration files and JavaScript interop |
| **Part 15** | Strategy Sources (Uploads + URLs) |

"@