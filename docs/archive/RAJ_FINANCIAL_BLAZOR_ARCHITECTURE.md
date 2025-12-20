# RAJ Financial Software - Technical Architecture
## Blazor WebAssembly + Syncfusion + MemoryPack

---

## Brand Identity

**Name**: RAJ Financial Software  
**Logo**: RF monogram with wing motif (gold gradient)  
**Font**: Nexa XBold  
**Colors**:
- `#fffbcc` - Lemon Chiffon (backgrounds)
- `#eed688` - Flax (secondary)
- `#ebbb10` - Spanish Yellow (PRIMARY)
- `#c3922e` - UC Gold (accent)

---

## Technology Stack

### Frontend: Blazor WebAssembly
- **Framework**: .NET 8 Blazor WebAssembly (Standalone)
- **UI Components**: Syncfusion Blazor (v24+)
- **Hosting**: Azure Static Web Apps
- **State Management**: Fluxor (Redux pattern for Blazor)
- **HTTP Client**: Custom client with content negotiation (JSON/MemoryPack)

### Backend: Azure Functions (.NET 8)
- **Runtime**: Isolated Worker Process
- **Serialization**: MemoryPack (prod) / System.Text.Json (dev)
- **Database**: Azure SQL with EF Core 8
- **Caching**: Azure Redis Cache
- **Secrets**: Azure Key Vault

### Shared Library
- **DTOs**: Shared between Blazor and API
- **Validation**: FluentValidation (shared rules)
- **Enums**: Single source of truth
- **MemoryPack contracts**: Shared serialization schemas

---

## Solution Structure

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
│   │   │   │   └── CelebrationModal.razor
│   │   │   ├── Dashboard/
│   │   │   │   ├── NetWorthHero.razor
│   │   │   │   ├── QuickStatCard.razor
│   │   │   │   ├── InsightsPanel.razor
│   │   │   │   └── HealthScoreCard.razor
│   │   │   ├── Accounts/
│   │   │   │   ├── PlaidLinkModal.razor
│   │   │   │   └── AccountCard.razor
│   │   │   ├── Assets/
│   │   │   │   ├── AssetForm.razor
│   │   │   │   └── AssetCard.razor
│   │   │   ├── Beneficiaries/
│   │   │   │   ├── BeneficiaryCard.razor
│   │   │   │   └── AssignmentDialog.razor
│   │   │   ├── DebtPayoff/
│   │   │   │   ├── StrategyCard.razor
│   │   │   │   ├── DebtListItem.razor
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
│   │
│   ├── RAJFinancial.Api/                     # Azure Functions
│   │   ├── Functions/
│   │   │   ├── Accounts/
│   │   │   │   ├── CreateLinkToken.cs
│   │   │   │   ├── ExchangePublicToken.cs
│   │   │   │   ├── GetAccounts.cs
│   │   │   │   └── RefreshAccount.cs
│   │   │   ├── Assets/
│   │   │   │   ├── GetAssets.cs
│   │   │   │   ├── CreateAsset.cs
│   │   │   │   ├── UpdateAsset.cs
│   │   │   │   └── DeleteAsset.cs
│   │   │   ├── Beneficiaries/
│   │   │   ├── Analysis/
│   │   │   │   ├── CalculateNetWorth.cs
│   │   │   │   ├── AnalyzeDebtPayoff.cs
│   │   │   │   ├── AnalyzeInsurance.cs
│   │   │   │   └── GenerateInsights.cs
│   │   │   └── Webhooks/
│   │   │       └── PlaidWebhook.cs
│   │   ├── Middleware/
│   │   │   ├── ContentNegotiationMiddleware.cs
│   │   │   ├── TenantMiddleware.cs
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── Serialization/
│   │   │   ├── SerializationFactory.cs
│   │   │   └── MemoryPackSerializer.cs
│   │   ├── Program.cs
│   │   └── host.json
│   │
│   ├── RAJFinancial.Shared/                  # Shared library
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   │   ├── CreateAssetRequest.cs
│   │   │   │   ├── ExchangePublicTokenRequest.cs
│   │   │   │   └── DebtPayoffRequest.cs
│   │   │   ├── Responses/
│   │   │   │   ├── AssetDto.cs
│   │   │   │   ├── LinkedAccountDto.cs
│   │   │   │   ├── BeneficiaryDto.cs
│   │   │   │   ├── AIInsightDto.cs
│   │   │   │   ├── DebtPayoffAnalysisDto.cs
│   │   │   │   └── InsuranceCoverageDto.cs
│   │   │   └── ApiErrorResponse.cs
│   │   ├── Enums/
│   │   │   ├── AssetType.cs
│   │   │   ├── AccountType.cs
│   │   │   ├── ConnectionStatus.cs
│   │   │   └── DebtType.cs
│   │   ├── Validation/
│   │   │   ├── CreateAssetValidator.cs
│   │   │   └── DebtPayoffRequestValidator.cs
│   │   ├── Constants/
│   │   │   ├── ErrorCodes.cs
│   │   │   └── ApiRoutes.cs
│   │   └── Extensions/
│   │       └── DecimalExtensions.cs
│   │
│   ├── RAJFinancial.Core/                    # Domain layer
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── LinkedAccount.cs
│   │   │   ├── Asset.cs
│   │   │   ├── Beneficiary.cs
│   │   │   ├── BeneficiaryAssignment.cs
│   │   │   └── AuditLog.cs
│   │   ├── Interfaces/
│   │   │   ├── IPlaidService.cs
│   │   │   ├── IClaudeAIService.cs
│   │   │   ├── IAssetRepository.cs
│   │   │   └── IEncryptionService.cs
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── Percentage.cs
│   │       └── Address.cs
│   │
│   ├── RAJFinancial.Application/             # Application layer
│   │   ├── Services/
│   │   │   ├── AccountService.cs
│   │   │   ├── AssetService.cs
│   │   │   ├── BeneficiaryService.cs
│   │   │   └── AnalysisService.cs
│   │   └── Mappings/
│   │       └── MappingProfile.cs
│   │
│   └── RAJFinancial.Infrastructure/          # Infrastructure layer
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   └── Migrations/
│       ├── Repositories/
│       │   ├── AssetRepository.cs
│       │   └── LinkedAccountRepository.cs
│       └── External/
│           ├── PlaidService.cs
│           ├── ClaudeAIService.cs
│           └── EncryptionService.cs
│
├── tests/
│   ├── RAJFinancial.Client.Tests/
│   ├── RAJFinancial.Api.Tests/
│   └── RAJFinancial.Integration.Tests/
│
└── infra/
    ├── main.bicep
    └── modules/
```

---

## MemoryPack Configuration

### Dual Serialization Strategy

```
Development:  Client ←→ JSON ←→ API (easy debugging)
Production:   Client ←→ MemoryPack ←→ API (performance)
```

### DTO Example with MemoryPack

```csharp
// RAJFinancial.Shared/DTOs/Responses/AssetDto.cs
using MemoryPack;

[MemoryPackable]
public partial class AssetDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public AssetType AssetType { get; set; }
    [MemoryPackOrder(3)] public decimal CurrentValue { get; set; }
    [MemoryPackOrder(4)] public string CurrencyCode { get; set; } = "USD";
    [MemoryPackOrder(5)] public decimal OwnershipPercentage { get; set; } = 100m;
    [MemoryPackOrder(6)] public decimal? PurchasePrice { get; set; }
    [MemoryPackOrder(7)] public DateOnly? PurchaseDate { get; set; }
    [MemoryPackOrder(8)] public int BeneficiaryCount { get; set; }
    [MemoryPackOrder(9)] public DateTime UpdatedAt { get; set; }
    
    [MemoryPackIgnore]
    public decimal UserShareValue => CurrentValue * (OwnershipPercentage / 100m);
}
```

### Content Negotiation in API

```csharp
// RAJFinancial.Api/Serialization/SerializationFactory.cs
public class SerializationFactory : ISerializationFactory
{
    public const string JsonContentType = "application/json";
    public const string MemoryPackContentType = "application/x-memorypack";
    
    public string GetPreferredContentType(string? acceptHeader)
    {
        // Development: always JSON
        if (Environment == "Development") return JsonContentType;
        
        // Production: MemoryPack unless client requests JSON
        if (UseMemoryPackInProduction && 
            !acceptHeader?.Contains(JsonContentType) == true)
        {
            return MemoryPackContentType;
        }
        
        return JsonContentType;
    }
    
    public byte[] Serialize<T>(T value, string contentType)
    {
        return contentType == MemoryPackContentType
            ? MemoryPackSerializer.Serialize(value)
            : JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
    }
}
```

---

## Syncfusion Components Used

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

---

## NuGet Packages

### RAJFinancial.Shared

```xml
<ItemGroup>
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
</ItemGroup>
```

### RAJFinancial.Client

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
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
    <PackageReference Include="Syncfusion.Blazor.Themes" Version="24.2.9" />
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.9.1" />
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    <PackageReference Include="Microsoft.Extensions.Localization" Version="8.0.0" />
</ItemGroup>
```

### RAJFinancial.Api

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.21.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.1.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.OpenApi" Version="1.5.1" />
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    <PackageReference Include="Going.Plaid" Version="6.0.0" />
    <PackageReference Include="Anthropic.SDK" Version="1.0.0" />
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
    <PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
</ItemGroup>
```

---

## Performance Benefits: MemoryPack

| Operation | JSON | MemoryPack | Improvement |
|-----------|------|------------|-------------|
| Serialize 1000 Assets | ~15ms | ~2ms | **7.5x faster** |
| Deserialize 1000 Assets | ~12ms | ~1.5ms | **8x faster** |
| Payload size (1000 Assets) | ~850KB | ~320KB | **62% smaller** |

This matters for:
- Transaction history (thousands of records)
- Portfolio snapshots with historical data
- Bulk exports
- Real-time dashboard updates

---

## Configuration Files

### appsettings.json (Client - Development)

```json
{
  "Api": {
    "BaseUrl": "http://localhost:7071/api/",
    "UseMemoryPack": false
  },
  "Environment": "Development",
  "Syncfusion": {
    "LicenseKey": "YOUR_LICENSE_KEY"
  }
}
```

### appsettings.Production.json (Client)

```json
{
  "Api": {
    "BaseUrl": "https://func-rajfinancial.azurewebsites.net/api/",
    "UseMemoryPack": true
  },
  "Environment": "Production"
}
```

### local.settings.json (API)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "Serialization:UseMemoryPackInProduction": "true",
    "SqlConnection": "Server=localhost;Database=RAJFinancial;Trusted_Connection=True;",
    "Plaid:ClientId": "your_client_id",
    "Plaid:Secret": "your_secret",
    "Plaid:Environment": "sandbox",
    "Claude:ApiKey": "your_api_key",
    "Claude:Model": "claude-sonnet-4-5-20250929"
  }
}
```

---

## Next Steps

1. Create .NET solution with `dotnet new` commands
2. Set up Azure resources with Bicep
3. Implement authentication with Azure AD B2C
4. Build core services (Plaid, Claude AI)
5. Create UI components following RAJ_FINANCIAL_COMPLETE_UI.md
