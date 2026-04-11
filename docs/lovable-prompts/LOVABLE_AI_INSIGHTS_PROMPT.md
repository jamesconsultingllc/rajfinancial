# Lovable Prompt — AI Insights Page

## Context

Build the **AI Insights** page for entity-scoped AI-powered financial analysis. This is an entity-level page that renders inside an `EntityProvider` layout. Each entity (Personal, Business, Trust) has its own AI analysis running on that entity's data — income, bills, assets, transactions, and documents. The page provides spending pattern insights, asset recommendations, and document processing (bank/brokerage statement upload & parsing). The app uses a BYOK (Bring Your Own Key) model — Free users provide their own Claude API key, Premium users use the platform key. Follow the same layout patterns as the existing **Assets** page and **Settings** page.

Per the entity structure design doc, the AI analysis widget is part of the Entity Overview page for quick summaries. This dedicated Insights page exists for deeper analysis, historical insight browsing, and document import/parsing.

## Route & Nav

- **Route**: `/:entityType/:slug/insights` (e.g., `/personal/insights`, `/business/acme-llc/insights`, `/trust/family-trust/insights`)
- Renders inside the `EntityProvider` layout — no separate `App.tsx` route registration or `DashboardLayout.tsx` nav entry needed
- **Nav**: Appears in the entity sidebar navigation under the entity's sub-pages. Icon: `Sparkles` from lucide-react. Label: "AI Insights"
- **Page file**: `src/pages/AIInsights.tsx`

## Entity Context

This page renders inside an `EntityProvider`:
```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```
AI analysis runs on this entity's data only — income, bills, assets, transactions, and documents scoped to `entityId`. When viewing a Business entity's insights, all analysis (spending, portfolio, etc.) is scoped to that business's financial data. When viewing a Personal entity, it analyzes personal finances.

## Data Types

Create `src/types/ai-insights.ts`:

```typescript
type InsightCategory = "Spending" | "Assets" | "Estate" | "General";
type InsightStatus = "Pending" | "Processing" | "Complete" | "Failed";

interface InsightRequestDto {
  entityId: string;
  category: InsightCategory;
  prompt?: string; // Optional user prompt
}

interface InsightResponseDto {
  id: string;
  entityId: string;
  category: InsightCategory;
  status: InsightStatus;
  title: string;
  summary: string;
  details: InsightDetail[];
  recommendations: RecommendationItem[];
  createdAt: string;
  processingTimeMs?: number;
}

interface InsightDetail {
  label: string;
  value: string;
  trend?: "up" | "down" | "stable";
  changePercentage?: number;
}

interface RecommendationItem {
  title: string;
  description: string;
  priority: "High" | "Medium" | "Low";
  category: InsightCategory;
  actionLabel?: string;
  actionPath?: string;
}

interface DocumentUploadDto {
  id: string;
  entityId: string;
  fileName: string;
  fileType: string;
  fileSize: number;
  status: "Uploaded" | "Processing" | "Parsed" | "Failed";
  transactionsFound?: number;
  uploadedAt: string;
  parsedAt?: string;
  error?: string;
}

interface AIUsageDto {
  monthlyLimit: number;     // 3 for Free, -1 for unlimited
  usedThisMonth: number;
  remainingThisMonth: number;
  hasApiKey: boolean;        // BYOK key configured
  isPremium: boolean;
}

interface TransactionImportPreview {
  transactions: ImportedTransaction[];
  duplicateCount: number;
  newCount: number;
  accountName?: string;
  statementPeriod?: string;
}

interface ImportedTransaction {
  date: string;
  description: string;
  amount: number;
  category?: string;
  isDuplicate: boolean;
}
```

## API Service

Create `src/services/ai-insights-service.ts` following the same TanStack Query pattern. All endpoints are entity-scoped:

- `GET /api/entities/{entityId}/insights` -> `useInsights(entityId)` — list past insights for entity
- `POST /api/entities/{entityId}/insights` -> `useRequestInsight(entityId, request)` — request new AI analysis scoped to entity
- `GET /api/entities/{entityId}/insights/usage` -> `useAIUsage(entityId)` — get usage limits for entity
- `POST /api/entities/{entityId}/documents/upload` -> `useUploadDocument(entityId, file)` — upload statement to entity's storage
- `GET /api/entities/{entityId}/documents` -> `useDocuments(entityId)` — list uploaded documents for entity
- Use mock data initially.

## Mock Data

**Usage**: Free tier, 3/month limit, 1 used, 2 remaining, no API key configured

**Past Insights** (2 items):
1. Spending Analysis — "Complete", title: "March 2026 Spending Analysis", summary: "Your spending is 12% higher than last month, driven by dining and entertainment...", 4 details (total spent, vs last month, top category, savings rate), 3 recommendations
2. Asset Review — "Complete", title: "Portfolio Diversification Review", summary: "Your portfolio is heavily concentrated in real estate (45%). Consider diversifying...", 3 details, 2 recommendations

**Documents** (1 item):
- "chase_statement_feb2026.pdf", 245KB, "Parsed", 47 transactions found

**Import Preview** (for document):
- 47 transactions, 3 duplicates, 44 new, account "Chase Checking", period "Feb 1–28, 2026"

## Page Layout

### Top Section — Usage & API Key Banner

Conditional banner at top:

**If no API key configured (Free tier)**:
```
[Key icon] Set up your AI key to get started
Configure your Claude API key in Settings to use AI-powered insights.
[Configure Key] button -> navigates to /settings/integrations/ai-keys
```

**If API key configured**:
```
Usage: 1 of 3 insights used this month [==----] (progress bar)
[Premium badge if applicable]
```

### Tab Navigation

Horizontal tabs:
`Insights | Document Import`

### Insights Tab (default)

**Quick Analysis Cards** — Row of 3-4 clickable cards (`grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4`):

| Card | Icon | Label | Description |
|------|------|-------|-------------|
| Spending | `PieChart` | "Analyze Spending" | "AI analysis of this entity's spending patterns" |
| Assets | `BarChart3` | "Review Portfolio" | "Diversification and allocation insights" |
| Estate | `Shield` | "Estate Check" | "Beneficiary and coverage recommendations" |
| Custom | `MessageSquare` | "Ask Anything" | "Ask a custom financial question about this entity" |

Clicking a card triggers `useRequestInsight(entityId, request)` (or opens custom prompt dialog for "Ask Anything"). All analysis is scoped to the current entity's data. For example, "Analyze Spending" on a Business entity analyzes that business's spending; on a Personal entity it analyzes personal spending.

**Rate Limit Warning** — Show when approaching limit:
```
You have 1 insight remaining this month. Upgrade to Premium for unlimited insights.
```

**Past Insights List** — Below quick actions, list of previous insight results:

Each insight card:
- **Header**: Title + category badge + timestamp
- **Status**: Complete (green check), Processing (spinner), Failed (red x)
- **Summary**: 2-3 line preview text
- **Details Grid**: Key metrics in 2x2 grid with trend arrows (up green, down red, stable gray)
- **Recommendations**: Expandable accordion section with priority badges (High=red, Medium=amber, Low=blue)
- **Actions**: "View Full Analysis" expand/collapse

**Empty State**:
- Sparkles icon
- "No insights yet"
- "Run your first AI analysis to get personalized financial recommendations"
- Quick action buttons

### Document Import Tab

Documents uploaded here are stored in the entity's storage connection (OneDrive, Google Drive, or Dropbox). The entity must have a storage connection configured before documents can be uploaded. If no storage connection exists, show a prompt to configure one in the entity's settings.

**Upload Section**:
- Drag-and-drop zone with dashed border
- Accepted formats: PDF, CSV, OFX, QFX
- Max file size: 10MB
- Upload button as alternative to drag-drop
- Progress bar during upload

**Uploaded Documents List**:
- Table (desktop) / Card list (mobile):

| Column | Content |
|--------|---------|
| File Name | Name + file type icon |
| Size | Formatted (e.g., "245 KB") |
| Status | Badge: Uploaded (blue), Processing (amber spinner), Parsed (green), Failed (red) |
| Transactions | Count found (e.g., "47 transactions") |
| Uploaded | Relative timestamp |
| Actions | "Import" (if Parsed), "Retry" (if Failed), "Delete" |

**Import Preview Modal** (shown when clicking "Import" on a parsed document):
- Dialog/Sheet showing:
  - Statement period and account name
  - Summary: "44 new transactions, 3 duplicates"
  - Scrollable transaction list with checkboxes
  - Duplicate rows shown grayed out with "Duplicate" badge
  - Each row: date, description, amount (green/red), category
  - "Select All New" / "Deselect All" toggles
  - "Import Selected" primary button + "Cancel"

**Empty State**:
- FileUp icon
- "No documents uploaded"
- "Upload bank or brokerage statements to automatically import transactions"
- Upload button

## Custom Prompt Dialog

When user clicks "Ask Anything":
- Dialog with text area (max 500 chars)
- Character count indicator
- Category selector dropdown (optional, defaults to "General")
- Context note: "Analysis will be scoped to {entityName}'s data"
- "Analyze" submit button
- Examples shown as placeholder or hint text: "What's my savings rate trend?", "Should I refinance my mortgage?", "How can I reduce my monthly expenses?"

## Premium Tier Gating

- **Free tier**: 3 insights/month, must provide own API key (BYOK), basic document import
- **Premium**: Unlimited insights, platform API key, advanced document parsing
- Show upgrade prompt when limit reached:
```tsx
<Card className="border-amber-200 bg-amber-50 dark:bg-amber-950/20">
  <CardContent className="flex items-center gap-4 py-4">
    <Crown className="h-8 w-8 text-amber-500" />
    <div>
      <p className="font-medium">Monthly limit reached</p>
      <p className="text-sm text-muted-foreground">Upgrade to Premium for unlimited AI insights</p>
    </div>
    <Button variant="outline" className="ml-auto">Upgrade</Button>
  </CardContent>
</Card>
```

## Skeleton Loading

- Usage banner: skeleton bar
- Quick action cards: 4 skeleton cards
- Insights list: 2-3 skeleton cards with `h-48`
- Documents table: skeleton rows

## Accessibility

- Drag-drop zone has keyboard alternative (button) and `aria-label`
- File upload uses proper `<input type="file">` with label
- Progress indicators use `role="progressbar"` with `aria-valuenow`
- Dialog uses `role="dialog"` with `aria-labelledby`
- Tab navigation uses `role="tablist"` / `role="tab"` / `role="tabpanel"`
- Status badges include `aria-label` for screen readers (e.g., `aria-label="Status: Processing"`)
- All interactive elements have minimum 44x44px touch targets

## i18n

- All text uses `useTranslation()` with `t("insights.usage.remaining")` pattern
- Create translation keys in `src/locales/en/insights.json`
- Date/number formatting via `Intl` APIs
