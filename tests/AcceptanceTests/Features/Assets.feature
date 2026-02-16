@api @assets @security
Feature: Asset CRUD Operations
    As an authenticated user
    I want to create, read, update, and delete my financial assets
    So that I can manage my asset portfolio

    Security coverage:
    - OWASP A01:2025 - Broken Access Control (IDOR prevention)
    - OWASP A07:2025 - Authentication Failures

    The three-tier authorization check evaluates in order:
    1. Resource Owner — is the requesting user the asset owner?
    2. Data Access Grant — does the user hold a valid grant for the asset category?
    3. Administrator — does the user have the Administrator role?

    Background:
        Given the API is running

    # =========================================================================
    # Authentication Guard
    # =========================================================================

    @security @A01
    Scenario: Unauthenticated request to list assets returns 401
        Given I am not authenticated
        When I send a GET request to "/api/assets"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to create asset returns 401
        Given I am not authenticated
        When I send a POST request to "/api/assets" with an empty body
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to get asset by ID returns 401
        Given I am not authenticated
        When I send a GET request to "/api/assets/00000000-0000-0000-0000-000000000001"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to update asset returns 401
        Given I am not authenticated
        When I send a PUT request to "/api/assets/00000000-0000-0000-0000-000000000001" with an empty body
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to delete asset returns 401
        Given I am not authenticated
        When I send a DELETE request to "/api/assets/00000000-0000-0000-0000-000000000001"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    # =========================================================================
    # POST /api/assets — Create Asset
    # =========================================================================

    @smoke
    Scenario: Create a new asset with required fields only
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an asset with the following details:
            | Name         | Type       | CurrentValue |
            | Family Home  | RealEstate | 450000       |
        Then the response status should be 201
        And the response should contain the asset name "Family Home"
        And the response should contain the asset type "RealEstate"

    Scenario: Create a new asset with all optional fields
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an asset with the following details:
            | Name           | Type       | CurrentValue | PurchasePrice | Description             | Location           | AccountNumber |
            | Rental Property| RealEstate | 325000       | 280000        | 3BR rental in downtown  | 123 Main St, NY    | PROP-001      |
        Then the response status should be 201
        And the response should contain the asset name "Rental Property"

    Scenario: Create asset with invalid data returns 400
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an asset with the following details:
            | Name | Type       | CurrentValue |
            |      | RealEstate | 450000       |
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    Scenario: Create asset with negative current value returns 400
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an asset with the following details:
            | Name       | Type       | CurrentValue |
            | Bad Asset  | RealEstate | -1000        |
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    # =========================================================================
    # GET /api/assets — List Assets
    # =========================================================================

    @smoke
    Scenario: Owner retrieves their own assets
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created the following assets:
            | Name        | Type       | CurrentValue |
            | Family Home | RealEstate | 450000       |
            | Tesla Model | Vehicle    | 65000        |
        When I send a GET request to "/api/assets"
        Then the response status should be 200
        And the response should contain 2 assets

    Scenario: Filter assets by type
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created the following assets:
            | Name        | Type       | CurrentValue |
            | Family Home | RealEstate | 450000       |
            | Tesla Model | Vehicle    | 65000        |
        When I send a GET request to "/api/assets?type=RealEstate"
        Then the response status should be 200
        And all returned assets should have type "RealEstate"

    Scenario: Disposed assets are excluded by default
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have an asset "Old Car" that has been disposed
        When I send a GET request to "/api/assets"
        Then the response should not contain asset "Old Car"

    Scenario: Include disposed assets with query parameter
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have an asset "Old Car" that has been disposed
        When I send a GET request to "/api/assets?includeDisposed=true"
        Then the response should contain asset "Old Car"

    # =========================================================================
    # GET /api/assets/{id} — Get Asset By ID
    # =========================================================================

    @smoke
    Scenario: Owner retrieves asset detail by ID
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created an asset "Family Home" of type "RealEstate" worth 450000
        When I request the asset by its ID
        Then the response status should be 200
        And the response should contain the asset name "Family Home"
        And the response should include depreciation details
        And the response should include beneficiary information

    Scenario: Request asset with invalid GUID returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/assets/not-a-guid"
        Then the response status should be 404

    Scenario: Request non-existent asset returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/assets/00000000-0000-0000-0000-000000000099"
        Then the response status should be 404

    # =========================================================================
    # PUT /api/assets/{id} — Update Asset
    # =========================================================================

    @smoke
    Scenario: Owner updates their asset
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created an asset "Family Home" of type "RealEstate" worth 450000
        When I update the asset with the following details:
            | Name                | Type       | CurrentValue |
            | Family Home Updated | RealEstate | 475000       |
        Then the response status should be 200
        And the response should contain the asset name "Family Home Updated"
        And the response should contain current value 475000

    Scenario: Update asset with invalid data returns 400
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created an asset "Family Home" of type "RealEstate" worth 450000
        When I update the asset with the following details:
            | Name | Type       | CurrentValue |
            |      | RealEstate | 475000       |
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    Scenario: Update non-existent asset returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a PUT request to "/api/assets/00000000-0000-0000-0000-000000000099" with:
            | Name       | Type       | CurrentValue |
            | Not Found  | RealEstate | 100000       |
        Then the response status should be 404

    # =========================================================================
    # DELETE /api/assets/{id} — Delete Asset
    # =========================================================================

    @smoke
    Scenario: Owner deletes their asset
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created an asset "Temp Asset" of type "Vehicle" worth 15000
        When I delete the asset by its ID
        Then the response status should be 204

    Scenario: Delete non-existent asset returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a DELETE request to "/api/assets/00000000-0000-0000-0000-000000000099"
        Then the response status should be 404

    # =========================================================================
    # IDOR Prevention — Cross-User Access
    # =========================================================================

    @security @A01 @idor
    Scenario: User cannot list another user's assets without a grant
        Given I am authenticated as user "other@rajfinancial.com" with role "Client"
        And user "owner@rajfinancial.com" has assets
        When I send a GET request to "/api/assets?ownerUserId={ownerUserId}"
        Then access should be denied or filtered by the service tier

    @security @A01 @idor
    Scenario: User cannot access another user's asset by ID without a grant
        Given I am authenticated as user "other@rajfinancial.com" with role "Client"
        And user "owner@rajfinancial.com" has an asset with a known ID
        When I request that asset by ID
        Then access should be denied by the service tier

    @security @A01
    Scenario: Administrator can access any user's assets
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And user "owner@rajfinancial.com" has assets
        When I send a GET request to "/api/assets?ownerUserId={ownerUserId}"
        Then the response status should be 200
