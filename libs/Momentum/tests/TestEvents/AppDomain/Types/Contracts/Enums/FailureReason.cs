namespace AppDomain.Types.Contracts.Enums;

/// <summary>
/// Enumeration of possible payment failure reasons
/// </summary>
public enum FailureReason
{
    /// <summary>
    /// Insufficient funds in the payment source
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// Payment method is invalid or expired
    /// </summary>
    InvalidPaymentMethod,

    /// <summary>
    /// Network connectivity issues during processing
    /// </summary>
    NetworkError,

    /// <summary>
    /// Security checks failed during payment processing
    /// </summary>
    SecurityViolation
}