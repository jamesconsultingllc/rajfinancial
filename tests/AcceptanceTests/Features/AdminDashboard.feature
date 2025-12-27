@admin @dashboard
Feature: Admin Dashboard
    As an administrator
    I want to view system statistics and quick actions
    So that I can monitor and manage the platform effectively

Background:
    Given I am logged in as an "Administrator"

@authenticated @smoke
Scenario: Administrator can view dashboard
    When I navigate to "/admin/dashboard"
    Then I should see the admin dashboard
    And I should see "Administrator Dashboard"
    And I should not see an access denied message

@ui @functionality
Scenario: Dashboard displays quick actions
    When I navigate to "/admin/dashboard"
    Then I should see the "Quick Actions" section
    And I should see a "Manage Users" button
    And I should see a "System Settings" button
    And I should see a "Audit Logs" button
    And I should see a "System Reports" button

@ui
Scenario: Dashboard displays recent activity
    When I navigate to "/admin/dashboard"
    Then I should see the "Recent Activity" section
    And I should see activity items in the recent activity section

@navigation
Scenario: Quick action buttons navigate correctly
    When I navigate to "/admin/dashboard"
    And I click the "Manage Users" button
    Then I should be on the "/admin/users" page

@responsive
Scenario: Dashboard is responsive on mobile
    Given the viewport is set to mobile size
    When I navigate to "/admin/dashboard"
    Then the page should not have horizontal scroll
    And the statistics cards should be stacked vertically
    And the quick actions should be visible

@responsive
Scenario: Dashboard is responsive on tablet
    Given the viewport is set to tablet size
    When I navigate to "/admin/dashboard"
    Then the page should not have horizontal scroll
    And the statistics cards should adapt to tablet layout

@accessibility
Scenario: Dashboard meets accessibility standards
    When I navigate to "/admin/dashboard"
    Then the page should have proper heading hierarchy
    And all statistics should have accessible labels
    And all buttons should have accessible labels
    And all icons should be hidden from screen readers or have labels
