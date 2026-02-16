import json

with open('tools/insomnia/raj-financial-auth-tests.json', 'r') as f:
    data = json.load(f)

existing = data['resources']

# Update workspace metadata
for r in existing:
    if r['_id'] == 'wrk_rajfinancial':
        r['name'] = 'RAJ Financial - API Tests'
        r['description'] = 'Test endpoints for validating Entra External ID authentication, role-based access control, user profiles, and asset CRUD operations.'

# Add asset_id to base environment
for r in existing:
    if r['_id'] == 'env_base':
        r['data']['asset_id'] = ''

data['__export_date'] = '2026-02-16T00:00:00.000Z'

# OAuth2 auth template
def oauth2_auth():
    return {
        "type": "oauth2",
        "grantType": "authorization_code",
        "authorizationUrl": "{{ _.authority }}/oauth2/v2.0/authorize",
        "accessTokenUrl": "{{ _.authority }}/oauth2/v2.0/token",
        "clientId": "{{ _.client_id }}",
        "scope": "{{ _.scopes }}",
        "redirectUrl": "http://localhost/callback",
        "usePkce": True,
        "pkceMethod": "S256",
        "tokenPrefix": "Bearer",
        "credentialsInBody": True
    }

# Request defaults
def req_defaults():
    return {
        "settingSendCookies": True,
        "settingStoreCookies": True,
        "settingDisableRenderRequestBody": False,
        "settingEncodeUrl": True,
        "settingRebuildPath": True,
        "settingFollowRedirects": "global"
    }

# New resources to add
new_resources = [
    # Profile folder
    {
        "_id": "fld_profile",
        "_type": "request_group",
        "name": "Profile Endpoints",
        "description": "User profile endpoints (database-backed, JIT provisioned)",
        "parentId": "wrk_rajfinancial"
    },
    # GET /profile/me
    {
        "_id": "req_profile_me",
        "_type": "request",
        "name": "GET /profile/me (My Persisted Profile)",
        "description": "Returns the authenticated user's database-backed profile created by JIT provisioning.",
        "method": "GET",
        "url": "{{ _.endpoint }}/profile/me",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "authentication": oauth2_auth(),
        "body": {},
        "parentId": "fld_profile",
        **req_defaults()
    },
    # Assets folder
    {
        "_id": "fld_assets",
        "_type": "request_group",
        "name": "Asset Endpoints",
        "description": "CRUD operations for assets. All endpoints require authentication. Authorization uses three-tier access: owner > DataAccessGrant > administrator.",
        "parentId": "wrk_rajfinancial"
    },
    # GET /assets
    {
        "_id": "req_assets_list",
        "_type": "request",
        "name": "GET /assets (List Assets)",
        "description": "Retrieves all assets for the authenticated user. Query params: ownerUserId (Guid), type (AssetType enum), includeDisposed (bool).",
        "method": "GET",
        "url": "{{ _.endpoint }}/assets",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "parameters": [],
        "authentication": oauth2_auth(),
        "body": {},
        "parentId": "fld_assets",
        **req_defaults()
    },
    # GET /assets?type=RealEstate
    {
        "_id": "req_assets_list_filtered",
        "_type": "request",
        "name": "GET /assets?type=RealEstate (Filtered by Type)",
        "description": "List assets filtered by type. AssetType: RealEstate=0, Vehicle=1, Investment=2, Retirement=3, BankAccount=4, Insurance=5, Business=6, PersonalProperty=7, Collectible=8, Cryptocurrency=9, IntellectualProperty=10, Other=99.",
        "method": "GET",
        "url": "{{ _.endpoint }}/assets?type=RealEstate&includeDisposed=false",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "authentication": oauth2_auth(),
        "body": {},
        "parentId": "fld_assets",
        **req_defaults()
    },
    # GET /assets/{id}
    {
        "_id": "req_assets_get_by_id",
        "_type": "request",
        "name": "GET /assets/{id} (Get Asset Detail)",
        "description": "Retrieves a single asset by ID with full details including depreciation, disposal info, and beneficiary assignments. Returns 404 if not found.",
        "method": "GET",
        "url": "{{ _.endpoint }}/assets/{{ _.asset_id }}",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "authentication": oauth2_auth(),
        "body": {},
        "parentId": "fld_assets",
        **req_defaults()
    },
    # POST /assets (Real Estate)
    {
        "_id": "req_assets_create",
        "_type": "request",
        "name": "POST /assets (Create Real Estate)",
        "description": "Creates a new real estate asset. Required: name, type, currentValue. Optional: purchasePrice, purchaseDate, description, location, accountNumber, institutionName, depreciationMethod, salvageValue, usefulLifeMonths, inServiceDate, marketValue, lastValuationDate. Returns 201 Created.",
        "method": "POST",
        "url": "{{ _.endpoint }}/assets",
        "headers": [
            {"name": "Content-Type", "value": "application/json"},
            {"name": "Accept", "value": "application/json"}
        ],
        "authentication": oauth2_auth(),
        "body": {
            "mimeType": "application/json",
            "text": json.dumps({
                "name": "Primary Residence",
                "type": 0,
                "currentValue": 450000.00,
                "purchasePrice": 350000.00,
                "purchaseDate": "2020-06-15T00:00:00Z",
                "description": "4BR/3BA single-family home",
                "location": "123 Main St, Springfield, IL",
                "marketValue": 475000.00,
                "lastValuationDate": "2025-12-01T00:00:00Z"
            }, indent=2)
        },
        "parentId": "fld_assets",
        **req_defaults()
    },
    # POST /assets (Vehicle with depreciation)
    {
        "_id": "req_assets_create_vehicle",
        "_type": "request",
        "name": "POST /assets (Create Vehicle with Depreciation)",
        "description": "Creates a vehicle asset with straight-line depreciation configured.",
        "method": "POST",
        "url": "{{ _.endpoint }}/assets",
        "headers": [
            {"name": "Content-Type", "value": "application/json"},
            {"name": "Accept", "value": "application/json"}
        ],
        "authentication": oauth2_auth(),
        "body": {
            "mimeType": "application/json",
            "text": json.dumps({
                "name": "2023 Tesla Model Y",
                "type": 1,
                "currentValue": 42000.00,
                "purchasePrice": 55000.00,
                "purchaseDate": "2023-03-01T00:00:00Z",
                "description": "Long Range AWD, Blue",
                "depreciationMethod": 1,
                "salvageValue": 15000.00,
                "usefulLifeMonths": 84,
                "inServiceDate": "2023-03-15T00:00:00Z"
            }, indent=2)
        },
        "parentId": "fld_assets",
        **req_defaults()
    },
    # POST /assets (Investment)
    {
        "_id": "req_assets_create_investment",
        "_type": "request",
        "name": "POST /assets (Create Investment Account)",
        "description": "Creates an investment/brokerage account asset.",
        "method": "POST",
        "url": "{{ _.endpoint }}/assets",
        "headers": [
            {"name": "Content-Type", "value": "application/json"},
            {"name": "Accept", "value": "application/json"}
        ],
        "authentication": oauth2_auth(),
        "body": {
            "mimeType": "application/json",
            "text": json.dumps({
                "name": "Fidelity Brokerage",
                "type": 2,
                "currentValue": 125000.00,
                "accountNumber": "X12-345678",
                "institutionName": "Fidelity Investments",
                "description": "Individual taxable brokerage account",
                "marketValue": 125000.00,
                "lastValuationDate": "2026-02-01T00:00:00Z"
            }, indent=2)
        },
        "parentId": "fld_assets",
        **req_defaults()
    },
    # PUT /assets/{id}
    {
        "_id": "req_assets_update",
        "_type": "request",
        "name": "PUT /assets/{id} (Update Asset)",
        "description": "Updates an existing asset. All fields required for full update. Set asset_id in environment first. Returns 200 OK or 404.",
        "method": "PUT",
        "url": "{{ _.endpoint }}/assets/{{ _.asset_id }}",
        "headers": [
            {"name": "Content-Type", "value": "application/json"},
            {"name": "Accept", "value": "application/json"}
        ],
        "authentication": oauth2_auth(),
        "body": {
            "mimeType": "application/json",
            "text": json.dumps({
                "name": "Primary Residence (Updated)",
                "type": 0,
                "currentValue": 475000.00,
                "purchasePrice": 350000.00,
                "purchaseDate": "2020-06-15T00:00:00Z",
                "description": "4BR/3BA single-family home - renovated kitchen 2025",
                "location": "123 Main St, Springfield, IL",
                "marketValue": 490000.00,
                "lastValuationDate": "2026-02-01T00:00:00Z"
            }, indent=2)
        },
        "parentId": "fld_assets",
        **req_defaults()
    },
    # DELETE /assets/{id}
    {
        "_id": "req_assets_delete",
        "_type": "request",
        "name": "DELETE /assets/{id} (Delete Asset)",
        "description": "Deletes an asset by ID. Beneficiary assignments removed first, then hard-deleted. Set asset_id in environment first. Returns 204 No Content or 404.",
        "method": "DELETE",
        "url": "{{ _.endpoint }}/assets/{{ _.asset_id }}",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "authentication": oauth2_auth(),
        "body": {},
        "parentId": "fld_assets",
        **req_defaults()
    },
    # Assets no-auth folder
    {
        "_id": "fld_assets_no_auth",
        "_type": "request_group",
        "name": "Asset Endpoints Without Auth (Expect 401)",
        "description": "Asset endpoints without tokens - verify they return 401",
        "parentId": "wrk_rajfinancial"
    },
    # GET /assets no auth
    {
        "_id": "req_assets_list_noauth",
        "_type": "request",
        "name": "GET /assets (No Token -> 401)",
        "description": "Expect 401 Unauthorized response.",
        "method": "GET",
        "url": "{{ _.endpoint }}/assets",
        "headers": [{"name": "Accept", "value": "application/json"}],
        "authentication": {},
        "body": {},
        "parentId": "fld_assets_no_auth",
        **req_defaults()
    },
    # POST /assets no auth
    {
        "_id": "req_assets_create_noauth",
        "_type": "request",
        "name": "POST /assets (No Token -> 401)",
        "description": "Expect 401 Unauthorized response.",
        "method": "POST",
        "url": "{{ _.endpoint }}/assets",
        "headers": [
            {"name": "Content-Type", "value": "application/json"},
            {"name": "Accept", "value": "application/json"}
        ],
        "authentication": {},
        "body": {
            "mimeType": "application/json",
            "text": json.dumps({"name": "Test", "type": 0, "currentValue": 100.00}, indent=2)
        },
        "parentId": "fld_assets_no_auth",
        **req_defaults()
    },
]

data['resources'].extend(new_resources)

with open('tools/insomnia/raj-financial-auth-tests.json', 'w') as f:
    json.dump(data, f, indent=2)
    f.write('\n')

print(f"Done. Total resources: {len(data['resources'])}")
