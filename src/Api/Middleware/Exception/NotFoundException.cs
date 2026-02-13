namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException(string errorCode, string message) : System.Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public static NotFoundException Asset(Guid assetId) =>
        new("ASSET_NOT_FOUND", $"Asset with ID {assetId} was not found");

    public static NotFoundException Account(Guid accountId) =>
        new("ACCOUNT_NOT_FOUND", $"Account with ID {accountId} was not found");

    public static NotFoundException Beneficiary(Guid beneficiaryId) =>
        new("BENEFICIARY_NOT_FOUND", $"Beneficiary with ID {beneficiaryId} was not found");

    public static NotFoundException User(string userId) =>
        new("USER_NOT_FOUND", $"User with ID {userId} was not found");
}