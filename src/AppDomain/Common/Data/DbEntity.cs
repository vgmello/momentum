// Copyright (c) OrgName. All rights reserved.

using LinqToDB.Concurrency;
using LinqToDB.Mapping;

namespace AppDomain.Common.Data;

#if INCLUDE_ORLEANS
/// <summary>
///     Base record for all database entities in the AppDomain schema.
///     Provides common auditing fields and optimistic concurrency control.
///     Includes Orleans serialization attributes.
/// </summary>
[Table(Schema = "main")]
[GenerateSerializer]
[Alias("AppDomain.Common.Data.DbEntity")]
public abstract record DbEntity
{
    /// <summary>
    ///     Gets or sets the UTC date and time when the entity was created.
    ///     This value is automatically set and will not be updated on subsequent changes.
    /// </summary>
    [Column(SkipOnUpdate = true)]
    [Id(0)]
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the UTC date and time when the entity was last updated.
    ///     This value is automatically updated on each change.
    /// </summary>
    [Id(1)]
    public DateTime UpdatedDateUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets the version number used for optimistic concurrency control.
    ///     This maps to PostgreSQL's xmin system column and is automatically managed.
    /// </summary>
    [Column("xmin", SkipOnInsert = true, SkipOnUpdate = true)]
    [OptimisticLockProperty(VersionBehavior.Auto)]
    [Id(2)]
    public int Version { get; init; }
}
#else
/// <summary>
///     Base record for all database entities in the AppDomain schema.
///     Provides common auditing fields and optimistic concurrency control.
/// </summary>
[Table(Schema = "main")]
public abstract record DbEntity
{
    /// <summary>
    ///     Gets or sets the UTC date and time when the entity was created.
    ///     This value is automatically set and will not be updated on subsequent changes.
    /// </summary>
    [Column(SkipOnUpdate = true)]
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the UTC date and time when the entity was last updated.
    ///     This value is automatically updated on each change.
    /// </summary>
    public DateTime UpdatedDateUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets the version number used for optimistic concurrency control.
    ///     This maps to PostgreSQL's xmin system column and is automatically managed.
    /// </summary>
    [Column("xmin", SkipOnInsert = true, SkipOnUpdate = true)]
    [OptimisticLockProperty(VersionBehavior.Auto)]
    public int Version { get; init; }
}
#endif
