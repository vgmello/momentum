// Copyright (c) ABCDEG. All rights reserved.

using FluentValidation.Results;
using OneOf;

namespace Operations.Extensions;

/// <summary>
///     Represents a result that can be either a success value or a list of validation failures.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
///     This class uses the OneOf library to provide a discriminated union pattern,
///     allowing methods to return either a successful result of type <typeparamref name="T" />
///     or a list of validation failures. This is particularly useful for command handlers
///     and other operations that need to communicate validation errors without throwing exceptions.
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
}
