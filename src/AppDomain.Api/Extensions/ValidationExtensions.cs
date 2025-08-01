// Copyright (c) ABCDEG. All rights reserved.

using FluentValidation.Results;

namespace AppDomain.Api.Extensions;

public static class ValidationExtensions
{
    public static bool IsConcurrencyConflict(this IEnumerable<ValidationFailure> errors)
    {
        return errors.Any(e => e.PropertyName == "Version" &&
                               e.ErrorMessage.Contains("modified by another user"));
    }
}
