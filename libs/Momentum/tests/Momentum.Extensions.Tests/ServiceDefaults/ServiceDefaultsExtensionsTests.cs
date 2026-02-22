// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.ServiceDefaults;
using System.Reflection;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class ServiceDefaultsExtensionsTests
{
    [Fact]
    public void EntryAssembly_SetAndGet_ShouldReturnSetValue()
    {
        // Arrange
        var testAssembly = typeof(ServiceDefaultsExtensionsTests).Assembly;

        // Act
        ServiceDefaultsExtensions.EntryAssembly = testAssembly;
        var result = ServiceDefaultsExtensions.EntryAssembly;

        // Assert
        result.ShouldBe(testAssembly);
    }

    [Fact]
    public void EntryAssembly_CalledMultipleTimes_ShouldReturnConsistentValue()
    {
        // Arrange - set the assembly so the Volatile.Read fast path is used
        var testAssembly = typeof(ServiceDefaultsExtensionsTests).Assembly;
        ServiceDefaultsExtensions.EntryAssembly = testAssembly;

        // Act - calling getter multiple times should consistently return the same value
        var first = ServiceDefaultsExtensions.EntryAssembly;
        var second = ServiceDefaultsExtensions.EntryAssembly;
        var third = ServiceDefaultsExtensions.EntryAssembly;

        // Assert
        first.ShouldBe(testAssembly);
        second.ShouldBe(testAssembly);
        third.ShouldBe(testAssembly);
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void EntryAssembly_SetDifferentAssembly_ShouldReturnLatestValue()
    {
        // Arrange
        var firstAssembly = typeof(ServiceDefaultsExtensionsTests).Assembly;
        var secondAssembly = typeof(Assembly).Assembly;

        // Act
        ServiceDefaultsExtensions.EntryAssembly = firstAssembly;
        ServiceDefaultsExtensions.EntryAssembly = secondAssembly;
        var result = ServiceDefaultsExtensions.EntryAssembly;

        // Assert
        result.ShouldBe(secondAssembly);
    }
}
