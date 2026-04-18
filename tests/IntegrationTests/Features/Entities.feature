@api @entities @security
Feature: Entity CRUD Operations
    As an authenticated user
    I want to create and manage financial entities (Personal, Business, Trust)
    So that I can organize my finances by entity

    Security coverage:
    - OWASP A01:2025 - Broken Access Control (IDOR prevention)
    - OWASP A07:2025 - Authentication Failures

    Background:
        Given the API is running

    # =========================================================================
    # Authentication Guard
    # =========================================================================

    @security @A01
    Scenario: Unauthenticated request to list entities returns 401
        Given I am not authenticated
        When I send a GET request to "/api/entities"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to create entity returns 401
        Given I am not authenticated
        When I send a POST request to "/api/entities" with an empty body
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    # =========================================================================
    # Auto-Provisioned Personal Entity
    # =========================================================================

    @smoke
    Scenario: New user automatically gets a Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/entities"
        Then the response status should be 200
        And the response should contain at least 1 entity
        And the response should contain an entity of type "Personal"

    # =========================================================================
    # POST /api/entities — Create Business Entity
    # =========================================================================

    @smoke
    Scenario: Create a new Business entity with required fields
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name     | Type     |
            | Acme LLC | Business |
        Then the response status should be 201
        And the response should contain the entity name "Acme LLC"
        And the response should contain the entity type "Business"
        And the response should contain a non-empty slug

    Scenario: Create a Business entity with metadata
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create a business entity "Acme LLC" with:
            | EntityFormationType | Ein        | Industry   | StateOfFormation |
            | MultiMemberLLC      | 12-3456789 | Technology | Delaware         |
        Then the response status should be 201
        And the response should contain EIN "12-3456789"

    # =========================================================================
    # POST /api/entities — Create Trust Entity
    # =========================================================================

    @smoke
    Scenario: Create a new Trust entity with required fields
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name         | Type  |
            | Family Trust | Trust |
        Then the response status should be 201
        And the response should contain the entity name "Family Trust"
        And the response should contain the entity type "Trust"

    Scenario: Create a Trust entity with metadata
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create a trust entity "Family Trust" with:
            | Category  | Purpose        | IsGrantorTrust | Jurisdiction |
            | Revocable | EstatePlanning | true           | California   |
        Then the response status should be 201
        And the response should contain jurisdiction "California"

    # =========================================================================
    # Validation
    # =========================================================================

    Scenario: Cannot create entity without name
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name | Type     |
            |      | Business |
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    Scenario: Cannot create a second Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name        | Type     |
            | My Personal | Personal |
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_ALREADY_EXISTS"

    Scenario: Cannot create entity with duplicate slug
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I create an entity with the following details:
            | Name     | Type     |
            | Acme LLC | Business |
        Then the response status should be 409
        And the error code should be "ENTITY_SLUG_DUPLICATE"

    # =========================================================================
    # GET /api/entities — List Entities
    # =========================================================================

    @smoke
    Scenario: User retrieves all their entities
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        And I have created a trust entity "Family Trust"
        When I send a GET request to "/api/entities"
        Then the response status should be 200
        And the response should contain at least 3 entities

    Scenario: Filter entities by type
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        And I have created a trust entity "Family Trust"
        When I send a GET request to "/api/entities?type=Business"
        Then the response status should be 200
        And all returned entities should have type "Business"

    # =========================================================================
    # GET /api/entities/{id} — Get Entity By ID
    # =========================================================================

    @smoke
    Scenario: User retrieves entity by ID
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I request the entity by its ID
        Then the response status should be 200
        And the response should contain the entity name "Acme LLC"

    Scenario: Request non-existent entity returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/entities/00000000-0000-0000-0000-000000000099"
        Then the response status should be 404

    # =========================================================================
    # PUT /api/entities/{id} — Update Entity
    # =========================================================================

    @smoke
    Scenario: User updates their business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I update the entity with name "Acme Holdings LLC"
        Then the response status should be 200
        And the response should contain the entity name "Acme Holdings LLC"

    Scenario: Cannot update Personal entity name
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I update my Personal entity with name "Custom Name"
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_NAME_IMMUTABLE"

    # =========================================================================
    # DELETE /api/entities/{id} — Delete Entity
    # =========================================================================

    @smoke
    Scenario: User deletes their business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Temp LLC"
        When I delete the entity by its ID
        Then the response status should be 204

    Scenario: Cannot delete Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I try to delete my Personal entity
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_CANNOT_DELETE"

    # =========================================================================
    # IDOR Prevention — Cross-User Access
    # =========================================================================

    @security @A01 @idor
    Scenario: User cannot access another user's entity by ID
        Given I am authenticated as user "other@rajfinancial.com" with role "Client"
        And user "owner@rajfinancial.com" has a business entity
        When I request that entity by ID
        Then access should be denied by the service tier

    @security @A01
    Scenario: Administrator can access any user's entities
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And user "owner@rajfinancial.com" has entities
        When I send a GET request to "/api/entities?ownerUserId={ownerUserId}"
        Then the response status should be 200
