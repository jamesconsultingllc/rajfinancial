using Xunit;

// Exclude this entire project from Live Unit Testing.
// Acceptance tests require Playwright browsers and are too slow for continuous execution.
[assembly: AssemblyTrait("Category", "SkipWhenLiveUnitTesting")]
