namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
///     Custom exception for tests that cannot run due to missing configuration.
/// </summary>
public class InconclusiveException(string message) : Exception(message);