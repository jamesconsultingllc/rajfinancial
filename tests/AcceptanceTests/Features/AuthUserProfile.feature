@api @authentication @security @P0
Feature: Auth User Profile & Roles API
    As an authenticated user
    I want to retrieve my profile and role information
    So that the UI can display my identity and enforce client-side access rules

    Security coverage:
    - OWASP A01:2025 - Broken Access Control
    - OWASP A07:2025 - Authentication Failures
    - OWASP A04:2025 - Insecure Design (JIT provisioning)

    Background:
        Given the API is running

    # =========================================================================
    # GET /api/auth/me — Production-quality user profile endpoint
    # =========================================================================

    @smoke @security @A01
    Scenario: Authenticated user retrieves their profile via /api/auth/me
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 200
        And the response should contain userId "550e8400-e29b-41d4-a716-446655440000"
        And the response should contain email "advisor@rajfinancial.com"
        And the response should contain displayName
        And the response should contain role "Advisor"
        And the response should contain isProfileComplete

    @security @A01
    Scenario: Unauthenticated request to /api/auth/me returns 401
        Given I am not authenticated
        When I send a GET request to "/api/auth/me"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"
        And the response should contain a traceId

    @security @A04
    Scenario: First-time authenticated user triggers JIT provisioning
        Given I am authenticated as user "newuser@rajfinancial.com" with role "Client"
        And my Entra Object ID is "660e8400-e29b-41d4-a716-446655440001"
        And no local profile exists for user "660e8400-e29b-41d4-a716-446655440001"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 200
        And a local UserProfile should be created for "660e8400-e29b-41d4-a716-446655440001"
        And the profile email should be "newuser@rajfinancial.com"
        And the profile role should be "Client"

    @security @A04
    Scenario: Returning user has mutable claims synced on login
        Given I am authenticated as user "returning@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "770e8400-e29b-41d4-a716-446655440002"
        And a local profile exists for user "770e8400-e29b-41d4-a716-446655440002" with email "old@rajfinancial.com"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 200
        And the profile email should be updated to "returning@rajfinancial.com"

    @security @A01
    Scenario: Administrator retrieving profile includes isAdministrator flag
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And my Entra Object ID is "880e8400-e29b-41d4-a716-446655440003"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 200
        And the response should contain isAdministrator true

    @security @A01
    Scenario: User with multiple roles gets highest-priority role mapped
        Given I am authenticated with roles "Client,Administrator"
        And my Entra Object ID is "990e8400-e29b-41d4-a716-446655440004"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 200
        And the profile role should be "Administrator"

    # =========================================================================
    # GET /api/auth/roles — Current user's role assignments
    # =========================================================================

    @smoke @security @A01
    Scenario: Authenticated user retrieves their role assignments
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a GET request to "/api/auth/roles"
        Then the response status should be 200
        And the response should contain role "Advisor"
        And the response should contain isAdministrator false

    @security @A01
    Scenario: Administrator retrieves role assignments showing admin flag
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        When I send a GET request to "/api/auth/roles"
        Then the response status should be 200
        And the response should contain role "Administrator"
        And the response should contain isAdministrator true

    @security @A01
    Scenario: Unauthenticated request to /api/auth/roles returns 401
        Given I am not authenticated
        When I send a GET request to "/api/auth/roles"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: User with multiple roles sees all roles in response
        Given I am authenticated with roles "Client,Advisor"
        When I send a GET request to "/api/auth/roles"
        Then the response status should be 200
        And the response should contain role "Client"
        And the response should contain role "Advisor"
        And the response should contain isAdministrator false
