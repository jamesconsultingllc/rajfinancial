@authentication
Feature: Authentication Flow
    As a new user
    I want to create an account and sign in
    So that I can access the RAJ Financial platform

Background:
    Given the application is running

@signup @cleanup
Scenario: New user can create an account through Entra External ID
    Given I am on the home page
    When I click the "Sign In / Sign Up" button
    Then I should be redirected to the Entra External ID login page
    
    # Step 1: Initiate account creation
    When I click the "Create account" link on Entra page
    
    # Step 2: Email entry and verification
    And I enter a unique test email address
    And I click the "Next" button on Entra page
    Then I should see the email verification code input
    When I retrieve and enter the email verification code

    # Step 3: Attribute collection (profile + password on same form)
    Then I should see the password creation form
    # Fill profile fields first (they appear at top of Entra form)
    When I enter "Test" in the "Given Name" field on Entra page
    And I enter "User" in the "Surname" field on Entra page
    And I enter a unique username
    # Then fill password fields (they appear at bottom of Entra form)
    When I enter a new password
    And I confirm the password
    And I click the "Next" button on Entra page
    
    # Step 5: Permissions consent
    Then I should see the permissions consent screen
    When I click the "Accept" button on Entra page
    
    # Step 6: Successful redirect and verification
    Then I should be redirected back to the RAJ Financial app
    And I should see the client dashboard
    And I should see my username next to the logout button
    And the test user should be marked for cleanup

@login @smoke
Scenario: Existing user can sign in
    Given I am on the home page
    When I click the "Sign In / Sign Up" button
    Then I should be redirected to the Entra External ID login page
    When I sign in with test "Client" credentials
    Then I should be redirected back to the RAJ Financial app
    And I should see the "My Account" section

@logout
Scenario: Authenticated user can log out
    Given I am logged in as a "Client"
    When I click the "Log out" button
    Then I should be logged out
    And I should see the "Sign In / Sign Up" button

@security
Scenario: Unauthenticated user cannot access protected pages
    Given I am not logged in
    When I navigate to "/portfolio"
    Then I should be redirected to the login page

@security @authorization
Scenario: Client user cannot access admin pages
    Given I am logged in as a "Client"
    When I navigate to "/admin/dashboard"
    Then I should see an "Access Denied" message or be redirected
