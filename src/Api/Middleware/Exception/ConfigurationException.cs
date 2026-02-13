namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when required configuration is missing or invalid.
/// </summary>
public class ConfigurationException(string message) : Exception(message);