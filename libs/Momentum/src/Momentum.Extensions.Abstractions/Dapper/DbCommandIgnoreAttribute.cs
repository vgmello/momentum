// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Dapper;

/// <summary>
///     Marks a property or parameter to be excluded from database parameter generation by the DbCommand source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DbCommandIgnoreAttribute : Attribute;
