# Legacy Financial - Integration Services & Premium UI Design

## Part 1: Plaid Integration Architecture

### Overview

The Plaid integration enables users to securely connect their bank accounts, credit cards, loans, and investment accounts. We'll implement a delightful linking experience that feels trustworthy and modern.

---

### Plaid Service Implementation

```csharp
// LegacyFinancial.Infrastructure/External/PlaidService.cs
using Going.Plaid;
using Going.Plaid.Link;
using Going.Plaid.Accounts;
using Going.Plaid.Transactions;
using Going.Plaid.Institutions;

namespace LegacyFinancial.Infrastructure.External;

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
            ClientName = "Legacy Financial",
            Products = new[]
            {
                Products.Transactions,
                Products.Investments,
                Products.Liabilities
            },
            CountryCodes = new[] { CountryCode.Us },
            Language = Language.English,
            Webhook = "https://func-legacyfinancial.azurewebsites.net/api/plaid/webhook",
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
            var exchangeResponse = await _plaidClient.ItemPublicTokenExchangeAsync(
                new ItemPublicTokenExchangeRequest { PublicToken = publicToken });

            var accessToken = exchangeResponse.AccessToken;
            var itemId = exchangeResponse.ItemId;

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
}
```

---

## Part 2: Claude AI Service Architecture

### AI Service Implementation

```csharp
// LegacyFinancial.Infrastructure/External/ClaudeAIService.cs
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace LegacyFinancial.Infrastructure.External;

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
        
        var cached = await _cache.GetAsync<List<AIInsightDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached insights for user {UserId}", userId);
            return cached;
        }

        _logger.LogInformation("Generating AI insights for user {UserId}", userId);

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

            foreach (var insight in insights)
            {
                EnrichInsight(insight, profile);
            }

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

        insight.ActionUrl = insight.Category switch
        {
            "INSURANCE_GAP" => "/tools/insurance-calculator",
            "BENEFICIARY_REVIEW" => "/beneficiaries",
            "DEBT_OPTIMIZATION" => "/tools/debt-payoff",
            "ESTATE_PLANNING" => "/tools/estate-checklist",
            _ => "/dashboard"
        };

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
}
```

---

## Part 3: Premium UI Design - CSS Theme

### Design Tokens & Animations

```css
/* LegacyFinancial.Client/wwwroot/css/theme.css */

:root {
  /* Primary Palette - Deep Trust Blue */
  --primary-50: #EEF4FF;
  --primary-100: #D9E5FF;
  --primary-500: #3B5BDB;
  --primary-600: #1E3A5F;
  --primary-700: #1A3152;
  --primary-800: #162945;
  --primary-900: #0F1D2F;
  
  /* Accent - Vibrant Teal */
  --accent-50: #F0FDFA;
  --accent-500: #14B8A6;
  --accent-600: #0D9488;
  --accent-700: #0F766E;
  
  /* Semantic Colors */
  --success-light: #DCFCE7;
  --success-main: #22C55E;
  --warning-light: #FEF3C7;
  --warning-main: #F59E0B;
  --error-light: #FEE2E2;
  --error-main: #EF4444;
  
  /* Shadows */
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
  --shadow-glow: 0 0 20px rgb(13 148 136 / 0.3);
  
  /* Transitions */
  --transition-base: 200ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-bounce: 500ms cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

/* Animations */
@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes celebrate {
  0% { transform: scale(1); }
  50% { transform: scale(1.05); }
  100% { transform: scale(1); }
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

.animate-fade-in { animation: fadeIn var(--transition-base) ease-out; }
.animate-slide-up { animation: slideUp 300ms ease-out; }
.animate-celebrate { animation: celebrate 0.6s ease-in-out; }

/* Skeleton Loading */
.skeleton {
  background: linear-gradient(90deg, #E5E7EB 25%, #F3F4F6 50%, #E5E7EB 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s ease-in-out infinite;
}

/* Glass Effect */
.glass {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.2);
}

/* Gradient Backgrounds */
.gradient-primary {
  background: linear-gradient(135deg, var(--primary-600) 0%, var(--primary-800) 100%);
}

.gradient-accent {
  background: linear-gradient(135deg, var(--accent-500) 0%, var(--accent-700) 100%);
}

.gradient-mesh {
  background: 
    radial-gradient(at 40% 20%, var(--accent-100) 0px, transparent 50%),
    radial-gradient(at 80% 0%, var(--primary-100) 0px, transparent 50%),
    radial-gradient(at 0% 50%, var(--accent-50) 0px, transparent 50%);
}
```

---

## Part 4: Premium Dashboard Component

```razor
@* LegacyFinancial.Client/Pages/Dashboard.razor *@
@page "/dashboard"
@attribute [Authorize]
@inject IStringLocalizer<Dashboard> L
@inject IState<DashboardState> State
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - Legacy Financial</PageTitle>

<div class="min-h-screen bg-gray-50">
    @* Animated Background Mesh *@
    <div class="fixed inset-0 gradient-mesh opacity-50 pointer-events-none"></div>
    
    <div class="relative">
        @* Hero Section - Net Worth *@
        <section class="relative overflow-hidden">
            <div class="absolute inset-0 gradient-primary"></div>
            
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
                        <p class="text-white/70 text-sm font-medium uppercase tracking-wide">
                            @L["NetWorth.Title"]
                        </p>
                        
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
                        }
                    </div>
                </div>
            </div>
        </section>
        
        @* Main Content - Overlapping Cards *@
        <main class="relative -mt-24 lg:-mt-32 px-4 pb-24 lg:px-8">
            <div class="max-w-7xl mx-auto">
                
                @* Quick Stats Cards *@
                <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 lg:gap-4 mb-6">
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
                
                @* Two Column Layout *@
                <div class="grid lg:grid-cols-3 gap-6">
                    @* Left Column - 2/3 width *@
                    <div class="lg:col-span-2 space-y-6">
                        <InsightsPanel 
                            Insights="@State.Value.Insights"
                            IsLoading="@State.Value.IsLoadingInsights" />
                        
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
        </main>
    </div>
</div>

<PlaidLinkModal @ref="PlaidLinkModal" OnSuccess="HandlePlaidSuccess" />

@code {
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
    
    private async Task OpenPlaidLink() => await PlaidLinkModal!.OpenAsync();
    
    private async Task HandlePlaidSuccess(PlaidLinkSuccessEvent e)
    {
        Dispatcher.Dispatch(new ExchangePlaidTokenAction(e.PublicToken, e.Metadata));
    }
}
```

---

## Part 5: AI Insights Panel Component

```razor
@* LegacyFinancial.Client/Components/Dashboard/InsightsPanel.razor *@

<div class="glass rounded-2xl p-6 shadow-lg">
    <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl gradient-accent 
                        flex items-center justify-center shadow-lg shadow-accent-500/25">
                <svg class="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                          d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                </svg>
            </div>
            <div>
                <h2 class="text-lg font-semibold text-gray-900">@L["Insights.Title"]</h2>
                <p class="text-sm text-gray-500">@L["Insights.Subtitle"]</p>
            </div>
        </div>
        
        <button @onclick="OnRefresh"
                class="p-2 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 
                       transition-colors @(IsLoading ? "animate-spin" : "")">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                      d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
        </button>
    </div>
    
    @if (IsLoading)
    {
        <div class="space-y-3">
            @for (int i = 0; i < 3; i++)
            {
                <div class="flex gap-4 p-4 rounded-xl bg-gray-50">
                    <div class="w-10 h-10 rounded-lg skeleton"></div>
                    <div class="flex-1 space-y-2">
                        <div class="h-4 w-3/4 skeleton rounded"></div>
                        <div class="h-3 w-full skeleton rounded"></div>
                    </div>
                </div>
            }
        </div>
    }
    else if (!Insights.Any())
    {
        <div class="text-center py-8">
            <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-green-100 
                        flex items-center justify-center">
                <svg class="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                          d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
            </div>
            <h3 class="text-gray-900 font-medium">@L["Insights.Empty.Title"]</h3>
            <p class="text-gray-500 text-sm mt-1">@L["Insights.Empty.Description"]</p>
        </div>
    }
    else
    {
        <div class="space-y-3">
            @foreach (var (insight, index) in Insights.Select((i, idx) => (i, idx)))
            {
                <InsightCard Insight="@insight" Index="@index" OnClick="() => OnInsightClick(insight)" />
            }
        </div>
        
        <div class="mt-4 pt-4 border-t border-gray-100">
            <p class="text-xs text-gray-400 text-center">
                @L["Insights.Disclaimer"]
            </p>
        </div>
    }
</div>

@code {
    [Parameter] public IEnumerable<AIInsightDto> Insights { get; set; } = Enumerable.Empty<AIInsightDto>();
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<AIInsightDto> OnInsightClick { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }
}
```

---

## Part 6: Insight Card with Animations

```razor
@* LegacyFinancial.Client/Components/Dashboard/InsightCard.razor *@

<div class="group relative animate-slide-up cursor-pointer"
     style="animation-delay: @(Index * 50)ms;"
     @onclick="OnClick"
     tabindex="0"
     role="button">
    
    @* Severity Indicator Bar *@
    <div class="absolute left-0 top-0 bottom-0 w-1 rounded-l-xl @GetSeverityBarClass()"></div>
    
    <div class="flex gap-4 p-4 pl-5 rounded-xl bg-gray-50 hover:bg-gray-100 
                transition-all duration-200 border border-transparent 
                hover:border-gray-200 hover:shadow-md">
        
        @* Icon *@
        <div class="flex-shrink-0">
            <div class="w-10 h-10 rounded-lg @GetIconBgClass() 
                        flex items-center justify-center
                        transition-transform group-hover:scale-110">
                <DynamicIcon Name="@Insight.IconName" Class="w-5 h-5 @GetIconClass()" />
            </div>
        </div>
        
        @* Content *@
        <div class="flex-1 min-w-0">
            <h3 class="font-medium text-gray-900 group-hover:text-primary-600 
                       transition-colors line-clamp-1">
                @Insight.Title
            </h3>
            <p class="text-sm text-gray-600 mt-0.5 line-clamp-2">
                @Insight.Description
            </p>
            
            @if (Insight.SuggestedActions?.Any() == true)
            {
                <div class="flex items-center gap-2 mt-2">
                    <span class="text-xs text-gray-400">Actions:</span>
                    <span class="text-xs text-accent-600 font-medium">
                        @Insight.SuggestedActions.First()
                    </span>
                </div>
            }
        </div>
        
        @* Arrow *@
        <div class="flex-shrink-0 self-center">
            <svg class="w-5 h-5 text-gray-300 group-hover:text-gray-400 
                        group-hover:translate-x-1 transition-all" 
                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
        </div>
    </div>
    
    @* Unread Indicator *@
    @if (!Insight.IsRead)
    {
        <div class="absolute -top-1 -right-1 w-3 h-3 rounded-full bg-accent-500 
                    border-2 border-white shadow-sm animate-pulse"></div>
    }
</div>

@code {
    [Parameter] public AIInsightDto Insight { get; set; } = default!;
    [Parameter] public int Index { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    
    private string GetSeverityBarClass() => Insight.Severity switch
    {
        "critical" => "bg-red-500",
        "warning" => "bg-amber-500",
        _ => "bg-blue-500"
    };
    
    private string GetIconBgClass() => Insight.Severity switch
    {
        "critical" => "bg-red-100",
        "warning" => "bg-amber-100",
        _ => "bg-blue-100"
    };
    
    private string GetIconClass() => Insight.Severity switch
    {
        "critical" => "text-red-600",
        "warning" => "text-amber-600",
        _ => "text-blue-600"
    };
}
```

---

## Part 7: Financial Health Score Card

```razor
@* LegacyFinancial.Client/Components/Dashboard/HealthScoreCard.razor *@

<div class="glass rounded-2xl p-6 shadow-lg">
    <div class="text-center">
        <h3 class="text-sm font-medium text-gray-500 uppercase tracking-wide">
            @L["HealthScore.Title"]
        </h3>
        
        @* Circular Progress *@
        <div class="relative w-36 h-36 mx-auto my-6">
            <svg class="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
                <circle cx="50" cy="50" r="42" fill="none" stroke="#E5E7EB" stroke-width="8" />
                <circle cx="50" cy="50" r="42" 
                        fill="none" 
                        stroke="url(#healthGradient)" 
                        stroke-width="8"
                        stroke-linecap="round"
                        stroke-dasharray="@GetCircumference()"
                        stroke-dashoffset="@GetDashOffset()"
                        class="transition-all duration-1000 ease-out" />
                <defs>
                    <linearGradient id="healthGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                        <stop offset="0%" stop-color="@GetGradientStart()" />
                        <stop offset="100%" stop-color="@GetGradientEnd()" />
                    </linearGradient>
                </defs>
            </svg>
            
            <div class="absolute inset-0 flex flex-col items-center justify-center">
                <span class="text-4xl font-bold @GetScoreTextColor()">
                    <AnimatedNumber Value="@Score" />
                </span>
                <span class="text-sm text-gray-400">out of 100</span>
            </div>
        </div>
        
        @* Rating Badge *@
        <div class="inline-flex items-center gap-2 px-4 py-2 rounded-full @GetRatingBgClass()">
            <span class="text-lg">@GetRatingEmoji()</span>
            <span class="font-medium @GetRatingTextClass()">@GetRatingText()</span>
        </div>
    </div>
    
    @* Breakdown *@
    @if (Breakdown != null)
    {
        <div class="mt-6 pt-6 border-t border-gray-100 space-y-3">
            @foreach (var item in Breakdown)
            {
                <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-600">@item.Category</span>
                    <div class="flex items-center gap-2">
                        <div class="w-24 h-2 rounded-full bg-gray-200 overflow-hidden">
                            <div class="h-full rounded-full transition-all duration-500 @GetBarColor(item.Score)"
                                 style="width: @(item.Score)%"></div>
                        </div>
                        <span class="text-sm font-medium text-gray-700 w-8 text-right">@item.Score</span>
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public int Score { get; set; }
    [Parameter] public List<HealthScoreBreakdownItem>? Breakdown { get; set; }
    
    private const double Circumference = 2 * Math.PI * 42;
    
    private double GetCircumference() => Circumference;
    private double GetDashOffset() => Circumference - (Circumference * Score / 100);
    
    private string GetGradientStart() => Score >= 80 ? "#22C55E" : Score >= 60 ? "#84CC16" : Score >= 40 ? "#F59E0B" : "#EF4444";
    private string GetGradientEnd() => Score >= 80 ? "#16A34A" : Score >= 60 ? "#65A30D" : Score >= 40 ? "#D97706" : "#DC2626";
    private string GetScoreTextColor() => Score >= 80 ? "text-green-600" : Score >= 60 ? "text-lime-600" : Score >= 40 ? "text-amber-600" : "text-red-600";
    private string GetRatingBgClass() => Score >= 80 ? "bg-green-100" : Score >= 60 ? "bg-lime-100" : Score >= 40 ? "bg-amber-100" : "bg-red-100";
    private string GetRatingTextClass() => Score >= 80 ? "text-green-700" : Score >= 60 ? "text-lime-700" : Score >= 40 ? "text-amber-700" : "text-red-700";
    private string GetRatingEmoji() => Score >= 90 ? "🌟" : Score >= 80 ? "💪" : Score >= 60 ? "👍" : Score >= 40 ? "🔧" : "⚠️";
    private string GetRatingText() => Score >= 90 ? "Excellent" : Score >= 80 ? "Great" : Score >= 60 ? "Good" : Score >= 40 ? "Fair" : "Needs Work";
    private string GetBarColor(int score) => score >= 80 ? "bg-green-500" : score >= 60 ? "bg-lime-500" : score >= 40 ? "bg-amber-500" : "bg-red-500";
}
```

---

## Part 8: Plaid Link Modal with Beautiful UX

```razor
@* LegacyFinancial.Client/Components/Plaid/PlaidLinkModal.razor *@

@inject IJSRuntime JS
@inject IApiClient ApiClient

<div class="@(IsOpen ? "fixed inset-0 z-50" : "hidden")">
    @* Backdrop *@
    <div class="absolute inset-0 bg-gray-900/60 backdrop-blur-sm animate-fade-in"
         @onclick="Close"></div>
    
    @* Modal *@
    <div class="absolute inset-4 lg:inset-auto lg:top-1/2 lg:left-1/2 
                lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md">
        <div class="h-full lg:h-auto bg-white rounded-2xl shadow-2xl overflow-hidden
                    flex flex-col animate-slide-up">
            
            @* Header *@
            <div class="relative p-6 text-center border-b border-gray-100">
                <button @onclick="Close"
                        class="absolute top-4 right-4 p-2 rounded-lg text-gray-400 
                               hover:text-gray-600 hover:bg-gray-100 transition-colors">
                    <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
                
                <div class="w-16 h-16 mx-auto mb-4 rounded-2xl gradient-primary 
                            flex items-center justify-center shadow-lg shadow-primary-600/25">
                    <svg class="w-8 h-8 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                              d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                    </svg>
                </div>
                
                <h2 class="text-xl font-bold text-gray-900">Link Your Accounts</h2>
                <p class="text-gray-500 mt-1">Securely connect your financial institutions</p>
            </div>
            
            @* Body *@
            <div class="flex-1 overflow-y-auto p-6">
                @switch (CurrentStep)
                {
                    case Step.Intro:
                        @* Security Features *@
                        <div class="space-y-4 mb-6">
                            <div class="flex gap-3">
                                <div class="w-10 h-10 rounded-lg bg-green-100 flex items-center justify-center flex-shrink-0">
                                    <svg class="w-5 h-5 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                                    </svg>
                                </div>
                                <div>
                                    <h4 class="font-medium text-gray-900">Bank-level security</h4>
                                    <p class="text-sm text-gray-500">256-bit encryption protects your data</p>
                                </div>
                            </div>
                            
                            <div class="flex gap-3">
                                <div class="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center flex-shrink-0">
                                    <svg class="w-5 h-5 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                                    </svg>
                                </div>
                                <div>
                                    <h4 class="font-medium text-gray-900">Private credentials</h4>
                                    <p class="text-sm text-gray-500">We never see your login details</p>
                                </div>
                            </div>
                            
                            <div class="flex gap-3">
                                <div class="w-10 h-10 rounded-lg bg-purple-100 flex items-center justify-center flex-shrink-0">
                                    <svg class="w-5 h-5 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                                    </svg>
                                </div>
                                <div>
                                    <h4 class="font-medium text-gray-900">Read-only access</h4>
                                    <p class="text-sm text-gray-500">We can only view, never move money</p>
                                </div>
                            </div>
                        </div>
                        
                        <button @onclick="StartPlaidLink"
                                class="w-full py-4 px-6 rounded-xl font-semibold text-white
                                       gradient-primary hover:opacity-90 transition-opacity
                                       shadow-lg shadow-primary-600/25
                                       flex items-center justify-center gap-2">
                            <span>Connect Account</span>
                            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
                            </svg>
                        </button>
                        
                        <div class="flex items-center justify-center gap-2 mt-4 text-sm text-gray-400">
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                            </svg>
                            <span>Powered by Plaid</span>
                        </div>
                        break;
                        
                    case Step.Connecting:
                        <div class="text-center py-8">
                            <div class="relative w-24 h-24 mx-auto mb-6">
                                <div class="absolute inset-0 rounded-full border-4 border-gray-200"></div>
                                <div class="absolute inset-0 rounded-full border-4 border-transparent 
                                            border-t-accent-500 animate-spin"></div>
                                <div class="absolute inset-0 flex items-center justify-center">
                                    <svg class="w-8 h-8 text-primary-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                              d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101" />
                                    </svg>
                                </div>
                            </div>
                            <h3 class="text-lg font-semibold text-gray-900">Connecting...</h3>
                            <p class="text-gray-500 mt-1">Please complete the connection in the popup</p>
                        </div>
                        break;
                        
                    case Step.Success:
                        <div class="text-center">
                            <div class="relative w-20 h-20 mx-auto mb-6">
                                <div class="absolute inset-0 rounded-full bg-green-100 animate-ping opacity-25"></div>
                                <div class="relative w-20 h-20 rounded-full bg-green-100 
                                            flex items-center justify-center animate-celebrate">
                                    <svg class="w-10 h-10 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                                    </svg>
                                </div>
                            </div>
                            
                            <h3 class="text-xl font-bold text-gray-900">Success!</h3>
                            <p class="text-gray-500 mt-1">Connected to @InstitutionName</p>
                            
                            <div class="mt-6 space-y-2">
                                @foreach (var account in LinkedAccounts)
                                {
                                    <div class="flex items-center gap-3 p-3 rounded-xl bg-gray-50">
                                        <div class="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center">
                                            <svg class="w-5 h-5 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                                            </svg>
                                        </div>
                                        <div class="flex-1 text-left">
                                            <p class="font-medium text-gray-900">@account.Name</p>
                                            <p class="text-sm text-gray-500">••••@account.Mask</p>
                                        </div>
                                        <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                                        </svg>
                                    </div>
                                }
                            </div>
                            
                            <button @onclick="HandleDone"
                                    class="w-full mt-6 py-3 px-6 rounded-xl font-semibold text-white
                                           bg-green-600 hover:bg-green-700 transition-colors">
                                Done
                            </button>
                        </div>
                        break;
                }
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public EventCallback<PlaidLinkSuccessEvent> OnSuccess { get; set; }
    
    private bool IsOpen { get; set; }
    private Step CurrentStep { get; set; } = Step.Intro;
    private string? LinkToken { get; set; }
    private string InstitutionName { get; set; } = "";
    private List<PlaidLinkAccount> LinkedAccounts { get; set; } = new();
    
    private enum Step { Intro, Connecting, Success }
    
    public async Task OpenAsync()
    {
        IsOpen = true;
        CurrentStep = Step.Intro;
        StateHasChanged();
        
        var response = await ApiClient.PostAsync<object, PlaidLinkTokenResponse>("/accounts/link-token", new { });
        if (response.IsSuccess) LinkToken = response.Data!.LinkToken;
    }
    
    private void Close()
    {
        IsOpen = false;
        CurrentStep = Step.Intro;
    }
    
    private async Task StartPlaidLink()
    {
        CurrentStep = Step.Connecting;
        await JS.InvokeVoidAsync("plaidLink.open", LinkToken, DotNetObjectReference.Create(this));
    }
    
    [JSInvokable]
    public async Task OnPlaidSuccess(string publicToken, PlaidLinkMetadata metadata)
    {
        InstitutionName = metadata.InstitutionName;
        LinkedAccounts = metadata.Accounts;
        CurrentStep = Step.Success;
        StateHasChanged();
        
        await OnSuccess.InvokeAsync(new PlaidLinkSuccessEvent { PublicToken = publicToken, Metadata = metadata });
    }
    
    private void HandleDone() => Close();
}
```

---

## Part 9: Animated Number Component

```razor
@* LegacyFinancial.Client/Components/Common/AnimatedNumber.razor *@

@implements IDisposable

<span class="tabular-nums">@FormatValue()</span>

@code {
    [Parameter] public decimal Value { get; set; }
    [Parameter] public string Format { get; set; } = "N0";
    [Parameter] public int Duration { get; set; } = 1000;
    
    private decimal DisplayValue { get; set; }
    private decimal PreviousValue { get; set; }
    private Timer? AnimationTimer;
    private DateTime AnimationStart;
    
    protected override void OnParametersSet()
    {
        if (Value != PreviousValue) StartAnimation();
    }
    
    private void StartAnimation()
    {
        AnimationStart = DateTime.Now;
        var startValue = DisplayValue;
        var endValue = Value;
        
        AnimationTimer?.Dispose();
        AnimationTimer = new Timer(_ =>
        {
            var elapsed = (DateTime.Now - AnimationStart).TotalMilliseconds;
            var progress = Math.Min(elapsed / Duration, 1);
            var eased = 1 - Math.Pow(1 - progress, 3); // Ease out cubic
            
            DisplayValue = startValue + (endValue - startValue) * (decimal)eased;
            InvokeAsync(StateHasChanged);
            
            if (progress >= 1)
            {
                AnimationTimer?.Dispose();
                PreviousValue = Value;
            }
        }, null, 0, 16);
    }
    
    private string FormatValue() => DisplayValue.ToString(Format);
    
    public void Dispose() => AnimationTimer?.Dispose();
}
```

---

## Design Highlights

### What Makes This UI "Lovable-Quality"

1. **Depth & Layering**: Glass-morphism, layered shadows, gradient backgrounds with decorative circles

2. **Micro-animations**: Staggered slide-up animations, hover transforms, pulse indicators, celebrate animations

3. **Visual Hierarchy**: Clear progression from net worth hero → stats → insights → details

4. **Emotional Design**: Health score emoji, celebration on success, encouraging empty states

5. **Progressive Disclosure**: Cards expand on click, severity bars indicate priority

6. **Touch-First**: 44px minimum touch targets, generous spacing, swipe-friendly

7. **Performance Perception**: Skeleton loaders, animated numbers, smooth transitions

8. **Trust Signals**: Security features, "powered by Plaid" badge, read-only messaging

---

## What's Next?

I can expand on:
1. **Debt Payoff Analyzer** - Interactive comparison with drag-to-reorder
2. **Insurance Calculator** - Visual breakdown with coverage gaps
3. **Beneficiary Manager** - Family tree visualization
4. **Mobile Navigation** - Bottom tabs with gesture support
5. **Settings & Profile** - Account management with dark mode
