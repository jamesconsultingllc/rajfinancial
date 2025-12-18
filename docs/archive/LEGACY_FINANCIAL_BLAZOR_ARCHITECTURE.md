# Legacy Builders Financial Platform - Technical Architecture
## Blazor WebAssembly + Syncfusion + MemoryPack

---

## Technology Stack Decision

### Frontend: Blazor WebAssembly
- **Framework**: .NET 8 Blazor WebAssembly (Standalone)
- **UI Components**: Syncfusion Blazor (v24+)
- **Hosting**: Azure Static Web Apps
- **State Management**: Fluxor (Redux pattern for Blazor)
- **HTTP Client**: Custom client with content negotiation

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
LegacyFinancial/
├── src/
│   ├── LegacyFinancial.Client/              # Blazor WebAssembly
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   ├── NavMenu.razor
│   │   │   │   └── MobileBottomNav.razor
│   │   │   ├── Dashboard/
│   │   │   ├── Accounts/
│   │   │   ├── Assets/
│   │   │   ├── Beneficiaries/
│   │   │   └── Tools/
│   │   ├── Pages/
│   │   │   ├── Index.razor
│   │   │   ├── Dashboard.razor
│   │   │   ├── Accounts.razor
│   │   │   ├── Assets.razor
│   │   │   ├── Beneficiaries.razor
│   │   │   └── Tools/
│   │   ├── Services/
│   │   │   ├── ApiClient.cs                  # Content negotiation handler
│   │   │   ├── IAccountService.cs
│   │   │   ├── IAssetService.cs
│   │   │   └── ...
│   │   ├── State/                            # Fluxor state management
│   │   │   ├── AppState.cs
│   │   │   ├── AccountState/
│   │   │   └── AssetState/
│   │   ├── wwwroot/
│   │   │   ├── index.html
│   │   │   └── css/
│   │   └── Program.cs
│   │
│   ├── LegacyFinancial.Api/                  # Azure Functions
│   │   ├── Functions/
│   │   │   ├── Accounts/
│   │   │   ├── Assets/
│   │   │   ├── Beneficiaries/
│   │   │   └── Analysis/
│   │   ├── Middleware/
│   │   │   ├── ContentNegotiationMiddleware.cs
│   │   │   ├── TenantMiddleware.cs
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── Serialization/
│   │   │   ├── SerializationFactory.cs
│   │   │   ├── MemoryPackSerializer.cs
│   │   │   └── JsonSerializer.cs
│   │   ├── Program.cs
│   │   └── host.json
│   │
│   ├── LegacyFinancial.Shared/               # Shared library (Client + API)
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   │   ├── CreateAssetRequest.cs
│   │   │   │   └── ...
│   │   │   ├── Responses/
│   │   │   │   ├── AssetDto.cs
│   │   │   │   ├── LinkedAccountDto.cs
│   │   │   │   └── ...
│   │   │   └── ApiErrorResponse.cs
│   │   ├── Enums/
│   │   │   ├── AssetType.cs
│   │   │   ├── AccountType.cs
│   │   │   └── ...
│   │   ├── Validation/
│   │   │   ├── CreateAssetValidator.cs
│   │   │   └── ...
│   │   ├── Constants/
│   │   │   ├── ErrorCodes.cs
│   │   │   └── ApiRoutes.cs
│   │   └── Extensions/
│   │       └── MoneyExtensions.cs
│   │
│   ├── LegacyFinancial.Core/                 # Domain layer
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── ValueObjects/
│   │
│   ├── LegacyFinancial.Application/          # Application layer
│   │   ├── Services/
│   │   └── Mappings/
│   │
│   └── LegacyFinancial.Infrastructure/       # Infrastructure layer
│       ├── Data/
│       ├── Repositories/
│       └── External/
│
├── tests/
│   ├── LegacyFinancial.Client.Tests/
│   ├── LegacyFinancial.Api.Tests/
│   └── LegacyFinancial.Integration.Tests/
│
└── infra/
    ├── main.bicep
    └── modules/
```

---

## MemoryPack Implementation

### 1. Shared DTO Definitions

```csharp
// LegacyFinancial.Shared/DTOs/Responses/AssetDto.cs
using MemoryPack;
using System.Text.Json.Serialization;

namespace LegacyFinancial.Shared.DTOs.Responses;

/// <summary>
/// Data transfer object for asset information.
/// Supports both JSON (development) and MemoryPack (production) serialization.
/// </summary>
[MemoryPackable]
public partial class AssetDto
{
    /// <summary>
    /// Unique identifier for the asset.
    /// </summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    /// <summary>
    /// User-defined name for the asset.
    /// </summary>
    [MemoryPackOrder(1)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Classification of the asset.
    /// </summary>
    [MemoryPackOrder(2)]
    public AssetType AssetType { get; set; }
    
    /// <summary>
    /// Current estimated market value.
    /// </summary>
    [MemoryPackOrder(3)]
    public decimal CurrentValue { get; set; }
    
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD").
    /// </summary>
    [MemoryPackOrder(4)]
    public string CurrencyCode { get; set; } = "USD";
    
    /// <summary>
    /// User's ownership percentage (0-100).
    /// </summary>
    [MemoryPackOrder(5)]
    public decimal OwnershipPercentage { get; set; } = 100m;
    
    /// <summary>
    /// Calculated value based on ownership percentage.
    /// </summary>
    [MemoryPackIgnore]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public decimal UserShareValue => CurrentValue * (OwnershipPercentage / 100m);
    
    /// <summary>
    /// Original purchase price, if known.
    /// </summary>
    [MemoryPackOrder(6)]
    public decimal? PurchasePrice { get; set; }
    
    /// <summary>
    /// Date of purchase, if known.
    /// </summary>
    [MemoryPackOrder(7)]
    public DateOnly? PurchaseDate { get; set; }
    
    /// <summary>
    /// Number of beneficiaries assigned to this asset.
    /// </summary>
    [MemoryPackOrder(8)]
    public int BeneficiaryCount { get; set; }
    
    /// <summary>
    /// When the asset record was last modified.
    /// </summary>
    [MemoryPackOrder(9)]
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Type-specific metadata (JSON stored, deserialized on demand).
    /// </summary>
    [MemoryPackOrder(10)]
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Request to create a new asset.
/// </summary>
[MemoryPackable]
public partial class CreateAssetRequest
{
    [MemoryPackOrder(0)]
    public string Name { get; set; } = string.Empty;
    
    [MemoryPackOrder(1)]
    public AssetType AssetType { get; set; }
    
    [MemoryPackOrder(2)]
    public decimal CurrentValue { get; set; }
    
    [MemoryPackOrder(3)]
    public string CurrencyCode { get; set; } = "USD";
    
    [MemoryPackOrder(4)]
    public decimal OwnershipPercentage { get; set; } = 100m;
    
    [MemoryPackOrder(5)]
    public decimal? PurchasePrice { get; set; }
    
    [MemoryPackOrder(6)]
    public DateOnly? PurchaseDate { get; set; }
    
    [MemoryPackOrder(7)]
    public string? Description { get; set; }
    
    [MemoryPackOrder(8)]
    public AddressDto? Address { get; set; }
    
    [MemoryPackOrder(9)]
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Address value object for real estate assets.
/// </summary>
[MemoryPackable]
public partial class AddressDto
{
    [MemoryPackOrder(0)]
    public string Street1 { get; set; } = string.Empty;
    
    [MemoryPackOrder(1)]
    public string? Street2 { get; set; }
    
    [MemoryPackOrder(2)]
    public string City { get; set; } = string.Empty;
    
    [MemoryPackOrder(3)]
    public string State { get; set; } = string.Empty;
    
    [MemoryPackOrder(4)]
    public string PostalCode { get; set; } = string.Empty;
    
    [MemoryPackOrder(5)]
    public string Country { get; set; } = "US";
}
```

### 2. API Serialization Factory

```csharp
// LegacyFinancial.Api/Serialization/SerializationFactory.cs
using MemoryPack;
using System.Text.Json;

namespace LegacyFinancial.Api.Serialization;

/// <summary>
/// Factory for creating serializers based on environment and content negotiation.
/// </summary>
public interface ISerializationFactory
{
    /// <summary>
    /// Serializes an object based on the requested content type.
    /// </summary>
    byte[] Serialize<T>(T value, string contentType);
    
    /// <summary>
    /// Deserializes bytes based on the content type.
    /// </summary>
    T? Deserialize<T>(byte[] data, string contentType);
    
    /// <summary>
    /// Gets the appropriate content type for the current environment.
    /// </summary>
    string GetPreferredContentType(string? acceptHeader);
}

public class SerializationFactory : ISerializationFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SerializationFactory> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public const string JsonContentType = "application/json";
    public const string MemoryPackContentType = "application/x-memorypack";
    
    public SerializationFactory(
        IConfiguration configuration,
        ILogger<SerializationFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    /// <summary>
    /// Determines if MemoryPack should be used based on environment.
    /// </summary>
    private bool UseMemoryPackInProduction => 
        _configuration.GetValue<bool>("Serialization:UseMemoryPackInProduction", true);
    
    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    private string Environment => 
        _configuration.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development";
    
    /// <inheritdoc />
    public string GetPreferredContentType(string? acceptHeader)
    {
        // In development, always prefer JSON for debugging
        if (Environment == "Development")
        {
            _logger.LogDebug("Development environment: using JSON serialization");
            return JsonContentType;
        }
        
        // In production, check Accept header and configuration
        if (UseMemoryPackInProduction)
        {
            // If client explicitly requests JSON, honor it
            if (acceptHeader?.Contains(JsonContentType) == true && 
                !acceptHeader.Contains(MemoryPackContentType))
            {
                return JsonContentType;
            }
            
            // Default to MemoryPack in production
            return MemoryPackContentType;
        }
        
        return JsonContentType;
    }
    
    /// <inheritdoc />
    public byte[] Serialize<T>(T value, string contentType)
    {
        if (contentType == MemoryPackContentType)
        {
            _logger.LogDebug("Serializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Serialize(value);
        }
        
        _logger.LogDebug("Serializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
    }
    
    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data, string contentType)
    {
        if (contentType == MemoryPackContentType)
        {
            _logger.LogDebug("Deserializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Deserialize<T>(data);
        }
        
        _logger.LogDebug("Deserializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }
}
```

### 3. Content Negotiation Middleware

```csharp
// LegacyFinancial.Api/Middleware/ContentNegotiationMiddleware.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace LegacyFinancial.Api.Middleware;

/// <summary>
/// Middleware that handles content negotiation between JSON and MemoryPack.
/// Sets the appropriate serializer based on Accept header and environment.
/// </summary>
public class ContentNegotiationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ISerializationFactory _serializationFactory;
    private readonly ILogger<ContentNegotiationMiddleware> _logger;
    
    public ContentNegotiationMiddleware(
        ISerializationFactory serializationFactory,
        ILogger<ContentNegotiationMiddleware> logger)
    {
        _serializationFactory = serializationFactory;
        _logger = logger;
    }
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Get Accept header from request
        var httpRequest = await context.GetHttpRequestDataAsync();
        var acceptHeader = httpRequest?.Headers
            .TryGetValues("Accept", out var values) == true 
                ? values.FirstOrDefault() 
                : null;
        
        // Determine content type for this request
        var contentType = _serializationFactory.GetPreferredContentType(acceptHeader);
        
        // Store in context for use by functions
        context.Items["ResponseContentType"] = contentType;
        context.Items["SerializationFactory"] = _serializationFactory;
        
        _logger.LogDebug(
            "Content negotiation: Accept={Accept}, Selected={ContentType}",
            acceptHeader,
            contentType);
        
        await next(context);
    }
}
```

### 4. Azure Function with Content Negotiation

```csharp
// LegacyFinancial.Api/Functions/Assets/GetAssets.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.OpenApi.Models;
using System.Net;

namespace LegacyFinancial.Api.Functions.Assets;

/// <summary>
/// Azure Function to retrieve user's assets.
/// </summary>
public class GetAssets
{
    private readonly IAssetService _assetService;
    private readonly ILogger<GetAssets> _logger;
    
    public GetAssets(IAssetService assetService, ILogger<GetAssets> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets all assets for the authenticated user.
    /// </summary>
    /// <param name="req">HTTP request with optional type filter.</param>
    /// <param name="context">Function execution context.</param>
    /// <returns>List of assets in negotiated format (JSON or MemoryPack).</returns>
    [Function("GetAssets")]
    [OpenApiOperation(operationId: "GetAssets", tags: new[] { "Assets" })]
    [OpenApiSecurity("bearer", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer)]
    [OpenApiParameter(name: "type", In = ParameterLocation.Query, Required = false, 
        Type = typeof(AssetType), Description = "Filter by asset type")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, 
        contentType: "application/json", 
        bodyType: typeof(IEnumerable<AssetDto>),
        Description = "List of assets")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets")] 
        HttpRequestData req,
        FunctionContext context)
    {
        // Get user ID from authenticated context
        var userId = context.GetUserId();
        if (userId == null)
        {
            return await CreateErrorResponse(req, context, 
                HttpStatusCode.Unauthorized, ErrorCodes.AUTH_REQUIRED);
        }
        
        // Parse optional filter
        AssetType? filterType = null;
        if (req.Query["type"] is string typeParam && 
            Enum.TryParse<AssetType>(typeParam, true, out var parsed))
        {
            filterType = parsed;
        }
        
        _logger.LogInformation(
            "GetAssets called for user {UserId} with filter {Filter}",
            userId,
            filterType?.ToString() ?? "none");
        
        // Get assets from service
        var assets = await _assetService.GetAssetsAsync(userId.Value, filterType);
        
        // Create response with content negotiation
        return await CreateSuccessResponse(req, context, assets);
    }
    
    /// <summary>
    /// Creates a success response with appropriate serialization.
    /// </summary>
    private async Task<HttpResponseData> CreateSuccessResponse<T>(
        HttpRequestData req, 
        FunctionContext context, 
        T data)
    {
        var factory = context.Items["SerializationFactory"] as ISerializationFactory;
        var contentType = context.Items["ResponseContentType"] as string ?? "application/json";
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", contentType);
        
        var bytes = factory!.Serialize(data, contentType);
        await response.Body.WriteAsync(bytes);
        
        return response;
    }
    
    /// <summary>
    /// Creates an error response (always JSON for debugging).
    /// </summary>
    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        FunctionContext context,
        HttpStatusCode statusCode,
        string errorCode,
        Dictionary<string, object>? details = null)
    {
        var error = new ApiErrorResponse
        {
            Code = errorCode,
            Message = GetDefaultMessage(errorCode),
            Details = details,
            TraceId = context.TraceContext.TraceParent
        };
        
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteAsJsonAsync(error);
        
        return response;
    }
    
    private static string GetDefaultMessage(string code) => code switch
    {
        ErrorCodes.AUTH_REQUIRED => "Authentication required",
        ErrorCodes.AUTH_FORBIDDEN => "Access denied",
        ErrorCodes.ASSET_NOT_FOUND => "Asset not found",
        ErrorCodes.VALIDATION_FAILED => "Validation failed",
        _ => "An error occurred"
    };
}
```

### 5. Blazor Client API Service

```csharp
// LegacyFinancial.Client/Services/ApiClient.cs
using MemoryPack;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LegacyFinancial.Client.Services;

/// <summary>
/// HTTP client that handles content negotiation between JSON and MemoryPack.
/// Automatically uses MemoryPack in production for better performance.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string JsonContentType = "application/json";
    private const string MemoryPackContentType = "application/x-memorypack";
    
    public ApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <summary>
    /// Determines the content type to use based on environment.
    /// </summary>
    private string PreferredContentType
    {
        get
        {
            var env = _configuration.GetValue<string>("Environment") ?? "Development";
            var useMemoryPack = _configuration.GetValue<bool>("Api:UseMemoryPack", true);
            
            return (env != "Development" && useMemoryPack) 
                ? MemoryPackContentType 
                : JsonContentType;
        }
    }
    
    /// <summary>
    /// Sends a GET request and deserializes the response.
    /// </summary>
    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PreferredContentType));
            
            // Also accept JSON as fallback
            if (PreferredContentType != JsonContentType)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonContentType, 0.9));
            }
            
            var response = await _httpClient.SendAsync(request, ct);
            return await HandleResponse<T>(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API GET request failed: {Endpoint}", endpoint);
            return ApiResponse<T>.Failure(ErrorCodes.SERVER_ERROR, ex.Message);
        }
    }
    
    /// <summary>
    /// Sends a POST request with a body and deserializes the response.
    /// </summary>
    public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest body, 
        CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PreferredContentType));
            
            // Serialize request body
            var contentType = PreferredContentType;
            byte[] bodyBytes;
            
            if (contentType == MemoryPackContentType)
            {
                bodyBytes = MemoryPackSerializer.Serialize(body);
            }
            else
            {
                bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body, _jsonOptions);
            }
            
            request.Content = new ByteArrayContent(bodyBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            
            var response = await _httpClient.SendAsync(request, ct);
            return await HandleResponse<TResponse>(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API POST request failed: {Endpoint}", endpoint);
            return ApiResponse<TResponse>.Failure(ErrorCodes.SERVER_ERROR, ex.Message);
        }
    }
    
    /// <summary>
    /// Handles the HTTP response and deserializes based on content type.
    /// </summary>
    private async Task<ApiResponse<T>> HandleResponse<T>(
        HttpResponseMessage response, 
        CancellationToken ct)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? JsonContentType;
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            // Errors are always JSON
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(bytes, _jsonOptions);
            return ApiResponse<T>.Failure(
                error?.Code ?? ErrorCodes.SERVER_ERROR,
                error?.Message ?? "Unknown error",
                error?.Details);
        }
        
        // Deserialize based on content type
        T? data;
        if (contentType == MemoryPackContentType)
        {
            _logger.LogDebug("Deserializing {Type} from MemoryPack", typeof(T).Name);
            data = MemoryPackSerializer.Deserialize<T>(bytes);
        }
        else
        {
            _logger.LogDebug("Deserializing {Type} from JSON", typeof(T).Name);
            data = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        
        return ApiResponse<T>.Success(data!);
    }
}

/// <summary>
/// Wrapper for API responses that handles success and error cases.
/// </summary>
public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? ErrorDetails { get; init; }
    
    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };
    
    public static ApiResponse<T> Failure(
        string code, 
        string message, 
        Dictionary<string, object>? details = null) => new()
    {
        IsSuccess = false,
        ErrorCode = code,
        ErrorMessage = message,
        ErrorDetails = details
    };
}
```

---

## Syncfusion Blazor Components

### Package Installation

```xml
<!-- LegacyFinancial.Client.csproj -->
<ItemGroup>
    <!-- Syncfusion Blazor -->
    <PackageReference Include="Syncfusion.Blazor.Grid" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Charts" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Inputs" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Buttons" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Navigations" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Popups" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Calendars" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.DropDowns" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Notifications" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Cards" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.ProgressBar" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Spinner" Version="24.*" />
    <PackageReference Include="Syncfusion.Blazor.Themes" Version="24.*" />
    
    <!-- MemoryPack -->
    <PackageReference Include="MemoryPack" Version="1.*" />
    
    <!-- State Management -->
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.*" />
    
    <!-- Localization -->
    <PackageReference Include="Microsoft.Extensions.Localization" Version="8.*" />
</ItemGroup>
```

### Program.cs Configuration

```csharp
// LegacyFinancial.Client/Program.cs
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

### Dashboard Component with Syncfusion

```razor
@* LegacyFinancial.Client/Pages/Dashboard.razor *@
@page "/dashboard"
@attribute [Authorize]
@inject IStringLocalizer<Dashboard> L
@inject IState<DashboardState> DashboardState
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - Legacy Financial</PageTitle>

@* Skip to main content for accessibility *@
<a href="#main-content" class="sr-only focus:not-sr-only">
    @L["SkipToContent"]
</a>

<div class="min-h-screen bg-gray-50">
    @* Mobile Header *@
    <header class="lg:hidden sticky top-0 z-40 bg-white border-b px-4 py-3">
        <div class="flex items-center justify-between">
            <button @onclick="ToggleMobileMenu" 
                    class="p-2 -ml-2"
                    aria-label="@L["Menu"]">
                <SfIcon Name="IconName.Menu" Size="IconSize.Medium" />
            </button>
            <img src="/images/logo.svg" alt="Legacy Financial" class="h-8" />
            <button class="p-2 -mr-2 relative" aria-label="@L["Notifications"]">
                <SfIcon Name="IconName.Notification" Size="IconSize.Medium" />
                @if (DashboardState.Value.UnreadNotifications > 0)
                {
                    <span class="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
                }
            </button>
        </div>
    </header>

    <div class="lg:flex">
        @* Desktop Sidebar *@
        <aside class="hidden lg:flex lg:flex-col lg:w-64 lg:fixed lg:inset-y-0 
                      bg-slate-900 text-white">
            <NavMenu />
        </aside>

        @* Main Content *@
        <main id="main-content" 
              class="flex-1 lg:pl-64 pb-20 lg:pb-0"
              role="main"
              aria-label="@L["MainContent"]">
            
            <div class="p-4 lg:p-8 max-w-7xl mx-auto">
                @* Net Worth Card *@
                <section aria-labelledby="net-worth-heading" class="mb-6">
                    <SfCard CssClass="overflow-hidden">
                        <div class="p-6 bg-gradient-to-r from-blue-900 to-blue-700 text-white">
                            <h2 id="net-worth-heading" class="text-sm font-medium opacity-80">
                                @L["NetWorth.Title"]
                            </h2>
                            @if (DashboardState.Value.IsLoading)
                            {
                                <SfSkeleton Width="200px" Height="48px" CssClass="mt-2" />
                            }
                            else
                            {
                                <div class="flex items-baseline gap-3 mt-2">
                                    <span class="text-4xl font-bold">
                                        @DashboardState.Value.NetWorth.ToString("C0")
                                    </span>
                                    <TrendIndicator Value="@DashboardState.Value.NetWorthChange" />
                                </div>
                                <p class="text-sm opacity-70 mt-2">
                                    @L["NetWorth.AsOf", DashboardState.Value.LastUpdated.ToString("MMM d, yyyy")]
                                </p>
                            }
                        </div>
                        <div class="p-4 bg-white">
                            <SfSparkline DataSource="@DashboardState.Value.NetWorthHistory"
                                        XName="Date"
                                        YName="Value"
                                        Height="60px"
                                        Width="100%"
                                        Type="SparklineType.Area"
                                        Fill="rgba(30, 58, 95, 0.1)"
                                        LineWidth="2">
                                <SparklineBorder Color="#1E3A5F" Width="2" />
                            </SfSparkline>
                        </div>
                    </SfCard>
                </section>

                @* Quick Stats - Horizontal scroll on mobile *@
                <section aria-label="@L["QuickStats.Title"]" class="mb-6">
                    <div class="flex gap-4 overflow-x-auto pb-2 -mx-4 px-4 lg:mx-0 lg:px-0 
                                lg:grid lg:grid-cols-4 snap-x snap-mandatory">
                        <StatCard 
                            Title="@L["QuickStats.TotalAssets"]"
                            Value="@DashboardState.Value.TotalAssets"
                            Icon="IconName.ChartPie"
                            CssClass="min-w-[200px] lg:min-w-0 snap-start" />
                        <StatCard 
                            Title="@L["QuickStats.TotalLiabilities"]"
                            Value="@DashboardState.Value.TotalLiabilities"
                            Icon="IconName.CreditCard"
                            ValueColor="text-red-600"
                            CssClass="min-w-[200px] lg:min-w-0 snap-start" />
                        <StatCard 
                            Title="@L["QuickStats.CashFlow"]"
                            Value="@DashboardState.Value.MonthlyCashFlow"
                            Icon="IconName.TrendingUp"
                            CssClass="min-w-[200px] lg:min-w-0 snap-start" />
                        <StatCard 
                            Title="@L["QuickStats.InsuranceCoverage"]"
                            Value="@DashboardState.Value.InsuranceCoverageRatio"
                            Icon="IconName.Shield"
                            IsPercentage="true"
                            WarningThreshold="80"
                            CssClass="min-w-[200px] lg:min-w-0 snap-start" />
                    </div>
                </section>

                @* Two-column layout on desktop *@
                <div class="grid lg:grid-cols-2 gap-6">
                    @* AI Insights *@
                    <section aria-labelledby="insights-heading">
                        <SfCard>
                            <div class="p-4 border-b flex items-center gap-2">
                                <SfIcon Name="IconName.Lightbulb" CssClass="text-teal-600" />
                                <h2 id="insights-heading" class="font-semibold">
                                    @L["Insights.Title"]
                                </h2>
                            </div>
                            <div class="divide-y">
                                @foreach (var insight in DashboardState.Value.Insights)
                                {
                                    <InsightCard Insight="@insight" />
                                }
                            </div>
                        </SfCard>
                    </section>

                    @* Recent Activity *@
                    <section aria-labelledby="activity-heading">
                        <SfCard>
                            <div class="p-4 border-b flex items-center justify-between">
                                <h2 id="activity-heading" class="font-semibold">
                                    @L["RecentActivity.Title"]
                                </h2>
                                <a href="/transactions" 
                                   class="text-sm text-teal-600 hover:text-teal-700">
                                    @L["RecentActivity.ViewAll"]
                                </a>
                            </div>
                            <div class="divide-y">
                                @foreach (var activity in DashboardState.Value.RecentActivity.Take(5))
                                {
                                    <ActivityItem Activity="@activity" />
                                }
                            </div>
                        </SfCard>
                    </section>
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
        <NavMenu OnNavigate="CloseMobileMenu" />
    </ChildContent>
</SfSidebar>

@code {
    private SfSidebar? MobileSidebar;
    
    protected override async Task OnInitializedAsync()
    {
        // Dispatch action to load dashboard data
        Dispatcher.Dispatch(new LoadDashboardDataAction());
    }
    
    private void ToggleMobileMenu()
    {
        MobileSidebar?.Toggle();
    }
    
    private void CloseMobileMenu()
    {
        MobileSidebar?.Hide();
    }
}
```

### Assets Grid with Responsive Layout

```razor
@* LegacyFinancial.Client/Pages/Assets.razor *@
@page "/assets"
@attribute [Authorize]
@inject IStringLocalizer<Assets> L
@inject IState<AssetState> AssetState
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - Legacy Financial</PageTitle>

<div class="p-4 lg:p-8 max-w-7xl mx-auto">
    @* Header *@
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <h1 class="text-2xl font-bold">@L["Title"]</h1>
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
            <TabItem>
                <HeaderTemplate>@L["Filter.All"]</HeaderTemplate>
            </TabItem>
            <TabItem>
                <HeaderTemplate>@L["Filter.RealEstate"]</HeaderTemplate>
            </TabItem>
            <TabItem>
                <HeaderTemplate>@L["Filter.Vehicles"]</HeaderTemplate>
            </TabItem>
            <TabItem>
                <HeaderTemplate>@L["Filter.Business"]</HeaderTemplate>
            </TabItem>
            <TabItem>
                <HeaderTemplate>@L["Filter.Other"]</HeaderTemplate>
            </TabItem>
        </TabItems>
    </SfTab>

    @* Desktop: Data Grid *@
    <div class="hidden lg:block">
        <SfGrid DataSource="@FilteredAssets"
                AllowPaging="true"
                AllowSorting="true"
                AllowFiltering="false"
                EnableAltRow="true">
            <GridPageSettings PageSize="10" />
            <GridColumns>
                <GridColumn Field="@nameof(AssetDto.Name)" 
                           HeaderText="@L["Grid.Name"]" 
                           Width="200">
                    <Template>
                        @{
                            var asset = context as AssetDto;
                            <div class="flex items-center gap-3">
                                <div class="w-10 h-10 rounded bg-gray-100 flex items-center justify-center">
                                    <AssetTypeIcon Type="@asset!.AssetType" />
                                </div>
                                <div>
                                    <p class="font-medium">@asset.Name</p>
                                    <p class="text-sm text-gray-500">@L[$"AssetType.{asset.AssetType}"]</p>
                                </div>
                            </div>
                        }
                    </Template>
                </GridColumn>
                <GridColumn Field="@nameof(AssetDto.CurrentValue)" 
                           HeaderText="@L["Grid.Value"]" 
                           Format="C0"
                           TextAlign="TextAlign.Right"
                           Width="150" />
                <GridColumn Field="@nameof(AssetDto.OwnershipPercentage)" 
                           HeaderText="@L["Grid.Ownership"]" 
                           Format="P0"
                           TextAlign="TextAlign.Right"
                           Width="120" />
                <GridColumn Field="@nameof(AssetDto.UserShareValue)" 
                           HeaderText="@L["Grid.YourShare"]" 
                           Format="C0"
                           TextAlign="TextAlign.Right"
                           Width="150" />
                <GridColumn Field="@nameof(AssetDto.BeneficiaryCount)" 
                           HeaderText="@L["Grid.Beneficiaries"]" 
                           TextAlign="TextAlign.Center"
                           Width="120">
                    <Template>
                        @{
                            var asset = context as AssetDto;
                            <div class="flex items-center justify-center gap-1">
                                @if (asset!.BeneficiaryCount > 0)
                                {
                                    <SfIcon Name="IconName.Users" CssClass="text-green-600" Size="IconSize.Small" />
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
                            <SfDropDownButton CssClass="e-flat" 
                                             IconCss="e-icons e-more-vertical"
                                             aria-label="@L["Actions"]">
                                <DropDownMenuItems>
                                    <DropDownMenuItem Text="@L["Action.Edit"]" 
                                                     @onclick="() => EditAsset(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.UpdateValue"]" 
                                                     @onclick="() => UpdateValue(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.ManageBeneficiaries"]" 
                                                     @onclick="() => ManageBeneficiaries(asset!.Id)" />
                                    <DropDownMenuItem Text="@L["Action.Delete"]" 
                                                     CssClass="text-red-600"
                                                     @onclick="() => ConfirmDelete(asset!)" />
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
                <SfCard>
                    <div class="p-4">
                        <SfSkeleton Width="60%" Height="24px" />
                        <SfSkeleton Width="40%" Height="20px" CssClass="mt-2" />
                        <SfSkeleton Width="30%" Height="32px" CssClass="mt-4" />
                    </div>
                </SfCard>
            }
        }
        else if (!FilteredAssets.Any())
        {
            <EmptyState 
                Icon="IconName.Package"
                Title="@L["Empty.Title"]"
                Description="@L["Empty.Description"]"
                ActionText="@L["AddAsset"]"
                OnAction="OpenAddAssetDialog" />
        }
        else
        {
            @foreach (var asset in FilteredAssets)
            {
                <SfCard @onclick="() => ViewAsset(asset.Id)" 
                        CssClass="cursor-pointer hover:shadow-md transition-shadow">
                    <div class="p-4">
                        <div class="flex items-start justify-between">
                            <div class="flex items-center gap-3">
                                <div class="w-12 h-12 rounded-lg bg-gray-100 
                                            flex items-center justify-center">
                                    <AssetTypeIcon Type="@asset.AssetType" Size="IconSize.Large" />
                                </div>
                                <div>
                                    <h3 class="font-semibold">@asset.Name</h3>
                                    <p class="text-sm text-gray-500">
                                        @L[$"AssetType.{asset.AssetType}"]
                                    </p>
                                </div>
                            </div>
                            @if (asset.BeneficiaryCount == 0)
                            {
                                <SfChip CssClass="e-warning e-small" 
                                       Text="@L["NoBeneficiaries"]" />
                            }
                        </div>
                        <div class="mt-4 flex items-end justify-between">
                            <div>
                                <p class="text-2xl font-bold">
                                    @asset.CurrentValue.ToString("C0")
                                </p>
                                @if (asset.OwnershipPercentage < 100)
                                {
                                    <p class="text-sm text-gray-500">
                                        @L["YourShare"]: @asset.UserShareValue.ToString("C0") 
                                        (@asset.OwnershipPercentage.ToString("P0"))
                                    </p>
                                }
                            </div>
                            <SfIcon Name="IconName.ChevronRight" CssClass="text-gray-400" />
                        </div>
                    </div>
                </SfCard>
            }
        }
    </div>
</div>

@* Add/Edit Asset Dialog *@
<SfDialog @bind-Visible="IsAddDialogVisible"
          Header="@(EditingAsset == null ? L["AddAsset"] : L["EditAsset"])"
          Width="600px"
          IsModal="true"
          ShowCloseIcon="true"
          CloseOnEscape="true"
          CssClass="max-h-[90vh] overflow-y-auto">
    <DialogTemplates>
        <Content>
            <AssetForm Asset="@EditingAsset" 
                      OnSubmit="HandleAssetSubmit"
                      OnCancel="CloseAddDialog" />
        </Content>
    </DialogTemplates>
</SfDialog>

@* Delete Confirmation Dialog *@
<SfDialog @bind-Visible="IsDeleteDialogVisible"
          Header="@L["Delete.Title"]"
          Width="400px"
          IsModal="true"
          ShowCloseIcon="true">
    <DialogTemplates>
        <Content>
            <p>@L["Delete.Confirmation", DeletingAsset?.Name ?? ""]</p>
        </Content>
    </DialogTemplates>
    <DialogButtons>
        <DialogButton Content="@L["Cancel"]" 
                     CssClass="e-flat" 
                     OnClick="() => IsDeleteDialogVisible = false" />
        <DialogButton Content="@L["Delete"]" 
                     CssClass="e-danger" 
                     IsPrimary="true"
                     OnClick="ExecuteDelete" />
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
        _ => AssetState.Value.Assets.Where(a => 
            a.AssetType != AssetType.RealEstate && 
            a.AssetType != AssetType.Vehicle && 
            a.AssetType != AssetType.BusinessInterest)
    };
    
    protected override void OnInitialized()
    {
        Dispatcher.Dispatch(new LoadAssetsAction());
    }
    
    private void OpenAddAssetDialog()
    {
        EditingAsset = null;
        IsAddDialogVisible = true;
    }
    
    private void CloseAddDialog()
    {
        IsAddDialogVisible = false;
        EditingAsset = null;
    }
    
    private void EditAsset(Guid id)
    {
        EditingAsset = AssetState.Value.Assets.FirstOrDefault(a => a.Id == id);
        IsAddDialogVisible = true;
    }
    
    private void ViewAsset(Guid id)
    {
        // Navigate to asset detail page on mobile
        NavigationManager.NavigateTo($"/assets/{id}");
    }
    
    private void ConfirmDelete(AssetDto asset)
    {
        DeletingAsset = asset;
        IsDeleteDialogVisible = true;
    }
    
    private async Task ExecuteDelete()
    {
        if (DeletingAsset != null)
        {
            Dispatcher.Dispatch(new DeleteAssetAction(DeletingAsset.Id));
        }
        IsDeleteDialogVisible = false;
        DeletingAsset = null;
    }
    
    private async Task HandleAssetSubmit(CreateAssetRequest request)
    {
        if (EditingAsset == null)
        {
            Dispatcher.Dispatch(new CreateAssetAction(request));
        }
        else
        {
            Dispatcher.Dispatch(new UpdateAssetAction(EditingAsset.Id, request));
        }
        CloseAddDialog();
    }
}
```

---

## Key Configuration Files

### appsettings.json (Blazor Client)

```json
{
  "Api": {
    "BaseUrl": "https://func-legacyfinancial-dev.azurewebsites.net/api/",
    "UseMemoryPack": false
  },
  "Environment": "Development",
  "Syncfusion": {
    "LicenseKey": "YOUR_LICENSE_KEY"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### appsettings.Production.json (Blazor Client)

```json
{
  "Api": {
    "BaseUrl": "https://func-legacyfinancial.azurewebsites.net/api/",
    "UseMemoryPack": true
  },
  "Environment": "Production"
}
```

### local.settings.json (Azure Functions)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "Serialization:UseMemoryPackInProduction": "true",
    "SqlConnection": "Server=localhost;Database=LegacyFinancial;Trusted_Connection=True;",
    "Plaid:ClientId": "your_client_id",
    "Plaid:Secret": "your_secret",
    "Plaid:Environment": "sandbox",
    "Claude:ApiKey": "your_api_key",
    "Claude:Model": "claude-sonnet-4-5-20250929"
  },
  "Host": {
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

---

## NuGet Packages Summary

### LegacyFinancial.Shared

```xml
<ItemGroup>
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
</ItemGroup>
```

### LegacyFinancial.Client

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

### LegacyFinancial.Api

```xml
<ItemGroup>
    <!-- Azure Functions -->
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.21.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.17.2" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.1.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="1.2.1" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.OpenApi" Version="1.5.1" />
    
    <!-- Serialization -->
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    
    <!-- Observability -->
    <PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
    
    <!-- Security -->
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
</ItemGroup>
```

---

## Performance Comparison: JSON vs MemoryPack

Expected improvements with MemoryPack in production:

| Operation | JSON | MemoryPack | Improvement |
|-----------|------|------------|-------------|
| Serialize 1000 Assets | ~15ms | ~2ms | **7.5x faster** |
| Deserialize 1000 Assets | ~12ms | ~1.5ms | **8x faster** |
| Payload size (1000 Assets) | ~850KB | ~320KB | **62% smaller** |
| Memory allocation | Higher | Near-zero | Significant GC reduction |

This matters especially for:
- Transaction history (thousands of records)
- Portfolio snapshots
- Bulk data exports
- Real-time dashboard updates

---

## Summary of Changes

1. **Frontend**: React → Blazor WebAssembly
2. **UI Components**: Custom/Shadcn → Syncfusion Blazor
3. **Serialization**: JSON only → JSON (dev) + MemoryPack (prod)
4. **Code Sharing**: Duplicated models → Shared .NET library
5. **State Management**: React Query → Fluxor (Redux for Blazor)

The architecture now maximizes your C# expertise across the entire stack while delivering better production performance with MemoryPack.
