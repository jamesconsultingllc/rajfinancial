@api @entity-roles @security
Feature: Entity Role Management
    As a user with entities
    I want to assign roles (owners, trustees, beneficiaries) to contacts on my entities
    So that I can track who is involved with each entity

    Security coverage:
    - OWASP A01:2025 - Broken Access Control (IDOR prevention)
    - OWASP A07:2025 - Authentication Failures

    Background:
        Given the API is running

    # =========================================================================
    # Authentication Guard
    # =========================================================================

    @security @A01
    Scenario: Unauthenticated request to list entity roles returns 401
        Given I am not authenticated
        When I send a GET request to "/api/entities/00000000-0000-0000-0000-000000000001/roles"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to assign an entity role returns 401
        Given I am not authenticated
        When I send a POST request to "/api/entities/00000000-0000-0000-0000-000000000001/roles" with an empty body
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to delete an entity role returns 401
        Given I am not authenticated
        When I send a DELETE request to "/api/entities/00000000-0000-0000-0000-000000000001/roles/00000000-0000-0000-0000-000000000002"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    # =========================================================================
    # POST /api/entities/{entityId}/roles — Assign Role
    # =========================================================================

    @smoke
    Scenario: Add an owner role to a business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC"
        And I have a contact "John Doe"
        When I assign role "Owner" to contact "John Doe" on entity "Acme LLC" with:
            | OwnershipPercent | Title           | IsSignatory |
            | 50.00            | Managing Member | true        |
        Then the response status should be 201
        And the response should contain role type "Owner"
        And the response should contain ownership percent 50.00

    @smoke
    Scenario: Add a trustee role to a trust entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Jane Smith"
        When I assign role "Trustee" to contact "Jane Smith" on entity "Family Trust" with:
            | IsPrimary | Title           |
            | true      | Primary Trustee |
        Then the response status should be 201
        And the response should contain role type "Trustee"

    Scenario: Add a beneficiary with beneficial interest percent
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Child Doe"
        When I assign role "Beneficiary" to contact "Child Doe" on entity "Family Trust" with:
            | BeneficialInterestPercent |
            | 33.33                     |
        Then the response status should be 201
        And the response should contain beneficial interest percent 33.33

    # =========================================================================
    # Validation
    # =========================================================================

    Scenario: Cannot assign trust-only role to business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC"
        And I have a contact "John Doe"
        When I assign role "Grantor" to contact "John Doe" on entity "Acme LLC"
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_INVALID_FOR_TYPE"

    Scenario: Cannot assign business-only role to trust entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Jane Smith"
        When I assign role "Officer" to contact "Jane Smith" on entity "Family Trust"
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_INVALID_FOR_TYPE"

    Scenario: Ownership percent must not exceed 100 across all owners
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with an owner at 80%
        And I have a contact "New Partner"
        When I assign role "Owner" to contact "New Partner" on entity "Acme LLC" with:
            | OwnershipPercent |
            | 30.00            |
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_OWNERSHIP_EXCEEDS_100"

    # =========================================================================
    # IDOR — Broken Access Control on roles sub-resource
    # =========================================================================

    @security @A01
    Scenario: Cannot list roles on an entity owned by another user
        Given I am authenticated as user "attacker@rajfinancial.com" with role "Client"
        And another user "victim@rajfinancial.com" has a business entity "Victim Co"
        When I send a GET request to "/api/entities/{victimEntityId}/roles"
        Then the response status should be 404

    @security @A01
    Scenario: Cannot assign a role on an entity owned by another user
        Given I am authenticated as user "attacker@rajfinancial.com" with role "Client"
        And another user "victim@rajfinancial.com" has a business entity "Victim Co"
        And I have a contact "Attacker Contact"
        When I assign role "Owner" to contact "Attacker Contact" on entity with id "{victimEntityId}"
        Then the response status should be 404

    @security @A01
    Scenario: Cannot delete a role on an entity owned by another user
        Given I am authenticated as user "attacker@rajfinancial.com" with role "Client"
        And another user "victim@rajfinancial.com" has a business entity "Victim Co" with a role assigned
        When I send a DELETE request to "/api/entities/{victimEntityId}/roles/{victimRoleId}"
        Then the response status should be 404

    # =========================================================================
    # GET /api/entities/{entityId}/roles — List Roles
    # =========================================================================

    @smoke
    Scenario: List all roles for a business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with roles assigned
        When I send a GET request to "/api/entities/{entityId}/roles"
        Then the response status should be 200
        And the response should contain at least 1 role

    # =========================================================================
    # DELETE /api/entities/{entityId}/roles/{roleId} — Remove Role
    # =========================================================================

    @smoke
    Scenario: Remove a role from an entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with a role assigned
        When I delete the role by its ID
        Then the response status should be 204
