// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Cashiers.Contracts.Models;

/// <summary>
///     Represents a cashier who processes transactions within a specific tenant.
/// </summary>
public record Cashier
{
    /// <summary>
    ///     Gets or sets the unique identifier for the tenant this cashier belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for the cashier.
    /// </summary>
    public Guid CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the name of the cashier.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Gets or sets the email address of the cashier.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    ///     Gets or sets the list of payment methods associated with this cashier.
    /// </summary>
    public List<CashierPayment> CashierPayments { get; set; } = [];

    /// <summary>
    ///     Gets or sets the version for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; }
}