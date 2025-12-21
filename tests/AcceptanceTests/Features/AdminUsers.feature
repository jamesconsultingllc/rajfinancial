@admin @users
Feature: Admin User Management
    As an administrator
    I want to manage user accounts and roles
    So that I can control access to the platform

Background:
    Given I am logged in as an "Administrator"

@authenticated @smoke
Scenario: Administrator can view user management page
    When I navigate to "/admin/users"
    Then I should see "User Management"
    And I should see "Manage user accounts and role assignments"
    And I should not see an access denied message

@ui
Scenario: User management page displays correctly
    When I navigate to "/admin/users"
    Then I should see a "Add User" button
    And I should see the "Active Users" section
    And I should see a table with user information

@ui
Scenario: User table has correct columns
    When I navigate to "/admin/users"
    Then the user table should have a "Name" column
    And the user table should have an "Email" column
    And the user table should have a "Role" column
    And the user table should have a "Status" column
    And the user table should have an "Actions" column

@responsive
Scenario: User management page is responsive on mobile
    Given the viewport is set to mobile size
    When I navigate to "/admin/users"
    Then the page should not have horizontal scroll
    And I should see a "Add User" button
    And the user table should be scrollable or use cards

@responsive
Scenario: User management page is responsive on tablet
    Given the viewport is set to tablet size
    When I navigate to "/admin/users"
    Then the page should not have horizontal scroll
    And I should see the user table

@accessibility
Scenario: User management page meets accessibility standards
    When I navigate to "/admin/users"
    Then the page should have proper heading hierarchy
    And the "Add User" button should have an accessible label
    And the user table should be properly structured
    And all action buttons should have accessible labels

@security @authorization
Scenario: Non-admin users cannot access user management
    Given I am logged in as a "Client"
    When I navigate to "/admin/users"
    Then I should see an access denied message
    And I should not see "User Management"

@security @authorization
Scenario: Advisor cannot access user management
    Given I am logged in as an "Advisor"
    When I navigate to "/admin/users"
    Then I should see an access denied message
    And I should not see "User Management"

@security @authorization
Scenario: Page displays access denied message when unauthorized
    Given I am logged in as a "Client"
    When I navigate to "/admin/users"
    Then I should see an access denied message
    And I should see "You do not have permission to access this page"
    And I should see "Administrator role required"
