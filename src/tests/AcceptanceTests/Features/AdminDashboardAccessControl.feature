@security @authorization
Feature: Admin Dashboard Access Control
    As a security measure
    The admin dashboard should only be accessible to administrators
    Non-admin users should see an access denied message

@security
Scenario: Non-admin Client users cannot access admin dashboard
    Given I am logged in as a "Client"
    When I navigate to "/admin/dashboard"
    Then I should see an access denied message
    And I should not see "Administrator Dashboard"

@security
Scenario: Non-admin Advisor users cannot access admin dashboard
    Given I am logged in as an "Advisor"
    When I navigate to "/admin/dashboard"
    Then I should see an access denied message
    And I should not see "Administrator Dashboard"

@security
Scenario: Non-admin Viewer users cannot access admin dashboard
    Given I am logged in as a "Viewer"
    When I navigate to "/admin/dashboard"
    Then I should see an access denied message
    And I should not see "Administrator Dashboard"

@security
Scenario: Unauthenticated users are redirected to login
    Given I am not logged in
    When I navigate to "/admin/dashboard"
    Then I should be redirected to the login page
