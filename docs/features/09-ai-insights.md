# 09 — AI Insights & Document Processing

> Claude AI integration architecture, BYOK key model, financial statement parsing, AI-powered spending analysis, document upload & processing, and rate limiting per subscription tier.

**ADO Tracking:** [Epic #393 — 09 - AI Insights & Document Processing](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/393)

| # | Feature | State |
|---|---------|-------|
| [394](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/394) | AI Insights UI | New |
| [395](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/395) | Strategy Sources & RAG | New |
| [396](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/396) | Claude AI Integration | New |
| [528](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/528) | Document Processing & Statement Parsing | New |

---

## Overview

RAJ Financial integrates **Claude AI** (Anthropic) to provide:

1. **Financial statement parsing** — Upload bank/brokerage statements and extract transactions automatically
2. **Spending insights** — AI-generated analysis of spending patterns, anomalies, and recommendations
3. **Asset recommendations** — Suggestions for portfolio diversification and estate planning improvements
4. **Document understanding** — Extract structured data from scanned financial documents

The AI architecture follows a **BYOK (Bring Your Own Key)** model on the free tier and a **platform-managed key** on Premium, ensuring the platform can offer AI capabilities without bearing API costs for free users.

---

## Design Goals

| Goal | Description |
|------|-------------|
| **BYOK-first** | Free users supply their own Anthropic API key — zero AI cost to the platform |
| **Premium escalation** | Premium users get a platform-managed key with higher limits |
| **Fail-safe** | AI features degrade gracefully — the platform works fully without AI |
| **Privacy-conscious** | Minimize PII sent to Claude; never send SSN/EIN/account numbers |
| **Observable** | Every AI call is logged with token usage, latency, and outcome |

---

## Architecture

### Key Management Flow

```
┌─────────────┐     ┌──────────────────┐     ┌───────────────┐
│  Free User  │────▶│  User provides   │────▶│  Stored in    │
│  (BYOK)     │     │  own Anthropic   │     │  Azure Key    │
│             │     │  API key         │     │  Vault (per   │
│             │     │                  │     │  user secret) │
└─────────────┘     └──────────────────┘     └───────┬───────┘
                                                      │
                                                      ▼
┌─────────────┐     ┌──────────────────┐     ┌───────────────┐
│  Premium    │────▶│  Platform key    │────▶│  Key Vault    │
│  User       │     │  used by default │     │  (shared      │
│             │     │                  │     │  platform key)│
└─────────────┘     └──────────────────┘     └───────┬───────┘
                                                      │
                              ┌────────────────────────┘
                              ▼
                    ┌──────────────────┐     ┌───────────────┐
                    │  IAiKeyResolver  │────▶│  Anthropic    │
                    │  selects key per │     │  Claude API   │
                    │  user/tier       │     │  (claude-     │
                    │                  │     │  sonnet-4-5-  │
                    │                  │     │  20250929)    │
                    └──────────────────┘     └───────────────┘
```

### Model Configuration

| Setting | Value | Source |
|---------|-------|--------|
| Model | `claude-sonnet-4-5-20250929` | `local.settings.json` → `Claude:Model` |
| Platform API Key | Azure Key Vault | Secret: `Claude--ApiKey` |
| User BYOK Key | Azure Key Vault | Secret: `claude-byok-{userId}` |
| Max Tokens (parse) | 4096 | Configurable |
| Max Tokens (insight) | 2048 | Configurable |
| Temperature (parse) | 0.1 | Low for accurate extraction |
| Temperature (insight) | 0.7 | Higher for creative analysis |

---

## Entities

### AiUsageRecord

Tracks every AI API call for rate limiting, cost analysis, and audit.

```csharp
/// <summary>
/// Records each AI API call for rate limiting and cost tracking.
/// </summary>
public class AiUsageRecord
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Parse, Insight, Chat — categorizes the AI operation.</summary>
    public AiOperationType OperationType { get; set; }

    /// <summary>claude-sonnet-4-5-20250929 etc.</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Whether the user's BYOK key was used (true) or platform key (false).</summary>
    public bool UsedByokKey { get; set; }

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    /// <summary>Milliseconds for the API call.</summary>
    public int LatencyMs { get; set; }

    /// <summary>True if the call completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Error code if the call failed (e.g., AI_RATE_LIMITED).</summary>
    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Categories of AI operations for rate limiting and analytics.
/// </summary>
public enum AiOperationType
{
    StatementParse = 0,
    SpendingInsight = 1,
    AssetRecommendation = 2,
    DocumentExtraction = 3,
    Chat = 4
}
```

### EF Core Configuration

```csharp
public class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        builder.ToTable("AiUsageRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).HasMaxLength(128).IsRequired();
        builder.Property(r => r.TenantId).HasMaxLength(128).IsRequired();
        builder.Property(r => r.ModelId).HasMaxLength(64).IsRequired();
        builder.Property(r => r.ErrorCode).HasMaxLength(64);

        // Index for rate-limit queries: user + operation + date
        builder.HasIndex(r => new { r.UserId, r.OperationType, r.CreatedAt })
            .HasDatabaseName("IX_AiUsage_User_Op_Date");

        // Index for cost analysis
        builder.HasIndex(r => new { r.TenantId, r.CreatedAt })
            .HasDatabaseName("IX_AiUsage_Tenant_Date");
    }
}
```

### DocumentUpload

Tracks uploaded financial documents for AI processing.

```csharp
/// <summary>
/// Represents a financial document uploaded for AI processing.
/// </summary>
public class DocumentUpload
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Original file name as uploaded by the user.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type (application/pdf, image/png, etc.).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Blob Storage path: documents/{userId}/{id}/{fileName}.</summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>Processing status of the document.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    /// <summary>Type of document (for choosing the right parser prompt).</summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>Id of the linked asset, if this document is associated with one.</summary>
    public Guid? LinkedAssetId { get; set; }

    /// <summary>Number of transactions/items extracted.</summary>
    public int? ExtractedItemCount { get; set; }

    /// <summary>AI usage record for the processing call.</summary>
    public Guid? AiUsageRecordId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PartiallyExtracted = 4
}

public enum DocumentType
{
    BankStatement = 0,
    BrokerageStatement = 1,
    CreditCardStatement = 2,
    TaxDocument = 3,
    InsurancePolicy = 4,
    PropertyAppraisal = 5,
    Other = 99
}
```

---

## Service Interfaces

### IAiKeyResolver

```csharp
/// <summary>
/// Resolves the appropriate Anthropic API key for a given user based on their
/// subscription tier and BYOK configuration.
/// </summary>
public interface IAiKeyResolver
{
    /// <summary>
    /// Get the API key to use for the given user.
    /// Returns the platform key for Premium users, or the user's BYOK key for Free users.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <returns>The resolved API key and whether it's a BYOK key.</returns>
    /// <exception cref="NotFoundException">User has no BYOK key configured and is on Free tier.</exception>
    Task<(string ApiKey, bool IsByok)> ResolveKeyAsync(string userId);
}
```

### IAiRateLimiter

```csharp
/// <summary>
/// Enforces per-user, per-tier rate limits on AI operations.
/// </summary>
public interface IAiRateLimiter
{
    /// <summary>
    /// Checks if the user can perform the given AI operation within their tier limits.
    /// </summary>
    /// <returns>True if the operation is allowed; false if rate-limited.</returns>
    Task<bool> CanExecuteAsync(string userId, AiOperationType operationType);

    /// <summary>
    /// Records a completed AI operation for rate-limiting tracking.
    /// </summary>
    Task RecordUsageAsync(string userId, AiOperationType operationType);

    /// <summary>
    /// Returns the user's current usage and remaining quota for each operation type.
    /// </summary>
    Task<AiUsageSummaryDto> GetUsageSummaryAsync(string userId);
}
```

### IAiInsightsService

```csharp
/// <summary>
/// Core AI service for financial insights and document processing.
/// </summary>
public interface IAiInsightsService
{
    /// <summary>
    /// Parses a financial statement document, extracting transactions and metadata.
    /// </summary>
    /// <param name="userId">Authenticated user ID.</param>
    /// <param name="documentId">The uploaded document ID.</param>
    /// <returns>Parsed result with extracted transactions.</returns>
    Task<StatementParseResultDto> ParseStatementAsync(string userId, Guid documentId);

    /// <summary>
    /// Generates spending insights for the given time period.
    /// Analyzes transaction patterns, anomalies, and trends.
    /// </summary>
    Task<SpendingInsightDto> GenerateSpendingInsightAsync(
        string userId, DateOnly from, DateOnly to);

    /// <summary>
    /// Generates asset and portfolio recommendations based on current holdings.
    /// </summary>
    Task<AssetRecommendationDto> GenerateAssetRecommendationsAsync(string userId);

    /// <summary>
    /// Extracts structured data from a generic financial document.
    /// </summary>
    Task<DocumentExtractionResultDto> ExtractDocumentDataAsync(
        string userId, Guid documentId);
}
```

### IDocumentUploadService

```csharp
/// <summary>
/// Manages document uploads to Blob Storage for AI processing.
/// </summary>
public interface IDocumentUploadService
{
    /// <summary>Uploads a document to Blob Storage and creates the tracking record.</summary>
    Task<DocumentUploadDto> UploadDocumentAsync(
        string userId, Stream fileStream, string fileName,
        string contentType, DocumentType documentType, Guid? linkedAssetId);

    /// <summary>Gets all documents for a user, ordered by upload date descending.</summary>
    Task<List<DocumentUploadDto>> GetDocumentsAsync(string userId);

    /// <summary>Gets a specific document by ID.</summary>
    Task<DocumentUploadDto> GetDocumentAsync(string userId, Guid documentId);

    /// <summary>Deletes a document from storage and removes the tracking record.</summary>
    Task DeleteDocumentAsync(string userId, Guid documentId);

    /// <summary>Generates a time-limited SAS URL for downloading the document.</summary>
    Task<string> GetDocumentDownloadUrlAsync(string userId, Guid documentId);
}
```

---

## DTOs

### Statement Parsing

```csharp
/// <summary>
/// Result of AI-powered financial statement parsing.
/// </summary>
public sealed record StatementParseResultDto
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }

    /// <summary>Institution name extracted from the statement.</summary>
    public string? InstitutionName { get; init; }

    /// <summary>Account number (masked — last 4 only).</summary>
    public string? AccountNumberMasked { get; init; }

    /// <summary>Statement period extracted from the document.</summary>
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }

    /// <summary>Opening and closing balances, if found.</summary>
    public decimal? OpeningBalance { get; init; }
    public decimal? ClosingBalance { get; init; }

    /// <summary>Extracted transactions ready for review/import.</summary>
    public List<ExtractedTransactionDto> Transactions { get; init; } = [];

    /// <summary>Confidence score (0.0–1.0) of the overall extraction.</summary>
    public decimal Confidence { get; init; }

    /// <summary>Any warnings (e.g., "Could not parse 3 line items").</summary>
    public List<string> Warnings { get; init; } = [];

    /// <summary>Token usage for this parse operation.</summary>
    public AiTokenUsageDto TokenUsage { get; init; } = null!;
}

/// <summary>
/// A single transaction extracted from a parsed statement.
/// </summary>
public sealed record ExtractedTransactionDto
{
    public DateOnly Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }

    /// <summary>AI-inferred category (Groceries, Utilities, etc.).</summary>
    public string? InferredCategory { get; init; }

    /// <summary>AI-inferred merchant name (cleaned from raw description).</summary>
    public string? InferredMerchant { get; init; }

    /// <summary>Confidence for this specific transaction (0.0–1.0).</summary>
    public decimal Confidence { get; init; }

    /// <summary>True if this transaction looks like a duplicate of existing data.</summary>
    public bool PossibleDuplicate { get; init; }
}

/// <summary>
/// Token usage breakdown for an AI call.
/// </summary>
public sealed record AiTokenUsageDto
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int LatencyMs { get; init; }
}
```

### Spending Insights

```csharp
/// <summary>
/// AI-generated spending insights for a given period.
/// </summary>
public sealed record SpendingInsightDto
{
    /// <summary>Human-readable summary paragraph.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Key findings and patterns.</summary>
    public List<InsightFinding> Findings { get; init; } = [];

    /// <summary>Actionable recommendations.</summary>
    public List<InsightRecommendation> Recommendations { get; init; } = [];

    /// <summary>Anomalies detected (unusual charges, spikes, etc.).</summary>
    public List<SpendingAnomaly> Anomalies { get; init; } = [];

    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public AiTokenUsageDto TokenUsage { get; init; } = null!;
}

/// <summary>
/// A specific finding from spending analysis.
/// </summary>
public sealed record InsightFinding
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    /// <summary>Positive = good, Negative = concerning, Neutral = informational.</summary>
    public string Sentiment { get; init; } = string.Empty;
}

/// <summary>
/// An actionable recommendation from AI analysis.
/// </summary>
public sealed record InsightRecommendation
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>Estimated monthly savings if implemented.</summary>
    public decimal? EstimatedMonthlySavings { get; init; }

    /// <summary>High, Medium, Low priority.</summary>
    public string Priority { get; init; } = string.Empty;
}

/// <summary>
/// An unusual spending pattern detected by AI.
/// </summary>
public sealed record SpendingAnomaly
{
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string Category { get; init; } = string.Empty;

    /// <summary>Why this was flagged as anomalous.</summary>
    public string Reason { get; init; } = string.Empty;
}
```

### Asset Recommendations

```csharp
/// <summary>
/// AI-generated portfolio and asset recommendations.
/// </summary>
public sealed record AssetRecommendationDto
{
    /// <summary>Overall portfolio health assessment.</summary>
    public string OverallAssessment { get; init; } = string.Empty;

    /// <summary>Specific recommendations for portfolio improvement.</summary>
    public List<InsightRecommendation> Recommendations { get; init; } = [];

    /// <summary>Diversification analysis by asset type.</summary>
    public List<DiversificationInsight> Diversification { get; init; } = [];

    public AiTokenUsageDto TokenUsage { get; init; } = null!;
}

/// <summary>
/// Diversification insight for a specific asset type.
/// </summary>
public sealed record DiversificationInsight
{
    public string AssetType { get; init; } = string.Empty;
    public decimal CurrentAllocationPercent { get; init; }
    public string Assessment { get; init; } = string.Empty;  // "Over-allocated", "Under-allocated", "Balanced"
}
```

### Document Upload

```csharp
/// <summary>
/// Document metadata returned from upload/query operations.
/// </summary>
public sealed record DocumentUploadDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DocumentStatus Status { get; init; }
    public DocumentType DocumentType { get; init; }
    public Guid? LinkedAssetId { get; init; }
    public int? ExtractedItemCount { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
```

### AI Usage Summary

```csharp
/// <summary>
/// User's current AI usage and remaining quota.
/// </summary>
public sealed record AiUsageSummaryDto
{
    public string Tier { get; init; } = string.Empty;
    public bool UsingByokKey { get; init; }
    public List<OperationQuota> Quotas { get; init; } = [];
    public int TotalCallsThisMonth { get; init; }
    public int TotalTokensThisMonth { get; init; }
}

/// <summary>
/// Quota details for a single operation type.
/// </summary>
public sealed record OperationQuota
{
    public AiOperationType OperationType { get; init; }
    public int Used { get; init; }
    public int Limit { get; init; }       // -1 = unlimited
    public int Remaining { get; init; }    // -1 = unlimited
    public string Period { get; init; } = "month";
}
```

---

## Rate Limits

### Per-Tier Limits

| Operation | Free (BYOK) | Premium (Platform Key) |
|-----------|-------------|----------------------|
| Statement parsing | 5/month | Unlimited |
| Spending insights | 3/month | Unlimited |
| Asset recommendations | 3/month | Unlimited |
| Document extraction | 5/month | Unlimited |
| Chat | — | Unlimited |

> Free-tier limits apply to **BYOK users** — they cover their own API costs, but the platform limits excessive usage to prevent abuse and keep infrastructure overhead low.

### Rate Limiting Implementation

```csharp
public class AiRateLimiter : IAiRateLimiter
{
    private readonly RajFinancialDbContext _db;
    private readonly ISubscriptionService _subscriptionService;

    // Tier limits per operation type per month
    private static readonly Dictionary<AiOperationType, int> FreeTierLimits = new()
    {
        [AiOperationType.StatementParse] = 5,
        [AiOperationType.SpendingInsight] = 3,
        [AiOperationType.AssetRecommendation] = 3,
        [AiOperationType.DocumentExtraction] = 5,
        [AiOperationType.Chat] = 0  // Not available on free tier
    };

    public async Task<bool> CanExecuteAsync(
        string userId, AiOperationType operationType)
    {
        var tier = await _subscriptionService.GetTierAsync(userId);
        if (tier == SubscriptionTier.Premium)
            return true;  // Unlimited for premium

        if (!FreeTierLimits.TryGetValue(operationType, out var limit))
            return false;

        if (limit == 0) return false;

        var monthStart = new DateTime(
            DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usageCount = await _db.AiUsageRecords
            .CountAsync(r => r.UserId == userId
                && r.OperationType == operationType
                && r.Success
                && r.CreatedAt >= monthStart);

        return usageCount < limit;
    }
}
```

---

## Prompt Engineering

### Statement Parsing Prompt

AI prompts are kept as **system prompts** in the codebase (not stored in DB) for versioning and review:

```csharp
/// <summary>
/// Prompt templates for AI operations. All prompts are versioned in code.
/// </summary>
public static class AiPrompts
{
    /// <summary>
    /// System prompt for financial statement parsing.
    /// Instructs Claude to extract structured data from statement images/PDFs.
    /// </summary>
    public const string StatementParseSystem = """
        You are a financial statement parser. Extract transaction data from the
        provided document with high accuracy.

        Rules:
        - Extract each transaction as: date, description, amount (positive=credit, negative=debit)
        - Infer the merchant name from the description (clean up abbreviations)
        - Categorize each transaction (Groceries, Dining, Utilities, Transportation, etc.)
        - Extract statement metadata: institution name, account number (last 4 only),
          period start/end dates, opening/closing balances
        - If you cannot parse a line item, skip it and add a warning
        - NEVER include full account numbers — only last 4 digits
        - Respond in the exact JSON format specified

        Output JSON schema:
        {
          "institutionName": "string",
          "accountNumberLast4": "string",
          "periodStart": "YYYY-MM-DD",
          "periodEnd": "YYYY-MM-DD",
          "openingBalance": number,
          "closingBalance": number,
          "transactions": [
            {
              "date": "YYYY-MM-DD",
              "description": "raw description",
              "amount": number,
              "inferredCategory": "string",
              "inferredMerchant": "string",
              "confidence": number (0.0-1.0)
            }
          ],
          "warnings": ["string"],
          "overallConfidence": number (0.0-1.0)
        }
        """;

    /// <summary>
    /// System prompt for spending insights generation.
    /// </summary>
    public const string SpendingInsightSystem = """
        You are a personal finance advisor analyzing spending patterns.
        Provide actionable, specific insights based on the transaction data provided.

        Rules:
        - Be specific — reference actual amounts and merchants
        - Identify patterns (recurring charges, spending spikes, category trends)
        - Flag anomalies (unusual amounts, new merchants, duplicate charges)
        - Provide practical recommendations with estimated savings
        - Be encouraging but honest
        - NEVER reference account numbers, SSN, or other PII
        - Respond in the exact JSON format specified

        Output JSON schema:
        {
          "summary": "string (3-5 sentences)",
          "findings": [
            { "title": "string", "description": "string", "category": "string", "sentiment": "Positive|Negative|Neutral" }
          ],
          "recommendations": [
            { "title": "string", "description": "string", "estimatedMonthlySavings": number|null, "priority": "High|Medium|Low" }
          ],
          "anomalies": [
            { "description": "string", "amount": number, "date": "YYYY-MM-DD", "category": "string", "reason": "string" }
          ]
        }
        """;
}
```

### Privacy Safeguards in Prompts

Before sending data to Claude, the service **sanitizes** all PII:

```csharp
/// <summary>
/// Sanitizes transaction data before sending to Claude API.
/// Removes all PII and sensitive identifiers.
/// </summary>
public static class AiDataSanitizer
{
    public static object SanitizeTransactions(
        IEnumerable<TransactionDto> transactions)
    {
        return transactions.Select(t => new
        {
            t.Date,
            t.Description,
            t.Amount,
            t.Category,
            t.MerchantName
            // Excluded: AccountId, PlaidTransactionId, UserId, etc.
        });
    }

    public static object SanitizeAssets(
        IEnumerable<AssetSummaryDto> assets)
    {
        return assets.Select(a => new
        {
            a.AssetType,
            a.Name,
            a.CurrentValue,
            a.PurchasePrice,
            a.PurchaseDate,
            a.IsDepreciable
            // Excluded: UserId, TenantId, AccountNumber, etc.
        });
    }
}
```

---

## API Endpoints

| Method | Route | Description | Tier |
|--------|-------|-------------|------|
| `POST` | `/api/ai/parse/{documentId}` | Parse a financial statement | All (rate-limited) |
| `POST` | `/api/ai/insights/spending` | Generate spending insights | All (rate-limited) |
| `POST` | `/api/ai/insights/assets` | Generate asset recommendations | All (rate-limited) |
| `POST` | `/api/ai/extract/{documentId}` | Extract data from document | All (rate-limited) |
| `GET` | `/api/ai/usage` | Get current AI usage and quotas | All |
| `POST` | `/api/documents` | Upload a document | All |
| `GET` | `/api/documents` | List user's documents | All |
| `GET` | `/api/documents/{id}` | Get document details | All |
| `GET` | `/api/documents/{id}/download` | Get SAS download URL | All |
| `DELETE` | `/api/documents/{id}` | Delete a document | All |

### Request Bodies

#### POST /api/ai/insights/spending

```json
{
  "from": "2025-01-01",
  "to": "2025-01-31"
}
```

#### POST /api/documents

Multipart form data:
- `file` — The document file (PDF, PNG, JPG)
- `documentType` — Integer enum value (0-99)
- `linkedAssetId` — Optional GUID

---

## Azure Functions

### ParseStatement

```csharp
/// <summary>
/// Parses a previously uploaded financial statement using Claude AI.
/// Extracts transactions and metadata for review/import.
/// </summary>
/// <response code="200">Parse result with extracted transactions.</response>
/// <response code="402">BYOK key not configured (free tier) or rate limit reached.</response>
/// <response code="429">Monthly AI usage limit reached.</response>
[Function("ParseStatement")]
[Authorize]
public async Task<HttpResponseData> ParseStatement(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/parse/{documentId}")]
    HttpRequestData req,
    string documentId,
    FunctionContext context)
{
    var userId = context.GetUserId();
    var docId = Guid.Parse(documentId);

    // Rate limit check
    if (!await _rateLimiter.CanExecuteAsync(userId, AiOperationType.StatementParse))
    {
        return req.CreateErrorResponse(
            HttpStatusCode.TooManyRequests,
            ErrorCodes.AI_RATE_LIMITED,
            "Monthly statement parsing limit reached");
    }

    var result = await _aiService.ParseStatementAsync(userId, docId);

    await _rateLimiter.RecordUsageAsync(userId, AiOperationType.StatementParse);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(result);
    return response;
}
```

### GenerateSpendingInsight

```csharp
/// <summary>
/// Generates AI-powered spending insights for the given period.
/// </summary>
[Function("GenerateSpendingInsight")]
[Authorize]
public async Task<HttpResponseData> GenerateSpendingInsight(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/insights/spending")]
    HttpRequestData req,
    FunctionContext context)
{
    var userId = context.GetUserId();
    var body = await req.ReadFromJsonAsync<SpendingInsightRequest>();

    if (!await _rateLimiter.CanExecuteAsync(userId, AiOperationType.SpendingInsight))
    {
        return req.CreateErrorResponse(
            HttpStatusCode.TooManyRequests,
            ErrorCodes.AI_RATE_LIMITED,
            "Monthly insight limit reached");
    }

    var insight = await _aiService.GenerateSpendingInsightAsync(
        userId, body!.From, body.To);

    await _rateLimiter.RecordUsageAsync(userId, AiOperationType.SpendingInsight);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(insight);
    return response;
}

public sealed record SpendingInsightRequest
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}
```

### UploadDocument

```csharp
/// <summary>
/// Uploads a financial document for AI processing.
/// Stores in Azure Blob Storage and creates tracking record.
/// </summary>
[Function("UploadDocument")]
[Authorize]
public async Task<HttpResponseData> UploadDocument(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents")]
    HttpRequestData req,
    FunctionContext context)
{
    var userId = context.GetUserId();
    var formData = await req.ReadMultipartFormDataAsync();

    var file = formData.Files.FirstOrDefault()
        ?? throw new ValidationException("No file provided");

    // Validate file
    ValidateUpload(file);

    var documentType = Enum.Parse<DocumentType>(
        formData.Fields["documentType"] ?? "99");
    Guid? linkedAssetId = formData.Fields.ContainsKey("linkedAssetId")
        ? Guid.Parse(formData.Fields["linkedAssetId"]!)
        : null;

    var document = await _documentService.UploadDocumentAsync(
        userId, file.OpenReadStream(), file.FileName,
        file.ContentType, documentType, linkedAssetId);

    var response = req.CreateResponse(HttpStatusCode.Created);
    await response.WriteAsJsonAsync(document);
    return response;
}

private static void ValidateUpload(IFormFile file)
{
    const long maxSize = 10 * 1024 * 1024; // 10 MB
    var allowedTypes = new[] {
        "application/pdf", "image/png", "image/jpeg", "image/webp" };

    if (file.Length > maxSize)
        throw new ValidationException("File size exceeds 10 MB limit");

    if (!allowedTypes.Contains(file.ContentType))
        throw new ValidationException(
            $"Unsupported file type: {file.ContentType}");
}
```

---

## BYOK Key Management

### User API Key Flow

```
User Settings Page
  └──▶ "Enter your Anthropic API key"
       └──▶ POST /api/settings/ai-key
            └──▶ Validate key against Anthropic API
                 └──▶ Store in Key Vault as "claude-byok-{userId}"
                      └──▶ Return success (key is masked in all UIs)
```

### API Endpoints for Key Management

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/settings/ai-key` | Set or update BYOK API key |
| `DELETE` | `/api/settings/ai-key` | Remove BYOK API key |
| `GET` | `/api/settings/ai-key/status` | Check if BYOK key is configured (never returns the key) |

### Key Validation

```csharp
/// <summary>
/// Validates an Anthropic API key by making a minimal API call.
/// </summary>
public async Task<bool> ValidateApiKeyAsync(string apiKey)
{
    try
    {
        using var client = new AnthropicClient(apiKey);
        // Minimal call to validate the key
        var response = await client.Messages.CreateAsync(new MessageRequest
        {
            Model = "claude-sonnet-4-5-20250929",
            MaxTokens = 5,
            Messages = [new("user", "hi")]
        });
        return true;
    }
    catch (AnthropicApiException ex) when (ex.StatusCode == 401)
    {
        return false;
    }
}
```

---

## Blob Storage Configuration

### Document Storage

| Setting | Value |
|---------|-------|
| Container | `documents` |
| Path pattern | `{userId}/{documentId}/{fileName}` |
| Access tier | Hot |
| Max file size | 10 MB |
| Allowed types | PDF, PNG, JPEG, WebP |
| SAS TTL | 15 minutes (download URLs) |
| Retention | Matches user's storage quota |

### Storage Quotas

| Tier | Total Storage | Document Count |
|------|--------------|----------------|
| Free | 100 MB | 5 documents/month parsed |
| Premium | 5 GB | Unlimited |

---

## TypeScript Types

```typescript
/** AI operation types for client-side rate limit display. */
type AiOperationType =
  | 'StatementParse'
  | 'SpendingInsight'
  | 'AssetRecommendation'
  | 'DocumentExtraction'
  | 'Chat';

/** Document processing status. */
type DocumentStatus =
  | 'Uploaded'
  | 'Processing'
  | 'Completed'
  | 'Failed'
  | 'PartiallyExtracted';

/** Document type for parser prompt selection. */
type DocumentType =
  | 'BankStatement'
  | 'BrokerageStatement'
  | 'CreditCardStatement'
  | 'TaxDocument'
  | 'InsurancePolicy'
  | 'PropertyAppraisal'
  | 'Other';

/** Statement parse result. */
interface StatementParseResultDto {
  documentId: string;
  fileName: string;
  documentType: DocumentType;
  institutionName?: string;
  accountNumberMasked?: string;
  periodStart?: string;
  periodEnd?: string;
  openingBalance?: number;
  closingBalance?: number;
  transactions: ExtractedTransactionDto[];
  confidence: number;
  warnings: string[];
  tokenUsage: AiTokenUsageDto;
}

interface ExtractedTransactionDto {
  date: string;
  description: string;
  amount: number;
  inferredCategory?: string;
  inferredMerchant?: string;
  confidence: number;
  possibleDuplicate: boolean;
}

interface AiTokenUsageDto {
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  latencyMs: number;
}

/** Spending insight generated by AI. */
interface SpendingInsightDto {
  summary: string;
  findings: InsightFinding[];
  recommendations: InsightRecommendation[];
  anomalies: SpendingAnomaly[];
  periodStart: string;
  periodEnd: string;
  tokenUsage: AiTokenUsageDto;
}

interface InsightFinding {
  title: string;
  description: string;
  category: string;
  sentiment: 'Positive' | 'Negative' | 'Neutral';
}

interface InsightRecommendation {
  title: string;
  description: string;
  estimatedMonthlySavings?: number;
  priority: 'High' | 'Medium' | 'Low';
}

interface SpendingAnomaly {
  description: string;
  amount: number;
  date: string;
  category: string;
  reason: string;
}

/** Asset recommendation generated by AI. */
interface AssetRecommendationDto {
  overallAssessment: string;
  recommendations: InsightRecommendation[];
  diversification: DiversificationInsight[];
  tokenUsage: AiTokenUsageDto;
}

interface DiversificationInsight {
  assetType: string;
  currentAllocationPercent: number;
  assessment: string;
}

/** Document upload metadata. */
interface DocumentUploadDto {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  status: DocumentStatus;
  documentType: DocumentType;
  linkedAssetId?: string;
  extractedItemCount?: number;
  uploadedAt: string;
  processedAt?: string;
}

/** AI usage and quota summary. */
interface AiUsageSummaryDto {
  tier: string;
  usingByokKey: boolean;
  quotas: OperationQuota[];
  totalCallsThisMonth: number;
  totalTokensThisMonth: number;
}

interface OperationQuota {
  operationType: AiOperationType;
  used: number;
  limit: number;       // -1 = unlimited
  remaining: number;   // -1 = unlimited
  period: string;
}
```

---

## React Query Hooks

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Fetches the user's AI usage summary and remaining quotas.
 *
 * @returns Usage data with per-operation quota breakdown.
 */
export function useAiUsage() {
  return useQuery({
    queryKey: ['ai', 'usage'],
    queryFn: fetchAiUsage,
    staleTime: 2 * 60 * 1000,  // 2 minutes
  });
}

/**
 * Fetches the user's uploaded documents.
 */
export function useDocuments() {
  return useQuery({
    queryKey: ['documents'],
    queryFn: fetchDocuments,
    staleTime: 30 * 1000,
  });
}

/**
 * Mutation to parse a financial statement via AI.
 * Invalidates usage and documents queries on success.
 */
export function useParseStatement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => parseStatement(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'usage'] });
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
}

/**
 * Mutation to generate spending insights via AI.
 */
export function useGenerateSpendingInsight() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { from: string; to: string }) =>
      generateSpendingInsight(params.from, params.to),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'usage'] });
    },
  });
}

/**
 * Mutation to upload a document.
 */
export function useUploadDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (formData: FormData) => uploadDocument(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
}

/**
 * Mutation to delete a document.
 */
export function useDeleteDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => deleteDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
}

/**
 * Checks if the user has a BYOK key configured.
 */
export function useByokKeyStatus() {
  return useQuery({
    queryKey: ['settings', 'ai-key-status'],
    queryFn: fetchByokKeyStatus,
    staleTime: 5 * 60 * 1000,
  });
}
```

---

## UI Layout

### AI Insights Page — Desktop

```
┌──────────────────────────────────────────────────────────┐
│  AI Insights                                      [Usage]│
├──────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐ ┌──────────────────────────────┐│
│  │ Upload & Parse       │ │  Spending Analysis           ││
│  │                      │ │                              ││
│  │ [Drop PDF/Image here]│ │  Period: [Jan 2025 ▼] [→]   ││
│  │ or [Browse files]    │ │                              ││
│  │                      │ │  "Your spending in January   ││
│  │ Recent Documents:    │ │   was $4,320. Dining out     ││
│  │ ┌──────────────────┐ │ │   increased 23% vs December"││
│  │ │ BofA Jan 2025    │ │ │                              ││
│  │ │ ✅ 47 txns       │ │ │  Findings:                  ││
│  │ │ [View] [Delete]  │ │ │  ✅ Subscription costs down  ││
│  │ ├──────────────────┤ │ │  ⚠️ Dining up 23%            ││
│  │ │ Chase Dec 2024   │ │ │  ℹ️ New merchant: Costco     ││
│  │ │ ⏳ Processing... │ │ │                              ││
│  │ └──────────────────┘ │ │  Recommendations:            ││
│  │                      │ │  💡 Switch to annual billing ││
│  │ Parse Quota: 3/5     │ │     Save ~$24/month          ││
│  └─────────────────────┘ └──────────────────────────────┘│
├──────────────────────────────────────────────────────────┤
│  Portfolio Recommendations                               │
│  "Your portfolio is concentrated in real estate (68%).   │
│   Consider diversifying into financial instruments."     │
│                                                          │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐           │
│  │ Real Estate│ │ Financial  │ │ Other      │           │
│  │ 68% ⚠️     │ │ 22%        │ │ 10%        │           │
│  │ Over-      │ │ Balanced   │ │ Under-     │           │
│  │ allocated  │ │            │ │ allocated  │           │
│  └────────────┘ └────────────┘ └────────────┘           │
└──────────────────────────────────────────────────────────┘
```

### Mobile Layout

Sections stack vertically:
1. Usage quota badges (horizontal scroll)
2. Document upload card
3. Recent documents list
4. Spending analysis accordion
5. Portfolio recommendations accordion

### Key Components

```tsx
/**
 * AI Insights page with document upload, parsing, and insight generation.
 */
export function AiInsightsPage() {
  const { t } = useTranslation();
  const { data: usage } = useAiUsage();
  const { data: keyStatus } = useByokKeyStatus();

  // If no BYOK key and free tier, show setup prompt
  if (keyStatus && !keyStatus.hasKey && !keyStatus.isPremium) {
    return <ByokSetupPrompt />;
  }

  return (
    <div className="space-y-4 md:space-y-6">
      {/* Usage quota display */}
      <AiUsageBar usage={usage} />

      {/* Two-column layout on desktop */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 md:gap-6">
        <DocumentUploadCard />
        <SpendingInsightCard />
      </div>

      {/* Portfolio recommendations — full width */}
      <PortfolioRecommendationsCard />
    </div>
  );
}
```

### BYOK Setup Prompt

```tsx
/**
 * Shown to free-tier users who haven't configured their Anthropic API key.
 * Guides them through setting up BYOK.
 */
export function ByokSetupPrompt() {
  const { t } = useTranslation();
  const [apiKey, setApiKey] = useState('');
  const [isValidating, setIsValidating] = useState(false);

  return (
    <Card className="max-w-lg mx-auto p-6 text-center">
      <Sparkles className="h-12 w-12 mx-auto text-primary mb-4" aria-hidden="true" />
      <h2 className="text-xl font-semibold mb-2">
        {t('ai.setup.title')}
      </h2>
      <p className="text-muted-foreground mb-6">
        {t('ai.setup.description')}
      </p>

      <div className="space-y-4 text-left">
        <div>
          <Label htmlFor="api-key">{t('ai.setup.keyLabel')}</Label>
          <Input
            id="api-key"
            type="password"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder="sk-ant-..."
            autoComplete="off"
          />
          <p className="text-xs text-muted-foreground mt-1">
            {t('ai.setup.keyHelp')}
          </p>
        </div>

        <Button
          onClick={handleSaveKey}
          disabled={!apiKey || isValidating}
          className="w-full"
          aria-busy={isValidating}
        >
          {isValidating ? t('ai.setup.validating') : t('ai.setup.save')}
        </Button>
      </div>

      <Separator className="my-6" />

      <p className="text-sm text-muted-foreground">
        {t('ai.setup.premiumCta')}
      </p>
      <Button variant="outline" className="mt-2">
        {t('ai.setup.upgradeToPremium')}
      </Button>
    </Card>
  );
}
```

---

## Statement Parse Review Flow

After parsing, users review extracted transactions before importing:

```
1. Upload Statement
   └──▶ 2. AI Parses (async, status polling)
        └──▶ 3. Review Extracted Transactions
             ├── ✅ Confirm correct transactions
             ├── ✏️ Edit mis-parsed items
             ├── ❌ Dismiss false positives
             └──▶ 4. Import Selected Transactions
                  └──▶ Transactions added to linked account
```

### Transaction Import

```csharp
/// <summary>
/// Imports reviewed transactions from a statement parse into the user's account.
/// </summary>
public interface ITransactionImportService
{
    /// <summary>
    /// Imports selected transactions from a parse result into the specified account.
    /// Performs duplicate detection against existing transactions.
    /// </summary>
    Task<TransactionImportResultDto> ImportTransactionsAsync(
        string userId, Guid documentId, TransactionImportRequest request);
}

public sealed record TransactionImportRequest
{
    /// <summary>The linked account to import transactions into.</summary>
    public Guid LinkedAccountId { get; init; }

    /// <summary>Indices of transactions from the parse result to import.</summary>
    public List<int> SelectedTransactionIndices { get; init; } = [];

    /// <summary>User-edited overrides (index → corrected values).</summary>
    public Dictionary<int, TransactionOverride>? Overrides { get; init; }
}

public sealed record TransactionOverride
{
    public DateOnly? Date { get; init; }
    public string? Description { get; init; }
    public decimal? Amount { get; init; }
    public string? Category { get; init; }
}

public sealed record TransactionImportResultDto
{
    public int TotalSelected { get; init; }
    public int Imported { get; init; }
    public int Duplicates { get; init; }
    public int Errors { get; init; }
    public List<string> ErrorDetails { get; init; } = [];
}
```

---

## Tier Gating

| Feature | Free (BYOK) | Premium |
|---------|-------------|---------|
| Statement parsing | ✅ (5/month) | ✅ (Unlimited) |
| Spending insights | ✅ (3/month) | ✅ (Unlimited) |
| Asset recommendations | ✅ (3/month) | ✅ (Unlimited) |
| Document extraction | ✅ (5/month) | ✅ (Unlimited) |
| AI Chat | ❌ | ✅ |
| BYOK key management | ✅ | ✅ (optional, uses platform key) |
| Document storage | 100 MB | 5 GB |
| Usage analytics | Basic | Full |

---

## Caching Strategy

| Data | Cache TTL | Notes |
|------|-----------|-------|
| AI usage summary | 2 min (Redis) | Invalidated on each AI call |
| Parse results | Permanent (DB) | Stored in DocumentUpload record |
| Generated insights | Not cached | Different each time, cost-tracked |
| BYOK key status | 5 min (Redis) | Invalidated on key set/delete |

### Cache Key Pattern

```
ai:usage:{userId}             → AiUsageSummaryDto
ai:byok-status:{userId}       → Boolean (has key configured)
```

---

## Security & Access Control

### Authorization Model

| Tier | Access Level | Description |
|------|-------------|-------------|
| **Owner** | Full access | User manages their own AI key, documents, and insights |
| **Granted** | Read-only insights | Advisor with `DataAccessGrant` including `Insights` category can view previous analyses |
| **Administrator** | Usage monitoring | Platform admins can view aggregate usage, not individual AI results |

### Data Privacy

- **Never send PII to Claude** — SSN, EIN, full account numbers are stripped
- **BYOK keys are write-only** — once stored in Key Vault, the key value cannot be retrieved (only tested)
- **API keys are never logged** — even in structured logs, the key is excluded
- **Document content stays in Azure** — Blob Storage is in the same region; no external storage
- **AI responses are not stored long-term** — parse results are linked to documents; insights are returned and discarded (user can save manually)

### Audit Logging

| Event | Logged Data | Severity |
|-------|------------|----------|
| AI operation executed | UserId, OperationType, TokenCount, LatencyMs, Success | Information |
| AI operation failed | UserId, OperationType, ErrorCode | Warning |
| BYOK key configured | UserId | Information |
| BYOK key removed | UserId | Information |
| Document uploaded | UserId, FileName, ContentType, SizeBytes | Information |
| Document deleted | UserId, DocumentId | Information |
| Rate limit hit | UserId, OperationType, CurrentCount, Limit | Warning |
| Unauthorized AI access attempt | RequestingUserId, TargetUserId | **Warning** |

---

## Error Codes

| Code | HTTP | Condition |
|------|------|-----------|
| `AUTH_REQUIRED` | 401 | Not authenticated |
| `AUTH_FORBIDDEN` | 403 | No DataAccessGrant for target user's Insights category |
| `AI_SERVICE_UNAVAILABLE` | 503 | Claude API is down or unreachable |
| `AI_RATE_LIMITED` | 429 | Monthly AI usage limit reached |
| `AI_INVALID_KEY` | 401 | BYOK key is invalid or expired |
| `VALIDATION_FAILED` | 400 | Invalid input (bad date range, unsupported file type) |
| `RESOURCE_NOT_FOUND` | 404 | Document ID does not exist |
| `TIER_LIMIT_REACHED` | 429 | Storage quota exceeded |
| `SERVER_ERROR` | 500 | Internal error during AI processing |

---

## Observability

### Structured Logging

```csharp
_logger.LogInformation(
    "AI operation completed: {OperationType} for {UserId} — " +
    "{InputTokens}/{OutputTokens} tokens, {LatencyMs}ms, BYOK={IsByok}",
    operationType, userId, inputTokens, outputTokens, latencyMs, isByok);

_logger.LogWarning(
    "AI rate limit hit: {UserId} attempted {OperationType}, " +
    "usage={CurrentCount}/{Limit}",
    userId, operationType, currentCount, limit);
```

### Application Insights Metrics

| Metric | Type | Dimensions |
|--------|------|-----------|
| `ai.operation.duration` | Histogram | OperationType, Success, IsByok |
| `ai.operation.tokens` | Counter | OperationType, TokenType (input/output) |
| `ai.operation.count` | Counter | OperationType, Success |
| `ai.ratelimit.hit` | Counter | OperationType, Tier |
| `document.upload.size` | Histogram | DocumentType |
| `document.upload.count` | Counter | DocumentType |

---

## Cross-References

- Platform infrastructure & Claude config: [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Authorization & `DataCategory.Insights`: [03-authorization-data-access.md](03-authorization-data-access.md)
- Transaction model (imported from parse): [06-accounts-transactions.md](06-accounts-transactions.md)
- Dashboard integration: [07-dashboard-reporting.md](07-dashboard-reporting.md)
- BYOK key management UI: [10-user-profile-settings.md](10-user-profile-settings.md)
- Asset model (for portfolio recommendations): [05-assets-portfolio.md](05-assets-portfolio.md)
- Error codes: [01-platform-infrastructure.md](01-platform-infrastructure.md) — `ErrorCodes` static class

---

*Last Updated: February 2026*
