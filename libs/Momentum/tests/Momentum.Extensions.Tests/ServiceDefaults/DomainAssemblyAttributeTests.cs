// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.ServiceDefaults;
using System.Reflection;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class DomainAssemblyAttributeTests
{
    [Fact]
    public void Constructor_WithTypeMarkers_ShouldCreateAttribute()
    {
        var attribute = new DomainAssemblyAttribute(typeof(string), typeof(int));

        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNoTypeMarkers_ShouldCreateAttribute()
    {
        var attribute = new DomainAssemblyAttribute();

        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void AttributeUsage_ShouldTargetAssembly()
    {
        var usage = typeof(DomainAssemblyAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Assembly);
    }

    [Fact]
    public void AttributeUsage_ShouldAllowMultiple()
    {
        var usage = typeof(DomainAssemblyAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.ShouldNotBeNull();
        usage.AllowMultiple.ShouldBeTrue();
    }
}
