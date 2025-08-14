// Copyright (c) ORG_NAME. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace AppDomain.Api.Cashiers.Models;

/// <summary>
///     Request to get a list of cashiers with pagination
/// </summary>
public record GetCashiersRequest
{
    /// <summary>
    ///     Maximum number of cashiers to return (1-1000)
    /// </summary>
    [Range(1, 1000)]
    public int Limit { get; init; } = 1000;

    /// <summary>
    ///     Number of cashiers to skip for pagination
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Offset { get; init; } = 0;
}