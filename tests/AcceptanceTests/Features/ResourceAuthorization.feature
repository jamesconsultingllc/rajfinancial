@api @authorization @security @idor
Feature: Resource-Level Authorization
    As the API backend
    I want to enforce resource-level access control using a three-tier check
    So that users can only access data they own, are granted access to, or have admin rights

    Security coverage:
    - OWASP A01:2025 - Broken Access Control (IDOR prevention)

    The three-tier authorization check evaluates in order:
    1. Resource Owner — is the requesting user the data owner?
    2. Data Access Grant — does the user hold a valid grant for this category/level?
    3. Administrator — does the user have the Administrator role?

    Background:
        Given user "owner@example.com" owns a resource in category "accounts"

    # =========================================================================
    # Tier 1: Resource Owner
    # =========================================================================

    @security @A01
    Scenario: Owner accesses their own resource
        When "owner@example.com" requests "Read" access to the resource
        Then access should be granted
        And the reason should be "ResourceOwner"
        And the granted access level should be "Owner"

    # =========================================================================
    # Tier 2: Data Access Grant
    # =========================================================================

    @security @A01
    Scenario: Grantee with valid Read grant accesses shared resource
        Given "grantee@example.com" has an active DataAccessGrant with "Read" access to "accounts"
        When "grantee@example.com" requests "Read" access to the resource
        Then access should be granted
        And the reason should be "DataAccessGrant"
        And the granted access level should be "Read"

    @security @A01
    Scenario: Grantee with Full grant accesses shared resource
        Given "grantee@example.com" has an active DataAccessGrant with "Full" access to "accounts"
        When "grantee@example.com" requests "Read" access to the resource
        Then access should be granted
        And the reason should be "DataAccessGrant"

    @security @A01
    Scenario: Grantee with expired grant is denied access
        Given "grantee@example.com" has an expired DataAccessGrant to "accounts"
        When "grantee@example.com" requests "Read" access to the resource
        Then access should be denied

    @security @A01
    Scenario: Grantee with revoked grant is denied access
        Given "grantee@example.com" has a revoked DataAccessGrant to "accounts"
        When "grantee@example.com" requests "Read" access to the resource
        Then access should be denied

    @security @A01
    Scenario: Grantee with Limited access to wrong category is denied
        Given "grantee@example.com" has an active DataAccessGrant with "Limited" access to "documents"
        When "grantee@example.com" requests "Read" access to the resource
        Then access should be denied

    # =========================================================================
    # Tier 3: Administrator
    # =========================================================================

    @security @A01
    Scenario: Administrator accesses any resource
        Given "admin@example.com" has the "Administrator" role
        When "admin@example.com" requests "Read" access to the resource
        Then access should be granted
        And the reason should be "Administrator"

    # =========================================================================
    # Denied: No matching tier
    # =========================================================================

    @security @A01
    Scenario: User with no grant and no admin role is denied access
        When "stranger@example.com" requests "Read" access to the resource
        Then access should be denied
        And the reason should be "Denied"

    @security @A01 @idor
    Scenario: User cannot access another user's resource without a grant
        When "other-user@example.com" requests "Read" access to the resource
        Then access should be denied
        And the reason should be "Denied"
