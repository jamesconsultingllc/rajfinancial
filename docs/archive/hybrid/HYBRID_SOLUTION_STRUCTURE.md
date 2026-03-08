# RAJ Financial - Hybrid Desktop Solution Structure

## Complete Project Structure for Blazor Hybrid (MAUI)

This document shows the full solution structure if RAJ Financial were built as a Blazor Hybrid desktop application.

---

## Solution Layout

```
RAJFinancial.Hybrid/
│
├── RAJFinancial.Hybrid.sln
│
├── src/
│   │
│   ├── RAJFinancial.Desktop/                    # MAUI Blazor Hybrid Host
│   │   ├── RAJFinancial.Desktop.csproj
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainPage.xaml                        # Hosts BlazorWebView
│   │   ├── MainPage.xaml.cs
│   │   ├── MauiProgram.cs                       # DI configuration
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   │
│   │   ├── Platforms/
│   │   │   └── Windows/
│   │   │       ├── App.xaml
│   │   │       ├── App.xaml.cs
│   │   │       ├── Package.appxmanifest
│   │   │       └── app.ico
│   │   │
│   │   ├── Resources/
│   │   │   ├── AppIcon/
│   │   │   │   ├── appicon.svg
│   │   │   │   └── appicon.png
│   │   │   ├── Splash/
│   │   │   │   └── splash.svg
│   │   │   └── Fonts/
│   │   │       └── Nexa-XBold.ttf
│   │   │
│   │   └── Services/
│   │       ├── WindowsCredentialService.cs      # Secure token storage
│   │       ├── NativeNotificationService.cs     # Windows toast notifications
│   │       └── FileSystemService.cs             # File operations
│   │
│   ├── RAJFinancial.UI/                         # Shared Razor Class Library
│   │   ├── RAJFinancial.UI.csproj
│   │   │
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   ├── MainLayout.razor.css
│   │   │   │   ├── DesktopSidebar.razor
│   │   │   │   └── NavMenu.razor
│   │   │   │
│   │   │   ├── Common/
│   │   │   │   ├── GlassCard.razor
│   │   │   │   ├── EmptyState.razor
│   │   │   │   ├── AnimatedNumber.razor
│   │   │   │   ├── CelebrationModal.razor
│   │   │   │   ├── TrendBadge.razor
│   │   │   │   └── LoadingSpinner.razor
│   │   │   │
│   │   │   ├── Dashboard/
│   │   │   │   ├── NetWorthHero.razor
│   │   │   │   ├── QuickStatCard.razor
│   │   │   │   ├── InsightsPanel.razor
│   │   │   │   ├── InsightCard.razor
│   │   │   │   ├── HealthScoreCard.razor
│   │   │   │   ├── NetWorthChartCard.razor
│   │   │   │   ├── AssetAllocationCard.razor
│   │   │   │   └── RecentActivityCard.razor
│   │   │   │
│   │   │   ├── Accounts/
│   │   │   │   ├── PlaidLinkModal.razor
│   │   │   │   ├── AccountCard.razor
│   │   │   │   └── ConnectionStatusBadge.razor
│   │   │   │
│   │   │   ├── Assets/
│   │   │   │   ├── AssetForm.razor
│   │   │   │   ├── AssetCard.razor
│   │   │   │   └── AssetTypeIcon.razor
│   │   │   │
│   │   │   ├── Beneficiaries/
│   │   │   │   ├── BeneficiaryCard.razor
│   │   │   │   ├── BeneficiaryForm.razor
│   │   │   │   └── AssignmentDialog.razor
│   │   │   │
│   │   │   ├── DebtPayoff/
│   │   │   │   ├── StrategyCard.razor
│   │   │   │   ├── DebtListItem.razor
│   │   │   │   ├── DebtForm.razor
│   │   │   │   └── PayoffScheduleTable.razor
│   │   │   │
│   │   │   └── Insurance/
│   │   │       ├── CoverageGauge.razor
│   │   │       └── BreakdownItem.razor
│   │   │
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
│   │   │
│   │   ├── State/                               # Fluxor State Management
│   │   │   ├── AppState/
│   │   │   │   ├── AppState.cs
│   │   │   │   ├── AppActions.cs
│   │   │   │   └── AppReducers.cs
│   │   │   ├── AuthState/
│   │   │   │   ├── AuthState.cs
│   │   │   │   ├── AuthActions.cs
│   │   │   │   ├── AuthReducers.cs
│   │   │   │   └── AuthEffects.cs
│   │   │   ├── DashboardState/
│   │   │   ├── AccountState/
│   │   │   ├── AssetState/
│   │   │   ├── BeneficiaryState/
│   │   │   └── DebtPayoffState/
│   │   │
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   │   ├── app.css
│   │   │   │   └── raj-theme.css
│   │   │   ├── js/
│   │   │   │   ├── app.js
│   │   │   │   └── plaid-link.js
│   │   │   ├── images/
│   │   │   │   ├── logo.svg
│   │   │   │   ├── logo_only.svg
│   │   │   │   └── logo_horizontal.svg
│   │   │   └── index.html                       # Only for web (not used in desktop)
│   │   │
│   │   ├── _Imports.razor
│   │   └── Routes.razor                         # Shared router
│   │
│   ├── RAJFinancial.Core/                       # Domain Layer (unchanged)
│   │   ├── RAJFinancial.Core.csproj
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── LinkedAccount.cs
│   │   │   ├── Asset.cs
│   │   │   ├── Beneficiary.cs
│   │   │   ├── BeneficiaryAssignment.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/
│   │   │   ├── AssetType.cs
│   │   │   ├── AccountType.cs
│   │   │   ├── BeneficiaryType.cs
│   │   │   ├── ConnectionStatus.cs
│   │   │   └── DebtType.cs
│   │   ├── Interfaces/
│   │   │   ├── IAccountService.cs
│   │   │   ├── IAssetService.cs
│   │   │   ├── IBeneficiaryService.cs
│   │   │   ├── IAnalysisService.cs
│   │   │   ├── IPlaidService.cs
│   │   │   ├── IClaudeAIService.cs
│   │   │   ├── ISyncService.cs                  # NEW: Cloud sync
│   │   │   └── ISecureStorage.cs                # NEW: Credential storage
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── Percentage.cs
│   │       └── Address.cs
│   │
│   ├── RAJFinancial.Application/                # Application Layer
│   │   ├── RAJFinancial.Application.csproj
│   │   ├── Services/
│   │   │   ├── AccountService.cs
│   │   │   ├── AssetService.cs
│   │   │   ├── BeneficiaryService.cs
│   │   │   └── AnalysisService.cs
│   │   ├── Validators/
│   │   │   ├── CreateAssetValidator.cs
│   │   │   ├── CreateBeneficiaryValidator.cs
│   │   │   └── DebtPayoffRequestValidator.cs
│   │   └── Mappings/
│   │       └── MappingProfile.cs
│   │
│   ├── RAJFinancial.Infrastructure/             # Infrastructure Layer
│   │   ├── RAJFinancial.Infrastructure.csproj
│   │   │
│   │   ├── Data/
│   │   │   ├── LocalDbContext.cs                # SQLite context
│   │   │   ├── Migrations/
│   │   │   └── Seeding/
│   │   │       └── InitialDataSeeder.cs
│   │   │
│   │   ├── External/
│   │   │   ├── PlaidService.cs                  # Direct Plaid API calls
│   │   │   ├── ClaudeAIService.cs               # Direct Claude API calls
│   │   │   └── PlaidLinkWebViewHandler.cs       # WebView integration
│   │   │
│   │   ├── Sync/
│   │   │   ├── CloudSyncService.cs              # Sync with cloud backend
│   │   │   ├── ConflictResolver.cs
│   │   │   └── OfflineQueueService.cs
│   │   │
│   │   ├── Security/
│   │   │   ├── EncryptionService.cs             # Local data encryption
│   │   │   ├── SqlCipherProvider.cs             # SQLite encryption
│   │   │   └── ApiKeyManager.cs                 # Secure API key storage
│   │   │
│   │   └── Logging/
│   │       └── LocalAuditLogger.cs
│   │
│   ├── RAJFinancial.Shared/                     # Shared DTOs (unchanged)
│   │   ├── RAJFinancial.Shared.csproj
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   └── Responses/
│   │   ├── Enums/
│   │   ├── Constants/
│   │   │   └── ErrorCodes.cs
│   │   └── Extensions/
│   │
│   └── RAJFinancial.CloudSync/                  # Optional Cloud Backend
│       ├── RAJFinancial.CloudSync.csproj        # Azure Functions (minimal)
│       ├── Functions/
│       │   ├── PlaidWebhook.cs                  # Receive Plaid webhooks
│       │   ├── SyncData.cs                      # Encrypted blob sync
│       │   ├── GetAiApiKey.cs                   # Secure key retrieval
│       │   └── PushNotification.cs              # Notify desktop clients
│       └── host.json
│
├── tests/
│   ├── RAJFinancial.Core.Tests/
│   ├── RAJFinancial.Application.Tests/
│   ├── RAJFinancial.Infrastructure.Tests/
│   └── RAJFinancial.UI.Tests/                   # bUnit tests
│
├── .github/
│   └── workflows/
│       ├── build-desktop.yml                    # Build MSIX package
│       ├── release-desktop.yml                  # Publish to Store/CDN
│       └── build-cloudsync.yml                  # Deploy cloud functions
│
├── docs/
│   ├── hybrid/
│   │   ├── BLAZOR_HYBRID_ARCHITECTURE.md
│   │   ├── HYBRID_SOLUTION_STRUCTURE.md         # This file
│   │   └── HYBRID_IMPLEMENTATION_GUIDE.md
│   └── (existing docs)
│
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props                     # Central package management
└── README.md
```

---

## Project Files

### RAJFinancial.Desktop.csproj (MAUI Host)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFrameworks>net9.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>RAJFinancial.Desktop</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Windows-specific -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
    
    <!-- App Identity -->
    <ApplicationTitle>RAJ Financial</ApplicationTitle>
    <ApplicationId>com.rajfinancial.desktop</ApplicationId>
    <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Blazor Hybrid -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="9.0.0" />
    
    <!-- Authentication -->
    <PackageReference Include="Microsoft.Identity.Client" Version="4.61.0" />
    <PackageReference Include="Microsoft.Identity.Client.Extensions.Msal" Version="4.61.0" />
    
    <!-- State Management -->
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.9.1" />
    
    <!-- UI Components -->
    <PackageReference Include="Syncfusion.Blazor" Version="24.2.9" />
    
    <!-- Local Storage -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    
    <!-- Secure Storage -->
    <PackageReference Include="Microsoft.Extensions.SecureStorage" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Project References -->
    <ProjectReference Include="..\RAJFinancial.UI\RAJFinancial.UI.csproj" />
    <ProjectReference Include="..\RAJFinancial.Application\RAJFinancial.Application.csproj" />
    <ProjectReference Include="..\RAJFinancial.Infrastructure\RAJFinancial.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- App Icon -->
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#18181b" />
    
    <!-- Splash Screen -->
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#18181b" BaseSize="128,128" />
    
    <!-- Custom Fonts -->
    <MauiFont Include="Resources\Fonts\*" />
  </ItemGroup>

</Project>
```

### RAJFinancial.UI.csproj (Shared Razor Library)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.0" />
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.9.1" />
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
    <PackageReference Include="Microsoft.Extensions.Localization" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\RAJFinancial.Core\RAJFinancial.Core.csproj" />
    <ProjectReference Include="..\RAJFinancial.Shared\RAJFinancial.Shared.csproj" />
  </ItemGroup>

</Project>
```

### MauiProgram.cs (Desktop Entry Point)

```csharp
using Microsoft.Extensions.Logging;
using RAJFinancial.Application.Services;
using RAJFinancial.Core.Interfaces;
using RAJFinancial.Infrastructure.Data;
using RAJFinancial.Infrastructure.External;
using RAJFinancial.Infrastructure.Security;
using RAJFinancial.Infrastructure.Sync;
using Syncfusion.Blazor;
using Fluxor;

namespace RAJFinancial.Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Nexa-XBold.ttf", "NexaXBold");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add Blazor WebView
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configuration
        var configPath = Path.Combine(
            FileSystem.AppDataDirectory, 
            "appsettings.json");
        
        if (File.Exists(configPath))
        {
            builder.Configuration.AddJsonFile(configPath);
        }
        builder.Configuration.AddJsonFile("appsettings.json", optional: true);

        // Syncfusion license
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
            builder.Configuration["Syncfusion:LicenseKey"]);

        // Syncfusion Blazor services
        builder.Services.AddSyncfusionBlazor();

        // Fluxor state management
        builder.Services.AddFluxor(options => options
            .ScanAssemblies(typeof(RAJFinancial.UI._Imports).Assembly)
            .UseReduxDevTools());

        // Localization
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        // Database (SQLite)
        builder.Services.AddDbContext<LocalDbContext>(options =>
        {
            var dbPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "rajfinancial.db");
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Security services
        builder.Services.AddSingleton<ISecureStorage, WindowsCredentialService>();
        builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
        builder.Services.AddSingleton<IApiKeyManager, ApiKeyManager>();

        // Application services (direct, not via HTTP)
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAssetService, AssetService>();
        builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();
        builder.Services.AddScoped<IAnalysisService, AnalysisService>();

        // External service integrations
        builder.Services.AddHttpClient<IPlaidService, PlaidService>(client =>
        {
            client.BaseAddress = new Uri("https://sandbox.plaid.com");
        });
        
        builder.Services.AddHttpClient<IClaudeAIService, ClaudeAIService>(client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com");
        });

        // Optional cloud sync
        builder.Services.AddScoped<ISyncService, CloudSyncService>();
        builder.Services.AddScoped<IOfflineQueue, OfflineQueueService>();

        // MSAL Authentication
        builder.Services.AddSingleton<IPublicClientApplication>(sp =>
        {
            return PublicClientApplicationBuilder
                .Create(builder.Configuration["AzureAd:ClientId"])
                .WithAuthority(builder.Configuration["AzureAd:Authority"])
                .WithRedirectUri("http://localhost")
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                .Build();
        });

        // Native services
        builder.Services.AddSingleton<INativeNotificationService, NativeNotificationService>();
        builder.Services.AddSingleton<IFileSystemService, FileSystemService>();

        return builder.Build();
    }
}
```

### MainPage.xaml (BlazorWebView Host)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:RAJFinancial.Desktop"
             x:Class="RAJFinancial.Desktop.MainPage"
             BackgroundColor="#18181b">

    <BlazorWebView x:Name="blazorWebView" HostPage="wwwroot/index.html">
        <BlazorWebView.RootComponents>
            <RootComponent Selector="#app" ComponentType="{x:Type local:Routes}" />
        </BlazorWebView.RootComponents>
    </BlazorWebView>

</ContentPage>
```

### LocalDbContext.cs (SQLite)

```csharp
using Microsoft.EntityFrameworkCore;
using RAJFinancial.Core.Entities;

namespace RAJFinancial.Infrastructure.Data;

public class LocalDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<LinkedAccount> LinkedAccounts => Set<LinkedAccount>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<BeneficiaryAssignment> BeneficiaryAssignments => Set<BeneficiaryAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    // Sync tracking
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<OfflineOperation> OfflineQueue => Set<OfflineOperation>();

    private readonly string _dbPath;

    public LocalDbContext()
    {
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RAJFinancial", "data.db");
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public LocalDbContext(DbContextOptions<LocalDbContext> options) 
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User - single user for desktop app
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // LinkedAccount
        modelBuilder.Entity<LinkedAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlaidItemId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.LinkedAccounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Asset
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssetType);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Assets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CurrentValue).HasConversion<double>();
            entity.Property(e => e.OwnershipPercentage).HasConversion<double>();
        });

        // Beneficiary
        modelBuilder.Entity<Beneficiary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Beneficiaries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BeneficiaryAssignment
        modelBuilder.Entity<BeneficiaryAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BeneficiaryId);
            entity.HasIndex(e => e.AssetId);
            entity.Property(e => e.AllocationPercentage).HasConversion<double>();
        });

        // Sync tracking
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SyncedAt);
        });

        modelBuilder.Entity<OfflineOperation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-set timestamps
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IHasTimestamps entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(ct);
    }
}

// Sync tracking entities
public class SyncLog
{
    public Guid Id { get; set; }
    public DateTime SyncedAt { get; set; }
    public string SyncType { get; set; } = string.Empty; // Full, Incremental
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OfflineOperation
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty; // Create, Update, Delete
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Synced { get; set; }
    public DateTime? SyncedAt { get; set; }
}
```

---

## Key Differences from Web Structure

| Aspect | Web | Hybrid |
|--------|-----|--------|
| **Host Project** | `RAJFinancial.Client` (WASM) | `RAJFinancial.Desktop` (MAUI) |
| **UI Components** | In Client project | Extracted to `RAJFinancial.UI` |
| **API Project** | `RAJFinancial.Api` (Azure Functions) | `RAJFinancial.CloudSync` (minimal) |
| **DbContext** | Azure SQL via API | `LocalDbContext` (SQLite) |
| **Auth** | MSAL.js (browser) | MSAL.NET (native) |
| **Secrets** | Azure Key Vault | Windows Credential Manager |
| **Sync** | N/A (cloud-first) | `CloudSyncService` (optional) |

---

## Benefits of This Structure

1. **95% UI code reuse** - Same Razor components for web and desktop
2. **Clean separation** - UI layer has no platform dependencies
3. **Easy testing** - UI library can be tested with bUnit
4. **Future web support** - Add `RAJFinancial.Web` project later if needed
5. **Offline-first** - Local SQLite works without internet
6. **Fast startup** - Native .NET, no WASM download
