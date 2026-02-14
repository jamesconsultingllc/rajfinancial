using Xunit;

// Exclude this entire project from Live Unit Testing.
// Integration tests require a running Functions host and are too slow for continuous execution.
[assembly: AssemblyTrait("Category", "SkipWhenLiveUnitTesting")]
