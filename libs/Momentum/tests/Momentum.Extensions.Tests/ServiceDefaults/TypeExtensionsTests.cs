// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Momentum.ServiceDefaults.Extensions;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class TypeExtensionsTests
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    private sealed class TestMarkerAttribute : Attribute;

    private record RecordWithPropertyAttribute([property: TestMarker] string Name, int Age);

    private record RecordWithParamAttribute([TestMarker] string Name, int Age);

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class ClassWithSettableProperty
    {
        public string? Name { get; set; }
    }

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class ClassWithReadOnlyProperty
    {
        public string Name { get; } = "test";
    }

    private record PlainRecord(string Name, int Age);

    // --- GetPropertiesWithAttribute ---

    [Fact]
    public void GetPropertiesWithAttribute_WithDirectPropertyAttribute_ShouldFindProperty()
    {
        var properties = typeof(RecordWithPropertyAttribute).GetPropertiesWithAttribute<TestMarkerAttribute>();

        properties.Count.ShouldBe(1);
        properties.ShouldContain(p => p.Name == "Name");
    }

    [Fact]
    public void GetPropertiesWithAttribute_WithConstructorParameterAttribute_ShouldFindProperty()
    {
        var properties = typeof(RecordWithParamAttribute).GetPropertiesWithAttribute<TestMarkerAttribute>();

        properties.Count.ShouldBe(1);
        properties.ShouldContain(p => p.Name == "Name");
    }

    [Fact]
    public void GetPropertiesWithAttribute_WithNoAttributes_ShouldReturnEmptySet()
    {
        var properties = typeof(PlainRecord).GetPropertiesWithAttribute<TestMarkerAttribute>();

        properties.ShouldBeEmpty();
    }

    // --- GetCustomAttribute ---

    [Fact]
    public void GetCustomAttribute_WithAttributeOnProperty_ShouldReturnAttribute()
    {
        var property = typeof(RecordWithPropertyAttribute).GetProperty("Name")!;
        var constructor = typeof(RecordWithPropertyAttribute).GetPrimaryConstructor();

        var attribute = property.GetCustomAttribute<TestMarkerAttribute>(constructor);

        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void GetCustomAttribute_WithAttributeOnConstructorParam_ShouldFallBackToParameter()
    {
        var property = typeof(RecordWithParamAttribute).GetProperty("Name")!;
        var constructor = typeof(RecordWithParamAttribute).GetPrimaryConstructor();

        var attribute = property.GetCustomAttribute<TestMarkerAttribute>(constructor);

        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void GetCustomAttribute_WithNoAttribute_ShouldReturnNull()
    {
        var property = typeof(PlainRecord).GetProperty("Name")!;
        var constructor = typeof(PlainRecord).GetPrimaryConstructor();

        var attribute = property.GetCustomAttribute<TestMarkerAttribute>(constructor);

        attribute.ShouldBeNull();
    }

    [Fact]
    public void GetCustomAttribute_WithNullConstructor_ShouldReturnNull()
    {
        var property = typeof(PlainRecord).GetProperty("Name")!;

        var attribute = property.GetCustomAttribute<TestMarkerAttribute>(primaryConstructor: null);

        attribute.ShouldBeNull();
    }

    // --- GetPrimaryConstructor ---

    [Fact]
    public void GetPrimaryConstructor_ForRecord_ShouldFindConstructor()
    {
        var constructor = typeof(PlainRecord).GetPrimaryConstructor();

        constructor.ShouldNotBeNull();
        constructor.GetParameters().Length.ShouldBe(2);
    }

    [Fact]
    public void GetPrimaryConstructor_ForClassWithNoInitProperties_ShouldReturnNull()
    {
        var constructor = typeof(ClassWithSettableProperty).GetPrimaryConstructor();

        constructor.ShouldBeNull();
    }

    [Fact]
    public void GetPrimaryConstructor_ForClassWithReadOnlyProperty_ShouldReturnNull()
    {
        var constructor = typeof(ClassWithReadOnlyProperty).GetPrimaryConstructor();

        constructor.ShouldBeNull();
    }

    // --- IsInitOnly ---

    [Fact]
    public void IsInitOnly_ForRecordProperty_ShouldReturnTrue()
    {
        var property = typeof(PlainRecord).GetProperty("Name")!;

        property.IsInitOnly().ShouldBeTrue();
    }

    [Fact]
    public void IsInitOnly_ForSettableProperty_ShouldReturnFalse()
    {
        var property = typeof(ClassWithSettableProperty).GetProperty("Name")!;

        property.IsInitOnly().ShouldBeFalse();
    }

    [Fact]
    public void IsInitOnly_ForReadOnlyProperty_ShouldReturnFalse()
    {
        var property = typeof(ClassWithReadOnlyProperty).GetProperty("Name")!;

        property.IsInitOnly().ShouldBeFalse();
    }
}
