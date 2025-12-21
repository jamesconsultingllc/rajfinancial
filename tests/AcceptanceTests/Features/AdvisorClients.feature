@advisor @clients
Feature: Advisor Client Management
    As a financial advisor
    I want to view and manage my client portfolios
    So that I can provide effective financial guidance

Background:
    Given I am logged in as an "Advisor"

@authenticated @smoke
Scenario: Advisor can view client list
    When I navigate to "/advisor/clients"
    Then I should see the client list page
    And I should see "Client Portfolio Management"
    And I should not see an access denied message

@ui
Scenario: Client list page displays correctly
    When I navigate to "/advisor/clients"
    Then I should see "View and manage client accounts and portfolios"
    And I should see a "Add New Client" button
    And I should see a search box with placeholder "Search clients..."
    And I should see a status filter dropdown

@ui @functionality
Scenario: Client list displays client cards
    When I navigate to "/advisor/clients"
    Then I should see client cards
    And each client card should display client name
    And each client card should display client email
    And each client card should display portfolio value
    And each client card should display YTD return
    And each client card should display status badge

@ui
Scenario: Client cards have action buttons
    When I navigate to "/advisor/clients"
    Then each client card should have a "View Details" button
    And each client card should have an edit button

@functionality @search
Scenario: Search clients by name
    When I navigate to "/advisor/clients"
    And I search for "John Smith"
    Then I should see clients matching "John Smith"
    And I should not see clients that don't match

@functionality @filter
Scenario: Filter clients by status
    When I navigate to "/advisor/clients"
    And I filter clients by status "Active"
    Then I should see only clients with "Active" status
    And I should not see clients with other statuses

@functionality @filter
Scenario: Filter shows all clients when cleared
    When I navigate to "/advisor/clients"
    And I filter clients by status "Active"
    And I clear the status filter
    Then I should see all clients

@functionality
Scenario: Empty state when no clients match search
    When I navigate to "/advisor/clients"
    And I search for "NonexistentClientName12345"
    Then I should see "No clients found matching your search criteria"

@responsive
Scenario: Client list is responsive on mobile
    Given the viewport is set to mobile size
    When I navigate to "/advisor/clients"
    Then the page should not have horizontal scroll
    And client cards should be stacked vertically
    And the search box should be full width
    And I should see the "Add New Client" button

@responsive
Scenario: Client list is responsive on tablet
    Given the viewport is set to tablet size
    When I navigate to "/advisor/clients"
    Then the page should not have horizontal scroll
    And client cards should display in a grid layout

@responsive
Scenario: Client list is responsive on desktop
    Given the viewport is set to desktop size
    When I navigate to "/advisor/clients"
    Then client cards should display in a multi-column grid

@accessibility
Scenario: Client list meets accessibility standards
    When I navigate to "/advisor/clients"
    Then the page should have proper heading hierarchy
    And the search box should have an accessible label
    And the status filter should have an accessible label
    And all client action buttons should have accessible labels
    And client status badges should be accessible

@security @authorization
Scenario: Non-advisor users cannot access client list
    Given I am logged in as a "Client"
    When I navigate to "/advisor/clients"
    Then I should see an access denied message
    And I should not see "Client Portfolio Management"

@security @authorization
Scenario: Administrator can access client list
    Given I am logged in as an "Administrator"
    When I navigate to "/advisor/clients"
    Then I should see the client list page
    And I should not see an access denied message
