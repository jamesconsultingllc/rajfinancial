@client @portfolio
Feature: Client Portfolio
    As a client
    I want to view my investment portfolio and performance
    So that I can track my financial progress

Background:
    Given I am logged in as a "Client"

@authenticated @smoke
Scenario: Client can view their portfolio
    When I navigate to "/portfolio"
    Then I should see the portfolio page
    And I should see "My Portfolio"
    And I should not see an access denied message

@ui
Scenario: Portfolio displays summary cards
    When I navigate to "/portfolio"
    Then I should see "Total Portfolio Value"
    And I should see "Total Gain/Loss"
    And I should see "Available Cash"

@ui
Scenario: Portfolio summary shows financial data
    When I navigate to "/portfolio"
    Then the total portfolio value should display a dollar amount
    And the daily change should show a positive or negative indicator
    And the total gain/loss should show a dollar amount
    And the available cash should display a dollar amount

@ui
Scenario: Portfolio displays holdings section
    When I navigate to "/portfolio"
    Then I should see the "Holdings" section
    And I should see a "New Trade" button
    And I should see a holdings table

@ui @desktop
Scenario: Holdings table displays correctly on desktop
    Given the viewport is set to desktop size
    When I navigate to "/portfolio"
    Then the holdings table should display in table format
    And the table should have columns: Symbol, Name, Shares, Price, Value, Gain/Loss, Actions

@ui @mobile
Scenario: Holdings display as cards on mobile
    Given the viewport is set to mobile size
    When I navigate to "/portfolio"
    Then the holdings should display as cards
    And each holding card should show symbol and name
    And each holding card should show value and gain/loss percentage
    And each holding card should have Buy and Sell buttons

@ui
Scenario: Each holding displays complete information
    When I navigate to "/portfolio"
    Then each holding should display stock symbol
    And each holding should display company name
    And each holding should display number of shares
    And each holding should display current price
    And each holding should display total value
    And each holding should display gain/loss with percentage

@ui
Scenario: Holdings have action buttons
    When I navigate to "/portfolio"
    Then each holding should have a "Buy" button
    And each holding should have a "Sell" button

@ui
Scenario: Portfolio displays recent transactions
    When I navigate to "/portfolio"
    Then I should see the "Recent Transactions" section
    And I should see transaction items
    And each transaction should show type (Buy/Sell)
    And each transaction should show symbol
    And each transaction should show shares and price
    And each transaction should show date

@ui
Scenario: Transaction badges are color-coded
    When I navigate to "/portfolio"
    Then "Buy" transactions should have a success/green badge
    And "Sell" transactions should have a danger/red badge

@responsive
Scenario: Portfolio is responsive on mobile
    Given the viewport is set to mobile size
    When I navigate to "/portfolio"
    Then the page should not have horizontal scroll
    And the summary cards should stack vertically
    And the holdings should display as cards
    And all buttons should be touch-friendly

@responsive
Scenario: Portfolio is responsive on tablet
    Given the viewport is set to tablet size
    When I navigate to "/portfolio"
    Then the page should not have horizontal scroll
    And the summary cards should adapt to tablet layout

@responsive
Scenario: Portfolio is responsive on desktop
    Given the viewport is set to desktop size
    When I navigate to "/portfolio"
    Then the holdings should display as a table
    And the summary cards should display in a row

@accessibility
Scenario: Portfolio meets accessibility standards
    When I navigate to "/portfolio"
    Then the page should have proper heading hierarchy
    And all buttons should have accessible labels
    And the holdings table should be properly structured for screen readers
    And financial data should have appropriate labels
    And color is not the only indicator of gain/loss

@security @authorization
Scenario: Administrator cannot access client portfolio
    Given I am logged in as an "Administrator"
    When I navigate to "/portfolio"
    Then I should see an access denied message
    And I should not see "My Portfolio"

@security @authorization
Scenario: Advisor can access portfolio (to view client data)
    Given I am logged in as an "Advisor"
    When I navigate to "/portfolio"
    Then I should see the portfolio page
    And I should not see an access denied message

@security @authorization
Scenario: Viewer can access shared portfolio data
    Given I am logged in as a "Viewer"
    When I navigate to "/portfolio"
    Then I should see data shared with me
    And I should not be able to edit any data
