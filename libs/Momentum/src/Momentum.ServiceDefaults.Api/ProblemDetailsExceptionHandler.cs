// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Configures the exception handler middleware to return RFC 7807 Problem Details responses.
/// </summary>
public static class ProblemDetailsExceptionHandler
{
    /// <summary>
    ///     Adds the exception handler middleware that maps exceptions to Problem Details responses.
    /// </summary>
    public static void UseProblemDetailsExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                context.Response.ContentType = "application/problem+json";

                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var (statusCode, title) = exception switch
                {
                    FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                    _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
                };

                context.Response.StatusCode = statusCode;

                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Instance = context.Request.Path
                };

                if (exception is FluentValidation.ValidationException validationException)
                {
                    problemDetails.Extensions["errors"] = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                }

                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });
    }
}
