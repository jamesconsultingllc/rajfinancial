namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
///     Custom exception for tests that cannot run due to missing configuration or prerequisites.
/// </summary>
public class InconclusiveException(string message) : Exception(message);
