// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.ServiceDefaults.Messaging.Wolverine;
using Wolverine;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class WolverineExtensionsTests
{
    private class SimpleMessage;

    private class OuterClass
    {
        public class NestedMessage;
    }

    [Fact]
    public void GetMessageName_WithMessage_ShouldReturnTypeName()
    {
        var envelope = new Envelope { Message = new SimpleMessage() };

        var name = envelope.GetMessageName();

        name.ShouldBe("SimpleMessage");
    }

    [Fact]
    public void GetMessageName_WithFullName_ShouldReturnFullTypeName()
    {
        var envelope = new Envelope { Message = new SimpleMessage() };

        var name = envelope.GetMessageName(fullName: true);

        name.ShouldContain("WolverineExtensionsTests");
        name.ShouldContain("SimpleMessage");
    }

    [Fact]
    public void GetMessageName_WithNullMessage_ShouldReturnMessageType()
    {
        var envelope = new Envelope { MessageType = "SomeMessageType" };

        var name = envelope.GetMessageName();

        name.ShouldBe("SomeMessageType");
    }

    [Fact]
    public void GetMessageName_WithNullMessageAndNullMessageType_ShouldReturnUnknownMessage()
    {
        var envelope = new Envelope();

        var name = envelope.GetMessageName();

        name.ShouldBe("UnknownMessage");
    }

    [Fact]
    public void GetMessageName_WithNestedType_ShouldSanitizeFullName()
    {
        var envelope = new Envelope { Message = new OuterClass.NestedMessage() };

        var name = envelope.GetMessageName(fullName: true);

        name.ShouldNotContain("+");
        name.ShouldContain("_");
    }

    [Fact]
    public void GetMessageName_WithGenericType_ShouldSanitizeFullName()
    {
        var envelope = new Envelope { Message = new List<string>() };

        var name = envelope.GetMessageName(fullName: true);

        name.ShouldNotContain("<");
        name.ShouldNotContain(">");
    }
}
