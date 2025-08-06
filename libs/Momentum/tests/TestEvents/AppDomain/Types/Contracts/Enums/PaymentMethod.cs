namespace AppDomain.Types.Contracts.Enums;

/// <summary>
/// Enumeration of supported payment methods
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Payment via credit card
    /// </summary>
    CreditCard,

    /// <summary>
    /// Payment via bank transfer
    /// </summary>
    BankTransfer,

    /// <summary>
    /// Payment via digital wallet service
    /// </summary>
    DigitalWallet
}