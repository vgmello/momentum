using Momentum.Extensions.Abstractions.Messaging;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class StringEncodingAttributeTests
{
    [Fact]
    public void DefaultBytesPerChar_ShouldBeOne()
    {
        var attr = new StringEncodingAttribute();
        attr.BytesPerChar.ShouldBe(1);
    }

    [Fact]
    public void BytesPerChar_ShouldBeSettable()
    {
        var attr = new StringEncodingAttribute { BytesPerChar = 4 };
        attr.BytesPerChar.ShouldBe(4);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToAssembly()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Assembly).ShouldNotBe((AttributeTargets)0);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClass()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Class).ShouldNotBe((AttributeTargets)0);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToProperty()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Property).ShouldNotBe((AttributeTargets)0);
    }
}
