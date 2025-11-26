// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation;
using FluentValidation.Results;

namespace Momentum.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Provides methods for executing FluentValidation validators against messages.
/// </summary>
/// <remarks>
///     This class is used by the Wolverine middleware to validate incoming messages
///     before they are processed by handlers. It supports both single and multiple
///     validator execution patterns.
/// </remarks>
public static class FluentValidationExecutor
{
    /// <summary>
    ///     Executes a single FluentValidation validator against a message.
    /// </summary>
    /// <typeparam name="T">The type of message to validate.</typeparam>
    /// <param name="validator">The validator to execute.</param>
    /// <param name="message">The message instance to validate.</param>
    /// <returns>A list of validation failures, empty if validation succeeds.</returns>
    public static async Task<List<ValidationFailure>> ExecuteOne<T>(IValidator<T> validator, T message)
    {
        var result = await validator.ValidateAsync(message);

        return result.Errors;
    }

    /// <summary>
    ///     Executes multiple FluentValidation validators against a message and aggregates all failures.
    /// </summary>
    /// <typeparam name="T">The type of message to validate.</typeparam>
    /// <param name="validators">The validators to execute.</param>
    /// <param name="message">The message instance to validate.</param>
    /// <returns>A list of all validation failures from all validators, empty if all validations succeed.</returns>
    public static async Task<List<ValidationFailure>> ExecuteMany<T>(IEnumerable<IValidator<T>> validators, T message)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(message);

            if (result.Errors.Count is not 0)
            {
                failures.AddRange(result.Errors);
            }
        }

        return failures;
    }
}
