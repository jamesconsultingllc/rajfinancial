@api @authorization @security @P1
Feature: Client Management API
    As a financial advisor
    I want to manage client access assignments
    So that clients can view their financial data through the platform

    Security coverage:
    - OWASP A01:2025 - Broken Access Control
    - OWASP A04:2025 - Insecure Design (privilege escalation)
    - OWASP A08:2025 - Software and Data Integrity Failures

    Background:
        Given the API is running

    # =========================================================================
    # POST /api/auth/clients — Assign a client to the advisor
    # =========================================================================

    @smoke @security @A01
    Scenario: Advisor assigns a new client by email
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "client@example.com",
                "accessType": "Read",
                "categories": ["accounts", "investments"],
                "relationshipLabel": "Primary Advisor"
            }
            """
        Then the response status should be 201
        And the response should contain a grantId
        And the response should contain clientEmail "client@example.com"
        And the response should contain accessType "Read"

    @security @A01
    Scenario: Client user cannot assign other clients
        Given I am authenticated as user "client@rajfinancial.com" with role "Client"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "other@example.com",
                "accessType": "Read",
                "categories": ["accounts"]
            }
            """
        Then the response status should be 403
        And the error code should be "AUTH_FORBIDDEN"

    @security @A01
    Scenario: Unauthenticated request to assign client returns 401
        Given I am not authenticated
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "client@example.com",
                "accessType": "Read",
                "categories": ["accounts"]
            }
            """
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @validation
    Scenario: Assigning client with missing email returns validation error
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "accessType": "Read",
                "categories": ["accounts"]
            }
            """
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    @validation
    Scenario: Assigning client with invalid access type returns validation error
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "client@example.com",
                "accessType": "InvalidType",
                "categories": ["accounts"]
            }
            """
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    @validation
    Scenario: Assigning client with empty categories returns validation error
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "client@example.com",
                "accessType": "Read",
                "categories": []
            }
            """
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    @security @A04
    Scenario: Advisor cannot assign themselves as a client
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "advisor@rajfinancial.com",
                "accessType": "Read",
                "categories": ["accounts"]
            }
            """
        Then the response status should be 400
        And the error code should be "SELF_ASSIGNMENT_NOT_ALLOWED"

    @security @A01
    Scenario: Administrator can assign clients on behalf of any advisor
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        When I send a POST request to "/api/auth/clients" with body:
            """
            {
                "clientEmail": "client@example.com",
                "accessType": "Full",
                "categories": ["accounts", "investments"],
                "relationshipLabel": "Admin-assigned"
            }
            """
        Then the response status should be 201

    # =========================================================================
    # GET /api/auth/clients — List assigned clients
    # =========================================================================

    @smoke @security @A01
    Scenario: Advisor retrieves their assigned client list
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        And the advisor has 3 assigned clients
        When I send a GET request to "/api/auth/clients"
        Then the response status should be 200
        And the response should contain 3 client assignments
        And each assignment should contain grantId
        And each assignment should contain clientEmail
        And each assignment should contain accessType
        And each assignment should contain status

    @security @A01
    Scenario: Advisor can only see their own client assignments
        Given I am authenticated as user "advisor1@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        And another advisor "advisor2@rajfinancial.com" has 2 assigned clients
        When I send a GET request to "/api/auth/clients"
        Then the response should not contain clients assigned to other advisors

    @security @A01
    Scenario: Client user cannot list client assignments
        Given I am authenticated as user "client@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/auth/clients"
        Then the response status should be 403
        And the error code should be "AUTH_FORBIDDEN"

    @security @A01
    Scenario: Unauthenticated request to list clients returns 401
        Given I am not authenticated
        When I send a GET request to "/api/auth/clients"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Administrator can see all client assignments
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And there are 5 total client assignments across all advisors
        When I send a GET request to "/api/auth/clients"
        Then the response status should be 200
        And the response should contain 5 client assignments

    # =========================================================================
    # DELETE /api/auth/clients/{id} — Remove client access
    # =========================================================================

    @smoke @security @A01
    Scenario: Advisor removes a client assignment
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        And I have an active client assignment with ID "aaa00000-0000-0000-0000-000000000001"
        When I send a DELETE request to "/api/auth/clients/aaa00000-0000-0000-0000-000000000001"
        Then the response status should be 204

    @security @A01
    Scenario: Advisor cannot remove another advisor's client assignment
        Given I am authenticated as user "advisor1@rajfinancial.com" with role "Advisor"
        And my Entra Object ID is "550e8400-e29b-41d4-a716-446655440000"
        And another advisor has an active client assignment with ID "bbb00000-0000-0000-0000-000000000002"
        When I send a DELETE request to "/api/auth/clients/bbb00000-0000-0000-0000-000000000002"
        Then the response status should be 403
        And the error code should be "AUTH_FORBIDDEN"

    @security @A01
    Scenario: Client user cannot remove client assignments
        Given I am authenticated as user "client@rajfinancial.com" with role "Client"
        When I send a DELETE request to "/api/auth/clients/aaa00000-0000-0000-0000-000000000001"
        Then the response status should be 403
        And the error code should be "AUTH_FORBIDDEN"

    @security @A01
    Scenario: Removing non-existent client assignment returns 404
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        When I send a DELETE request to "/api/auth/clients/00000000-0000-0000-0000-000000000000"
        Then the response status should be 404
        And the error code should be "RESOURCE_NOT_FOUND"

    @security @A01
    Scenario: Unauthenticated request to remove client returns 401
        Given I am not authenticated
        When I send a DELETE request to "/api/auth/clients/aaa00000-0000-0000-0000-000000000001"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Administrator can remove any client assignment
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And an advisor has an active client assignment with ID "aaa00000-0000-0000-0000-000000000001"
        When I send a DELETE request to "/api/auth/clients/aaa00000-0000-0000-0000-000000000001"
        Then the response status should be 204

    @security @A08
    Scenario: Removing a client assignment soft-deletes the grant
        Given I am authenticated as user "advisor@rajfinancial.com" with role "Advisor"
        And I have an active client assignment with ID "aaa00000-0000-0000-0000-000000000001"
        When I send a DELETE request to "/api/auth/clients/aaa00000-0000-0000-0000-000000000001"
        Then the response status should be 204
        And the grant should be marked as revoked, not physically deleted
        And the grant RevokedAt timestamp should be set
