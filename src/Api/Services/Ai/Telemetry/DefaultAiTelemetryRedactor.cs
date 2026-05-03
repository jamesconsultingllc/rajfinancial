using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// Default <see cref="IAiTelemetryRedactor"/>. Pure (no I/O), thread-safe, deterministic
/// per <see cref="AiTelemetryRedactorOptions.MerchantHashSecret"/>.
/// </summary>
internal sealed class DefaultAiTelemetryRedactor : IAiTelemetryRedactor
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "pagenumber",
        "pagesize",
        "limit",
        "offset",
        "scope",
        "type",
        "kind",
        "category",
        "currency",
        "locale",
        "language",
        "sort",
        "order",
        "fromdate",
        "todate",
        "from",
        "to",
        "year",
        "month",
        "day",
    };

    private static readonly HashSet<string> MerchantLikeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "merchantname",
        "merchant",
        "payee",
        "vendor",
        "vendorname",
        "counterparty",
        "description",
    };

    private static readonly HashSet<string> AccountLikeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "accountnumber",
        "accountnum",
        "cardnumber",
        "cardnum",
        "cardlast4",
        "routingnumber",
        "iban",
        "ssn",
        "taxid",
        "institutionaccountid",
    };

    private readonly byte[] _hmacKey;
    private readonly int _hmacPrefixLength;

    public DefaultAiTelemetryRedactor(IOptions<AiTelemetryRedactorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value
            ?? throw new InvalidOperationException("AiTelemetryRedactorOptions is null.");
        _hmacKey = Encoding.UTF8.GetBytes(value.MerchantHashSecret);
        _hmacPrefixLength = value.HmacPrefixLength;
    }

    public string Redact(string toolName, string argumentName, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);

        if (value is null)
        {
            return "<null>";
        }

        // Allow-list: pass numerics + bools + short safe identifiers verbatim.
        if (AllowList.Contains(argumentName))
        {
            return RenderAllowed(value);
        }

        var raw = ConvertToString(value);

        if (MerchantLikeNames.Contains(argumentName))
        {
            return $"[REDACTED:HMAC={ComputeHmacPrefix(raw)}]";
        }

        if (AccountLikeNames.Contains(argumentName))
        {
            return MaskTrailing(raw, 4);
        }

        // Fall through: never echo unknown arg values into telemetry.
        return "[REDACTED]";
    }

    internal string ComputeHmacPrefix(string raw)
    {
        Span<byte> hash = stackalloc byte[32];
        var written = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(raw), hash);
        var hex = Convert.ToHexString(hash[..written]).ToLowerInvariant();
        return hex[..Math.Min(_hmacPrefixLength, hex.Length)];
    }

    internal static string MaskTrailing(string raw, int keepTrailing)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return "****";
        }

        if (raw.Length <= keepTrailing)
        {
            return new string('*', raw.Length);
        }

        return string.Concat(new string('*', raw.Length - keepTrailing), raw.AsSpan(raw.Length - keepTrailing));
    }

    private static string RenderAllowed(object value) => value switch
    {
        string s => s.Length <= 32 ? s : s[..32],
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "<null>",
    };

    private static string ConvertToString(object value) => value switch
    {
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
