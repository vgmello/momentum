// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation.Results;
using OneOf;

namespace Momentum.Extensions;

/// <summary>
///     Represents a result that can be either a success value or a list of validation failures.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
///     <!--@include: @code/patterns/result-detailed.md#overview -->
/// </remarks>
/// <example>
///     <code>
/// public Result&lt;User&gt; CreateUser(CreateUserCommand command)
/// {
///     var validationResult = validator.Validate(command);
///     if (!validationResult.IsValid)
///         return validationResult.Errors;
/// 
///     var user = new User(command.Name, command.Email);
///     return user;
/// }
/// </code>
/// </example>
[GenerateOneOf]
public partial class Result<T> : OneOfBase<T, List<ValidationFailure>>
{
    /// <summary>
    ///     Creates a failed <see cref="Result{T}" /> from a single <see cref="ValidationFailure" />,
    ///     so a handler can <c>return failure;</c> directly.
    /// </summary>
    public static implicit operator Result<T>(ValidationFailure failure) =>
        new Result<T>(new List<ValidationFailure> { failure });

    /// <summary>
    ///     Creates a failed <see cref="Result{T}" /> from a <see cref="ValidationResult" />'s errors.
    ///     Explicit because a valid result carries no failures — check <see cref="ValidationResult.IsValid" />
    ///     before converting.
    /// </summary>
    public static explicit operator Result<T>(ValidationResult validationResult) =>
        new Result<T>(validationResult.Errors);
}
