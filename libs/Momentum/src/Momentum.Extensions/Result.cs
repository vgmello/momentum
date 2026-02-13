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
public partial class Result<T> : OneOfBase<T, List<ValidationFailure>>;
