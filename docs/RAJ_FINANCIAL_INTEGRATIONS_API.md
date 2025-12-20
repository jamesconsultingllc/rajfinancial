# RAJ Financial Software - API & Integrations Guide
## Azure Functions + Plaid + Claude AI + MemoryPack

---

## Part 1: Solution Architecture

### Technology Stack

```
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Static Web Apps                          │
│                 (Blazor WebAssembly Frontend)                    │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Azure API Management                          │
│              (Gateway, Rate Limiting, Versioning)                │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Functions (.NET 9)                       │
│                  (Isolated Worker Process)                       │
├─────────────────────────────────────────────────────────────────┤
│  AccountService │ AssetService │ BeneficiaryService │ AIService │
└─────────────────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Azure SQL   │     │  Azure Key Vault │     │  Azure Redis    │
│   Database    │     │    (Secrets)     │     │     Cache       │
└───────────────┘     └─────────────────┘     └─────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    External Integrations                         │
├─────────────────────────────────────────────────────────────────┤
│     Plaid (Account Aggregation)  │  Claude API (AI Insights)    │
└─────────────────────────────────────────────────────────────────┘
```

---

### Project Structure

```
RAJFinancial/
├── src/
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
│   │   │   │   ├── GetBeneficiaries.cs
│   │   │   │   ├── CreateBeneficiary.cs
│   │   │   │   ├── UpdateBeneficiary.cs
│   │   │   │   ├── DeleteBeneficiary.cs
│   │   │   │   └── AssignBeneficiary.cs
│   │   │   ├── Analysis/
│   │   │   │   ├── CalculateNetWorth.cs
│   │   │   │   ├── AnalyzeDebtPayoff.cs
│   │   │   │   ├── AnalyzeInsurance.cs
│   │   │   │   └── GenerateInsights.cs
│   │   │   ├── Auth/
│   │   │   │   ├── GetCurrentUser.cs
│   │   │   │   ├── GetUserRoles.cs
│   │   │   │   ├── AssignClient.cs
│   │   │   │   ├── GetAssignedClients.cs
│   │   │   │   └── RemoveClientAccess.cs
│   │   │   └── Webhooks/
│   │   │       └── PlaidWebhook.cs
│   │   ├── Middleware/
│   │   │   ├── ContentNegotiationMiddleware.cs
│   │   │   ├── TenantMiddleware.cs
│   │   │   ├── AuthenticationMiddleware.cs
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── Serialization/
│   │   │   ├── SerializationFactory.cs
│   │   │   └── MemoryPackSerializer.cs
│   │   ├── Program.cs
│   │   └── host.json
│   │
│   ├── RAJFinancial.Core/                    # Domain layer
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
│   │   │   ├── IPlaidService.cs
│   │   │   ├── IClaudeAIService.cs
│   │   │   ├── IAssetRepository.cs
│   │   │   ├── IAccountService.cs
│   │   │   ├── IBeneficiaryService.cs
│   │   │   ├── IAnalysisService.cs
│   │   │   ├── IAuditService.cs
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
│   │   ├── Validators/
│   │   │   ├── CreateAssetValidator.cs
│   │   │   ├── CreateBeneficiaryValidator.cs
│   │   │   └── DebtPayoffRequestValidator.cs
│   │   └── Mappings/
│   │       └── MappingProfile.cs
│   │
│   ├── RAJFinancial.Infrastructure/          # Infrastructure layer
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── AssetRepository.cs
│   │   │   └── LinkedAccountRepository.cs
│   │   ├── External/
│   │   │   ├── PlaidService.cs
│   │   │   ├── ClaudeAIService.cs
│   │   │   └── EncryptionService.cs
│   │   └── Logging/
│   │       └── AuditLogger.cs
│   │
│   └── RAJFinancial.Shared/                  # Shared library
│       ├── DTOs/
│       │   ├── Requests/
│       │   │   ├── CreateAssetRequest.cs
│       │   │   ├── ExchangePublicTokenRequest.cs
│       │   │   └── DebtPayoffRequest.cs
│       │   ├── Responses/
│       │   │   ├── AssetDto.cs
│       │   │   ├── LinkedAccountDto.cs
│       │   │   ├── BeneficiaryDto.cs
│       │   │   ├── AIInsightDto.cs
│       │   │   ├── DebtPayoffAnalysisDto.cs
│       │   │   └── InsuranceCoverageDto.cs
│       │   └── ApiErrorResponse.cs
│       ├── Enums/
│       ├── Validation/
│       ├── Constants/
│       │   ├── ErrorCodes.cs
│       │   └── ApiRoutes.cs
│       └── Extensions/
│           └── DecimalExtensions.cs
│
└── tests/
    ├── RAJFinancial.Api.Tests/
    ├── RAJFinancial.Application.Tests/
    └── RAJFinancial.Integration.Tests/
```

---

## Part 2: Shared DTOs with MemoryPack

### Asset DTOs

```csharp
// RAJFinancial.Shared/DTOs/Responses/AssetDto.cs
using MemoryPack;
using System.Text.Json.Serialization;

namespace RAJFinancial.Shared.DTOs.Responses;

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
    
    /// <summary>
    /// Calculated value based on ownership percentage.
    /// </summary>
    [MemoryPackIgnore]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public decimal UserShareValue => CurrentValue * (OwnershipPercentage / 100m);
}
```

### Create Asset Request

```csharp
// RAJFinancial.Shared/DTOs/Requests/CreateAssetRequest.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Requests;

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
```

### Address DTO

```csharp
// RAJFinancial.Shared/DTOs/AddressDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs;

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

### Linked Account DTO

```csharp
// RAJFinancial.Shared/DTOs/Responses/LinkedAccountDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Responses;

/// <summary>
/// Data transfer object for linked financial accounts.
/// </summary>
[MemoryPackable]
public partial class LinkedAccountDto
{
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    [MemoryPackOrder(1)]
    public string InstitutionId { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public string InstitutionName { get; set; } = string.Empty;
    
    [MemoryPackOrder(3)]
    public string? InstitutionLogo { get; set; }
    
    [MemoryPackOrder(4)]
    public string? InstitutionPrimaryColor { get; set; }
    
    [MemoryPackOrder(5)]
    public string AccountName { get; set; } = string.Empty;
    
    [MemoryPackOrder(6)]
    public string? AccountOfficialName { get; set; }
    
    [MemoryPackOrder(7)]
    public string AccountMask { get; set; } = string.Empty;
    
    [MemoryPackOrder(8)]
    public AccountType AccountType { get; set; }
    
    [MemoryPackOrder(9)]
    public string? AccountSubtype { get; set; }
    
    [MemoryPackOrder(10)]
    public decimal? CurrentBalance { get; set; }
    
    [MemoryPackOrder(11)]
    public decimal? AvailableBalance { get; set; }
    
    [MemoryPackOrder(12)]
    public string CurrencyCode { get; set; } = "USD";
    
    [MemoryPackOrder(13)]
    public ConnectionStatus ConnectionStatus { get; set; }
    
    [MemoryPackOrder(14)]
    public DateTime? LastSyncedAt { get; set; }
}
```

### AI Insight DTO

```csharp
// RAJFinancial.Shared/DTOs/Responses/AIInsightDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Responses;

/// <summary>
/// AI-generated financial insight.
/// </summary>
[MemoryPackable]
public partial class AIInsightDto
{
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    [MemoryPackOrder(1)]
    public string Category { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public string Title { get; set; } = string.Empty;
    
    [MemoryPackOrder(3)]
    public string Description { get; set; } = string.Empty;
    
    [MemoryPackOrder(4)]
    public string? DetailedAnalysis { get; set; }
    
    [MemoryPackOrder(5)]
    public string Severity { get; set; } = "info";
    
    [MemoryPackOrder(6)]
    public string? ActionUrl { get; set; }
    
    [MemoryPackOrder(7)]
    public string? IconName { get; set; }
    
    [MemoryPackOrder(8)]
    public List<string>? SuggestedActions { get; set; }
    
    [MemoryPackOrder(9)]
    public DateTime GeneratedAt { get; set; }
    
    [MemoryPackOrder(10)]
    public bool IsRead { get; set; }
}
```

### API Error Response

```csharp
// RAJFinancial.Shared/DTOs/ApiErrorResponse.cs
namespace RAJFinancial.Shared.DTOs;

/// <summary>
/// Standardized error response for API errors.
/// </summary>
public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
    public string? TraceId { get; set; }
}
```

---

## Part 3: Error Codes & Constants

```csharp
// RAJFinancial.Shared/Constants/ErrorCodes.cs
namespace RAJFinancial.Shared.Constants;

/// <summary>
/// Standardized error codes for the API.
/// </summary>
public static class ErrorCodes
{
    // Authentication
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";
    public const string AUTH_FORBIDDEN = "AUTH_FORBIDDEN";
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
    public const string AUTH_EMAIL_EXISTS = "AUTH_EMAIL_EXISTS";
    public const string AUTH_ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    public const string AUTH_WEAK_PASSWORD = "AUTH_WEAK_PASSWORD";
    public const string AUTH_EMAIL_NOT_VERIFIED = "AUTH_EMAIL_NOT_VERIFIED";
    
    // Validation
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
    
    // Resources
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
    public const string ACCOUNT_NOT_FOUND = "ACCOUNT_NOT_FOUND";
    public const string ASSET_NOT_FOUND = "ASSET_NOT_FOUND";
    public const string BENEFICIARY_NOT_FOUND = "BENEFICIARY_NOT_FOUND";
    
    // Business Logic
    public const string ALLOCATION_INVALID = "ALLOCATION_INVALID";
    public const string DUPLICATE_BENEFICIARY = "DUPLICATE_BENEFICIARY";
    public const string ASSET_CREATE_FAILED = "ASSET_CREATE_FAILED";
    public const string ASSET_DUPLICATE_DETECTED = "ASSET_DUPLICATE_DETECTED";
    public const string BENEFICIARY_CREATE_FAILED = "BENEFICIARY_CREATE_FAILED";
    
    // Plaid Integration
    public const string PLAID_LINK_ERROR = "PLAID_LINK_ERROR";
    public const string INSTITUTION_NOT_SUPPORTED = "INSTITUTION_NOT_SUPPORTED";
    
    // System
    public const string RATE_LIMITED = "RATE_LIMITED";
    public const string SERVER_ERROR = "SERVER_ERROR";
}
```

---

## Part 4: Serialization Factory

```csharp
// RAJFinancial.Api/Serialization/SerializationFactory.cs
using MemoryPack;
using System.Text.Json;

namespace RAJFinancial.Api.Serialization;

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

---

## Part 5: Content Negotiation Middleware

```csharp
// RAJFinancial.Api/Middleware/ContentNegotiationMiddleware.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace RAJFinancial.Api.Middleware;

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

---

## Part 6: Exception Middleware

```csharp
// RAJFinancial.Api/Middleware/ExceptionMiddleware.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;

namespace RAJFinancial.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// </summary>
public class ExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;
    
    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ErrorCodes.VALIDATION_FAILED, ex.Message, ex.Errors);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ErrorCodes.AUTH_REQUIRED, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Forbidden, ErrorCodes.AUTH_FORBIDDEN, ex.Message);
        }
        catch (IntegrationException ex)
        {
            _logger.LogError(ex, "Integration error: {Code} - {Message}", ex.ErrorCode, ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadGateway, ex.ErrorCode, ex.Message, ex.Details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, ErrorCodes.SERVER_ERROR, "An unexpected error occurred");
        }
    }
    
    private async Task WriteErrorResponse(
        FunctionContext context,
        HttpStatusCode statusCode,
        string code,
        string message,
        Dictionary<string, object>? details = null)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null) return;
        
        var response = httpRequest.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        
        var error = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details,
            TraceId = context.TraceContext.TraceParent
        };
        
        await response.WriteAsJsonAsync(error);
    }
}

// Custom Exceptions
public class NotFoundException : Exception
{
    public string ErrorCode { get; }
    public NotFoundException(string errorCode, string message) : base(message) => ErrorCode = errorCode;
}

public class ValidationException : Exception
{
    public Dictionary<string, object>? Errors { get; }
    public ValidationException(string message, Dictionary<string, object>? errors = null) : base(message) => Errors = errors;
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class IntegrationException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object>? Details { get; }
    public IntegrationException(string errorCode, string message, Dictionary<string, object>? details = null) 
        : base(message) { ErrorCode = errorCode; Details = details; }
}
```

---

## Part 7: Azure Function Example - GetAssets

```csharp
// RAJFinancial.Api/Functions/Assets/GetAssets.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.OpenApi.Models;
using System.Net;

namespace RAJFinancial.Api.Functions.Assets;

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

---

## Part 8: API Endpoints Reference

### Accounts Endpoints

```
GET    /api/accounts              → GetAccounts
POST   /api/accounts/link-token   → CreateLinkToken
POST   /api/accounts/exchange     → ExchangePublicToken
POST   /api/accounts/{id}/refresh → RefreshAccount
DELETE /api/accounts/{id}         → UnlinkAccount
```

### Assets Endpoints

```
GET    /api/assets                → GetAssets (with ?type= filter)
GET    /api/assets/{id}           → GetAssetById
POST   /api/assets                → CreateAsset
PUT    /api/assets/{id}           → UpdateAsset
DELETE /api/assets/{id}           → DeleteAsset
GET    /api/assets/summary        → GetAssetSummary
```

### Beneficiaries Endpoints

```
GET    /api/beneficiaries              → GetBeneficiaries
GET    /api/beneficiaries/{id}         → GetBeneficiaryById
POST   /api/beneficiaries              → CreateBeneficiary
PUT    /api/beneficiaries/{id}         → UpdateBeneficiary
DELETE /api/beneficiaries/{id}         → DeleteBeneficiary
POST   /api/beneficiaries/assign       → AssignBeneficiary
PUT    /api/assignments/{id}           → UpdateAssignment
DELETE /api/assignments/{id}           → RemoveAssignment
GET    /api/beneficiaries/coverage     → GetCoverageSummary
```

### Analysis Endpoints

```
GET    /api/analysis/net-worth         → GetNetWorth
POST   /api/analysis/debt-payoff       → AnalyzeDebtPayoff
POST   /api/analysis/insurance         → AnalyzeInsuranceCoverage
GET    /api/analysis/insights          → GetAIInsights
```

### Authentication & Authorization

> **Important**: User authentication (registration, login, password reset, MFA, token refresh) is handled entirely by **Microsoft Entra External ID**. The Blazor WASM client uses MSAL to redirect users to Entra-hosted login pages. Our API only validates JWT tokens issued by Entra.

**Entra External ID Handles (NOT in our API):**
- User registration (sign-up flow)
- User login (sign-in flow)
- Password reset
- MFA enrollment and verification
- Token issuance and refresh
- Social identity provider login (Google, Microsoft, Apple)

**Our API Auth Endpoints (User Profile & Relationships):**

```
GET    /api/auth/me                    → GetCurrentUser (profile from token)
GET    /api/auth/roles                 → GetUserRoles (roles/permissions)
POST   /api/auth/clients               → AssignClient (professional assigns to client)
GET    /api/auth/clients               → GetAssignedClients (list professional's clients)
DELETE /api/auth/clients/{id}          → RemoveClientAccess (revoke access)
```

### Multi-Tenant Environment Strategy

> **Critical**: Use **separate Microsoft Entra External ID tenants** for development and production environments to ensure complete isolation of user data and configuration.

#### Why Separate Tenants?

| Benefit | Description |
|---------|-------------|
| **Complete Isolation** | Dev/test users cannot accidentally access production data |
| **Independent Configuration** | Different branding, policies, user flows per environment |
| **Safe Testing** | Test authentication changes without risking production |
| **Compliance** | Financial regulations require production data isolation |
| **Cost Tracking** | Easier to monitor usage per environment |
| **Security** | Compromised dev credentials don't affect production |

#### Tenant Structure

| Environment | Tenant Domain | CIAM Login URL | Purpose |
|-------------|---------------|----------------|---------|
| **Development** | `rajfinancialdev.onmicrosoft.com` | `https://rajfinancialdev.ciamlogin.com/` | Local dev, CI/CD, testing |
| **Production** | `rajfinancial.onmicrosoft.com` | `https://rajfinancial.ciamlogin.com/` | Live users, production data |

#### App Registration Per Tenant

Each tenant requires its own app registrations:

| Registration | Dev Tenant | Prod Tenant |
|--------------|------------|-------------|
| **SPA (Blazor WASM)** | `rajfinancial-spa-dev` | `rajfinancial-spa` |
| **API (Azure Functions)** | `rajfinancial-api-dev` | `rajfinancial-api` |

#### Environment Configuration

**appsettings.Development.json** (Development Tenant):

```json
{
  "AzureAdB2C": {
    "Instance": "https://rajfinancialdev.ciamlogin.com/",
    "Domain": "rajfinancialdev.onmicrosoft.com",
    "TenantId": "<dev-tenant-id>",
    "ClientId": "<dev-spa-client-id>",
    "CallbackPath": "/authentication/login-callback",
    "SignedOutCallbackPath": "/authentication/logout-callback"
  },
  "ApiScopes": {
    "Default": "https://rajfinancialdev.onmicrosoft.com/api/user_impersonation"
  }
}
```

**appsettings.Production.json** (Production Tenant):

```json
{
  "AzureAdB2C": {
    "Instance": "https://rajfinancial.ciamlogin.com/",
    "Domain": "rajfinancial.onmicrosoft.com",
    "TenantId": "<prod-tenant-id>",
    "ClientId": "<prod-spa-client-id>",
    "CallbackPath": "/authentication/login-callback",
    "SignedOutCallbackPath": "/authentication/logout-callback"
  },
  "ApiScopes": {
    "Default": "https://rajfinancial.onmicrosoft.com/api/user_impersonation"
  }
}
```

#### Azure Functions API Token Validation

```csharp
// Program.cs - Environment-aware Entra configuration
var builder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                config.Bind("AzureAdB2C", options);
                options.TokenValidationParameters.ValidAudiences = new[]
                {
                    config["AzureAdB2C:ClientId"],
                    $"api://{config["AzureAdB2C:ClientId"]}"
                };
            }, options =>
            {
                config.Bind("AzureAdB2C", options);
            });
    });
```

#### Tenant Setup Checklist

**Development Tenant (`rajfinancialdev`):**

| Task | Status | Notes |
|------|--------|-------|
| Create Entra External ID tenant | ⬜ Not Started | Azure Portal → Create tenant |
| Configure user flow (Sign up/Sign in) | ⬜ Not Started | Use default branding initially |
| Register SPA application | ⬜ Not Started | Redirect: `https://localhost:5001/authentication/login-callback` |
| Register API application | ⬜ Not Started | Expose `user_impersonation` scope |
| Add test users | ⬜ Not Started | Create test accounts for each role |
| Document tenant IDs in Key Vault | ⬜ Not Started | Store in dev Key Vault |

**Production Tenant (`rajfinancial`):**

| Task | Status | Notes |
|------|--------|-------|
| Create Entra External ID tenant | ⬜ Not Started | Azure Portal → Create tenant |
| Configure user flow (Sign up/Sign in) | ⬜ Not Started | Apply RAJ Financial branding |
| Register SPA application | ⬜ Not Started | Redirect: `https://rajfinancial.com/authentication/login-callback` |
| Register API application | ⬜ Not Started | Expose `user_impersonation` scope |
| Configure MFA policy | ⬜ Not Started | Require MFA for all users |
| Configure session lifetime | ⬜ Not Started | 24-hour token lifetime |
| Document tenant IDs in Key Vault | ⬜ Not Started | Store in prod Key Vault |

#### CI/CD Pipeline Integration

```yaml
# azure-pipelines.yml or GitHub Actions
env:
  # Development deployment
  DEV_AZURE_AD_TENANT_ID: $(DEV_TENANT_ID)
  DEV_AZURE_AD_CLIENT_ID: $(DEV_SPA_CLIENT_ID)
  
  # Production deployment  
  PROD_AZURE_AD_TENANT_ID: $(PROD_TENANT_ID)
  PROD_AZURE_AD_CLIENT_ID: $(PROD_SPA_CLIENT_ID)
```

#### Key Vault Secret Management

| Secret Name | Dev Key Vault | Prod Key Vault |
|-------------|---------------|----------------|
| `AzureAd--TenantId` | Dev tenant GUID | Prod tenant GUID |
| `AzureAd--ClientId` | Dev SPA client ID | Prod SPA client ID |
| `AzureAd--ApiClientId` | Dev API client ID | Prod API client ID |
| `AzureAd--ApiClientSecret` | Dev API secret | Prod API secret |

> **Never** commit tenant IDs or client IDs to source control. Use Azure Key Vault references in app settings.

---

## Part 9: Plaid Integration Service

```csharp
// RAJFinancial.Infrastructure/External/PlaidService.cs
using Going.Plaid;
using Going.Plaid.Link;
using Going.Plaid.Accounts;
using Going.Plaid.Transactions;
using Going.Plaid.Institutions;

namespace RAJFinancial.Infrastructure.External;

/// <summary>
/// Service for interacting with Plaid API for account aggregation.
/// Handles link token creation, token exchange, and data synchronization.
/// </summary>
public class PlaidService : IPlaidService
{
    private readonly PlaidClient _plaidClient;
    private readonly ILinkedAccountRepository _accountRepository;
    private readonly ILogger<PlaidService> _logger;
    private readonly IAuditService _auditService;
    private readonly IEncryptionService _encryptionService;

    public PlaidService(
        PlaidClient plaidClient,
        ILinkedAccountRepository accountRepository,
        ILogger<PlaidService> logger,
        IAuditService auditService,
        IEncryptionService encryptionService)
    {
        _plaidClient = plaidClient;
        _accountRepository = accountRepository;
        _logger = logger;
        _auditService = auditService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Creates a Link token for initializing Plaid Link in the client.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="accessToken">Optional existing access token for update mode.</param>
    /// <returns>Link token response with token and expiration.</returns>
    public async Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(
        Guid userId, 
        string? accessToken = null)
    {
        _logger.LogInformation(
            "Creating Plaid Link token for user {UserId}, UpdateMode: {UpdateMode}",
            userId,
            accessToken != null);

        var request = new LinkTokenCreateRequest
        {
            User = new LinkTokenCreateRequestUser
            {
                ClientUserId = userId.ToString()
            },
            ClientName = "RAJ Financial",
            Products = new[]
            {
                Products.Transactions,
                Products.Investments,
                Products.Liabilities
            },
            CountryCodes = new[] { CountryCode.Us },
            Language = Language.English,
            Webhook = "https://func-rajfinancial.azurewebsites.net/api/plaid/webhook",
            AccountFilters = new LinkTokenAccountFilters
            {
                Depository = new DepositoryFilter
                {
                    AccountSubtypes = new[]
                    {
                        AccountSubtype.Checking,
                        AccountSubtype.Savings,
                        AccountSubtype.MoneyMarket
                    }
                },
                Credit = new CreditFilter
                {
                    AccountSubtypes = new[] { AccountSubtype.CreditCard }
                },
                Investment = new InvestmentFilter
                {
                    AccountSubtypes = new[]
                    {
                        AccountSubtype.Brokerage,
                        AccountSubtype.Ira,
                        AccountSubtype._401k,
                        AccountSubtype.Roth
                    }
                },
                Loan = new LoanFilter
                {
                    AccountSubtypes = new[]
                    {
                        AccountSubtype.Mortgage,
                        AccountSubtype.Auto,
                        AccountSubtype.Student
                    }
                }
            }
        };

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.AccessToken = _encryptionService.Decrypt(accessToken);
        }

        try
        {
            var response = await _plaidClient.LinkTokenCreateAsync(request);

            await _auditService.LogAsync(new AuditEntry
            {
                UserId = userId,
                Action = "PlaidLinkTokenCreated",
                Details = new { UpdateMode = accessToken != null }
            });

            return new PlaidLinkTokenResponse
            {
                LinkToken = response.LinkToken,
                Expiration = response.Expiration,
                RequestId = response.RequestId
            };
        }
        catch (PlaidException ex)
        {
            _logger.LogError(ex, 
                "Plaid Link token creation failed for user {UserId}: {ErrorCode}",
                userId, ex.ErrorCode);

            throw new IntegrationException(
                ErrorCodes.PLAID_LINK_ERROR,
                "Unable to initialize bank connection",
                new { PlaidErrorCode = ex.ErrorCode });
        }
    }

    /// <summary>
    /// Exchanges a public token from Plaid Link for an access token
    /// and retrieves initial account data.
    /// </summary>
    public async Task<IEnumerable<LinkedAccountDto>> ExchangePublicTokenAsync(
        Guid userId,
        string publicToken,
        PlaidLinkMetadata metadata)
    {
        _logger.LogInformation(
            "Exchanging public token for user {UserId}, Institution: {Institution}",
            userId,
            metadata.InstitutionName);

        try
        {
            // Exchange public token for access token
            var exchangeResponse = await _plaidClient.ItemPublicTokenExchangeAsync(
                new ItemPublicTokenExchangeRequest { PublicToken = publicToken });

            var accessToken = exchangeResponse.AccessToken;
            var itemId = exchangeResponse.ItemId;

            // Get institution details
            var institutionResponse = await _plaidClient.InstitutionsGetByIdAsync(
                new InstitutionsGetByIdRequest
                {
                    InstitutionId = metadata.InstitutionId,
                    CountryCodes = new[] { CountryCode.Us },
                    Options = new InstitutionsGetByIdRequestOptions
                    {
                        IncludeOptionalMetadata = true
                    }
                });

            var institution = institutionResponse.Institution;

            // Get accounts and balances
            var accountsResponse = await _plaidClient.AccountsGetAsync(
                new AccountsGetRequest { AccessToken = accessToken });

            var balancesResponse = await _plaidClient.AccountsBalanceGetAsync(
                new AccountsBalanceGetRequest { AccessToken = accessToken });

            var linkedAccounts = new List<LinkedAccount>();

            foreach (var account in accountsResponse.Accounts)
            {
                var balance = balancesResponse.Accounts
                    .FirstOrDefault(a => a.AccountId == account.AccountId);

                var linkedAccount = new LinkedAccount
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlaidItemId = itemId,
                    PlaidAccountId = account.AccountId,
                    PlaidAccessToken = _encryptionService.Encrypt(accessToken),
                    InstitutionId = metadata.InstitutionId,
                    InstitutionName = institution.Name,
                    InstitutionLogo = institution.Logo,
                    InstitutionPrimaryColor = institution.PrimaryColor,
                    AccountName = account.Name,
                    AccountOfficialName = account.OfficialName,
                    AccountMask = account.Mask,
                    AccountType = MapAccountType(account.Type),
                    AccountSubtype = account.Subtype?.ToString(),
                    CurrentBalance = balance?.Balances.Current,
                    AvailableBalance = balance?.Balances.Available,
                    CurrencyCode = balance?.Balances.IsoCurrencyCode ?? "USD",
                    ConnectionStatus = ConnectionStatus.Connected,
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                linkedAccounts.Add(linkedAccount);
            }

            await _accountRepository.AddRangeAsync(linkedAccounts);

            await _auditService.LogAsync(new AuditEntry
            {
                UserId = userId,
                Action = "AccountsLinked",
                Details = new
                {
                    InstitutionName = institution.Name,
                    AccountCount = linkedAccounts.Count,
                    AccountTypes = linkedAccounts.Select(a => a.AccountType).Distinct()
                }
            });

            _logger.LogInformation(
                "Successfully linked {Count} accounts for user {UserId} from {Institution}",
                linkedAccounts.Count, userId, institution.Name);

            // Trigger background transaction sync
            await TriggerTransactionSyncAsync(userId, itemId);

            return linkedAccounts.Select(MapToDto);
        }
        catch (PlaidException ex)
        {
            _logger.LogError(ex,
                "Plaid token exchange failed for user {UserId}: {ErrorCode}",
                userId, ex.ErrorCode);

            var errorCode = ex.ErrorCode switch
            {
                "INSTITUTION_NOT_SUPPORTED" => ErrorCodes.INSTITUTION_NOT_SUPPORTED,
                "INVALID_PUBLIC_TOKEN" => ErrorCodes.PLAID_LINK_ERROR,
                _ => ErrorCodes.PLAID_LINK_ERROR
            };

            throw new IntegrationException(errorCode, ex.ErrorMessage);
        }
    }

    /// <summary>
    /// Handles Plaid webhooks for real-time updates.
    /// </summary>
    public async Task HandleWebhookAsync(PlaidWebhookPayload payload)
    {
        _logger.LogInformation(
            "Received Plaid webhook: {Type} - {Code}",
            payload.WebhookType,
            payload.WebhookCode);

        switch (payload.WebhookType)
        {
            case "TRANSACTIONS":
                await HandleTransactionWebhookAsync(payload);
                break;

            case "ITEM":
                await HandleItemWebhookAsync(payload);
                break;

            case "HOLDINGS":
                await HandleHoldingsWebhookAsync(payload);
                break;
        }
    }

    private async Task HandleItemWebhookAsync(PlaidWebhookPayload payload)
    {
        var accounts = await _accountRepository.GetByPlaidItemIdAsync(payload.ItemId);

        switch (payload.WebhookCode)
        {
            case "ERROR":
                foreach (var account in accounts)
                {
                    account.ConnectionStatus = payload.Error?.ErrorCode switch
                    {
                        "ITEM_LOGIN_REQUIRED" => ConnectionStatus.NeedsReauth,
                        _ => ConnectionStatus.Disconnected
                    };
                    account.UpdatedAt = DateTime.UtcNow;
                }
                await _accountRepository.UpdateRangeAsync(accounts);
                break;

            case "PENDING_EXPIRATION":
                foreach (var account in accounts)
                {
                    await _notificationService.SendAsync(account.UserId, new Notification
                    {
                        Type = NotificationType.AccountAttention,
                        Title = "Action Required",
                        Message = $"Your {account.InstitutionName} connection will expire soon.",
                        ActionUrl = $"/accounts?reconnect={account.Id}"
                    });
                }
                break;
        }
    }
    
    private async Task HandleTransactionWebhookAsync(PlaidWebhookPayload payload)
    {
        // Sync new transactions when received
        _logger.LogInformation("Transaction webhook received for item {ItemId}", payload.ItemId);
    }
    
    private async Task HandleHoldingsWebhookAsync(PlaidWebhookPayload payload)
    {
        // Sync investment holdings when updated
        _logger.LogInformation("Holdings webhook received for item {ItemId}", payload.ItemId);
    }
    
    private async Task TriggerTransactionSyncAsync(Guid userId, string itemId)
    {
        // Queue background job to sync transactions
        _logger.LogInformation("Queuing transaction sync for user {UserId}, item {ItemId}", userId, itemId);
    }
    
    private AccountType MapAccountType(string plaidType) => plaidType switch
    {
        "depository" => AccountType.Depository,
        "credit" => AccountType.Credit,
        "investment" => AccountType.Investment,
        "loan" => AccountType.Loan,
        _ => AccountType.Other
    };
    
    private LinkedAccountDto MapToDto(LinkedAccount account) => new()
    {
        Id = account.Id,
        InstitutionId = account.InstitutionId,
        InstitutionName = account.InstitutionName,
        InstitutionLogo = account.InstitutionLogo,
        InstitutionPrimaryColor = account.InstitutionPrimaryColor,
        AccountName = account.AccountName,
        AccountOfficialName = account.AccountOfficialName,
        AccountMask = account.AccountMask,
        AccountType = account.AccountType,
        AccountSubtype = account.AccountSubtype,
        CurrentBalance = account.CurrentBalance,
        AvailableBalance = account.AvailableBalance,
        CurrencyCode = account.CurrencyCode,
        ConnectionStatus = account.ConnectionStatus,
        LastSyncedAt = account.LastSyncedAt
    };
}
```

---

## Part 10: Claude AI Service

```csharp
// RAJFinancial.Infrastructure/External/ClaudeAIService.cs
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace RAJFinancial.Infrastructure.External;

/// <summary>
/// Service for generating AI-powered financial insights using Claude.
/// Implements the "tools not advice" framing for regulatory compliance.
/// </summary>
public class ClaudeAIService : IClaudeAIService
{
    private readonly AnthropicClient _client;
    private readonly IFinancialDataService _financialDataService;
    private readonly ILogger<ClaudeAIService> _logger;
    private readonly IDistributedCache _cache;

    private const string Model = "claude-sonnet-4-5-20250929";
    private const int MaxTokens = 4096;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public ClaudeAIService(
        AnthropicClient client,
        IFinancialDataService financialDataService,
        ILogger<ClaudeAIService> logger,
        IDistributedCache cache)
    {
        _client = client;
        _financialDataService = financialDataService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Generates personalized financial insights based on user's complete profile.
    /// </summary>
    public async Task<IEnumerable<AIInsightDto>> GenerateInsightsAsync(Guid userId)
    {
        var cacheKey = $"insights:{userId}";
        
        // Check cache first
        var cached = await _cache.GetAsync<List<AIInsightDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached insights for user {UserId}", userId);
            return cached;
        }

        _logger.LogInformation("Generating AI insights for user {UserId}", userId);

        // Gather user's financial profile
        var profile = await _financialDataService.GetComprehensiveProfileAsync(userId);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildInsightsPrompt(profile);

        try
        {
            var response = await _client.Messages.CreateAsync(new MessageRequest
            {
                Model = Model,
                MaxTokens = MaxTokens,
                System = systemPrompt,
                Messages = new[]
                {
                    new Message { Role = "user", Content = userPrompt }
                }
            });

            var insights = ParseInsightsResponse(response.Content.FirstOrDefault()?.Text);

            // Enrich each insight with action URLs and icons
            foreach (var insight in insights)
            {
                EnrichInsight(insight, profile);
            }

            // Cache the results
            await _cache.SetAsync(cacheKey, insights, CacheDuration);

            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate insights for user {UserId}", userId);
            return GenerateFallbackInsights(profile);
        }
    }

    private string BuildSystemPrompt() => @"
You are a sophisticated financial analysis tool integrated into a personal finance platform.
Your role is to analyze financial data and provide objective observations and educational information.

CRITICAL RULES:
1. You are a TOOL, not an advisor. Present analysis as observations, not recommendations.
2. NEVER use phrases like: ""you should"", ""I recommend"", ""you need to"", ""I advise""
3. ALWAYS use phrases like: ""analysis indicates"", ""data shows"", ""options include""
4. Present multiple options when applicable, with pros and cons for each
5. Always note that professional consultation is advised for major decisions
6. Be warm and encouraging while remaining objective
7. Use clear, jargon-free language accessible to non-experts

OUTPUT FORMAT:
Always respond with valid JSON matching the requested structure.";

    private string BuildInsightsPrompt(ComprehensiveProfile profile) => $@"
Analyze this financial profile and generate 3-5 actionable insights.

PROFILE DATA:
{JsonSerializer.Serialize(SanitizeProfile(profile), new JsonSerializerOptions { WriteIndented = true })}

Generate insights focusing on:
- Coverage gaps (insurance, beneficiaries)
- Optimization opportunities (debt payoff, tax efficiency)
- Goal progress and projections
- Areas needing attention

For each insight, provide:
- category: One of [INSURANCE_GAP, BENEFICIARY_REVIEW, DEBT_OPTIMIZATION, TAX_PLANNING, GOAL_PROGRESS, ESTATE_PLANNING]
- severity: One of [info, warning, critical]
- title: Brief, action-oriented title (max 50 chars)
- description: Clear explanation with specific numbers (max 200 chars)
- detailedAnalysis: Deeper explanation with context (max 500 chars)
- suggestedActions: Array of 2-3 specific steps the user could consider

Respond with JSON array.";

    private void EnrichInsight(AIInsightDto insight, ComprehensiveProfile profile)
    {
        insight.Id = Guid.NewGuid();
        insight.GeneratedAt = DateTime.UtcNow;

        // Map category to action URL
        insight.ActionUrl = insight.Category switch
        {
            "INSURANCE_GAP" => "/tools/insurance",
            "BENEFICIARY_REVIEW" => "/beneficiaries",
            "DEBT_OPTIMIZATION" => "/tools/debt-payoff",
            "ESTATE_PLANNING" => "/tools/estate-checklist",
            "GOAL_PROGRESS" => "/goals",
            "TAX_PLANNING" => "/tools/tax-planning",
            _ => "/dashboard"
        };

        // Map category to icon
        insight.IconName = insight.Category switch
        {
            "INSURANCE_GAP" => "Shield",
            "BENEFICIARY_REVIEW" => "Users",
            "DEBT_OPTIMIZATION" => "TrendingDown",
            "TAX_PLANNING" => "Receipt",
            "GOAL_PROGRESS" => "Target",
            "ESTATE_PLANNING" => "FileText",
            _ => "Lightbulb"
        };
    }
    
    private List<AIInsightDto> ParseInsightsResponse(string? responseText)
    {
        if (string.IsNullOrEmpty(responseText))
            return new List<AIInsightDto>();
            
        try
        {
            return JsonSerializer.Deserialize<List<AIInsightDto>>(responseText) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON");
            return new List<AIInsightDto>();
        }
    }
    
    private ComprehensiveProfile SanitizeProfile(ComprehensiveProfile profile)
    {
        // Remove PII before sending to AI
        return new ComprehensiveProfile
        {
            NetWorth = profile.NetWorth,
            TotalAssets = profile.TotalAssets,
            TotalLiabilities = profile.TotalLiabilities,
            AssetsByType = profile.AssetsByType,
            DebtsByType = profile.DebtsByType,
            InsuranceCoverage = profile.InsuranceCoverage,
            BeneficiaryStatus = profile.BeneficiaryStatus,
            // Exclude: Name, Email, SSN, Account Numbers, etc.
        };
    }
    
    private List<AIInsightDto> GenerateFallbackInsights(ComprehensiveProfile profile)
    {
        var insights = new List<AIInsightDto>();
        
        // Generate basic insights based on data analysis
        if (profile.BeneficiaryStatus.AssetsWithoutBeneficiaries > 0)
        {
            insights.Add(new AIInsightDto
            {
                Id = Guid.NewGuid(),
                Category = "BENEFICIARY_REVIEW",
                Severity = "warning",
                Title = "Beneficiary Review Needed",
                Description = $"{profile.BeneficiaryStatus.AssetsWithoutBeneficiaries} assets have no beneficiaries assigned.",
                ActionUrl = "/beneficiaries",
                IconName = "Users",
                GeneratedAt = DateTime.UtcNow
            });
        }
        
        if (profile.InsuranceCoverage.CoverageRatio < 0.8m)
        {
            insights.Add(new AIInsightDto
            {
                Id = Guid.NewGuid(),
                Category = "INSURANCE_GAP",
                Severity = profile.InsuranceCoverage.CoverageRatio < 0.5m ? "critical" : "warning",
                Title = "Insurance Coverage Gap Detected",
                Description = $"Current coverage is {profile.InsuranceCoverage.CoverageRatio:P0} of calculated need.",
                ActionUrl = "/tools/insurance",
                IconName = "Shield",
                GeneratedAt = DateTime.UtcNow
            });
        }
        
        return insights;
    }
}
```

---

## Part 11: User Strategy Sources (Uploads + URLs) + Retrieval-Augmented Planning

This feature lets users upload documents (e.g., books/notes they own) and add reference URLs containing strategies they want to follow. The platform retrieves relevant excerpts and generates suggestions grounded in both:
- the user’s current financial profile, and
- the user’s selected strategy sources.

### Architecture

**Storage**
- Raw files: Azure Blob Storage (private container)
- Metadata: Azure SQL (Source record, ownership, status, URL)
- Vector index: Azure AI Search (chunk embeddings)

**Ingestion (async)**
1. Upload file or submit URL
2. Create Source record (status: Pending)
3. Queue ingestion job (Azure Storage Queue)
4. Worker extracts text, chunks, embeds, and upserts to vector index
5. Mark Source status (Ready/Failed)

### Safety & Copyright

Operational rules:
- Treat uploaded content as private user-provided data.
- For generated responses, prefer summaries + citations; do not reproduce large portions of copyrighted text.
- For URL sources, respect robots.txt/terms and store only what is needed for retrieval.

### API Endpoints (MVP)

#### Strategy Sources

| Endpoint | Purpose |
|----------|---------|
| POST /api/strategy-sources/upload | Upload a document (multipart) |
| POST /api/strategy-sources/url | Add a URL source |
| GET /api/strategy-sources | List user sources |
| GET /api/strategy-sources/{id} | Get source metadata |
| GET /api/strategy-sources/{id}/status | Get ingestion status |
| DELETE /api/strategy-sources/{id} | Delete source (and vectors) |

#### Planning / Suggestions

| Endpoint | Purpose |
|----------|---------|
| POST /api/analysis/planning-suggestions | Generate suggestions grounded in selected sources |

### Response Shape (citations)

Recommendations should include citations back to source chunks:
- sourceId
- sourceType (upload/url)
- title
- url (if applicable)
- snippet (short excerpt)

### Services (recommended)

- StrategySourceService: create/list/delete sources
- StrategyIngestionWorker: extract/chunk/embed/index
- RetrievalService: query Azure AI Search vector index
- PlanningService: build prompt with (profile + retrieved chunks) and call the planning model

### AI Model Strategy

**MVP (single model):**
- **Insights & Planning**: Claude Sonnet 4.5 (`claude-sonnet-4-5-20250929`)
- **Classification**: No AI—use Plaid categories + user-selected dropdowns
- **Strategy source processing**: Text extraction only (no AI classification)

**Future consideration:**
- If AI costs become significant at scale, consider adding a cheaper model (e.g., Azure OpenAI gpt-4o-mini) for high-volume, low-complexity tasks (transaction labeling, entity extraction).
- For MVP, keep it simple with one model.

---

## Part 11: Service Interfaces

### Account Service Interface

```csharp
// RAJFinancial.Core/Interfaces/IAccountService.cs
namespace RAJFinancial.Core.Interfaces;

/// <summary>
/// Service for managing linked financial accounts via Plaid.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates a Plaid Link token for account connection.
    /// </summary>
    Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(Guid userId);
    
    /// <summary>
    /// Exchanges a public token for access token and links accounts.
    /// </summary>
    Task<IEnumerable<LinkedAccountDto>> ExchangePublicTokenAsync(Guid userId, string publicToken, PlaidLinkMetadata metadata);
    
    /// <summary>
    /// Gets all linked accounts for a user.
    /// </summary>
    Task<IEnumerable<LinkedAccountDto>> GetAccountsAsync(Guid userId);
    
    /// <summary>
    /// Refreshes account balances from Plaid.
    /// </summary>
    Task<LinkedAccountDto> RefreshAccountAsync(Guid userId, Guid accountId);
    
    /// <summary>
    /// Unlinks an account (revokes Plaid access).
    /// </summary>
    Task UnlinkAccountAsync(Guid userId, Guid accountId);
}
```

### Asset Service Interface

```csharp
// RAJFinancial.Core/Interfaces/IAssetService.cs
namespace RAJFinancial.Core.Interfaces;

/// <summary>
/// Service for managing manual assets.
/// </summary>
public interface IAssetService
{
    Task<IEnumerable<AssetDto>> GetAssetsAsync(Guid userId, AssetType? filterType = null);
    Task<AssetDto> GetAssetByIdAsync(Guid userId, Guid assetId);
    Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request);
    Task<AssetDto> UpdateAssetAsync(Guid userId, Guid assetId, UpdateAssetRequest request);
    Task DeleteAssetAsync(Guid userId, Guid assetId);
    Task<AssetSummaryDto> GetAssetSummaryAsync(Guid userId);
}
```

### Beneficiary Service Interface

```csharp
// RAJFinancial.Core/Interfaces/IBeneficiaryService.cs
namespace RAJFinancial.Core.Interfaces;

/// <summary>
/// Service for managing beneficiaries and assignments.
/// </summary>
public interface IBeneficiaryService
{
    Task<IEnumerable<BeneficiaryDto>> GetBeneficiariesAsync(Guid userId);
    Task<BeneficiaryDto> GetBeneficiaryByIdAsync(Guid userId, Guid beneficiaryId);
    Task<BeneficiaryDto> CreateBeneficiaryAsync(Guid userId, CreateBeneficiaryRequest request);
    Task<BeneficiaryDto> UpdateBeneficiaryAsync(Guid userId, Guid beneficiaryId, UpdateBeneficiaryRequest request);
    Task DeleteBeneficiaryAsync(Guid userId, Guid beneficiaryId);
    
    // Assignment operations
    Task<BeneficiaryAssignmentDto> AssignBeneficiaryAsync(Guid userId, AssignBeneficiaryRequest request);
    Task UpdateAssignmentAsync(Guid userId, Guid assignmentId, UpdateAssignmentRequest request);
    Task RemoveAssignmentAsync(Guid userId, Guid assignmentId);
    
    // Analysis
    Task<BeneficiaryCoverageDto> GetCoverageSummaryAsync(Guid userId);
}
```

### Analysis Service Interface

```csharp
// RAJFinancial.Core/Interfaces/IAnalysisService.cs
namespace RAJFinancial.Core.Interfaces;

/// <summary>
/// Service for financial analysis and planning tools.
/// </summary>
public interface IAnalysisService
{
    Task<NetWorthDto> CalculateNetWorthAsync(Guid userId);
    Task<DebtPayoffAnalysisDto> AnalyzeDebtPayoffAsync(Guid userId, DebtPayoffRequest request);
    Task<InsuranceCoverageAnalysisDto> AnalyzeInsuranceCoverageAsync(Guid userId, InsuranceCoverageRequest request);
    Task<IEnumerable<AIInsightDto>> GetAIInsightsAsync(Guid userId);
}
```

---

## Part 12: Analysis DTOs

### Debt Payoff Analysis

```csharp
// RAJFinancial.Shared/DTOs/Responses/DebtPayoffAnalysisDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Responses;

[MemoryPackable]
public partial class DebtPayoffAnalysisDto
{
    [MemoryPackOrder(0)]
    public List<DebtDto> Debts { get; set; } = new();
    
    [MemoryPackOrder(1)]
    public List<PayoffStrategyDto> Strategies { get; set; } = new();
    
    [MemoryPackOrder(2)]
    public string Disclaimer { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class DebtDto
{
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    [MemoryPackOrder(1)]
    public string Name { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public DebtType DebtType { get; set; }
    
    [MemoryPackOrder(3)]
    public decimal Balance { get; set; }
    
    [MemoryPackOrder(4)]
    public decimal OriginalBalance { get; set; }
    
    [MemoryPackOrder(5)]
    public decimal InterestRate { get; set; }
    
    [MemoryPackOrder(6)]
    public decimal MinimumPayment { get; set; }
}

[MemoryPackable]
public partial class PayoffStrategyDto
{
    [MemoryPackOrder(0)]
    public string Name { get; set; } = string.Empty;
    
    [MemoryPackOrder(1)]
    public string Description { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public int MonthsToPayoff { get; set; }
    
    [MemoryPackOrder(3)]
    public decimal TotalInterestPaid { get; set; }
    
    [MemoryPackOrder(4)]
    public decimal SavingsVsMinimum { get; set; }
    
    [MemoryPackOrder(5)]
    public List<Guid> PayoffOrder { get; set; } = new();
    
    [MemoryPackOrder(6)]
    public List<PayoffDataPoint> MonthlyProjection { get; set; } = new();
}

[MemoryPackable]
public partial class PayoffDataPoint
{
    [MemoryPackOrder(0)]
    public int Month { get; set; }
    
    [MemoryPackOrder(1)]
    public decimal TotalBalance { get; set; }
    
    [MemoryPackOrder(2)]
    public decimal TotalPaid { get; set; }
    
    [MemoryPackOrder(3)]
    public decimal InterestPaid { get; set; }
}
```

### Insurance Coverage Analysis

```csharp
// RAJFinancial.Shared/DTOs/Responses/InsuranceCoverageDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Responses;

[MemoryPackable]
public partial class InsuranceCoverageAnalysisDto
{
    [MemoryPackOrder(0)]
    public decimal CalculatedNeed { get; set; }
    
    [MemoryPackOrder(1)]
    public decimal CurrentCoverage { get; set; }
    
    [MemoryPackOrder(2)]
    public decimal Gap { get; set; }
    
    [MemoryPackOrder(3)]
    public decimal CoverageRatio { get; set; }
    
    [MemoryPackOrder(4)]
    public InsuranceBreakdownDto Breakdown { get; set; } = new();
    
    [MemoryPackOrder(5)]
    public List<string> Considerations { get; set; } = new();
    
    [MemoryPackOrder(6)]
    public string Disclaimer { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class InsuranceBreakdownDto
{
    [MemoryPackOrder(0)]
    public decimal IncomeReplacement { get; set; }
    
    [MemoryPackOrder(1)]
    public decimal DebtPayoff { get; set; }
    
    [MemoryPackOrder(2)]
    public decimal EducationFunding { get; set; }
    
    [MemoryPackOrder(3)]
    public decimal FinalExpenses { get; set; }
    
    [MemoryPackOrder(4)]
    public decimal EmergencyFund { get; set; }
}
```

### Beneficiary Coverage

```csharp
// RAJFinancial.Shared/DTOs/Responses/BeneficiaryCoverageDto.cs
using MemoryPack;

namespace RAJFinancial.Shared.DTOs.Responses;

[MemoryPackable]
public partial class BeneficiaryCoverageDto
{
    [MemoryPackOrder(0)]
    public int TotalAssets { get; set; }
    
    [MemoryPackOrder(1)]
    public int AssetsWithBeneficiaries { get; set; }
    
    [MemoryPackOrder(2)]
    public List<Guid> AssetsNeedingReview { get; set; } = new();
    
    [MemoryPackOrder(3)]
    public DateTime? LastReviewDate { get; set; }
    
    [MemoryPackOrder(4)]
    public decimal CoveragePercentage { get; set; }
}
```

---

## Part 13: Blazor Client API Service

```csharp
// RAJFinancial.Client/Services/ApiClient.cs
using MemoryPack;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RAJFinancial.Client.Services;

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
    /// Sends a PUT request with a body and deserializes the response.
    /// </summary>
    public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest body, 
        CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PreferredContentType));
            
            var contentType = PreferredContentType;
            byte[] bodyBytes = contentType == MemoryPackContentType
                ? MemoryPackSerializer.Serialize(body)
                : JsonSerializer.SerializeToUtf8Bytes(body, _jsonOptions);
            
            request.Content = new ByteArrayContent(bodyBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            
            var response = await _httpClient.SendAsync(request, ct);
            return await HandleResponse<TResponse>(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API PUT request failed: {Endpoint}", endpoint);
            return ApiResponse<TResponse>.Failure(ErrorCodes.SERVER_ERROR, ex.Message);
        }
    }
    
    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions, ct);
                return ApiResponse<bool>.Failure(error?.Code ?? ErrorCodes.SERVER_ERROR, error?.Message ?? "Delete failed");
            }
            
            return ApiResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API DELETE request failed: {Endpoint}", endpoint);
            return ApiResponse<bool>.Failure(ErrorCodes.SERVER_ERROR, ex.Message);
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

/// <summary>
/// Interface for the API client.
/// </summary>
public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(string endpoint, CancellationToken ct = default);
}
```

---

## Part 14: Domain Entities

### User Entity

```csharp
// RAJFinancial.Core/Entities/User.cs
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Represents a user of the RAJ Financial platform.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<LinkedAccount> LinkedAccounts { get; set; } = new List<LinkedAccount>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}
```

### Linked Account Entity

```csharp
// RAJFinancial.Core/Entities/LinkedAccount.cs
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Represents a financial account linked via Plaid.
/// </summary>
public class LinkedAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PlaidItemId { get; set; } = string.Empty;
    public string PlaidAccountId { get; set; } = string.Empty;
    public string PlaidAccessToken { get; set; } = string.Empty; // Encrypted
    public string InstitutionId { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionLogo { get; set; }
    public string? InstitutionPrimaryColor { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? AccountOfficialName { get; set; }
    public string AccountMask { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string? AccountSubtype { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public ConnectionStatus ConnectionStatus { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public ICollection<BeneficiaryAssignment> BeneficiaryAssignments { get; set; } = new List<BeneficiaryAssignment>();
}
```

### Asset Entity

```csharp
// RAJFinancial.Core/Entities/Asset.cs
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Represents a manually tracked asset.
/// </summary>
public class Asset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public string? Description { get; set; }
    public decimal CurrentValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal? PurchasePrice { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal OwnershipPercentage { get; set; } = 100m;
    public Guid? AssociatedEntityId { get; set; }
    public string? AssociatedEntityName { get; set; }
    public string? AddressJson { get; set; } // For real estate
    public string? MetadataJson { get; set; } // Type-specific data
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public ICollection<BeneficiaryAssignment> BeneficiaryAssignments { get; set; } = new List<BeneficiaryAssignment>();
    
    // Calculated
    public decimal UserShareValue => CurrentValue * (OwnershipPercentage / 100m);
}
```

### Beneficiary Entity

```csharp
// RAJFinancial.Core/Entities/Beneficiary.cs
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Represents a beneficiary (individual, trust, or organization).
/// </summary>
public class Beneficiary
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public BeneficiaryType BeneficiaryType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? OrganizationName { get; set; }
    public string? TrustName { get; set; }
    public RelationshipType? Relationship { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AddressJson { get; set; }
    public string? TaxId { get; set; } // Encrypted
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public ICollection<BeneficiaryAssignment> Assignments { get; set; } = new List<BeneficiaryAssignment>();
    
    // Calculated
    public string DisplayName => BeneficiaryType switch
    {
        BeneficiaryType.Individual => $"{FirstName} {LastName}",
        BeneficiaryType.Trust => TrustName ?? "Unknown Trust",
        BeneficiaryType.Charity => OrganizationName ?? "Unknown Charity",
        BeneficiaryType.Organization => OrganizationName ?? "Unknown Organization",
        _ => "Unknown"
    };
}
```

### Beneficiary Assignment Entity

```csharp
// RAJFinancial.Core/Entities/BeneficiaryAssignment.cs
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Links a beneficiary to an asset or account with allocation details.
/// </summary>
public class BeneficiaryAssignment
{
    public Guid Id { get; set; }
    public Guid BeneficiaryId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? LinkedAccountId { get; set; }
    public DesignationType DesignationType { get; set; }
    public decimal AllocationPercentage { get; set; }
    public bool PerStirpes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public Beneficiary Beneficiary { get; set; } = null!;
    public Asset? Asset { get; set; }
    public LinkedAccount? LinkedAccount { get; set; }
}
```

---

## Part 15: Enums

```csharp
// RAJFinancial.Core/Enums/AssetType.cs
namespace RAJFinancial.Core.Enums;

public enum AssetType
{
    RealEstate,
    Vehicle,
    BusinessInterest,
    PersonalProperty,
    Collectible,
    Other
}

// RAJFinancial.Core/Enums/AccountType.cs
public enum AccountType
{
    Depository,
    Credit,
    Investment,
    Loan,
    Other
}

// RAJFinancial.Core/Enums/ConnectionStatus.cs
public enum ConnectionStatus
{
    Connected,
    NeedsReauth,
    Disconnected,
    Syncing
}

// RAJFinancial.Core/Enums/BeneficiaryType.cs
public enum BeneficiaryType
{
    Individual,
    Trust,
    Charity,
    Organization
}

// RAJFinancial.Core/Enums/RelationshipType.cs
public enum RelationshipType
{
    Spouse,
    Child,
    Parent,
    Sibling,
    Grandchild,
    Other
}

// RAJFinancial.Core/Enums/DesignationType.cs
public enum DesignationType
{
    Primary,
    Contingent
}

// RAJFinancial.Core/Enums/DebtType.cs
public enum DebtType
{
    CreditCard,
    Mortgage,
    AutoLoan,
    StudentLoan,
    PersonalLoan,
    MedicalDebt,
    Other
}
```

---

## Part 16: Database Context & Configuration

```csharp
// RAJFinancial.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace RAJFinancial.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<LinkedAccount> LinkedAccounts => Set<LinkedAccount>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<BeneficiaryAssignment> BeneficiaryAssignments => Set<BeneficiaryAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    /// <summary>
    /// Current user ID for tenant filtering.
    /// </summary>
    public Guid? CurrentUserId { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply global query filters for tenant isolation
        modelBuilder.Entity<LinkedAccount>()
            .HasQueryFilter(a => CurrentUserId == null || a.UserId == CurrentUserId);
            
        modelBuilder.Entity<Asset>()
            .HasQueryFilter(a => CurrentUserId == null || a.UserId == CurrentUserId);
            
        modelBuilder.Entity<Beneficiary>()
            .HasQueryFilter(b => CurrentUserId == null || b.UserId == CurrentUserId);
            
        modelBuilder.Entity<BeneficiaryAssignment>()
            .HasQueryFilter(ba => CurrentUserId == null || ba.Beneficiary.UserId == CurrentUserId);
        
        // Configure relationships
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        modelBuilder.Entity<LinkedAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PlaidItemId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.LinkedAccounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.AssetType });
            entity.HasOne(e => e.User)
                .WithMany(u => u.Assets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.OwnershipPercentage).HasPrecision(5, 2);
        });
        
        modelBuilder.Entity<Beneficiary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Beneficiaries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<BeneficiaryAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BeneficiaryId);
            entity.HasIndex(e => e.AssetId);
            entity.HasIndex(e => e.LinkedAccountId);
            entity.Property(e => e.AllocationPercentage).HasPrecision(5, 2);
        });
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
        
        return base.SaveChangesAsync(cancellationToken);
    }
}

public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

---

## Part 17: NuGet Packages

### RAJFinancial.Shared

```xml
<ItemGroup>
    <PackageReference Include="MemoryPack" Version="1.21.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
</ItemGroup>
```

### RAJFinancial.Api

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
    
    <!-- Integrations -->
    <PackageReference Include="Going.Plaid" Version="6.0.0" />
    <PackageReference Include="Anthropic.SDK" Version="1.0.0" />
    
    <!-- Observability -->
    <PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
    
    <!-- Security -->
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
</ItemGroup>
```

### RAJFinancial.Infrastructure

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
    <PackageReference Include="Going.Plaid" Version="6.0.0" />
    <PackageReference Include="Anthropic.SDK" Version="1.0.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
</ItemGroup>
```

---

## Part 18: Configuration Files

### local.settings.json (API)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "Serialization:UseMemoryPackInProduction": "true",
    "SqlConnection": "Server=localhost;Database=RAJFinancial;Trusted_Connection=True;TrustServerCertificate=True;",
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

### host.json (API)

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    },
    "logLevel": {
      "default": "Information",
      "Host.Results": "Error",
      "Function": "Information",
      "Host.Aggregator": "Trace"
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
```

---

## Part 19: Performance Benefits - MemoryPack

### Serialization Comparison

| Operation | JSON | MemoryPack | Improvement |
|-----------|------|------------|-------------|
| Serialize 1000 Assets | ~15ms | ~2ms | **7.5x faster** |
| Deserialize 1000 Assets | ~12ms | ~1.5ms | **8x faster** |
| Payload size (1000 Assets) | ~850KB | ~320KB | **62% smaller** |
| Memory allocation | Higher | Near-zero | Significant GC reduction |

### When MemoryPack Matters

- Transaction history (thousands of records)
- Portfolio snapshots with historical data
- Bulk exports
- Real-time dashboard updates
- Large asset portfolios

### Serialization Strategy

```
Development:  Client ←→ JSON ←→ API (easy debugging)
Production:   Client ←→ MemoryPack ←→ API (performance)
```

---

## Summary

This API & Integrations guide provides:

1. **Complete Solution Architecture** - Azure Functions, EF Core, Clean Architecture
2. **Shared DTOs with MemoryPack** - Dual serialization support
3. **Middleware** - Content negotiation, exception handling
4. **Plaid Integration** - Full account linking, webhooks, sync
5. **Claude AI Service** - Insight generation with compliance framing
6. **Service Interfaces** - Account, Asset, Beneficiary, Analysis
7. **Domain Entities** - User, LinkedAccount, Asset, Beneficiary, Assignment
8. **Database Context** - Tenant isolation, audit trails
9. **Configuration** - NuGet packages, settings files

All implementations follow:
- Tenant isolation for multi-user security
- Audit logging for compliance
- Structured logging with correlation IDs
- OpenAPI documentation for all endpoints
- Proper error handling with standardized codes

