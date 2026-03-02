namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Types of cryptocurrency wallets and custody solutions.
/// </summary>
public enum CryptoWalletType
{
    /// <summary>Centralized exchange account (Coinbase, Binance, etc.).</summary>
    Exchange,

    /// <summary>Hardware wallet (Ledger, Trezor, etc.).</summary>
    HardwareWallet,

    /// <summary>Software/hot wallet (MetaMask, Trust Wallet, etc.).</summary>
    SoftwareWallet,

    /// <summary>Custodial account managed by a third party.</summary>
    CustodialAccount,

    /// <summary>Other wallet type.</summary>
    Other
}
