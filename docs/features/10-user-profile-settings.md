# 10 — User Profile & Settings

> User profile management, subscription tier control, BYOK API key settings, data sharing administration, notification preferences, data export (GDPR), and account deletion.

**ADO Tracking:** [Epic #451 — 10 - User Profile & Settings](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/451)

| # | Feature | State |
|---|---------|-------|
| [529](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/529) | User Profile Service & API | New |
| [530](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/530) | Data Export & GDPR Compliance | New |
| [531](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/531) | Subscription Management | New |
| [532](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/532) | Settings & Preferences UI | New |

---

## Overview

The User Profile & Settings area is the single hub for all user-configurable state:

1. **Profile information** — Display name, avatar, locale, timezone
2. **Subscription management** — View current tier, upgrade to Premium, manage billing
3. **AI key management** — BYOK Anthropic API key configuration (see also doc 09)
4. **Data sharing** — Manage DataAccessGrant invitations and active advisor access
5. **Notification preferences** — Email digest, alert types, frequency
6. **Appearance** — Theme (light/dark/system), density, chart colors
7. **Data export** — GDPR right-to-data-portability full account export
8. **Account deletion** — Soft-delete with 30-day grace period, then hard-delete

---

## Design Goals

| Goal | Description |
|------|-------------|
| **Self-service** | Users manage everything without contacting support |
| **Privacy-first** | Export and delete flows satisfy GDPR Article 17 & 20 |
| **Tier visibility** | Users always know their current tier, usage, and upgrade path |
| **Minimal data** | Collect only what's necessary; no marketing fields |

---

## Entities

### UserProfile

```csharp
/// <summary>
/// Stores user preferences and settings beyond what Entra ID provides.
/// Created on first login (JIT provisioning).
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }

    /// <summary>Entra Object ID — primary identifier.</summary>
    public string UserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    /// <summary>Display name (synced from Entra on first login, editable).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Email address (synced from Entra, read-only in app).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>URL to avatar image in Blob Storage, null if using initials.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>ISO locale code (e.g., "en-US", "es-MX").</summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>IANA timezone (e.g., "America/New_York").</summary>
    public string Timezone { get; set; } = "America/New_York";

    /// <summary>Currency code for display formatting (e.g., "USD").</summary>
    public string CurrencyCode { get; set; } = "USD";

    // — Subscription —

    /// <summary>Current subscription tier.</summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

    /// <summary>Stripe Customer ID, null for free tier.</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>Stripe Subscription ID for active premium subscription.</summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>When the premium subscription started (null for free).</summary>
    public DateTime? SubscriptionStartDate { get; set; }

    /// <summary>When the current billing period ends.</summary>
    public DateTime? SubscriptionEndDate { get; set; }

    // — BYOK AI —

    /// <summary>Whether the user has a BYOK Anthropic key configured in Key Vault.</summary>
    public bool HasByokKey { get; set; }

    // — Notification Preferences —

    /// <summary>Receive weekly email digest.</summary>
    public bool EmailDigestEnabled { get; set; } = true;

    /// <summary>Receive alerts for account sync issues.</summary>
    public bool AlertSyncIssues { get; set; } = true;

    /// <summary>Receive alerts for low beneficiary coverage.</summary>
    public bool AlertCoverageGaps { get; set; } = true;

    /// <summary>Receive alerts when approaching tier limits.</summary>
    public bool AlertTierLimits { get; set; } = true;

    // — Appearance —

    /// <summary>Theme preference: Light, Dark, System.</summary>
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    // — Lifecycle —

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Non-null if the user has requested account deletion.</summary>
    public DateTime? DeletionRequestedAt { get; set; }

    /// <summary>True once hard-delete has been executed (for audit trail).</summary>
    public bool IsDeleted { get; set; }
}

public enum SubscriptionTier
{
    Free = 0,
    Premium = 1
}

public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2
}
```

### EF Core Configuration

```csharp
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).HasMaxLength(128).IsRequired();
        builder.Property(p => p.TenantId).HasMaxLength(128).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(320).IsRequired();
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
        builder.Property(p => p.Locale).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Timezone).HasMaxLength(64).IsRequired();
        builder.Property(p => p.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(p => p.StripeCustomerId).HasMaxLength(64);
        builder.Property(p => p.StripeSubscriptionId).HasMaxLength(64);

        builder.HasIndex(p => p.UserId).IsUnique()
            .HasDatabaseName("IX_UserProfile_UserId");
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_UserProfile_TenantId");

        // Global query filter excludes deleted users
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

---

## Service Interfaces

### IUserProfileService

```csharp
/// <summary>
/// Manages user profile CRUD, preferences, and lifecycle operations.
/// </summary>
public interface IUserProfileService
{
    /// <summary>Gets the profile for the authenticated user. Creates on first call (JIT).</summary>
    Task<UserProfileDto> GetOrCreateProfileAsync(string userId, string email, string displayName);

    /// <summary>Updates user-editable profile fields.</summary>
    Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request);

    /// <summary>Updates notification preferences.</summary>
    Task<UserProfileDto> UpdateNotificationPreferencesAsync(
        string userId, UpdateNotificationPreferencesRequest request);

    /// <summary>Updates appearance settings.</summary>
    Task<UserProfileDto> UpdateAppearanceAsync(
        string userId, UpdateAppearanceRequest request);

    /// <summary>Uploads a new avatar image and returns the URL.</summary>
    Task<string> UploadAvatarAsync(string userId, Stream imageStream, string contentType);

    /// <summary>Removes the user's avatar, reverting to initials.</summary>
    Task RemoveAvatarAsync(string userId);
}
```

### ISubscriptionService

```csharp
/// <summary>
/// Manages subscription tier transitions and Stripe billing integration.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Gets the user's current subscription tier.</summary>
    Task<SubscriptionTier> GetTierAsync(string userId);

    /// <summary>Gets the user's detailed subscription info including usage.</summary>
    Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(string userId);

    /// <summary>
    /// Creates a Stripe Checkout session for upgrading to Premium.
    /// Returns the Checkout URL to redirect the user to.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(string userId);

    /// <summary>
    /// Handles Stripe webhook events (checkout.session.completed,
    /// customer.subscription.deleted, etc.).
    /// </summary>
    Task HandleStripeWebhookAsync(string payload, string signature);

    /// <summary>
    /// Cancels the user's premium subscription.
    /// Access continues until the end of the current billing period.
    /// </summary>
    Task CancelSubscriptionAsync(string userId);
}
```

### IDataExportService

```csharp
/// <summary>
/// Handles GDPR data export (right to data portability).
/// Generates a complete archive of the user's data.
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Initiates a full data export for the user.
    /// Returns an export job ID — processing is async.
    /// </summary>
    Task<Guid> RequestExportAsync(string userId);

    /// <summary>
    /// Checks the status of an export job.
    /// </summary>
    Task<DataExportStatusDto> GetExportStatusAsync(string userId, Guid exportId);

    /// <summary>
    /// Gets a time-limited SAS URL to download the completed export.
    /// </summary>
    Task<string> GetExportDownloadUrlAsync(string userId, Guid exportId);
}
```

### IAccountDeletionService

```csharp
/// <summary>
/// Manages user account deletion with soft-delete grace period.
/// </summary>
public interface IAccountDeletionService
{
    /// <summary>
    /// Initiates account deletion. Sets a 30-day grace period.
    /// User can cancel during the grace period.
    /// </summary>
    Task RequestDeletionAsync(string userId, AccountDeletionRequest request);

    /// <summary>
    /// Cancels a pending deletion request during the grace period.
    /// </summary>
    Task CancelDeletionAsync(string userId);

    /// <summary>
    /// Checks the status of a pending deletion request.
    /// </summary>
    Task<DeletionStatusDto?> GetDeletionStatusAsync(string userId);

    /// <summary>
    /// Executes the hard-delete for users past the grace period.
    /// Called by a timer-triggered function.
    /// </summary>
    Task ProcessExpiredDeletionsAsync();
}
```

---

## DTOs

### User Profile

```csharp
/// <summary>
/// User profile data returned to the client.
/// </summary>
public sealed record UserProfileDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;

    // Subscription summary
    public SubscriptionTier Tier { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }

    // AI status
    public bool HasByokKey { get; init; }

    // Notification preferences
    public bool EmailDigestEnabled { get; init; }
    public bool AlertSyncIssues { get; init; }
    public bool AlertCoverageGaps { get; init; }
    public bool AlertTierLimits { get; init; }

    // Appearance
    public ThemePreference Theme { get; init; }

    // Lifecycle
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
}
```

### Request Models

```csharp
public sealed record UpdateProfileRequest
{
    public string? DisplayName { get; init; }
    public string? Locale { get; init; }
    public string? Timezone { get; init; }
    public string? CurrencyCode { get; init; }
}

public sealed record UpdateNotificationPreferencesRequest
{
    public bool EmailDigestEnabled { get; init; }
    public bool AlertSyncIssues { get; init; }
    public bool AlertCoverageGaps { get; init; }
    public bool AlertTierLimits { get; init; }
}

public sealed record UpdateAppearanceRequest
{
    public ThemePreference Theme { get; init; }
}

public sealed record AccountDeletionRequest
{
    /// <summary>User must type their email to confirm deletion.</summary>
    public string ConfirmationEmail { get; init; } = string.Empty;

    /// <summary>Optional reason for leaving — stored for analytics.</summary>
    public string? Reason { get; init; }
}
```

### Subscription

```csharp
/// <summary>
/// Detailed subscription information including usage stats.
/// </summary>
public sealed record SubscriptionDetailsDto
{
    public SubscriptionTier Tier { get; init; }
    public DateTime? SubscriptionStartDate { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }

    /// <summary>True if user has requested cancellation but period hasn't ended.</summary>
    public bool IsCancelling { get; init; }

    /// <summary>Current usage against tier limits.</summary>
    public TierUsageDto Usage { get; init; } = null!;
}

/// <summary>
/// Current usage of tier-limited features.
/// </summary>
public sealed record TierUsageDto
{
    public int AssetCount { get; init; }
    public int AssetLimit { get; init; }         // 10 or -1

    public int ContactCount { get; init; }
    public int ContactLimit { get; init; }       // 5 or -1

    public int ManualAccountCount { get; init; }
    public int ManualAccountLimit { get; init; } // 3 or -1

    public long StorageUsedBytes { get; init; }
    public long StorageLimitBytes { get; init; } // 100MB or 5GB

    public int AiCallsThisMonth { get; init; }
    public int AiCallLimit { get; init; }        // varies or -1
}
```

### Data Export

```csharp
public sealed record DataExportStatusDto
{
    public Guid ExportId { get; init; }
    public ExportStatus Status { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long? FileSizeBytes { get; init; }

    /// <summary>SAS URL, only populated when status is Completed.</summary>
    public string? DownloadUrl { get; init; }

    /// <summary>Download URL expiry time.</summary>
    public DateTime? DownloadExpiresAt { get; init; }
}

public enum ExportStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Expired = 4
}
```

### Account Deletion

```csharp
public sealed record DeletionStatusDto
{
    public DateTime RequestedAt { get; init; }
    public DateTime ScheduledDeletionAt { get; init; }
    public int DaysRemaining { get; init; }
    public string? Reason { get; init; }
}
```

---

## Validation

### UpdateProfileRequest Validation

```csharp
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly HashSet<string> SupportedLocales =
        ["en-US", "es-MX", "es-ES", "fr-FR", "pt-BR"];

    private static readonly HashSet<string> SupportedCurrencies =
        ["USD", "EUR", "GBP", "MXN", "BRL", "CAD"];

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MinimumLength(2).MaximumLength(200)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Locale)
            .Must(l => SupportedLocales.Contains(l!))
            .WithMessage("Unsupported locale")
            .When(x => x.Locale is not null);

        RuleFor(x => x.Timezone)
            .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz!, out _)
                || TryIanaToWindows(tz!))
            .WithMessage("Invalid timezone identifier")
            .When(x => x.Timezone is not null);

        RuleFor(x => x.CurrencyCode)
            .Must(c => SupportedCurrencies.Contains(c!))
            .WithMessage("Unsupported currency code")
            .When(x => x.CurrencyCode is not null);
    }
}
```

### AccountDeletionRequest Validation

```csharp
public class AccountDeletionRequestValidator
    : AbstractValidator<AccountDeletionRequest>
{
    public AccountDeletionRequestValidator()
    {
        RuleFor(x => x.ConfirmationEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Please confirm your email address to proceed");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}
```

---

## API Endpoints

### Profile

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/profile` | Get or create user profile (JIT) |
| `PUT` | `/api/profile` | Update profile fields |
| `POST` | `/api/profile/avatar` | Upload avatar image |
| `DELETE` | `/api/profile/avatar` | Remove avatar |

### Preferences

| Method | Route | Description |
|--------|-------|-------------|
| `PUT` | `/api/profile/notifications` | Update notification preferences |
| `PUT` | `/api/profile/appearance` | Update appearance settings |

### Subscription

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/subscription` | Get subscription details + usage |
| `POST` | `/api/subscription/checkout` | Create Stripe Checkout session |
| `POST` | `/api/subscription/cancel` | Cancel premium subscription |
| `POST` | `/api/webhooks/stripe` | Stripe webhook handler (no auth) |

### AI Key Management

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/settings/ai-key` | Set or update BYOK API key |
| `DELETE` | `/api/settings/ai-key` | Remove BYOK API key |
| `GET` | `/api/settings/ai-key/status` | Check if BYOK key is configured |

### Data Sharing

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/sharing/grants` | List active DataAccessGrants (as owner) |
| `GET` | `/api/sharing/received` | List grants received (as grantee) |
| `POST` | `/api/sharing/invite` | Send a sharing invitation |
| `POST` | `/api/sharing/accept/{inviteId}` | Accept a sharing invitation |
| `DELETE` | `/api/sharing/grants/{grantId}` | Revoke a grant |
| `PUT` | `/api/sharing/grants/{grantId}` | Update grant scope (categories, access type) |

### Data Export & Deletion

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/profile/export` | Request full data export |
| `GET` | `/api/profile/export/{exportId}` | Check export status |
| `GET` | `/api/profile/export/{exportId}/download` | Download export file (SAS URL) |
| `POST` | `/api/profile/delete` | Request account deletion |
| `POST` | `/api/profile/delete/cancel` | Cancel pending deletion |
| `GET` | `/api/profile/delete/status` | Check deletion status |

---

## Azure Functions

### GetOrCreateProfile

```csharp
/// <summary>
/// Gets the user's profile, creating it on first access (JIT provisioning).
/// Claims are extracted from the JWT to seed the profile.
/// </summary>
[Function("GetOrCreateProfile")]
[Authorize]
public async Task<HttpResponseData> GetOrCreateProfile(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profile")]
    HttpRequestData req,
    FunctionContext context)
{
    var userId = context.GetUserId();
    var email = context.GetClaimValue("email") ?? context.GetClaimValue("emails");
    var displayName = context.GetClaimValue("name") ?? "User";

    var profile = await _profileService.GetOrCreateProfileAsync(
        userId, email, displayName);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(profile);
    return response;
}
```

### CreateCheckoutSession

```csharp
/// <summary>
/// Creates a Stripe Checkout session for upgrading to Premium.
/// Returns the Checkout URL for client-side redirect.
/// </summary>
[Function("CreateCheckoutSession")]
[Authorize]
public async Task<HttpResponseData> CreateCheckoutSession(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscription/checkout")]
    HttpRequestData req,
    FunctionContext context)
{
    var userId = context.GetUserId();

    var checkoutUrl = await _subscriptionService.CreateCheckoutSessionAsync(userId);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(new { url = checkoutUrl });
    return response;
}
```

### StripeWebhook

```csharp
/// <summary>
/// Handles Stripe webhook events. No authentication — uses Stripe signature verification.
/// </summary>
[Function("StripeWebhook")]
public async Task<HttpResponseData> StripeWebhook(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/stripe")]
    HttpRequestData req)
{
    var payload = await req.ReadAsStringAsync();
    var signature = req.Headers.GetValues("Stripe-Signature").FirstOrDefault()
        ?? throw new ValidationException("Missing Stripe signature");

    await _subscriptionService.HandleStripeWebhookAsync(payload!, signature);

    return req.CreateResponse(HttpStatusCode.OK);
}
```

### RequestDataExport

```csharp
/// <summary>
/// Initiates a full data export (GDPR Article 20).
/// Processing is async — client polls for status.
/// </summary>
[Function("RequestDataExport")]
[Authorize]
public async Task<HttpResponseData> RequestDataExport(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profile/export")]
    HttpRequestData req,
    FunctionContext context)
{
    var userId = context.GetUserId();

    var exportId = await _exportService.RequestExportAsync(userId);

    var response = req.CreateResponse(HttpStatusCode.Accepted);
    await response.WriteAsJsonAsync(new { exportId });
    return response;
}
```

### ProcessExpiredDeletions (Timer)

```csharp
/// <summary>
/// Timer-triggered function that processes account deletions past the 30-day grace period.
/// Runs daily at 2:00 AM UTC.
/// </summary>
[Function("ProcessExpiredDeletions")]
public async Task ProcessExpiredDeletions(
    [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
    FunctionContext context)
{
    _logger.LogInformation("Processing expired account deletions");

    await _deletionService.ProcessExpiredDeletionsAsync();
}
```

---

## Data Export Contents

The GDPR export generates a ZIP archive containing:

```
raj-financial-export-{userId}-{date}.zip
├── profile.json         — UserProfile record
├── assets.json          — All BaseAsset records
├── contacts.json        — All Contact records (with SSN/EIN masked)
├── accounts.json        — All LinkedAccount records (tokens excluded)
├── transactions.json    — All Transaction records
├── documents/           — All uploaded documents
│   ├── metadata.json    — DocumentUpload records
│   └── files/           — Actual document files from Blob Storage
├── grants-given.json    — DataAccessGrant records (as owner)
├── grants-received.json — DataAccessGrant records (as grantee)
├── ai-usage.json        — AiUsageRecord records
├── audit-log.json       — AuditLog entries for this user
└── README.md            — Explanation of the export format
```

### Export Processing

```csharp
/// <summary>
/// Background job that creates the export ZIP in Blob Storage.
/// Gathers all user data across all entities and packages into a ZIP.
/// </summary>
public async Task<string> GenerateExportAsync(string userId, Guid exportId)
{
    // Fan-out to gather all data in parallel
    var (profile, assets, contacts, accounts, transactions,
         documents, grantsGiven, grantsReceived, aiUsage, auditLog) =
        await (
            _profileService.GetProfileAsync(userId),
            _assetService.GetAllAssetsAsync(userId),
            _contactService.GetAllContactsAsync(userId),
            _accountService.GetAllAccountsAsync(userId),
            _transactionService.GetAllTransactionsAsync(userId),
            _documentService.GetDocumentsAsync(userId),
            _sharingService.GetGrantsGivenAsync(userId),
            _sharingService.GetGrantsReceivedAsync(userId),
            _aiUsageService.GetAllUsageAsync(userId),
            _auditService.GetUserAuditLogAsync(userId)
        );

    // Create ZIP in memory, upload to Blob Storage
    using var memoryStream = new MemoryStream();
    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
    {
        AddJsonEntry(archive, "profile.json", profile);
        AddJsonEntry(archive, "assets.json", assets);
        AddJsonEntry(archive, "contacts.json", contacts);
        // ... etc.

        // Download and include actual document files
        foreach (var doc in documents)
        {
            var blob = await _blobService.DownloadAsync(doc.BlobPath);
            var entry = archive.CreateEntry($"documents/files/{doc.FileName}");
            await using var entryStream = entry.Open();
            await blob.CopyToAsync(entryStream);
        }
    }

    memoryStream.Position = 0;
    var blobPath = $"exports/{userId}/{exportId}.zip";
    await _blobService.UploadAsync(blobPath, memoryStream);

    return blobPath;
}
```

---

## Account Deletion Flow

```
1. User clicks "Delete Account" in settings
   └──▶ 2. Confirmation dialog: type email, optional reason
        └──▶ 3. POST /api/profile/delete
             └──▶ 4. Set DeletionRequestedAt = now
                  ├──▶ 5a. Cancel billing (if Premium)
                  ├──▶ 5b. Revoke all DataAccessGrants
                  └──▶ 5c. Send confirmation email with cancel link
                       │
                       ├── 30-day grace period ──
                       │   User can sign in and cancel deletion
                       │   Banner: "Your account is scheduled for deletion on {date}"
                       │
                       └──▶ 6. Timer function processes deletion
                            ├── Hard-delete all user data from SQL
                            ├── Hard-delete all Blob Storage documents
                            ├── Remove Key Vault secrets (BYOK key)
                            ├── Revoke Plaid access tokens
                            └── Log final audit entry
```

### Cascade Deletion Order

To respect foreign key constraints:

1. `AuditLog` entries (copy to cold storage first)
2. `AiUsageRecord` entries
3. `DocumentUpload` records + Blob files
4. `AssetContactLink` records
5. `TrustRole` records
6. `Transaction` records
7. `LinkedAccount` records (+ Plaid token revocation)
8. `Contact` records (Individual, Trust, Organization)
9. `BaseAsset` records
10. `DataAccessGrant` records (given + received)
11. `UserProfile` record (mark `IsDeleted = true` for audit)
12. Key Vault secrets (BYOK key)

---

## Stripe Integration

### Configuration

```json
{
  "Stripe": {
    "SecretKey": "@Microsoft.KeyVault(SecretUri=...)",
    "PublishableKey": "pk_...",
    "WebhookSecret": "@Microsoft.KeyVault(SecretUri=...)",
    "PremiumPriceId": "price_...",
    "SuccessUrl": "https://app.rajfinancial.com/settings/subscription?success=true",
    "CancelUrl": "https://app.rajfinancial.com/settings/subscription?cancelled=true"
  }
}
```

### Webhook Events Handled

| Event | Action |
|-------|--------|
| `checkout.session.completed` | Upgrade user to Premium, store StripeCustomerId + StripeSubscriptionId |
| `customer.subscription.updated` | Update subscription dates |
| `customer.subscription.deleted` | Downgrade to Free, clear Stripe fields |
| `invoice.payment_failed` | Log warning, notify user |

---

## TypeScript Types

```typescript
/** Subscription tier. */
type SubscriptionTier = 'Free' | 'Premium';

/** Theme preference. */
type ThemePreference = 'System' | 'Light' | 'Dark';

/** User profile returned from API. */
interface UserProfileDto {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  locale: string;
  timezone: string;
  currencyCode: string;
  tier: SubscriptionTier;
  subscriptionEndDate?: string;
  hasByokKey: boolean;
  emailDigestEnabled: boolean;
  alertSyncIssues: boolean;
  alertCoverageGaps: boolean;
  alertTierLimits: boolean;
  theme: ThemePreference;
  createdAt: string;
  deletionRequestedAt?: string;
}

/** Request to update profile fields. */
interface UpdateProfileRequest {
  displayName?: string;
  locale?: string;
  timezone?: string;
  currencyCode?: string;
}

/** Notification preferences update. */
interface UpdateNotificationPreferencesRequest {
  emailDigestEnabled: boolean;
  alertSyncIssues: boolean;
  alertCoverageGaps: boolean;
  alertTierLimits: boolean;
}

/** Appearance settings update. */
interface UpdateAppearanceRequest {
  theme: ThemePreference;
}

/** Subscription details including usage. */
interface SubscriptionDetailsDto {
  tier: SubscriptionTier;
  subscriptionStartDate?: string;
  subscriptionEndDate?: string;
  isCancelling: boolean;
  usage: TierUsageDto;
}

/** Current usage against tier limits. */
interface TierUsageDto {
  assetCount: number;
  assetLimit: number;
  contactCount: number;
  contactLimit: number;
  manualAccountCount: number;
  manualAccountLimit: number;
  storageUsedBytes: number;
  storageLimitBytes: number;
  aiCallsThisMonth: number;
  aiCallLimit: number;
}

/** Data export status. */
type ExportStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed' | 'Expired';

interface DataExportStatusDto {
  exportId: string;
  status: ExportStatus;
  requestedAt: string;
  completedAt?: string;
  fileSizeBytes?: number;
  downloadUrl?: string;
  downloadExpiresAt?: string;
}

/** Account deletion status. */
interface DeletionStatusDto {
  requestedAt: string;
  scheduledDeletionAt: string;
  daysRemaining: number;
  reason?: string;
}

/** Account deletion request. */
interface AccountDeletionRequest {
  confirmationEmail: string;
  reason?: string;
}

/** Data sharing grant (owner perspective). */
interface DataAccessGrantDto {
  id: string;
  granteeId: string;
  granteeName: string;
  granteeEmail: string;
  accessType: 'Read' | 'Limited' | 'Full' | 'Owner';
  dataCategories: string[];   // ["Assets", "Contacts", etc.]
  expiresAt?: string;
  isActive: boolean;
  createdAt: string;
}

/** Share invitation request. */
interface ShareInvitationRequest {
  granteeEmail: string;
  accessType: 'Read' | 'Limited' | 'Full';
  dataCategories: string[];
  expiresAt?: string;
}
```

---

## React Query Hooks

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Fetches the current user's profile (creates on first call).
 */
export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: fetchProfile,
    staleTime: 5 * 60 * 1000,  // 5 minutes
  });
}

/**
 * Mutation to update profile fields.
 */
export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => updateProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

/**
 * Fetches subscription details including usage stats.
 */
export function useSubscription() {
  return useQuery({
    queryKey: ['subscription'],
    queryFn: fetchSubscription,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Mutation to create a Stripe Checkout session.
 * On success, redirects to the Stripe Checkout page.
 */
export function useCreateCheckout() {
  return useMutation({
    mutationFn: () => createCheckoutSession(),
    onSuccess: (data) => {
      window.location.href = data.url;
    },
  });
}

/**
 * Mutation to cancel premium subscription.
 */
export function useCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cancelSubscription(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscription'] });
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

/**
 * Fetches active data sharing grants (as owner).
 */
export function useDataGrants() {
  return useQuery({
    queryKey: ['sharing', 'grants'],
    queryFn: fetchDataGrants,
    staleTime: 30 * 1000,
  });
}

/**
 * Fetches data grants received (as grantee).
 */
export function useReceivedGrants() {
  return useQuery({
    queryKey: ['sharing', 'received'],
    queryFn: fetchReceivedGrants,
    staleTime: 30 * 1000,
  });
}

/**
 * Mutation to send a sharing invitation.
 */
export function useSendInvitation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ShareInvitationRequest) => sendInvitation(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sharing'] });
    },
  });
}

/**
 * Mutation to request full data export.
 */
export function useRequestExport() {
  return useMutation({
    mutationFn: () => requestExport(),
  });
}

/**
 * Polls export status until completed or failed.
 */
export function useExportStatus(exportId: string | null) {
  return useQuery({
    queryKey: ['export', exportId],
    queryFn: () => fetchExportStatus(exportId!),
    enabled: !!exportId,
    refetchInterval: (query) => {
      const status = query?.state?.data?.status;
      return status === 'Queued' || status === 'Processing' ? 3000 : false;
    },
  });
}

/**
 * Mutation to request account deletion.
 */
export function useRequestDeletion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: AccountDeletionRequest) => requestDeletion(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

/**
 * Mutation to cancel pending account deletion.
 */
export function useCancelDeletion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cancelDeletion(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
```

---

## UI Layout

### Settings Page — Desktop

```
┌──────────────────────────────────────────────────────────────┐
│  Settings                                                    │
├──────────────┬───────────────────────────────────────────────┤
│              │                                               │
│  ┌─────────┐ │  Profile                                      │
│  │ Profile  │ │  ┌───────────────────────────────────────────┐│
│  ├─────────┤ │  │ [Avatar]  John Smith                      ││
│  │ Subscr.  │ │  │          john@example.com (read-only)    ││
│  ├─────────┤ │  │                                           ││
│  │ AI Keys  │ │  │ Display Name: [John Smith        ]       ││
│  ├─────────┤ │  │ Locale:       [English (US)    ▼]        ││
│  │ Sharing  │ │  │ Timezone:     [America/New_York ▼]       ││
│  ├─────────┤ │  │ Currency:     [USD ▼]                     ││
│  │ Notif.   │ │  │                                           ││
│  ├─────────┤ │  │ [Save Changes]                             ││
│  │ Appear.  │ │  └───────────────────────────────────────────┘│
│  ├─────────┤ │                                               │
│  │ Export   │ │                                               │
│  ├─────────┤ │                                               │
│  │ 🔴 Delete│ │                                               │
│  └─────────┘ │                                               │
│              │                                               │
├──────────────┴───────────────────────────────────────────────┤
```

### Settings Page — Mobile

Tab-based navigation replaces sidebar:

```
┌──────────────────────────┐
│  Settings                │
├──────────────────────────┤
│ [Profile] [Sub] [AI] ... │  ← horizontal scrollable tabs
├──────────────────────────┤
│                          │
│  Profile                 │
│  ┌──────────────────────┐│
│  │ [Avatar] J. Smith    ││
│  │ john@example.com     ││
│  │                      ││
│  │ Name: [John Smith  ] ││
│  │ Locale: [en-US    ▼] ││
│  │ Timezone: [EST    ▼] ││
│  │ Currency: [USD    ▼] ││
│  │                      ││
│  │ [Save Changes]       ││
│  └──────────────────────┘│
└──────────────────────────┘
```

### Subscription Section

```tsx
/**
 * Displays current subscription tier, usage, and upgrade/cancel controls.
 */
export function SubscriptionSection() {
  const { t } = useTranslation();
  const { data: subscription } = useSubscription();
  const checkout = useCreateCheckout();
  const cancel = useCancelSubscription();

  if (!subscription) return <Skeleton className="h-64" />;

  return (
    <div className="space-y-6">
      {/* Current tier badge */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold">
            {t('settings.subscription.title')}
          </h3>
          <Badge variant={subscription.tier === 'Premium' ? 'default' : 'secondary'}>
            {subscription.tier}
          </Badge>
        </div>
        {subscription.tier === 'Free' && (
          <Button onClick={() => checkout.mutate()} disabled={checkout.isPending}>
            {t('settings.subscription.upgrade')}
          </Button>
        )}
        {subscription.tier === 'Premium' && !subscription.isCancelling && (
          <Button
            variant="outline"
            onClick={() => cancel.mutate()}
            disabled={cancel.isPending}
          >
            {t('settings.subscription.cancel')}
          </Button>
        )}
      </div>

      {/* Cancellation notice */}
      {subscription.isCancelling && (
        <Alert variant="destructive">
          <AlertDescription>
            {t('settings.subscription.cancellingNotice', {
              date: formatDate(subscription.subscriptionEndDate),
            })}
          </AlertDescription>
        </Alert>
      )}

      {/* Usage meters */}
      <UsageMeters usage={subscription.usage} />
    </div>
  );
}
```

### Usage Meters Component

```tsx
/**
 * Displays usage meters for tier-limited features.
 * Shows progress bars with color coding near limits.
 */
export function UsageMeters({ usage }: { usage: TierUsageDto }) {
  const { t } = useTranslation();

  const meters = [
    {
      label: t('usage.assets'),
      used: usage.assetCount,
      limit: usage.assetLimit,
    },
    {
      label: t('usage.contacts'),
      used: usage.contactCount,
      limit: usage.contactLimit,
    },
    {
      label: t('usage.accounts'),
      used: usage.manualAccountCount,
      limit: usage.manualAccountLimit,
    },
    {
      label: t('usage.storage'),
      used: usage.storageUsedBytes,
      limit: usage.storageLimitBytes,
      format: formatBytes,
    },
    {
      label: t('usage.aiCalls'),
      used: usage.aiCallsThisMonth,
      limit: usage.aiCallLimit,
    },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {meters.map((meter) => (
        <UsageMeter key={meter.label} {...meter} />
      ))}
    </div>
  );
}
```

### Data Sharing Section

```tsx
/**
 * Manages DataAccessGrants — invite advisors, view active shares, revoke access.
 * Premium feature — wrapped in PremiumGate.
 */
export function DataSharingSection() {
  const { t } = useTranslation();
  const { data: grants } = useDataGrants();
  const sendInvite = useSendInvitation();

  return (
    <PremiumGate feature="dataSharing">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">
            {t('settings.sharing.title')}
          </h3>
          <Dialog>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="h-4 w-4 mr-2" aria-hidden="true" />
                {t('settings.sharing.invite')}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <ShareInviteForm onSubmit={sendInvite.mutate} />
            </DialogContent>
          </Dialog>
        </div>

        {/* Active grants list */}
        <div className="space-y-3">
          {grants?.map((grant) => (
            <GrantCard key={grant.id} grant={grant} />
          ))}
          {grants?.length === 0 && (
            <p className="text-muted-foreground text-center py-8">
              {t('settings.sharing.noGrants')}
            </p>
          )}
        </div>
      </div>
    </PremiumGate>
  );
}
```

### Account Deletion Section

```tsx
/**
 * Account deletion with confirmation dialog and grace period display.
 */
export function AccountDeletionSection() {
  const { t } = useTranslation();
  const { data: profile } = useProfile();
  const { data: deletionStatus } = useDeletionStatus();
  const requestDeletion = useRequestDeletion();
  const cancelDeletion = useCancelDeletion();

  // Show grace period banner if deletion is pending
  if (profile?.deletionRequestedAt) {
    return (
      <Alert variant="destructive" className="border-2">
        <AlertTriangle className="h-5 w-5" aria-hidden="true" />
        <AlertTitle>{t('settings.delete.pendingTitle')}</AlertTitle>
        <AlertDescription className="space-y-4">
          <p>
            {t('settings.delete.pendingDescription', {
              date: deletionStatus?.scheduledDeletionAt,
              days: deletionStatus?.daysRemaining,
            })}
          </p>
          <Button
            variant="outline"
            onClick={() => cancelDeletion.mutate()}
            disabled={cancelDeletion.isPending}
          >
            {t('settings.delete.cancel')}
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold text-destructive">
        {t('settings.delete.title')}
      </h3>
      <p className="text-muted-foreground">
        {t('settings.delete.description')}
      </p>

      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button variant="destructive">
            {t('settings.delete.button')}
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <DeletionConfirmationForm
            email={profile?.email ?? ''}
            onConfirm={requestDeletion.mutate}
            isPending={requestDeletion.isPending}
          />
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
```

---

## JIT Profile Provisioning

User profiles are created **on first API call**, not during registration:

```csharp
public async Task<UserProfileDto> GetOrCreateProfileAsync(
    string userId, string email, string displayName)
{
    var profile = await _db.UserProfiles
        .FirstOrDefaultAsync(p => p.UserId == userId);

    if (profile is null)
    {
        profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = userId, // Single-user tenancy
            DisplayName = displayName,
            Email = email,
        };

        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "JIT profile created for {UserId}: {Email}",
            userId, email);
    }

    return MapToDto(profile);
}
```

---

## Tier Gating Summary

| Feature | Free | Premium |
|---------|------|---------|
| Profile management | ✅ | ✅ |
| Notification preferences | ✅ | ✅ |
| Theme/appearance | ✅ | ✅ |
| BYOK key management | ✅ | ✅ |
| Data export | ✅ | ✅ |
| Account deletion | ✅ | ✅ |
| Data sharing | ❌ | ✅ |
| Subscription management | N/A | ✅ |
| Usage analytics (full) | ❌ | ✅ |

---

## Caching Strategy

| Data | Cache TTL | Notes |
|------|-----------|-------|
| User profile | 5 min (Redis) | Invalidated on update |
| Subscription details | 2 min (Redis) | Invalidated on webhook |
| BYOK key status | 5 min (Redis) | Invalidated on key set/delete |
| Data grants | 30 sec (Redis) | Invalidated on grant changes |
| Tier usage | 2 min (Redis) | Invalidated on any tracked action |

### Cache Key Pattern

```
profile:{userId}              → UserProfileDto
subscription:{userId}          → SubscriptionDetailsDto
ai:byok-status:{userId}       → Boolean
sharing:grants:{userId}        → List<DataAccessGrantDto>
tier:usage:{userId}            → TierUsageDto
```

---

## Security & Access Control

### Authorization Model

| Operation | Access Rule |
|-----------|------------|
| View own profile | Owner only |
| Update own profile | Owner only |
| View own subscription | Owner only |
| Manage BYOK key | Owner only |
| View shared data (grantee) | Requires active DataAccessGrant |
| Manage grants (owner) | Owner only (Premium) |
| Data export | Owner only |
| Account deletion | Owner only |
| Stripe webhook | No auth — signature verified |
| Admin: view user profiles | Admin role only |

### Sensitive Data

- **BYOK API key**: Write-only in API; never returned to client; stored in Key Vault
- **Stripe IDs**: Not exposed to client; internal use only
- **Email**: Read-only in app (sourced from Entra)
- **Deletion reason**: Stored for analytics; not exposed to other users
- **Export files**: Time-limited SAS URLs (1 hour), auto-expire

### Audit Logging

| Event | Logged Data | Severity |
|-------|------------|----------|
| Profile created (JIT) | UserId, Email | Information |
| Profile updated | UserId, ChangedFields | Information |
| Subscription upgraded | UserId, StripeSubscriptionId | Information |
| Subscription cancelled | UserId | Information |
| BYOK key configured | UserId | Information |
| BYOK key removed | UserId | Information |
| Data export requested | UserId, ExportId | Information |
| Data export completed | UserId, ExportId, FileSizeBytes | Information |
| Account deletion requested | UserId | **Warning** |
| Account deletion cancelled | UserId | Information |
| Account deletion executed | UserId | **Warning** |
| Unauthorized access attempt | RequestingUserId, TargetUserId | **Warning** |

---

## Error Codes

| Code | HTTP | Condition |
|------|------|-----------|
| `AUTH_REQUIRED` | 401 | Not authenticated |
| `AUTH_FORBIDDEN` | 403 | Attempting to access another user's profile |
| `VALIDATION_FAILED` | 400 | Invalid profile fields, bad locale/timezone |
| `RESOURCE_NOT_FOUND` | 404 | Profile, export, or grant not found |
| `TIER_PREMIUM_REQUIRED` | 402 | Data sharing requires Premium |
| `AI_INVALID_KEY` | 401 | BYOK key validation failed |
| `DELETION_ALREADY_REQUESTED` | 409 | Account deletion already pending |
| `EXPORT_ALREADY_PENDING` | 409 | Export already in progress |
| `SERVER_ERROR` | 500 | Internal error |

---

## Navigation Entry Points

```
Sidebar: Settings (cog icon)
└── /settings/profile        (default tab)
└── /settings/subscription
└── /settings/ai-keys
└── /settings/sharing
└── /settings/notifications
└── /settings/appearance
└── /settings/export
└── /settings/delete

User avatar dropdown:
├── Profile → /settings/profile
├── Settings → /settings
└── Sign Out
```

---

## Cross-References

- Platform infrastructure & tier model: [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Identity & authentication (Entra, MSAL): [02-identity-authentication.md](02-identity-authentication.md)
- Authorization & DataAccessGrant: [03-authorization-data-access.md](03-authorization-data-access.md)
- AI BYOK architecture: [09-ai-insights.md](09-ai-insights.md)
- Dashboard tier gating: [07-dashboard-reporting.md](07-dashboard-reporting.md)
- Error codes: [01-platform-infrastructure.md](01-platform-infrastructure.md) — `ErrorCodes` static class

---

*Last Updated: February 2026*
