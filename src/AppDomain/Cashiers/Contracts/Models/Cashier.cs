// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Cashiers.Contracts.Models;

/// <summary>
///     Represents a cashier who processes transactions within a specific tenant.
/// </summary>
public record Cashier
{
    /// <summary>
    ///     Gets the unique identifier for the tenant this cashier belongs to.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    ///     Gets the unique identifier for the cashier.
    /// </summary>
    public Guid CashierId { get; init; }

    /// <summary>
    ///     Gets the name of the cashier.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the email address of the cashier.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Gets the list of payment methods associated with this cashier.
    /// </summary>
    public IReadOnlyList<CashierPayment> CashierPayments { get; init; } = [];

    /// <summary>
    ///     Gets the date and time when the cashier was created (UTC).
    /// </summary>
    public DateTime CreatedDateUtc { get; init; }

    /// <summary>
    ///     Gets the date and time when the cashier was last updated (UTC).
    /// </summary>
    public DateTime UpdatedDateUtc { get; init; }

    /// <summary>
    ///     Gets the version for optimistic concurrency control.
    /// </summary>
    public int Version { get; init; }
}
