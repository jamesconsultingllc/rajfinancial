@navigation @authorization
Feature: Navigation Menu
    As a user of RAJ Financial
    I want to see navigation options based on my role
    So that I can access the features available to me

@unauthenticated
Scenario: Unauthenticated user sees limited navigation
    Given I am not logged in
    When I view the navigation menu
    Then I should see the "Home" link
    And I should not see the "My Account" section
    And I should not see the "Advisor Tools" section
    And I should not see the "Administration" section

@authenticated @client
Scenario: Client user sees client navigation
    Given I am logged in as a "Client"
    When I view the navigation menu
    Then I should see the "Home" link
    And I should see the "My Account" section
    And I should see the "My Portfolio" link
    And I should not see the "Advisor Tools" section
    And I should not see the "Administration" section

@authenticated @administrator
Scenario: Administrator sees admin navigation only
    Given I am logged in as an "Administrator"
    When I view the navigation menu
    Then I should see the "Home" link
    And I should see the "Administration" section
    And I should see the "Dashboard" link in admin section
    And I should see the "User Management" link
    And I should see the "My Account" section

@accessibility
Scenario: Navigation is keyboard accessible
    Given I am on the home page
    When I press Tab multiple times
    Then I should be able to navigate through menu items
    And each focused item should have a visible focus indicator

@mobile @authenticated @client
Scenario: Mobile navigation uses hamburger menu
    Given the viewport is set to mobile size
    And I am logged in as a "Client"
    And I am on the home page
    Then I should see a hamburger menu button
    When I click the hamburger menu button
    Then the navigation menu should be visible
