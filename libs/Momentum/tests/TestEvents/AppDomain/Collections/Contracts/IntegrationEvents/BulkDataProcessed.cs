// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.AppDomain.Collections.Contracts.IntegrationEvents;

/// <summary>
///     Published when bulk data processing Momentum complete with mixed result types
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns the bulk operation</param>
/// <param name="ProcessedFiles">Array of file names that were processed during the operation</param>
/// <param name="RecordCounts">List of record counts for each processed batch</param>
/// <param name="ErrorMessages">Enumerable collection of error messages encountered during processing</param>
/// <param name="Amounts">Collection of monetary amounts processed in the operation</param>
/// <param name="Metadata">Dictionary of key-value pairs containing operation metadata</param>
/// <param name="CompletedAt">Timestamp when the bulk processing operation completed</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     - Batch processing jobs complete
///     - Multiple data types are processed together
///     - Collection-based Momentum finish
/// 
///     ## Collection Types
/// 
///     This event demonstrates various collection patterns:
///     - Arrays of primitive types
///     - Lists of complex objects
///     - Generic collections with different element types
///     - Nested collections within complex types
/// </remarks>
[EventTopic<BulkDataProcessed>]
public sealed record BulkDataProcessed(
    [PartitionKey(Order = 0)] Guid TenantId,
    string[] ProcessedFiles,
    List<int> RecordCounts,
    IEnumerable<string> ErrorMessages,
    ICollection<decimal> Amounts,
    Dictionary<string, string> Metadata,
    DateTime CompletedAt
);
