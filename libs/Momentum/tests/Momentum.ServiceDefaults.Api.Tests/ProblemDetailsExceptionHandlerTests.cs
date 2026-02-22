// Copyright (c) Momentum .NET. All rights reserved.

using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Momentum.ServiceDefaults.Api.Tests;

public class ProblemDetailsExceptionHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProblemDetailsExceptionHandler _handler = new();

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldReturn400WithErrors()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email is invalid")
        };
        var exception = new ValidationException(failures);
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var body = await DeserializeProblemDetails(httpContext);
        body.Status.ShouldBe(StatusCodes.Status400BadRequest);
        body.Title.ShouldBe("Validation Error");
    }

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_ShouldReturn401()
    {
        var exception = new UnauthorizedAccessException("Access denied");
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        var body = await DeserializeProblemDetails(httpContext);
        body.Status.ShouldBe(StatusCodes.Status401Unauthorized);
        body.Title.ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_ShouldReturn404()
    {
        var exception = new KeyNotFoundException("Item not found");
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        var body = await DeserializeProblemDetails(httpContext);
        body.Status.ShouldBe(StatusCodes.Status404NotFound);
        body.Title.ShouldBe("Not Found");
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_ShouldReturn500()
    {
        var exception = new InvalidOperationException("Something went wrong");
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        var body = await DeserializeProblemDetails(httpContext);
        body.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        body.Title.ShouldBe("Internal Server Error");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldAlwaysReturnTrue()
    {
        var httpContext = CreateHttpContext();

        var result = await _handler.TryHandleAsync(
            httpContext, new Exception("any"), TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetContentType()
    {
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(
            httpContext, new Exception("any"), TestContext.Current.CancellationToken);

        httpContext.Response.ContentType.ShouldStartWith("application/json");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetInstanceToRequestPath()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Path = "/api/customers/42";

        await _handler.TryHandleAsync(
            httpContext, new Exception("any"), TestContext.Current.CancellationToken);

        var body = await DeserializeProblemDetails(httpContext);
        body.Instance.ShouldBe("/api/customers/42");
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldGroupErrorsByPropertyName()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Name", "Name must be at least 3 characters"),
            new("Email", "Email is invalid")
        };
        var exception = new ValidationException(failures);
        var httpContext = CreateHttpContext();

        await _handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(
            httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        var errors = document.RootElement.GetProperty("errors");

        var nameErrors = errors.GetProperty("Name").EnumerateArray().Select(e => e.GetString()).ToArray();
        nameErrors.ShouldBe(["Name is required", "Name must be at least 3 characters"]);

        var emailErrors = errors.GetProperty("Email").EnumerateArray().Select(e => e.GetString()).ToArray();
        emailErrors.ShouldBe(["Email is invalid"]);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = "/test";
        return httpContext;
    }

    private static async Task<ProblemDetails> DeserializeProblemDetails(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body, JsonOptions);
        body.ShouldNotBeNull();
        return body;
    }
}
