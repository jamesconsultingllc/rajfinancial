@homepage
Feature: Home Page
    As a visitor to RAJ Financial
    I want to see an informative landing page
    So that I can learn about the platform and get started

Background:
    Given I am on the home page

@smoke
Scenario: Home page loads successfully
    Then the page should load successfully
    And the page title should contain "RAJ Financial"

@ui
Scenario: Home page displays brand identity
    Then I should see the brand name "RAJ Financial"
    And I should see the tagline "Your Financial Future"

@ui
Scenario: Home page displays hero section
    Then I should see the hero section
    And I should see a "Get Started" button
    And I should see a "View Portfolio" button

@ui
Scenario: Home page displays feature cards
    Then I should see at least 3 feature cards
    And I should see features describing the platform benefits

@ui
Scenario: Home page displays call-to-action section
    Then I should see the CTA section
    And the CTA section should encourage users to sign up

@responsive
Scenario: Home page is responsive on mobile
    Given the viewport is set to mobile size
    Then the page should not have horizontal scroll
    And the hero section should be visible
    And the navigation should be accessible

@responsive
Scenario: Home page is responsive on tablet
    Given the viewport is set to tablet size
    Then the page should not have horizontal scroll
    And the hero section should be visible

@responsive
Scenario: Home page is responsive on desktop
    Given the viewport is set to desktop size
    Then the hero section should be visible
    And the feature cards should be visible

@accessibility
Scenario: Home page meets accessibility standards
    Then the page should have proper heading hierarchy
    And all buttons should have accessible labels
    And all images should have alt text
    And focus indicators should be visible when tabbing

@unauthenticated
Scenario: Unauthenticated user sees login button
    Given I am not logged in
    Then I should see a "Log in" button
    And I should not see a "Log out" button
