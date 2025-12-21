@authorization @security
Feature: Authorization and Access Control
    As a platform administrator
    I want users to only access features based on their roles
    So that data and functionality remain secure

Background:
    Given the application is running

@unauthenticated
Scenario: Unauthenticated user is redirected to login for protected pages
    Given I am not logged in
    When I navigate to "/portfolio"
    Then I should be redirected to the login page

@authenticated @client
Scenario: Client can access their portfolio
    Given I am logged in as a "Client"
    When I navigate to "/portfolio"
    Then I should see the portfolio page
    And I should not see an access denied message

@authenticated @client
Scenario: Client cannot access admin dashboard
    Given I am logged in as a "Client"
    When I navigate to "/admin/dashboard"
    Then I should see an access denied message

@authenticated @administrator
Scenario: Administrator can access admin dashboard
    Given I am logged in as an "Administrator"
    When I navigate to "/admin/dashboard"
    Then I should see the admin dashboard
    And I should not see an access denied message

@authenticated @administrator
Scenario: Administrator cannot access client portfolio features
    Given I am logged in as an "Administrator"
    When I navigate to "/portfolio"
    Then I should see an access denied message

@authenticated @advisor
Scenario: Advisor can access advisor tools
    Given I am logged in as an "Advisor"
    When I navigate to "/advisor/clients"
    Then I should see the client list page
    And I should not see an access denied message

@authenticated @viewer
Scenario: Viewer can only see shared data
    Given I am logged in as a "Viewer"
    When I navigate to "/portfolio"
    Then I should see data shared with me
    And I should not be able to edit any data

@security
Scenario: Access denied page is styled appropriately
    Given I am logged in as a "Client"
    When I navigate to "/admin/dashboard"
    Then I should see an access denied message
    And I should see a "Return to Home" button
    And the page should use the brand styling
