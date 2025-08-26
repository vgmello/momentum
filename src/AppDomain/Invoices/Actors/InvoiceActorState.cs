// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Invoices.Actors;

/// <summary>
///     State class for Orleans grain persistence containing only metadata.
///     Invoice data is loaded from the database on demand.
/// </summary>
[GenerateSerializer]
public sealed class InvoiceActorState
{
    /// <summary>
    ///     Gets or sets the last accessed timestamp.
    /// </summary>
    [Id(0)]
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the grain activation timestamp for monitoring.
    /// </summary>
    [Id(1)]
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the number of operations performed on this grain.
    /// </summary>
    [Id(2)]
    public long OperationCount { get; set; }

    /// <summary>
    ///     Gets or sets metadata about the grain state for debugging and monitoring.
    /// </summary>
    [Id(3)]
    public Dictionary<string, string>? Metadata { get; set; } = new();

    /// <summary>
    ///     Gets or sets cached tenant ID for this grain.
    /// </summary>
    [Id(4)]
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets whether this grain has been initialized.
    /// </summary>
    [Id(5)]
    public bool IsInitialized { get; set; }
}
