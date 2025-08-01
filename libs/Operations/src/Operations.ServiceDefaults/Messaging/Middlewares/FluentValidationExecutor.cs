// Copyright (c) ABCDEG. All rights reserved.

using FluentValidation;
using FluentValidation.Results;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

public static class FluentValidationExecutor
{
    public static async Task<List<ValidationFailure>> ExecuteOne<T>(IValidator<T> validator, T message)
    {
        var result = await validator.ValidateAsync(message);

        return result.Errors;
    }

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
