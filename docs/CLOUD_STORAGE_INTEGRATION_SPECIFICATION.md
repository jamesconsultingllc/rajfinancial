# Cloud Storage Integration Specification

> **Last Updated**: March 7, 2026
> **Status**: Draft
> **Depends On**: LinkedAccount entity (spec'd in `TRANSACTION_STORAGE_SPECIFICATION.md`), Azure Document Intelligence (external service), Microsoft Graph API / Google Drive API / Dropbox API (external services)
> **Related**: `TRANSACTION_STORAGE_SPECIFICATION.md` (transaction pipeline), `ASSET_TYPE_SPECIFICATIONS.md` (asset model), `docs/features/06-accounts-transactions.md` (epic tracking)

---

## Design Principle

Instead of users uploading financial statements directly (creating storage liability and compliance burden), users **connect their existing cloud storage** (OneDrive, Google Drive, Dropbox) via OAuth. The app scans for financial documents (bank statements, brokerage statements, tax forms), extracts structured data using **Azure Document Intelligence**, and populates the existing `LinkedAccount` and `Transaction` entities with `Source = DocumentImport`.

Documents **never leave the user's cloud storage** — RAJ Financial only reads them for processing and stores the extracted structured data. This minimizes PII liability and leverages storage users already have.

---

## Table of Contents

1. [Design Decisions](#design-decisions)
2. [Architecture Overview](#architecture-overview)
3. [Cloud Storage Connection Entity](#cloud-storage-connection-entity)
4. [Document Scan Record Entity](#document-scan-record-entity)
5. [Extracted Statement Entity](#extracted-statement-entity)
6. [Enum Definitions](#enum-definitions)
7. [DTOs & Contracts](#dtos--contracts)
8. [API Endpoints](#api-endpoints)
9. [Service Layer](#service-layer)
10. [OAuth Flows](#oauth-flows)
11. [Document Discovery Pipeline](#document-discovery-pipeline)
12. [Document Parsing Pipeline](#document-parsing-pipeline)
13. [Transaction Extraction & Mapping](#transaction-extraction--mapping)
14. [Tier Gating & Access Control](#tier-gating--access-control)
15. [Security & Privacy](#security--privacy)
16. [Error Codes](#error-codes)
17. [Validation Rules](#validation-rules)
18. [UI Design](#ui-design)
19. [Cost Analysis](#cost-analysis)
20. [Future Considerations](#future-considerations)

---

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Document storage | **User's cloud — never copied** | Minimizes PII liability; user retains document custody |
| Parsing engine | **Azure Document Intelligence** (prebuilt invoice/receipt models + custom model for bank statements) | Best .NET/Azure integration; supports 100+ statement formats |
| Token storage | **Encrypted refresh tokens in DB** (AES-256, keys in Key Vault) | Consistent with Plaid access token pattern |
| Integration with existing model | **Extends `LinkedAccount` + `Transaction`** with `Source = DocumentImport` | No parallel entity graph; same queries, same UI, same authorization |
| User confirmation | **Required before committing** | AI extraction isn't perfect; user reviews extracted data before it's saved |
| Re-scan capability | **On-demand + optional scheduled** | User can re-scan folder to pick up new statements |
| Supported providers | **OneDrive, Google Drive, Dropbox** (Phase 1) | Covers ~95% of consumer cloud storage |
| Folder selection | **User picks a folder** | More predictable than full-drive scan; respects user privacy |
| File types | **PDF, PNG, JPG, TIFF** | Standard statement formats; Azure Doc Intelligence supports all |
| Tier placement | **Free tier** (with monthly scan limits) | Key differentiator — Plaid is Premium, document import is Free |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         User's Browser                              │
│                                                                     │
│  1. User clicks "Connect OneDrive"                                  │
│  2. OAuth consent → Microsoft/Google/Dropbox login                  │
│  3. User picks a folder (e.g., "Financial Documents")               │
│  4. App scans folder → shows discovered statements                  │
│  5. User selects statements to import → reviews extracted data      │
│  6. Confirmed data → LinkedAccount + Transaction entities           │
└──────────────────────┬──────────────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     Azure Functions API                               │
│                                                                      │
│  CloudStorageFunctions         DocumentImportFunctions                │
│  ├─ POST /connect              ├─ POST /scan                         │
│  ├─ GET  /connections          ├─ GET  /scan/{id}/results             │
│  ├─ DELETE /disconnect         ├─ POST /scan/{id}/parse              │
│  └─ POST /refresh-token        ├─ GET  /parse/{id}/preview           │
│                                └─ POST /parse/{id}/confirm           │
└──────────┬───────────────────────────┬───────────────────────────────┘
           │                           │
           ▼                           ▼
┌───────────────────┐    ┌─────────────────────────────┐
│  Cloud Providers   │    │  Azure Document Intelligence  │
│  ├─ Microsoft      │    │  (Form Recognizer)            │
│  │   Graph API     │    │                               │
│  ├─ Google Drive   │    │  ├─ prebuilt-invoice          │
│  │   API           │    │  ├─ prebuilt-receipt          │
│  └─ Dropbox API    │    │  └─ custom: bank-statement    │
└───────────────────┘    └─────────────────────────────┘
           │                           │
           │  (read-only file access)  │  (extracted structured data)
           ▼                           ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     Azure SQL Database                                │
│                                                                      │
│  CloudStorageConnections  →  DocumentScanRecords  →  Transactions    │
│  (OAuth tokens)              (scan history)          (Source =       │
│                                                       DocumentImport)│
└──────────────────────────────────────────────────────────────────────┘
```

---

## Cloud Storage Connection Entity

```csharp
/// <summary>
/// An OAuth connection to a user's cloud storage provider (OneDrive, Google Drive, Dropbox).
/// Stores encrypted refresh tokens for ongoing read-only access to the user's chosen folder.
/// </summary>
public class CloudStorageConnection
{
    // === Identity & tenant scoping ===
    public Guid Id { get; set; }

    /// <summary>
    /// Entra Object ID of the connection owner.
    /// String type — consistent with UserProfile.Id, Asset.UserId, Contact.UserId.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for advisor-client data sharing.
    /// Matches UserId for direct owners; used by DataAccessGrant authorization.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    // === Provider details ===
    public CloudStorageProvider Provider { get; set; }

    /// <summary>
    /// Display name of the cloud account (e.g., "john@gmail.com" or "John's OneDrive").
    /// Captured during OAuth for display purposes.
    /// </summary>
    public string AccountDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific account ID (e.g., Google user ID, Microsoft user principal name).
    /// Used to prevent duplicate connections to the same cloud account.
    /// </summary>
    public string ProviderAccountId { get; set; } = string.Empty;

    // === OAuth tokens (encrypted) ===

    /// <summary>
    /// Encrypted OAuth refresh token. AES-256, keys in Azure Key Vault.
    /// Used to obtain new access tokens without re-prompting the user.
    /// Access tokens are short-lived and never stored.
    /// </summary>
    public string EncryptedRefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the current refresh token was last refreshed.
    /// Used to detect stale tokens that may need re-authorization.
    /// </summary>
    public DateTimeOffset TokenRefreshedAt { get; set; }

    /// <summary>
    /// Whether the connection is currently valid.
    /// Set to false if token refresh fails (user revoked access, token expired).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason the connection was deactivated, if applicable.
    /// </summary>
    public string? DeactivationReason { get; set; }

    // === Folder selection ===

    /// <summary>
    /// Provider-specific folder ID selected by the user for scanning.
    /// E.g., OneDrive driveItem ID, Google Drive folder ID, Dropbox path.
    /// </summary>
    public string? SelectedFolderId { get; set; }

    /// <summary>
    /// Human-readable folder path for display (e.g., "/Documents/Financial").
    /// </summary>
    public string? SelectedFolderPath { get; set; }

    /// <summary>
    /// Whether to recurse into subfolders when scanning.
    /// </summary>
    public bool ScanSubfolders { get; set; } = true;

    // === Scan tracking ===
    public DateTimeOffset? LastScanAt { get; set; }
    public int TotalDocumentsFound { get; set; }
    public int TotalDocumentsProcessed { get; set; }

    // === Audit ===
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

---

## Document Scan Record Entity

```csharp
/// <summary>
/// Tracks a discovered financial document in the user's cloud storage.
/// Each scan of a connected folder produces DocumentScanRecords for files
/// that match financial document patterns (PDF statements, etc.).
/// </summary>
public class DocumentScanRecord
{
    // === Identity ===
    public Guid Id { get; set; }
    public Guid CloudStorageConnectionId { get; set; }

    /// <summary>
    /// Entra Object ID of the document owner.
    /// Denormalized from CloudStorageConnection for query efficiency.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    // === File reference (in user's cloud) ===

    /// <summary>
    /// Provider-specific file ID (e.g., OneDrive driveItem ID, Google Drive file ID).
    /// Used to re-access the file for parsing without re-scanning.
    /// </summary>
    public string ProviderFileId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable file name (e.g., "Chase_Statement_Jan2026.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full path in user's cloud storage for display.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes. Used for cost estimation before parsing.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type (e.g., "application/pdf", "image/png").
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Last modified date in cloud storage. Used to detect changes on re-scan.
    /// </summary>
    public DateTimeOffset FileModifiedAt { get; set; }

    /// <summary>
    /// SHA-256 hash of the file content. Used to skip re-processing
    /// identical files across re-scans.
    /// </summary>
    public string? ContentHash { get; set; }

    // === Classification ===

    /// <summary>
    /// What type of financial document this appears to be.
    /// Classified by file name heuristics and/or first-page analysis.
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Confidence score (0.0–1.0) of the document type classification.
    /// </summary>
    public double ClassificationConfidence { get; set; }

    /// <summary>
    /// Detected financial institution name (e.g., "Chase", "Fidelity").
    /// Extracted from file name or first-page header.
    /// </summary>
    public string? DetectedInstitution { get; set; }

    /// <summary>
    /// Detected statement period start date, if parseable.
    /// </summary>
    public DateOnly? DetectedPeriodStart { get; set; }

    /// <summary>
    /// Detected statement period end date, if parseable.
    /// </summary>
    public DateOnly? DetectedPeriodEnd { get; set; }

    // === Processing status ===
    public DocumentProcessingStatus Status { get; set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ProcessingError { get; set; }

    /// <summary>
    /// When the document was last sent to Azure Document Intelligence.
    /// </summary>
    public DateTimeOffset? ParsedAt { get; set; }

    /// <summary>
    /// When the user confirmed the extracted data (or rejected it).
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// Number of transactions extracted from this document.
    /// </summary>
    public int ExtractedTransactionCount { get; set; }

    // === Audit ===
    public DateTimeOffset DiscoveredAt { get; set; }

    // === Navigation ===
    public CloudStorageConnection CloudStorageConnection { get; set; } = null!;
}
```

---

## Extracted Statement Entity

```csharp
/// <summary>
/// Holds the raw extraction result from Azure Document Intelligence before
/// user confirmation. Acts as a staging area — data moves to LinkedAccount
/// and Transaction entities only after the user reviews and confirms.
/// </summary>
public class ExtractedStatement
{
    // === Identity ===
    public Guid Id { get; set; }
    public Guid DocumentScanRecordId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    // === Extracted account info ===

    /// <summary>
    /// Institution name extracted from the statement header.
    /// </summary>
    public string InstitutionName { get; set; } = string.Empty;

    /// <summary>
    /// Masked account number extracted from the statement (e.g., "••••1234").
    /// We only store the masked version — the full account number is never persisted.
    /// </summary>
    public string? AccountNumberMasked { get; set; }

    /// <summary>
    /// Detected account type based on statement content.
    /// </summary>
    public AccountType? DetectedAccountType { get; set; }

    /// <summary>
    /// Statement period start.
    /// </summary>
    public DateOnly StatementPeriodStart { get; set; }

    /// <summary>
    /// Statement period end.
    /// </summary>
    public DateOnly StatementPeriodEnd { get; set; }

    // === Extracted balances ===
    public decimal? OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public string IsoCurrencyCode { get; set; } = "USD";

    // === Extracted transactions (JSON) ===

    /// <summary>
    /// JSON array of extracted transactions. Stored as JSON because this is
    /// a staging record — structured data moves to the Transaction table
    /// only after user confirmation.
    /// Format: ExtractedTransactionItem[] serialized as JSON.
    /// </summary>
    public string ExtractedTransactionsJson { get; set; } = "[]";

    // === Confidence & quality ===

    /// <summary>
    /// Overall extraction confidence (0.0–1.0) from Azure Document Intelligence.
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Number of fields with low confidence (&lt; 0.8) that need user review.
    /// </summary>
    public int LowConfidenceFieldCount { get; set; }

    /// <summary>
    /// Number of pages in the source document.
    /// </summary>
    public int PageCount { get; set; }

    // === User review ===
    public ExtractionReviewStatus ReviewStatus { get; set; }

    /// <summary>
    /// If the user linked this to an existing LinkedAccount instead of creating a new one.
    /// </summary>
    public Guid? LinkedToAccountId { get; set; }

    // === Audit ===
    public DateTimeOffset ExtractedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    // === Navigation ===
    public DocumentScanRecord DocumentScanRecord { get; set; } = null!;
}

/// <summary>
/// A single transaction extracted from a financial statement.
/// Serialized to JSON in ExtractedStatement.ExtractedTransactionsJson.
/// </summary>
public sealed record ExtractedTransactionItem
{
    public DateOnly Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Category { get; init; }
    public string? CheckNumber { get; init; }
    public double Confidence { get; init; }

    /// <summary>
    /// Whether this item had low extraction confidence and was
    /// flagged for user review.
    /// </summary>
    public bool NeedsReview { get; init; }
}
```

---

## Enum Definitions

```csharp
/// <summary>
/// Supported cloud storage providers for document import.
/// </summary>
public enum CloudStorageProvider
{
    OneDrive,       // Microsoft Graph API — Files.Read scope
    GoogleDrive,    // Google Drive API — drive.readonly scope
    Dropbox         // Dropbox API — files.content.read scope
}

/// <summary>
/// Type of financial document detected in cloud storage.
/// </summary>
public enum DocumentType
{
    BankStatement,
    CreditCardStatement,
    InvestmentStatement,
    RetirementStatement,
    MortgageStatement,
    LoanStatement,
    InsuranceStatement,
    TaxDocument,
    Receipt,
    Unknown
}

/// <summary>
/// Processing pipeline status for a discovered document.
/// </summary>
public enum DocumentProcessingStatus
{
    Discovered,     // Found in scan, not yet processed
    Queued,         // Queued for parsing
    Parsing,        // Currently being processed by Azure Doc Intelligence
    Parsed,         // Extraction complete, awaiting user review
    Confirmed,      // User confirmed extracted data → committed to DB
    Rejected,       // User rejected extraction (bad data)
    Failed,         // Parsing or extraction error
    Skipped         // User chose to skip this document
}

/// <summary>
/// User review status for extracted statement data.
/// </summary>
public enum ExtractionReviewStatus
{
    Pending,        // Awaiting user review
    Confirmed,      // User confirmed — data committed
    PartialConfirm, // User confirmed some transactions, edited others
    Rejected        // User rejected — data discarded
}
```

### Extend Existing Enums

```csharp
// Add to existing AccountSource enum
public enum AccountSource
{
    Plaid,          // Linked via Plaid Link — Premium only
    Manual,         // Entered by user — Free + Premium
    DocumentImport  // Extracted from cloud storage document — Free + Premium
}

// Add to existing TransactionSource enum
public enum TransactionSource
{
    Plaid,          // Synced from Plaid
    Manual,         // Entered by user (future feature)
    DocumentImport  // Extracted from financial statement via Azure Doc Intelligence
}
```

---

## DTOs & Contracts

### CloudStorageConnectionDto

```csharp
/// <summary>
/// Cloud storage connection info returned to the client.
/// Never includes tokens or provider account IDs.
/// </summary>
public sealed record CloudStorageConnectionDto
{
    public required Guid Id { get; init; }
    public required CloudStorageProvider Provider { get; init; }
    public required string AccountDisplayName { get; init; }
    public string? SelectedFolderPath { get; init; }
    public bool ScanSubfolders { get; init; }
    public bool IsActive { get; init; }
    public string? DeactivationReason { get; init; }
    public DateTimeOffset? LastScanAt { get; init; }
    public int TotalDocumentsFound { get; init; }
    public int TotalDocumentsProcessed { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

### DocumentScanResultDto

```csharp
/// <summary>
/// A discovered document from a cloud storage scan.
/// </summary>
public sealed record DocumentScanResultDto
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string MimeType { get; init; }
    public required DateTimeOffset FileModifiedAt { get; init; }
    public required DocumentType DocumentType { get; init; }
    public required double ClassificationConfidence { get; init; }
    public string? DetectedInstitution { get; init; }
    public DateOnly? DetectedPeriodStart { get; init; }
    public DateOnly? DetectedPeriodEnd { get; init; }
    public required DocumentProcessingStatus Status { get; init; }
    public string? ProcessingError { get; init; }
    public int ExtractedTransactionCount { get; init; }
}
```

### ExtractionPreviewDto

```csharp
/// <summary>
/// Preview of data extracted from a financial statement, shown to the user
/// for review before committing. Includes confidence scores so the user
/// knows which fields may need correction.
/// </summary>
public sealed record ExtractionPreviewDto
{
    public required Guid ExtractedStatementId { get; init; }
    public required Guid DocumentScanRecordId { get; init; }
    public required string FileName { get; init; }

    // === Account info ===
    public required string InstitutionName { get; init; }
    public string? AccountNumberMasked { get; init; }
    public AccountType? DetectedAccountType { get; init; }
    public required DateOnly StatementPeriodStart { get; init; }
    public required DateOnly StatementPeriodEnd { get; init; }

    // === Balances ===
    public decimal? OpeningBalance { get; init; }
    public decimal? ClosingBalance { get; init; }
    public string IsoCurrencyCode { get; init; } = "USD";

    // === Extracted transactions ===
    public required List<ExtractedTransactionPreviewDto> Transactions { get; init; }

    // === Quality indicators ===
    public required double OverallConfidence { get; init; }
    public required int LowConfidenceFieldCount { get; init; }
    public required int PageCount { get; init; }

    // === Matching ===
    /// <summary>
    /// If the detected account matches an existing LinkedAccount, suggest linking.
    /// </summary>
    public Guid? SuggestedLinkedAccountId { get; init; }
    public string? SuggestedLinkedAccountName { get; init; }
}

public sealed record ExtractedTransactionPreviewDto
{
    public required int Index { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }
    public string? Category { get; init; }
    public string? CheckNumber { get; init; }
    public required double Confidence { get; init; }
    public required bool NeedsReview { get; init; }
}
```

### ConfirmExtractionRequest

```csharp
/// <summary>
/// User's confirmation of extracted data. Allows editing individual
/// transactions before committing, and linking to existing accounts.
/// </summary>
public sealed record ConfirmExtractionRequest
{
    /// <summary>
    /// Link to an existing LinkedAccount, or null to create a new one.
    /// </summary>
    public Guid? LinkToExistingAccountId { get; init; }

    /// <summary>
    /// If creating a new account, the user-provided name.
    /// Required when LinkToExistingAccountId is null.
    /// </summary>
    public string? NewAccountName { get; init; }

    /// <summary>
    /// Account type override (user can correct the detected type).
    /// </summary>
    public AccountType? AccountTypeOverride { get; init; }

    /// <summary>
    /// Transactions to import. User may have edited amounts/descriptions
    /// or excluded some transactions.
    /// </summary>
    public required List<ConfirmedTransactionItem> Transactions { get; init; }
}

public sealed record ConfirmedTransactionItem
{
    /// <summary>Index in the original extraction (for traceability).</summary>
    public required int OriginalIndex { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }
    public string? Category { get; init; }
    public string? CheckNumber { get; init; }
    /// <summary>Whether the user edited this item from the original extraction.</summary>
    public bool WasEdited { get; init; }
}
```

### ConnectCloudStorageRequest

```csharp
/// <summary>
/// Request to initiate a cloud storage OAuth connection.
/// </summary>
public sealed record ConnectCloudStorageRequest
{
    public required CloudStorageProvider Provider { get; init; }

    /// <summary>
    /// OAuth authorization code received after user consent.
    /// Exchanged server-side for access + refresh tokens.
    /// </summary>
    public required string AuthorizationCode { get; init; }

    /// <summary>
    /// The redirect URI used in the OAuth flow (must match registered URI).
    /// </summary>
    public required string RedirectUri { get; init; }
}

/// <summary>
/// Request to update folder selection for an existing connection.
/// </summary>
public sealed record UpdateFolderSelectionRequest
{
    /// <summary>Provider-specific folder ID.</summary>
    public required string FolderId { get; init; }
    /// <summary>Human-readable path for display.</summary>
    public required string FolderPath { get; init; }
    public bool ScanSubfolders { get; init; } = true;
}
```

### TypeScript Types

```typescript
interface CloudStorageConnectionDto {
  id: string;
  provider: CloudStorageProvider;
  accountDisplayName: string;
  selectedFolderPath?: string;
  scanSubfolders: boolean;
  isActive: boolean;
  deactivationReason?: string;
  lastScanAt?: string;
  totalDocumentsFound: number;
  totalDocumentsProcessed: number;
  createdAt: string;
}

type CloudStorageProvider = "OneDrive" | "GoogleDrive" | "Dropbox";

interface DocumentScanResultDto {
  id: string;
  fileName: string;
  filePath: string;
  fileSizeBytes: number;
  mimeType: string;
  fileModifiedAt: string;
  documentType: DocumentType;
  classificationConfidence: number;
  detectedInstitution?: string;
  detectedPeriodStart?: string;
  detectedPeriodEnd?: string;
  status: DocumentProcessingStatus;
  processingError?: string;
  extractedTransactionCount: number;
}

type DocumentType =
  | "BankStatement"
  | "CreditCardStatement"
  | "InvestmentStatement"
  | "RetirementStatement"
  | "MortgageStatement"
  | "LoanStatement"
  | "InsuranceStatement"
  | "TaxDocument"
  | "Receipt"
  | "Unknown";

type DocumentProcessingStatus =
  | "Discovered"
  | "Queued"
  | "Parsing"
  | "Parsed"
  | "Confirmed"
  | "Rejected"
  | "Failed"
  | "Skipped";

interface ExtractionPreviewDto {
  extractedStatementId: string;
  documentScanRecordId: string;
  fileName: string;
  institutionName: string;
  accountNumberMasked?: string;
  detectedAccountType?: AccountType;
  statementPeriodStart: string;
  statementPeriodEnd: string;
  openingBalance?: number;
  closingBalance?: number;
  isoCurrencyCode: string;
  transactions: ExtractedTransactionPreviewDto[];
  overallConfidence: number;
  lowConfidenceFieldCount: number;
  pageCount: number;
  suggestedLinkedAccountId?: string;
  suggestedLinkedAccountName?: string;
}

interface ExtractedTransactionPreviewDto {
  index: number;
  date: string;
  description: string;
  amount: number;
  category?: string;
  checkNumber?: string;
  confidence: number;
  needsReview: boolean;
}

interface ConfirmExtractionRequest {
  linkToExistingAccountId?: string;
  newAccountName?: string;
  accountTypeOverride?: AccountType;
  transactions: ConfirmedTransactionItem[];
}

interface ConfirmedTransactionItem {
  originalIndex: number;
  date: string;
  description: string;
  amount: number;
  category?: string;
  checkNumber?: string;
  wasEdited: boolean;
}

interface ConnectCloudStorageRequest {
  provider: CloudStorageProvider;
  authorizationCode: string;
  redirectUri: string;
}

interface UpdateFolderSelectionRequest {
  folderId: string;
  folderPath: string;
  scanSubfolders: boolean;
}
```

---

## API Endpoints

### Cloud Storage Connection Management

```
# OAuth connection lifecycle
POST   /api/cloud-storage/connect                        → ConnectCloudStorage
GET    /api/cloud-storage/connections                     → GetConnections
GET    /api/cloud-storage/connections/{id}                → GetConnection
DELETE /api/cloud-storage/connections/{id}                → DisconnectCloudStorage
PUT    /api/cloud-storage/connections/{id}/folder         → UpdateFolderSelection

# OAuth helpers
GET    /api/cloud-storage/auth-url?provider={provider}   → GetOAuthUrl
POST   /api/cloud-storage/connections/{id}/refresh        → RefreshToken

# Folder browsing (via provider API)
GET    /api/cloud-storage/connections/{id}/folders         → BrowseFolders
GET    /api/cloud-storage/connections/{id}/folders/{folderId} → BrowseFolder
```

### Document Scanning & Import

```
# Scan user's selected folder for financial documents
POST   /api/cloud-storage/connections/{id}/scan            → StartScan
GET    /api/cloud-storage/connections/{id}/documents        → GetDiscoveredDocuments

# Parse a specific document
POST   /api/cloud-storage/documents/{documentId}/parse     → ParseDocument
GET    /api/cloud-storage/documents/{documentId}/preview    → GetExtractionPreview

# User review & confirmation
POST   /api/cloud-storage/documents/{documentId}/confirm    → ConfirmExtraction
POST   /api/cloud-storage/documents/{documentId}/reject     → RejectExtraction
POST   /api/cloud-storage/documents/{documentId}/skip       → SkipDocument

# Batch operations
POST   /api/cloud-storage/connections/{id}/parse-all        → ParseAllDiscovered
```

### Query Parameters for `GET .../documents`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `status` | `DocumentProcessingStatus?` | null | Filter by processing status |
| `documentType` | `DocumentType?` | null | Filter by document type |
| `institution` | `string?` | null | Filter by detected institution |
| `from` | `DateOnly?` | null | Filter by detected period start |
| `to` | `DateOnly?` | null | Filter by detected period end |
| `page` | `int` | 1 | Page number (offset pagination — scan results are stable) |
| `pageSize` | `int` | 25 | Page size (max 100) |

---

## Service Layer

### ICloudStorageService

```csharp
/// <summary>
/// Manages cloud storage provider connections (OAuth lifecycle, folder browsing).
/// Handles token encryption/decryption and provider-specific API differences
/// behind a unified interface.
/// </summary>
public interface ICloudStorageService
{
    /// <summary>
    /// Generates the OAuth authorization URL for the specified provider.
    /// Client redirects the user to this URL to initiate consent.
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(
        string userId,
        CloudStorageProvider provider,
        string redirectUri);

    /// <summary>
    /// Exchanges the OAuth authorization code for tokens and creates
    /// a CloudStorageConnection record.
    /// </summary>
    Task<CloudStorageConnectionDto> ConnectAsync(
        string userId,
        ConnectCloudStorageRequest request);

    /// <summary>
    /// Lists all cloud storage connections for a user.
    /// </summary>
    Task<IReadOnlyList<CloudStorageConnectionDto>> GetConnectionsAsync(
        string requestingUserId,
        string ownerUserId);

    /// <summary>
    /// Gets a single connection by ID.
    /// </summary>
    Task<CloudStorageConnectionDto> GetConnectionByIdAsync(
        string requestingUserId,
        Guid connectionId);

    /// <summary>
    /// Disconnects (soft-deletes) a cloud storage connection.
    /// Revokes the OAuth token with the provider.
    /// </summary>
    Task DisconnectAsync(
        string requestingUserId,
        Guid connectionId);

    /// <summary>
    /// Updates the folder selection for scanning.
    /// </summary>
    Task<CloudStorageConnectionDto> UpdateFolderSelectionAsync(
        string requestingUserId,
        Guid connectionId,
        UpdateFolderSelectionRequest request);

    /// <summary>
    /// Browses folders in the user's cloud storage (for folder picker UI).
    /// If folderId is null, returns root-level folders.
    /// </summary>
    Task<IReadOnlyList<CloudFolderDto>> BrowseFoldersAsync(
        string requestingUserId,
        Guid connectionId,
        string? parentFolderId);

    /// <summary>
    /// Attempts to refresh the OAuth token. Deactivates connection
    /// if refresh fails (e.g., user revoked access).
    /// </summary>
    Task<bool> RefreshTokenAsync(Guid connectionId);
}

public sealed record CloudFolderDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required bool HasChildren { get; init; }
}
```

### IDocumentImportService

```csharp
/// <summary>
/// Scans cloud storage for financial documents, orchestrates parsing
/// via Azure Document Intelligence, and commits confirmed extractions
/// to the LinkedAccount + Transaction tables.
/// </summary>
public interface IDocumentImportService
{
    /// <summary>
    /// Scans the selected folder for financial documents.
    /// Classifies files by name/extension heuristics and creates DocumentScanRecords.
    /// Returns count of newly discovered documents.
    /// </summary>
    Task<int> ScanForDocumentsAsync(
        string requestingUserId,
        Guid connectionId);

    /// <summary>
    /// Gets all discovered documents for a connection with optional filtering.
    /// </summary>
    Task<PaginatedResult<DocumentScanResultDto>> GetDiscoveredDocumentsAsync(
        string requestingUserId,
        Guid connectionId,
        DocumentQueryParameters parameters);

    /// <summary>
    /// Sends a discovered document to Azure Document Intelligence for parsing.
    /// Streams the file from cloud storage → Doc Intelligence (never stored locally).
    /// Creates an ExtractedStatement with the results.
    /// </summary>
    Task<ExtractionPreviewDto> ParseDocumentAsync(
        string requestingUserId,
        Guid documentId);

    /// <summary>
    /// Retrieves the extraction preview for a parsed document.
    /// </summary>
    Task<ExtractionPreviewDto> GetExtractionPreviewAsync(
        string requestingUserId,
        Guid documentId);

    /// <summary>
    /// Commits user-confirmed extraction data to the database.
    /// Creates or updates a LinkedAccount and inserts Transaction records
    /// with Source = DocumentImport.
    /// </summary>
    Task<DocumentImportResult> ConfirmExtractionAsync(
        string requestingUserId,
        Guid documentId,
        ConfirmExtractionRequest request);

    /// <summary>
    /// Marks a document extraction as rejected (bad data).
    /// </summary>
    Task RejectExtractionAsync(
        string requestingUserId,
        Guid documentId);

    /// <summary>
    /// Marks a document as skipped (user chose not to import).
    /// </summary>
    Task SkipDocumentAsync(
        string requestingUserId,
        Guid documentId);

    /// <summary>
    /// Queues all discovered (unprocessed) documents for parsing.
    /// Returns count of documents queued.
    /// </summary>
    Task<int> ParseAllDiscoveredAsync(
        string requestingUserId,
        Guid connectionId);
}

public sealed record DocumentImportResult
{
    public required Guid LinkedAccountId { get; init; }
    public required string AccountName { get; init; }
    public required bool IsNewAccount { get; init; }
    public required int TransactionsImported { get; init; }
    public required int DuplicatesSkipped { get; init; }
}

public sealed record DocumentQueryParameters
{
    public DocumentProcessingStatus? Status { get; init; }
    public DocumentType? DocumentType { get; init; }
    public string? Institution { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
```

### ICloudStorageProviderAdapter (Provider Abstraction)

```csharp
/// <summary>
/// Abstracts provider-specific API calls behind a common interface.
/// Implementations: OneDriveAdapter, GoogleDriveAdapter, DropboxAdapter.
/// </summary>
public interface ICloudStorageProviderAdapter
{
    CloudStorageProvider Provider { get; }

    /// <summary>
    /// Generates the OAuth authorization URL for this provider.
    /// </summary>
    string GetAuthorizationUrl(string redirectUri, string state);

    /// <summary>
    /// Exchanges an authorization code for access + refresh tokens.
    /// </summary>
    Task<OAuthTokenResult> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    Task<OAuthTokenResult> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes the OAuth token (cleanup on disconnect).
    /// </summary>
    Task RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Gets the authenticated user's display name and provider account ID.
    /// </summary>
    Task<CloudAccountInfo> GetAccountInfoAsync(string accessToken);

    /// <summary>
    /// Lists folders at the specified path (or root if parentFolderId is null).
    /// </summary>
    Task<IReadOnlyList<CloudFolderDto>> ListFoldersAsync(
        string accessToken,
        string? parentFolderId);

    /// <summary>
    /// Lists files in the specified folder matching financial document patterns.
    /// </summary>
    Task<IReadOnlyList<CloudFileInfo>> ListFinancialDocumentsAsync(
        string accessToken,
        string folderId,
        bool includeSubfolders);

    /// <summary>
    /// Opens a read-only stream to a file for piping to Azure Doc Intelligence.
    /// The file is never stored locally — streamed directly.
    /// </summary>
    Task<Stream> OpenFileStreamAsync(string accessToken, string fileId);
}

public sealed record OAuthTokenResult
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed record CloudAccountInfo
{
    public required string ProviderAccountId { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }
}

public sealed record CloudFileInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required long SizeBytes { get; init; }
    public required string MimeType { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
    public string? ContentHash { get; init; }
}
```

---

## OAuth Flows

### OneDrive (Microsoft Graph API)

| Setting | Value |
|---------|-------|
| Auth endpoint | `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` |
| Token endpoint | `https://login.microsoftonline.com/common/oauth2/v2.0/token` |
| Scopes | `Files.Read User.Read offline_access` |
| App registration | Same Entra tenant, separate app registration for cloud storage |

```
User clicks "Connect OneDrive"
  → Client redirects to Microsoft login
  → User consents to Files.Read + User.Read
  → Microsoft redirects back with authorization code
  → Client sends code to POST /api/cloud-storage/connect
  → API exchanges code for tokens via token endpoint
  → API stores encrypted refresh token in CloudStorageConnection
  → API calls Graph /me to get display name
  → Client receives CloudStorageConnectionDto
  → Client shows folder picker (GET /api/cloud-storage/connections/{id}/folders)
```

### Google Drive

| Setting | Value |
|---------|-------|
| Auth endpoint | `https://accounts.google.com/o/oauth2/v2/auth` |
| Token endpoint | `https://oauth2.googleapis.com/token` |
| Scopes | `https://www.googleapis.com/auth/drive.readonly https://www.googleapis.com/auth/userinfo.profile` |
| Access type | `offline` (for refresh token) |

### Dropbox

| Setting | Value |
|---------|-------|
| Auth endpoint | `https://www.dropbox.com/oauth2/authorize` |
| Token endpoint | `https://api.dropboxapi.com/2/auth/token/revoke` |
| Scopes | `files.content.read account_info.read` |
| Token access type | `offline` |

### Token Refresh Strategy

```csharp
/// <summary>
/// Middleware-level token refresh. Before any cloud storage API call,
/// ensure we have a valid access token. Refresh tokens are long-lived
/// but access tokens expire (typically 1 hour).
/// </summary>
private async Task<string> GetValidAccessTokenAsync(CloudStorageConnection connection)
{
    // Access tokens are not stored — always get a fresh one from the refresh token
    var adapter = _adapterFactory.GetAdapter(connection.Provider);
    try
    {
        var decryptedRefreshToken = _encryptionService.Decrypt(connection.EncryptedRefreshToken);
        var result = await adapter.RefreshAccessTokenAsync(decryptedRefreshToken);

        // If provider issued a new refresh token (token rotation), update it
        if (result.RefreshToken != decryptedRefreshToken)
        {
            connection.EncryptedRefreshToken = _encryptionService.Encrypt(result.RefreshToken);
            connection.TokenRefreshedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return result.AccessToken;
    }
    catch (OAuthTokenException ex)
    {
        // Token revoked or expired — deactivate connection
        connection.IsActive = false;
        connection.DeactivationReason = ex.Message;
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning(
            "Cloud storage connection {ConnectionId} deactivated: {Reason}",
            connection.Id, ex.Message);

        throw new BusinessRuleException(
            CloudStorageErrorCodes.CONNECTION_TOKEN_EXPIRED,
            "Cloud storage access has been revoked. Please reconnect.");
    }
}
```

---

## Document Discovery Pipeline

### File Pattern Matching

Financial documents are identified by a combination of file extension, name patterns, and folder location:

```csharp
/// <summary>
/// Heuristic rules for identifying financial documents in cloud storage.
/// </summary>
public static class DocumentDiscoveryRules
{
    // Supported file types
    public static readonly string[] SupportedExtensions =
        [".pdf", ".png", ".jpg", ".jpeg", ".tiff", ".tif"];

    // File name patterns that suggest financial documents
    // Matched case-insensitively against the file name
    public static readonly string[] StatementNamePatterns =
    [
        @"statement",
        @"bank[\s_-]?statement",
        @"account[\s_-]?summary",
        @"monthly[\s_-]?statement",
        @"quarterly[\s_-]?statement",
        @"annual[\s_-]?statement",
        @"tax[\s_-]?(form|return|document)",
        @"1099",
        @"w[\s_-]?2",
        @"1098",
        @"(chase|bofa|wells[\s_-]?fargo|citi|amex|discover|capital[\s_-]?one|schwab|fidelity|vanguard|etrade|td[\s_-]?ameritrade|merrill)",
        @"\d{4}[\s_-]\d{2}",  // YYYY-MM pattern (date in filename)
    ];

    // Folders that commonly contain financial documents
    public static readonly string[] FinancialFolderPatterns =
    [
        @"financ",
        @"bank",
        @"statement",
        @"tax",
        @"invest",
        @"account",
        @"receipt",
    ];
}
```

### Scan Flow

```
1. Retrieve file list from provider API (selected folder ± subfolders)
2. Filter by supported extensions
3. Score each file against name/path patterns → ClassificationConfidence
4. Skip files already tracked (match by ProviderFileId + ContentHash)
5. Create DocumentScanRecord for each new/changed file
6. Attempt to detect institution + period from file name
7. Return count of newly discovered documents
```

### Deduplication

Documents are deduplicated across re-scans using:

1. **`ProviderFileId`** — same file, even if renamed
2. **`ContentHash`** — same content, even if copied to different location
3. **`InstitutionName + AccountNumberMasked + StatementPeriodEnd`** — same statement, even from different source

---

## Document Parsing Pipeline

### Azure Document Intelligence Integration

```csharp
/// <summary>
/// Orchestrates document parsing via Azure Document Intelligence.
/// Files are streamed from cloud storage → Doc Intelligence → structured data.
/// No files are stored locally or in RAJ Financial's blob storage.
/// </summary>
public interface IDocumentParsingService
{
    /// <summary>
    /// Parses a financial document and returns structured extraction results.
    /// </summary>
    /// <param name="documentStream">Read-only stream from cloud storage provider.</param>
    /// <param name="mimeType">MIME type of the document.</param>
    /// <param name="documentType">Hint about expected document type (may be Unknown).</param>
    /// <returns>Structured extraction result.</returns>
    Task<DocumentExtractionResult> ParseAsync(
        Stream documentStream,
        string mimeType,
        DocumentType documentType);
}

public sealed record DocumentExtractionResult
{
    public required string InstitutionName { get; init; }
    public string? AccountNumberMasked { get; init; }
    public AccountType? DetectedAccountType { get; init; }
    public required DateOnly StatementPeriodStart { get; init; }
    public required DateOnly StatementPeriodEnd { get; init; }
    public decimal? OpeningBalance { get; init; }
    public decimal? ClosingBalance { get; init; }
    public string IsoCurrencyCode { get; init; } = "USD";
    public required List<ExtractedTransactionItem> Transactions { get; init; }
    public required double OverallConfidence { get; init; }
    public required int PageCount { get; init; }
}
```

### Model Selection Strategy

| Document Type | Azure Doc Intelligence Model | Rationale |
|---------------|------------------------------|-----------|
| BankStatement | Custom model: `raj-bank-statement-v1` | Trained on major US bank statement layouts |
| CreditCardStatement | Custom model: `raj-credit-card-v1` | Credit card statements have unique layouts |
| InvestmentStatement | `prebuilt-invoice` + custom post-processing | Investment summaries are structured like invoices |
| Receipt | `prebuilt-receipt` | Native Azure receipt model |
| TaxDocument | Custom model: `raj-tax-form-v1` | W-2, 1099, 1098 layouts |
| Unknown | `prebuilt-layout` → classify → re-parse with specific model | Two-pass approach |

### Streaming Architecture (No Local Storage)

```csharp
public async Task<ExtractionPreviewDto> ParseDocumentAsync(
    string requestingUserId, Guid documentId)
{
    var record = await GetDocumentRecordAsync(requestingUserId, documentId);
    var connection = record.CloudStorageConnection;

    // 1. Get valid access token for the cloud provider
    var accessToken = await GetValidAccessTokenAsync(connection);
    var adapter = _adapterFactory.GetAdapter(connection.Provider);

    // 2. Stream file directly from cloud storage → Azure Doc Intelligence
    //    File content never touches our disk or blob storage
    await using var fileStream = await adapter.OpenFileStreamAsync(
        accessToken, record.ProviderFileId);

    // 3. Parse via Azure Document Intelligence
    var result = await _parsingService.ParseAsync(
        fileStream, record.MimeType, record.DocumentType);

    // 4. Store extraction results in staging table
    var statement = new ExtractedStatement
    {
        DocumentScanRecordId = documentId,
        UserId = record.UserId,
        TenantId = record.TenantId,
        InstitutionName = result.InstitutionName,
        AccountNumberMasked = result.AccountNumberMasked,
        DetectedAccountType = result.DetectedAccountType,
        StatementPeriodStart = result.StatementPeriodStart,
        StatementPeriodEnd = result.StatementPeriodEnd,
        OpeningBalance = result.OpeningBalance,
        ClosingBalance = result.ClosingBalance,
        IsoCurrencyCode = result.IsoCurrencyCode,
        ExtractedTransactionsJson = JsonSerializer.Serialize(result.Transactions),
        OverallConfidence = result.OverallConfidence,
        LowConfidenceFieldCount = result.Transactions.Count(t => t.Confidence < 0.8),
        PageCount = result.PageCount,
        ReviewStatus = ExtractionReviewStatus.Pending,
        ExtractedAt = DateTimeOffset.UtcNow,
    };

    // ... save and return preview DTO
}
```

---

## Transaction Extraction & Mapping

### Confirmation Flow

When the user confirms an extraction, data flows into the existing entity model:

```
ExtractedStatement (staging)
  ├─ Account info → LinkedAccount (Source = DocumentImport)
  └─ Transactions → Transaction[] (Source = DocumentImport)
```

```csharp
public async Task<DocumentImportResult> ConfirmExtractionAsync(
    string requestingUserId, Guid documentId, ConfirmExtractionRequest request)
{
    var record = await GetDocumentRecordWithStatementAsync(requestingUserId, documentId);
    var statement = record.ExtractedStatement;

    // 1. Determine target LinkedAccount
    LinkedAccount account;
    bool isNewAccount;

    if (request.LinkToExistingAccountId.HasValue)
    {
        account = await _accountRepository.GetByIdAsync(
            requestingUserId, request.LinkToExistingAccountId.Value);
        isNewAccount = false;
    }
    else
    {
        account = new LinkedAccount
        {
            UserId = record.UserId,
            TenantId = record.TenantId,
            Name = request.NewAccountName ?? statement.InstitutionName,
            InstitutionName = statement.InstitutionName,
            Mask = statement.AccountNumberMasked,
            Type = request.AccountTypeOverride ?? statement.DetectedAccountType ?? AccountType.Other,
            Source = AccountSource.DocumentImport,
            CurrentBalance = statement.ClosingBalance,
            IsoCurrencyCode = statement.IsoCurrencyCode,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await _dbContext.LinkedAccounts.AddAsync(account);
        isNewAccount = true;
    }

    // 2. Map confirmed transactions → Transaction entities
    var duplicatesSkipped = 0;
    var transactionsToAdd = new List<Transaction>();

    foreach (var item in request.Transactions)
    {
        // Dedup: skip if an identical transaction already exists for this account
        var isDuplicate = await _dbContext.Transactions.AnyAsync(t =>
            t.LinkedAccountId == account.Id &&
            t.Date == item.Date &&
            t.Amount == item.Amount &&
            t.Name == item.Description);

        if (isDuplicate)
        {
            duplicatesSkipped++;
            continue;
        }

        transactionsToAdd.Add(new Transaction
        {
            UserId = record.UserId,
            TenantId = record.TenantId,
            LinkedAccountId = account.Id,
            PlaidTransactionId = $"doc-import-{documentId}-{item.OriginalIndex}",
            Amount = item.Amount,
            IsoCurrencyCode = statement.IsoCurrencyCode,
            Date = item.Date,
            Name = item.Description,
            PlaidPrimaryCategory = item.Category,
            PaymentChannel = "Unknown",
            TransactionType = TransactionType.Unresolved,
            Source = TransactionSource.DocumentImport,
            IsPending = false,
            SyncedAt = DateTimeOffset.UtcNow,
        });
    }

    await _dbContext.Transactions.AddRangeAsync(transactionsToAdd);

    // 3. Update processing status
    record.Status = DocumentProcessingStatus.Confirmed;
    record.ConfirmedAt = DateTimeOffset.UtcNow;
    record.ExtractedTransactionCount = transactionsToAdd.Count;
    statement.ReviewStatus = ExtractionReviewStatus.Confirmed;
    statement.LinkedToAccountId = account.Id;
    statement.ReviewedAt = DateTimeOffset.UtcNow;

    await _dbContext.SaveChangesAsync();

    _auditLogger.LogDataModification(
        action: "DocumentImportConfirmed",
        resourceType: "DocumentScanRecord",
        resourceId: documentId,
        userId: requestingUserId,
        details: new
        {
            FileName = record.FileName,
            AccountId = account.Id,
            TransactionsImported = transactionsToAdd.Count,
            DuplicatesSkipped = duplicatesSkipped,
            IsNewAccount = isNewAccount,
        });

    return new DocumentImportResult
    {
        LinkedAccountId = account.Id,
        AccountName = account.Name,
        IsNewAccount = isNewAccount,
        TransactionsImported = transactionsToAdd.Count,
        DuplicatesSkipped = duplicatesSkipped,
    };
}
```

---

## Tier Gating & Access Control

### Subscription Tiers

Document import via cloud storage is available on **Free tier** with limits:

| Capability | Free Tier | Premium Tier |
|------------|-----------|-------------|
| Cloud storage connections | 1 provider | Unlimited |
| Monthly document scans | 5 scans/month | Unlimited |
| Monthly document parses | 10 documents/month | Unlimited |
| Transaction history (imported) | 3-month query window | Full history |
| Plaid account linking | Not available | Up to 10 linked accounts |
| Manual transaction entry | Up to 3 months | Unlimited |
| CSV export | Not available | Available |

### Enforcement

```csharp
public async Task<int> ScanForDocumentsAsync(string requestingUserId, Guid connectionId)
{
    // 1. Authorization
    var connection = await GetConnectionAsync(requestingUserId, connectionId);

    // 2. Tier gating — enforce monthly scan limit for free users
    var tier = await _subscriptionService.GetTierAsync(requestingUserId);
    if (tier == SubscriptionTier.Free)
    {
        var scansThisMonth = await _dbContext.CloudStorageConnections
            .Where(c => c.UserId == requestingUserId && c.LastScanAt != null
                && c.LastScanAt.Value.Month == DateTime.UtcNow.Month
                && c.LastScanAt.Value.Year == DateTime.UtcNow.Year)
            .CountAsync();

        if (scansThisMonth >= 5)
            throw new BusinessRuleException(
                CloudStorageErrorCodes.TIER_SCAN_LIMIT_REACHED,
                "Free tier allows 5 scans per month. Upgrade to Premium for unlimited scans.");
    }

    // 3. Execute scan...
}
```

---

## Security & Privacy

### Access Control

| Operation | Authorization | Notes |
|-----------|--------------|-------|
| Connect cloud storage | Owner only | Cannot connect on behalf of another user |
| Browse folders | Owner + DataAccessGrant(Documents, Read) + Admin | Advisors can view connected folders |
| Scan for documents | Owner + DataAccessGrant(Documents, Full) + Admin | Requires write-level access |
| Parse document | Owner + DataAccessGrant(Documents, Full) + Admin | |
| Review & confirm | Owner only | Only the user can commit extracted data |
| View connections | Owner + DataAccessGrant(Documents, Read) + Admin | |
| Disconnect | Owner only | Cannot disconnect on behalf of another user |

### Data Classification

| Data | Classification | Treatment |
|------|---------------|-----------|
| OAuth refresh tokens | **Critical secret** | Encrypted at rest (AES-256, Key Vault keys). Never logged. Never returned in DTOs. |
| Provider account ID | Internal identifier | Never returned in DTOs |
| File content | **User-owned PII** | Streamed, never stored. No blob storage, no temp files. |
| Extracted transactions | Financial data | Stored in standard Transaction table with tenant isolation |
| Account numbers | **Sensitive** | Only masked version stored (e.g., `••••1234`). Full number is never persisted. |
| File paths in cloud | Metadata | Stored for re-scan; paths may reveal folder names |

### Key Security Properties

1. **No document storage** — Files are streamed from cloud → Azure Doc Intelligence. RAJ Financial never stores the original document.
2. **No full account numbers** — Only the masked version from the statement is stored.
3. **Tokens are encrypted** — Same pattern as Plaid access tokens.
4. **Minimal scopes** — Read-only access to cloud storage. Never write to user's files.
5. **Token revocation on disconnect** — Actively revoke OAuth token with provider, don't just delete our record.
6. **CSRF protection** — OAuth state parameter validated on callback to prevent CSRF attacks.

### Audit Logging

```csharp
// Connection lifecycle
_auditLogger.LogDataModification("CloudStorageConnected", "CloudStorageConnection", connectionId, userId,
    new { Provider = provider.ToString(), AccountDisplayName = displayName });

_auditLogger.LogDataModification("CloudStorageDisconnected", "CloudStorageConnection", connectionId, userId,
    new { Provider = provider.ToString() });

// Document operations
_auditLogger.LogDataModification("DocumentScanStarted", "CloudStorageConnection", connectionId, userId,
    new { FolderPath = connection.SelectedFolderPath });

_auditLogger.LogDataModification("DocumentParsed", "DocumentScanRecord", documentId, userId,
    new { FileName = record.FileName, Confidence = result.OverallConfidence });

_auditLogger.LogDataModification("DocumentImportConfirmed", "DocumentScanRecord", documentId, userId,
    new { TransactionsImported = count, AccountId = accountId });

_auditLogger.LogDataModification("DocumentImportRejected", "DocumentScanRecord", documentId, userId,
    new { FileName = record.FileName, Reason = "User rejected extraction" });
```

---

## Error Codes

```csharp
public static class CloudStorageErrorCodes
{
    // === Connection errors ===
    public const string CONNECTION_NOT_FOUND = "CLOUD_STORAGE_CONNECTION_NOT_FOUND";
    public const string CONNECTION_ALREADY_EXISTS = "CLOUD_STORAGE_CONNECTION_ALREADY_EXISTS";
    public const string CONNECTION_INACTIVE = "CLOUD_STORAGE_CONNECTION_INACTIVE";
    public const string CONNECTION_TOKEN_EXPIRED = "CLOUD_STORAGE_TOKEN_EXPIRED";
    public const string CONNECTION_PROVIDER_ERROR = "CLOUD_STORAGE_PROVIDER_ERROR";

    // === OAuth errors ===
    public const string OAUTH_CODE_INVALID = "CLOUD_STORAGE_OAUTH_CODE_INVALID";
    public const string OAUTH_CONSENT_DENIED = "CLOUD_STORAGE_OAUTH_CONSENT_DENIED";
    public const string OAUTH_STATE_MISMATCH = "CLOUD_STORAGE_OAUTH_STATE_MISMATCH";

    // === Folder errors ===
    public const string FOLDER_NOT_FOUND = "CLOUD_STORAGE_FOLDER_NOT_FOUND";
    public const string FOLDER_NOT_SELECTED = "CLOUD_STORAGE_FOLDER_NOT_SELECTED";

    // === Scan errors ===
    public const string SCAN_IN_PROGRESS = "CLOUD_STORAGE_SCAN_IN_PROGRESS";
    public const string SCAN_NO_DOCUMENTS = "CLOUD_STORAGE_SCAN_NO_DOCUMENTS";

    // === Parsing errors ===
    public const string DOCUMENT_NOT_FOUND = "CLOUD_STORAGE_DOCUMENT_NOT_FOUND";
    public const string DOCUMENT_ALREADY_PARSED = "CLOUD_STORAGE_DOCUMENT_ALREADY_PARSED";
    public const string DOCUMENT_PARSE_FAILED = "CLOUD_STORAGE_DOCUMENT_PARSE_FAILED";
    public const string DOCUMENT_TOO_LARGE = "CLOUD_STORAGE_DOCUMENT_TOO_LARGE";
    public const string DOCUMENT_FORMAT_UNSUPPORTED = "CLOUD_STORAGE_DOCUMENT_FORMAT_UNSUPPORTED";

    // === Extraction errors ===
    public const string EXTRACTION_NOT_FOUND = "CLOUD_STORAGE_EXTRACTION_NOT_FOUND";
    public const string EXTRACTION_NOT_READY = "CLOUD_STORAGE_EXTRACTION_NOT_READY";
    public const string EXTRACTION_ALREADY_CONFIRMED = "CLOUD_STORAGE_EXTRACTION_ALREADY_CONFIRMED";
    public const string EXTRACTION_NO_TRANSACTIONS = "CLOUD_STORAGE_EXTRACTION_NO_TRANSACTIONS";

    // === Tier gating ===
    public const string TIER_CONNECTION_LIMIT = "CLOUD_STORAGE_TIER_CONNECTION_LIMIT";
    public const string TIER_SCAN_LIMIT_REACHED = "CLOUD_STORAGE_TIER_SCAN_LIMIT_REACHED";
    public const string TIER_PARSE_LIMIT_REACHED = "CLOUD_STORAGE_TIER_PARSE_LIMIT_REACHED";

    // === Validation ===
    public const string PROVIDER_REQUIRED = "CLOUD_STORAGE_PROVIDER_REQUIRED";
    public const string AUTH_CODE_REQUIRED = "CLOUD_STORAGE_AUTH_CODE_REQUIRED";
    public const string REDIRECT_URI_REQUIRED = "CLOUD_STORAGE_REDIRECT_URI_REQUIRED";
    public const string ACCOUNT_NAME_REQUIRED = "CLOUD_STORAGE_ACCOUNT_NAME_REQUIRED";
    public const string TRANSACTIONS_REQUIRED = "CLOUD_STORAGE_TRANSACTIONS_REQUIRED";
}
```

---

## Validation Rules

### ConnectCloudStorageRequest

| Field | Rule | Error Code |
|-------|------|------------|
| `Provider` | Must be a valid enum value | `PROVIDER_REQUIRED` |
| `AuthorizationCode` | Required, non-empty | `AUTH_CODE_REQUIRED` |
| `RedirectUri` | Required, valid URI, must match registered redirect URIs | `REDIRECT_URI_REQUIRED` |

### ConfirmExtractionRequest

| Field | Rule | Error Code |
|-------|------|------------|
| `NewAccountName` | Required when `LinkToExistingAccountId` is null. Max 200 chars. | `ACCOUNT_NAME_REQUIRED` |
| `Transactions` | At least 1 transaction required | `TRANSACTIONS_REQUIRED` |
| `Transactions[].Date` | Must be a valid date | `VALIDATION_FAILED` |
| `Transactions[].Amount` | Must be non-zero | `VALIDATION_FAILED` |
| `Transactions[].Description` | Required, max 500 chars | `VALIDATION_FAILED` |

### File Size Limits

| Constraint | Limit | Rationale |
|------------|-------|-----------|
| Max file size | 50 MB | Azure Doc Intelligence limit; most statements < 5 MB |
| Max pages per document | 100 pages | Cost control; most statements < 20 pages |
| Max documents per parse batch | 20 | Prevent runaway costs |

---

## UI Design

### Connected Accounts Page

> **Route**: `/settings/cloud-storage` (under Settings)
> **Auth Policy**: `RequireClient`
> **Layout**: `DashboardLayout` (sidebar)

#### Connection Cards

```tsx
// Desktop: card grid. Mobile: stacked cards.
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
  {/* Provider connection cards */}
  <CloudStorageCard
    provider="OneDrive"
    icon={<OneDriveIcon />}
    connection={oneDriveConnection}
    onConnect={handleConnect}
    onDisconnect={handleDisconnect}
    onScan={handleScan}
  />
  <CloudStorageCard
    provider="GoogleDrive"
    icon={<GoogleDriveIcon />}
    connection={googleDriveConnection}
    onConnect={handleConnect}
    onDisconnect={handleDisconnect}
    onScan={handleScan}
  />
  <CloudStorageCard
    provider="Dropbox"
    icon={<DropboxIcon />}
    connection={dropboxConnection}
    onConnect={handleConnect}
    onDisconnect={handleDisconnect}
    onScan={handleScan}
  />
</div>
```

#### States

| State | Display |
|-------|---------|
| Not connected | Provider logo + "Connect" button + description of what it does |
| Connected, no folder | "Select a folder to scan" CTA |
| Connected, folder selected | Folder path + last scan date + document count + "Scan Now" button |
| Connection expired | Warning banner + "Reconnect" button |

### Folder Picker Dialog

```tsx
// Tree-view folder browser (like a file manager)
<Dialog>
  <DialogHeader>{t('cloudStorage.folderPicker.title')}</DialogHeader>
  <DialogContent>
    <FolderTree
      folders={folders}
      selectedFolderId={selectedFolderId}
      onSelect={setSelectedFolderId}
      onExpand={handleLoadChildren}
      aria-label={t('cloudStorage.folderPicker.treeLabel')}
    />
    <Checkbox
      checked={scanSubfolders}
      onCheckedChange={setScanSubfolders}
      label={t('cloudStorage.folderPicker.includeSubfolders')}
    />
  </DialogContent>
  <DialogFooter>
    <Button onClick={handleSave}>{t('cloudStorage.folderPicker.save')}</Button>
  </DialogFooter>
</Dialog>
```

### Document Discovery Results

After a scan, show discovered documents in a list/table:

```tsx
// Desktop: table. Mobile: card list.
<div className="hidden md:block">
  <Table aria-label={t('cloudStorage.documents.tableLabel')}>
    <TableHeader>
      <TableColumn>{t('cloudStorage.documents.fileName')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.type')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.institution')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.period')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.confidence')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.status')}</TableColumn>
      <TableColumn>{t('cloudStorage.documents.actions')}</TableColumn>
    </TableHeader>
    {/* ... rows */}
  </Table>
</div>
<div className="md:hidden space-y-4">
  {documents.map(doc => (
    <DocumentCard key={doc.id} document={doc} onParse={handleParse} />
  ))}
</div>
```

### Extraction Review Page

> **Route**: `/cloud-storage/documents/{id}/review`

The most critical UI — users must review and correct AI extraction results:

```tsx
<div className="flex flex-col lg:flex-row gap-6">
  {/* Left: Account info summary */}
  <div className="lg:w-1/3 space-y-4">
    <Card>
      <h2>{t('cloudStorage.review.accountInfo')}</h2>
      <dl>
        <dt>{t('cloudStorage.review.institution')}</dt>
        <dd>{preview.institutionName}</dd>
        <dt>{t('cloudStorage.review.accountNumber')}</dt>
        <dd>{preview.accountNumberMasked}</dd>
        <dt>{t('cloudStorage.review.period')}</dt>
        <dd>{formatDateRange(preview.statementPeriodStart, preview.statementPeriodEnd)}</dd>
        <dt>{t('cloudStorage.review.closingBalance')}</dt>
        <dd>{formatCurrency(preview.closingBalance)}</dd>
      </dl>

      {/* Link to existing account or create new */}
      {preview.suggestedLinkedAccountId ? (
        <Alert>
          {t('cloudStorage.review.matchFound', { name: preview.suggestedLinkedAccountName })}
          <Button onClick={() => setLinkToAccount(preview.suggestedLinkedAccountId)}>
            {t('cloudStorage.review.linkToExisting')}
          </Button>
        </Alert>
      ) : (
        <Input
          label={t('cloudStorage.review.newAccountName')}
          value={newAccountName}
          onChange={setNewAccountName}
        />
      )}
    </Card>

    {/* Confidence indicator */}
    <ConfidenceMeter value={preview.overallConfidence} />
  </div>

  {/* Right: Transaction list (editable) */}
  <div className="lg:w-2/3">
    <h2>{t('cloudStorage.review.transactions', { count: preview.transactions.length })}</h2>
    <p className="text-muted-foreground">
      {t('cloudStorage.review.reviewInstructions')}
    </p>

    {preview.transactions.map(tx => (
      <TransactionReviewRow
        key={tx.index}
        transaction={tx}
        highlighted={tx.needsReview}
        onEdit={handleEdit}
        onRemove={handleRemove}
      />
    ))}

    <div className="flex gap-3 mt-6">
      <Button variant="default" onClick={handleConfirm}>
        {t('cloudStorage.review.confirmImport')}
      </Button>
      <Button variant="outline" onClick={handleReject}>
        {t('cloudStorage.review.reject')}
      </Button>
    </div>
  </div>
</div>
```

Low-confidence fields are visually highlighted (amber border, warning icon) so the user knows which values to double-check.

---

## Cost Analysis

### Azure Document Intelligence Pricing (as of 2026)

| Model | Cost per Page | Typical Statement | Monthly Cost (50 docs) |
|-------|--------------|-------------------|------------------------|
| `prebuilt-layout` | $0.01 | 5 pages | $2.50 |
| `prebuilt-invoice` | $0.01 | 3 pages | $1.50 |
| `prebuilt-receipt` | $0.01 | 1 page | $0.50 |
| Custom model | $0.05 | 5 pages | $12.50 |

**Estimated monthly cost per active user**: $5–15 (depending on document volume and model usage).

### Comparison with Plaid

| Cost Factor | Plaid | Cloud Storage + Doc Intelligence |
|-------------|-------|----------------------------------|
| Per-connection fee | ~$1-5/month/connection | $0 (OAuth is free) |
| Per-sync cost | Included in connection fee | $0.05–0.25 per document parsed |
| Data freshness | Real-time | Statement publication cadence (monthly) |
| Setup friction | Bank credentials via Plaid Link | OAuth consent + folder picker |
| Accuracy | 99.9%+ (structured API) | 90–98% (AI extraction, varies by format) |
| Institution coverage | ~12,000 institutions | Any institution that issues statements |

---

## Database Schema

### Table: `CloudStorageConnections`

```sql
CREATE TABLE CloudStorageConnections (
    Id                      uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  nvarchar(128)       NOT NULL,
    TenantId                nvarchar(128)       NOT NULL,
    Provider                nvarchar(20)        NOT NULL,
    AccountDisplayName      nvarchar(200)       NOT NULL,
    ProviderAccountId       nvarchar(200)       NOT NULL,
    EncryptedRefreshToken   nvarchar(max)       NOT NULL,
    TokenRefreshedAt        datetimeoffset      NOT NULL,
    IsActive                bit                 NOT NULL DEFAULT 1,
    DeactivationReason      nvarchar(500)       NULL,
    SelectedFolderId        nvarchar(500)       NULL,
    SelectedFolderPath      nvarchar(1000)      NULL,
    ScanSubfolders          bit                 NOT NULL DEFAULT 1,
    LastScanAt              datetimeoffset      NULL,
    TotalDocumentsFound     int                 NOT NULL DEFAULT 0,
    TotalDocumentsProcessed int                 NOT NULL DEFAULT 0,
    CreatedAt               datetimeoffset      NOT NULL,
    UpdatedAt               datetimeoffset      NULL,

    CONSTRAINT PK_CloudStorageConnections PRIMARY KEY (Id),
    CONSTRAINT UQ_CloudStorage_User_Provider_Account
        UNIQUE (UserId, Provider, ProviderAccountId)
);

CREATE NONCLUSTERED INDEX IX_CloudStorage_User
    ON CloudStorageConnections (UserId, IsActive);
```

### Table: `DocumentScanRecords`

```sql
CREATE TABLE DocumentScanRecords (
    Id                          uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    CloudStorageConnectionId    uniqueidentifier    NOT NULL,
    UserId                      nvarchar(128)       NOT NULL,
    TenantId                    nvarchar(128)       NOT NULL,
    ProviderFileId              nvarchar(500)       NOT NULL,
    FileName                    nvarchar(500)       NOT NULL,
    FilePath                    nvarchar(1000)      NOT NULL,
    FileSizeBytes               bigint              NOT NULL,
    MimeType                    nvarchar(50)        NOT NULL,
    FileModifiedAt              datetimeoffset      NOT NULL,
    ContentHash                 nvarchar(64)        NULL,
    DocumentType                nvarchar(30)        NOT NULL,
    ClassificationConfidence    float               NOT NULL,
    DetectedInstitution         nvarchar(200)       NULL,
    DetectedPeriodStart         date                NULL,
    DetectedPeriodEnd           date                NULL,
    Status                      nvarchar(20)        NOT NULL DEFAULT 'Discovered',
    ProcessingError             nvarchar(2000)      NULL,
    ParsedAt                    datetimeoffset      NULL,
    ConfirmedAt                 datetimeoffset      NULL,
    ExtractedTransactionCount   int                 NOT NULL DEFAULT 0,
    DiscoveredAt                datetimeoffset      NOT NULL,

    CONSTRAINT PK_DocumentScanRecords PRIMARY KEY (Id),
    CONSTRAINT FK_DocumentScanRecords_Connection FOREIGN KEY (CloudStorageConnectionId)
        REFERENCES CloudStorageConnections(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_DocumentScanRecords_File
        UNIQUE (CloudStorageConnectionId, ProviderFileId)
);

CREATE NONCLUSTERED INDEX IX_DocumentScan_User_Status
    ON DocumentScanRecords (UserId, Status)
    INCLUDE (FileName, DocumentType, DetectedInstitution);

CREATE NONCLUSTERED INDEX IX_DocumentScan_ContentHash
    ON DocumentScanRecords (ContentHash)
    WHERE ContentHash IS NOT NULL;
```

### Table: `ExtractedStatements`

```sql
CREATE TABLE ExtractedStatements (
    Id                          uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    DocumentScanRecordId        uniqueidentifier    NOT NULL,
    UserId                      nvarchar(128)       NOT NULL,
    TenantId                    nvarchar(128)       NOT NULL,
    InstitutionName             nvarchar(200)       NOT NULL,
    AccountNumberMasked         nvarchar(20)        NULL,
    DetectedAccountType         nvarchar(20)        NULL,
    StatementPeriodStart        date                NOT NULL,
    StatementPeriodEnd          date                NOT NULL,
    OpeningBalance              decimal(18,2)       NULL,
    ClosingBalance              decimal(18,2)       NULL,
    IsoCurrencyCode             nvarchar(3)         NOT NULL DEFAULT 'USD',
    ExtractedTransactionsJson   nvarchar(max)       NOT NULL,
    OverallConfidence           float               NOT NULL,
    LowConfidenceFieldCount     int                 NOT NULL DEFAULT 0,
    PageCount                   int                 NOT NULL DEFAULT 0,
    ReviewStatus                nvarchar(20)        NOT NULL DEFAULT 'Pending',
    LinkedToAccountId           uniqueidentifier    NULL,
    ExtractedAt                 datetimeoffset      NOT NULL,
    ReviewedAt                  datetimeoffset      NULL,

    CONSTRAINT PK_ExtractedStatements PRIMARY KEY (Id),
    CONSTRAINT FK_ExtractedStatements_Document FOREIGN KEY (DocumentScanRecordId)
        REFERENCES DocumentScanRecords(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ExtractedStatements_Account FOREIGN KEY (LinkedToAccountId)
        REFERENCES LinkedAccounts(Id) ON DELETE SET NULL
);

CREATE UNIQUE NONCLUSTERED INDEX IX_ExtractedStatements_Document
    ON ExtractedStatements (DocumentScanRecordId);
```

---

## Future Considerations

### Phase 2 Enhancements

1. **Scheduled auto-scan** — Background job scans connected folders weekly/monthly for new statements. Requires Azure Functions Timer trigger.

2. **Smart notification** — "We found 3 new bank statements in your OneDrive. Review them?" Push notification or in-app alert.

3. **Custom model training** — Train Azure Doc Intelligence custom models on user-corrected data. When users fix extraction errors, feed corrections back to improve the model.

4. **Statement-to-asset linking** — Investment/retirement statements can update asset values in the Asset table (e.g., quarterly portfolio statement updates the asset's `currentValue`).

5. **Multi-currency support** — Detect and handle statements in non-USD currencies with exchange rate conversion.

6. **iCloud Drive / Box** — Additional cloud provider adapters.

7. **Email attachment scanning** — Connect Gmail/Outlook, scan for financial statement emails, extract attached PDFs. (Requires broader scopes and more complex consent.)

8. **Direct upload fallback** — For users who don't use cloud storage, allow direct PDF upload as a secondary path. Documents uploaded to Azure Blob Storage with TTL (deleted after extraction).

9. **Recurring import** — Detect when a new statement for the same account/period arrives and auto-suggest import.

10. **Balance reconciliation** — Compare extracted closing balance with last known balance on the LinkedAccount. Flag discrepancies for user review.
