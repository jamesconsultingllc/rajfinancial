@entities @requires-auth
Feature: Entity Management UI
    As a logged-in user
    I want to see my entities in the sidebar and manage them
    So that I can organize my financial data by entity

    Background:
        Given I am logged in as a "Client"

    # =========================================================================
    # Sidebar Navigation
    # =========================================================================

    @smoke
    Scenario: User sees Personal section in sidebar
        When I view the navigation menu
        Then I should see the "Personal" section
        And I should see the "Overview" link under "Personal"

    @smoke
    Scenario: User sees Business section in sidebar
        When I view the navigation menu
        Then I should see the "Business" section

    @smoke
    Scenario: User sees Trusts section in sidebar
        When I view the navigation menu
        Then I should see the "Trusts" section

    # =========================================================================
    # Entity Creation
    # =========================================================================

    Scenario: User creates a new Business entity
        When I click the "Add Business" button
        Then I should see the "Create Business Entity" form
        When I fill in "Entity Name" with "Acme LLC"
        And I select "Multi-Member LLC" for "Formation Type"
        And I click the "Create" button
        Then I should see "Acme LLC" in the Business section
        And I should be on the "business/acme-llc/overview" page

    Scenario: User creates a new Trust entity
        When I click the "Add Trust" button
        Then I should see the "Create Trust Entity" form
        When I fill in "Entity Name" with "Family Trust"
        And I select "Revocable" for "Category"
        And I click the "Create" button
        Then I should see "Family Trust" in the Trusts section

    # =========================================================================
    # Entity Overview Page
    # =========================================================================

    @smoke
    Scenario: Personal overview shows summary cards
        When I navigate to "/personal/overview"
        Then I should see the "Net Worth" card
        And I should see the "Monthly Income" card
        And I should see the "Monthly Expenses" card

    # =========================================================================
    # Entity Sub-Navigation
    # =========================================================================

    Scenario: Personal entity has all sub-navigation links
        When I navigate to "/personal/overview"
        Then I should see the "Income" link
        And I should see the "Bills & Expenses" link
        And I should see the "Assets" link
        And I should see the "Accounts" link
        And I should see the "Insurance" link
        And I should see the "Documents" link

    # =========================================================================
    # Mobile
    # =========================================================================

    @mobile
    Scenario: Entity navigation works on mobile
        Given the viewport is set to mobile size
        When I click the hamburger menu button
        Then I should see the "Personal" section
        And I should see the "Business" section
        And I should see the "Trusts" section

    # =========================================================================
    # Accessibility
    # =========================================================================

    @accessibility
    Scenario: Entity sections are keyboard navigable
        When I press Tab multiple times
        Then I should be able to navigate through menu items
        And each focused item should have a visible focus indicator
