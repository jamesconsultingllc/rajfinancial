@api @integration @userprofile @provisioning
Feature: UserProfile JIT Provisioning
    As a system operator
    I want user profiles to be automatically created on first authenticated API call
    So that downstream features (assets, accounts, beneficiaries) can hold FK references
    without requiring a separate user registration step

    Background:
        Given the Functions host is running

    # ==========================================================================
    # Happy Path: First-time user provisioning
    # ==========================================================================

    @smoke @devOnly
    Scenario: First authenticated request creates a UserProfile
        Given no UserProfile exists for a new test user
        When I send an authenticated request to "/api/profile/me" as the new test user
        Then the HTTP response status should be 200
        And the response should contain a persisted UserProfile
        And the UserProfile email should match the JWT email claim
        And the UserProfile role should be "Client"
        And the UserProfile should be active

    @devOnly
    Scenario: First authenticated request sets CreatedAt and LastLoginAt
        Given no UserProfile exists for a new test user
        When I send an authenticated request to "/api/profile/me" as the new test user
        Then the HTTP response status should be 200
        And the UserProfile CreatedAt should be recent
        And the UserProfile LastLoginAt should be recent

    # ==========================================================================
    # Subsequent requests: LastLoginAt update
    # ==========================================================================

    @devOnly
    Scenario: Subsequent authenticated request updates LastLoginAt
        Given a UserProfile already exists for a returning test user
        When I send an authenticated request to "/api/profile/me" as the returning test user
        Then the HTTP response status should be 200
        And the UserProfile LastLoginAt should be after the previous login

    # ==========================================================================
    # Unauthenticated requests: No provisioning
    # ==========================================================================

    @security @devOnly
    Scenario: Unauthenticated request does not trigger provisioning
        When I send a GET request to "/api/auth/public" without authentication
        Then the HTTP response status should be 200
        And no provisioning should have occurred

    # ==========================================================================
    # Role mapping from JWT claims
    # ==========================================================================

    @devOnly
    Scenario: Administrator role is mapped from JWT claims
        Given no UserProfile exists for a new admin test user
        When I send an authenticated request to "/api/profile/me" as an administrator
        Then the HTTP response status should be 200
        And the UserProfile role should be "Administrator"

    @devOnly
    Scenario: Advisor role is mapped from JWT claims
        Given no UserProfile exists for a new advisor test user
        When I send an authenticated request to "/api/profile/me" with role "Advisor"
        Then the HTTP response status should be 200
        And the UserProfile role should be "Advisor"

    # ==========================================================================
    # Claims sync on subsequent login
    # ==========================================================================

    @devOnly
    Scenario: Updated email claim is synced on subsequent request
        Given a UserProfile exists for a user with email "old@example.com"
        When I send an authenticated request to "/api/profile/me" with updated email "new@example.com"
        Then the HTTP response status should be 200
        And the UserProfile email should be "new@example.com"

    @devOnly
    Scenario: Updated display name is synced on subsequent request
        Given a UserProfile exists for a user with display name "Old Name"
        When I send an authenticated request to "/api/profile/me" with updated name "New Name"
        Then the HTTP response status should be 200
        And the UserProfile display name should be "New Name"
