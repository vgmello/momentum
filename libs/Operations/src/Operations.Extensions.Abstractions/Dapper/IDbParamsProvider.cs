// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.Abstractions.Dapper;

/// <summary>
///     Defines a contract for objects that can provide database parameters for Dapper queries.
/// </summary>
/// <remarks>
///     Implement this interface on command or query objects to provide a custom mapping
///     between the object's properties and database parameters. This is particularly useful
///     when the object structure doesn't directly match the expected database parameters.
/// </remarks>
public interface IDbParamsProvider
{
    /// <summary>
    ///     Converts the current object to a database parameter object.
    /// </summary>
    /// <returns>
    ///     An object containing the parameters to be used in a database query.
    ///     This is typically an anonymous object or a dynamic object with properties
    ///     matching the parameter names expected by the SQL query.
    /// </returns>
    /// <example>
    ///     <code>
    /// public object ToDbParams() => new
    /// {
    ///     Id = this.UserId,
    ///     Name = this.UserName,
    ///     CreatedAt = DateTime.UtcNow
    /// };
    /// </code>
    /// </example>
    object ToDbParams();
}
