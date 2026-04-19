using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Shared;

public static class Constants
{
    public const string DefaultCurrency = "USD";
    public const string DefaultLanguage = "en-US";
    public const string DefaultTimeZone = "America/New_York";
    public const string DefaultDateFormat = "yyyy-MM-dd";
    public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string DefaultTimeFormat = "HH:mm:ss";
    public const string DefaultShortDateFormat = "MM/dd/yyyy";
    public const string DefaultShortTimeFormat = "HH:mm";
    public static readonly string AdministratorRole = nameof(UserRole.Administrator);
}