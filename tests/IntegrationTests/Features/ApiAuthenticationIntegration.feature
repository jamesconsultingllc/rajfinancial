@api @integration @authentication @security
Feature: API Authentication Integration
    As the API backend
    I want to validate that endpoints enforce authentication via HTTP
    So that the middleware pipeline correctly returns 401/403 over real HTTP

    Security coverage:
    - OWASP A01:2025 - Broken Access Control
    - OWASP A07:2025 - Authentication Failures

    Background:
        Given the Functions host is running

    @smoke @security
    Scenario: Health endpoint is publicly accessible
        When I send a GET request to "/api/health"
        Then the HTTP response status should be 200 or 503
        And the response body should contain "RajFinancial API"

    @security @A01
    Scenario: Public auth endpoint is accessible without authentication
        When I send a GET request to "/api/auth/public"
        Then the HTTP response status should be 200
        And the response body should contain "public"

    @security @A07
    Scenario: Protected endpoint returns 401 without authentication
        When I send a GET request to "/api/auth/me" without authentication
        Then the HTTP response status should be 401

    @security @A07
    Scenario: Client endpoint returns 401 without authentication
        When I send a GET request to "/api/auth/client" without authentication
        Then the HTTP response status should be 401

    @security @A01
    Scenario: Admin endpoint returns 401 without authentication
        When I send a GET request to "/api/auth/admin" without authentication
        Then the HTTP response status should be 401

    # =========================================================================
    # Authenticated scenarios (using unsigned test JWTs — Development mode only)
    # =========================================================================

    @security @A07
    Scenario: Authenticated user can access protected endpoint
        When I send a GET request to "/api/auth/me" with a valid user token
        Then the HTTP response status should be 200
        And the response body should contain "authenticated"

    @security @A07
    Scenario: Authenticated user can access client endpoint
        When I send a GET request to "/api/auth/client" with a valid user token
        Then the HTTP response status should be 200
        And the response body should contain "Welcome, Client!"

    @security @A01
    Scenario: Non-admin user gets 403 on admin endpoint
        When I send a GET request to "/api/auth/admin" with a "Client" role token
        Then the HTTP response status should be 403

    @security @A01
    Scenario: Administrator can access admin endpoint
        When I send a GET request to "/api/auth/admin" with an administrator token
        Then the HTTP response status should be 200
        And the response body should contain "Welcome, Administrator!"

    @security @A01
    Scenario: Public endpoint includes user info when authenticated
        When I send a GET request to "/api/auth/public" with a valid user token
        Then the HTTP response status should be 200
        And the response body should contain "\"authenticated\":true"
