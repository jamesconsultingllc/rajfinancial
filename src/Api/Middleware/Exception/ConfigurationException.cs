namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when required configuration is missing or invalid.
/// </summary>
public class ConfigurationException(string message) : System.Exception(message);