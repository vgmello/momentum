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

    // --- Edge Case Test Types ---

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class PocoWithAttribute
    {
        [TestMarker]
        public string? Tagged { get; set; }

        public int Untagged { get; set; }
    }

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class ClassWithInitAndExtraConstructorParam
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }

        [SuppressMessage("Sonar", "S1172", Justification = "Extra parameter intentionally tests mismatched parameter count")]
        public ClassWithInitAndExtraConstructorParam(string name, int age, bool extra)
        {
            Name = name;
            Age = age;
        }

        public ClassWithInitAndExtraConstructorParam() { }
    }

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class ClassWithComputedProperty
    {
        [SuppressMessage("Sonar", "S2325", Justification = "Instance property required to test reflection-based IsInitOnly()")]
        [SuppressMessage("CodeQuality", "CA1822", Justification = "Instance property required to test reflection-based IsInitOnly()")]
        public string FullName => "computed";
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class ParamOnlyMarkerAttribute : Attribute;

    private record RecordWithParamOnlyTypeMismatch([ParamOnlyMarker] string Name, int Age);

    [SuppressMessage("Sonar", "S1144", Justification = "Properties are accessed via reflection in tests")]
    private class ClassWithInitAndMatchingConstructor
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }

        public ClassWithInitAndMatchingConstructor(string Name, int Age)
        {
            this.Name = Name;
            this.Age = Age;
        }
    }

    // --- Edge Case Tests ---

    [Fact]
    public void GetPropertiesWithAttribute_OnClassWithNoConstructor_ShouldOnlyCheckDirectAttributes()
    {
        var properties = typeof(PocoWithAttribute).GetPropertiesWithAttribute<TestMarkerAttribute>();

        properties.Count.ShouldBe(1);
        properties.ShouldContain(p => p.Name == "Tagged");
    }

    [Fact]
    public void GetPrimaryConstructor_ForClassWithMultipleConstructors_ShouldReturnMatchingOne()
    {
        var constructor = typeof(ClassWithInitAndMatchingConstructor).GetPrimaryConstructor();

        constructor.ShouldNotBeNull();
        constructor.GetParameters().Length.ShouldBe(2);
        constructor.GetParameters().ShouldContain(p => p.Name == "Name");
        constructor.GetParameters().ShouldContain(p => p.Name == "Age");
    }

    [Fact]
    public void GetPrimaryConstructor_ForClassWithMismatchedParameterCount_ShouldReturnNull()
    {
        var constructor = typeof(ClassWithInitAndExtraConstructorParam).GetPrimaryConstructor();

        constructor.ShouldBeNull();
    }

    [Fact]
    public void IsInitOnly_ForPropertyWithoutSetter_ShouldReturnFalse()
    {
        var property = typeof(ClassWithComputedProperty).GetProperty("FullName")!;

        property.IsInitOnly().ShouldBeFalse();
    }

    [Fact]
    public void GetCustomAttribute_WithNonMatchingParameterType_ShouldReturnNull()
    {
        var property = typeof(RecordWithParamOnlyTypeMismatch).GetProperty("Age")!;
        var constructor = typeof(RecordWithParamOnlyTypeMismatch).GetPrimaryConstructor();

        var attribute = property.GetCustomAttribute<ParamOnlyMarkerAttribute>(constructor);

        attribute.ShouldBeNull();
    }
}
