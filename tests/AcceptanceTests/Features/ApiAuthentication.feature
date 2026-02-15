@api @authentication @security @middleware
Feature: API Authentication Middleware
    As the API backend
    I want to validate authentication on every request
    So that only authorized users can access protected resources

    Security coverage:
    - OWASP A01:2025 - Broken Access Control
    - OWASP A07:2025 - Authentication Failures
    - OWASP A09:2025 - Security Logging and Monitoring Failures

    Background:
        Given the API is running

    @smoke @security @A01
    Scenario: Unauthenticated request to public endpoint succeeds
        When I send a GET request to "/api/health" without authentication
        Then the response status should be 200

    @security @A01
    Scenario: Unauthenticated request to protected endpoint returns 401
        Given I am not authenticated
        When I send a GET request to "/api/auth/me"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Authenticated Client user can access client endpoints
        Given I am authenticated as user "testuser@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/auth/client"
        Then the response status should be 200

    @security @A01
    Scenario: Client user cannot access admin-only endpoints
        Given I am authenticated as user "testuser@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/auth/admin"
        Then the response status should be 403
        And the error code should be "AUTH_FORBIDDEN"

    @security @A01
    Scenario: Administrator can access admin endpoints
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        When I send a GET request to "/api/auth/admin"
        Then the response status should be 200

    @security @A07
    Scenario: Malformed JWT token is treated as unauthenticated
        Given I have a malformed JWT token "not-a-valid-token"
        When I send a GET request to "/api/auth/me"
        Then the response status should be 401

    @security @A01
    Scenario: User context is populated from Entra ID claims
        Given I am authenticated as user "testuser@rajfinancial.com" with role "Client"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        When I send a GET request to "/api/auth/me"
        Then the response should contain userId "550e8400-e29b-41d4-a716-446655440000"
        And the response should contain email "testuser@rajfinancial.com"

    @security @A01
    Scenario: Multiple roles are correctly extracted from JWT claims
        Given I am authenticated with roles "Client,Administrator"
        When I send a GET request to "/api/auth/me"
        Then the response should contain role "Client"
        And the response should contain role "Administrator"
