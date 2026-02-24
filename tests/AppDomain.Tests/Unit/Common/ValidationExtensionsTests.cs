// Copyright (c) OrgName. All rights reserved.

using AppDomain.Api.Common.Extensions;
using FluentValidation.Results;

namespace AppDomain.Tests.Unit.Common;

public class ValidationExtensionsTests
{
    [Fact]
    public void GetTenantId_FromHttpContext_ShouldReturnExpectedTenantId()
    {
        var context = new DefaultHttpContext();

        var tenantId = context.GetTenantId();

        tenantId.ShouldBe(Guid.Parse("12345678-0000-0000-0000-000000000000"));
    }

    [Fact]
    public void IsConcurrencyConflict_WithVersionConflictError_ShouldReturnTrue()
    {
        var errors = new List<ValidationFailure>
        {
            new("Version", "The entity was modified by another user")
        };

        errors.IsConcurrencyConflict().ShouldBeTrue();
    }

    [Fact]
    public void IsConcurrencyConflict_WithDifferentPropertyName_ShouldReturnFalse()
    {
        var errors = new List<ValidationFailure>
        {
            new("Name", "The entity was modified by another user")
        };

        errors.IsConcurrencyConflict().ShouldBeFalse();
    }

    [Fact]
    public void IsConcurrencyConflict_WithDifferentErrorMessage_ShouldReturnFalse()
    {
        var errors = new List<ValidationFailure>
        {
            new("Version", "Version is required")
        };

        errors.IsConcurrencyConflict().ShouldBeFalse();
    }

    [Fact]
    public void IsConcurrencyConflict_WithEmptyErrors_ShouldReturnFalse()
    {
        var errors = new List<ValidationFailure>();

        errors.IsConcurrencyConflict().ShouldBeFalse();
    }

    [Fact]
    public void ToValidationErrors_ShouldGroupByPropertyName()
    {
        var errors = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Name", "Name must be at least 3 characters"),
            new("Amount", "Amount must be positive")
        };

        var result = errors.ToValidationErrors();

        result.Keys.Count.ShouldBe(2);
        result["Name"].Length.ShouldBe(2);
        result["Name"].ShouldContain("Name is required");
        result["Name"].ShouldContain("Name must be at least 3 characters");
        result["Amount"].Length.ShouldBe(1);
        result["Amount"].ShouldContain("Amount must be positive");
    }

    [Fact]
    public void ToValidationErrors_WithSingleError_ShouldReturnSingleEntry()
    {
        var errors = new List<ValidationFailure>
        {
            new("Email", "Email is invalid")
        };

        var result = errors.ToValidationErrors();

        result.Keys.Count.ShouldBe(1);
        result["Email"].ShouldBe(["Email is invalid"]);
    }
}
