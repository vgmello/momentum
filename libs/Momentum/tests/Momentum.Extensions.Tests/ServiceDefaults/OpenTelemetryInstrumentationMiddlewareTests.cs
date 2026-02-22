// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.ServiceDefaults.Messaging;
using Momentum.ServiceDefaults.Messaging.Middlewares;
using Wolverine;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public sealed class OpenTelemetryInstrumentationMiddlewareTests : IDisposable
{
    private record SimpleMessage(string Value);

    private record TestCommand(string Value) : ICommand<string>;

    private record TestQuery(string Value) : IQuery<string>;

    private readonly ActivitySource _activitySource = new("TestSource.OTelMiddleware");
    private readonly ActivityListener _listener;

    public OpenTelemetryInstrumentationMiddlewareTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
    }

    [Fact]
    public void Before_WithMessage_ShouldStartActivity()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(nameof(SimpleMessage));
        activity.Dispose();
    }

    [Fact]
    public void Before_WithMessage_ShouldSetMessageIdTag()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("message.id").ShouldBe(envelope.Id.ToString());
        activity.Dispose();
    }

    [Fact]
    public void Before_WithMessage_ShouldSetDestinationTag()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"))
        {
            Destination = new Uri("rabbitmq://queue/test-queue")
        };

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("messaging.destination").ShouldBe("rabbitmq://queue/test-queue");
        activity.Dispose();
    }

    [Fact]
    public void Before_WithMessage_ShouldSetMessageNameTag()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        var messageName = (string?)activity.GetTagItem("message.name");
        messageName.ShouldNotBeNull();
        messageName.ShouldContain(nameof(SimpleMessage));
        activity.Dispose();
    }

    [Fact]
    public void Before_WithCommandMessage_ShouldSetOperationTypeToCommand()
    {
        // Arrange
        var envelope = new Envelope(new TestCommand("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("operation.type").ShouldBe("command");
        activity.Dispose();
    }

    [Fact]
    public void Before_WithQueryMessage_ShouldSetOperationTypeToQuery()
    {
        // Arrange
        var envelope = new Envelope(new TestQuery("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("operation.type").ShouldBe("query");
        activity.Dispose();
    }

    [Fact]
    public void Before_WithSimpleMessage_ShouldNotSetOperationType()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("operation.type").ShouldBeNull();
        activity.Dispose();
    }

    [Fact]
    public void Before_WithSource_ShouldSetSourceTag()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"))
        {
            Source = "test-source-app"
        };

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("message.source").ShouldBe("test-source-app");
        activity.Dispose();
    }

    [Fact]
    public void Before_WithoutSource_ShouldNotSetSourceTag()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("message.source").ShouldBeNull();
        activity.Dispose();
    }

    [Fact]
    public void Before_WithTraceParentHeader_ShouldUseAsParentId()
    {
        // Arrange
        const string traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        var envelope = new Envelope(new SimpleMessage("test"));

        var headersProperty = typeof(Envelope).GetProperty(nameof(Envelope.Headers))!;
        headersProperty.SetValue(envelope, new Dictionary<string, string?>
        {
            [DistributedTracingExtensions.TraceParentAttribute.Name] = traceParent
        });

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.ParentId.ShouldBe(traceParent);
        activity.Dispose();
    }

    [Fact]
    public void Before_WithParentId_ShouldUseAsParentIdFallback()
    {
        // Arrange
        const string parentId = "00-abcdef1234567890abcdef1234567890-1234567890abcdef-01";
        var envelope = new Envelope(new SimpleMessage("test"))
        {
            ParentId = parentId
        };

        // Act
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);

        // Assert
        activity.ShouldNotBeNull();
        activity.ParentId.ShouldBe(parentId);
        activity.Dispose();
    }

    [Fact]
    public void Finally_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));

        // Act & Assert
        Should.NotThrow(() => OpenTelemetryInstrumentationMiddleware.Finally(null, envelope));
    }

    [Fact]
    public void Finally_WithSuccessfulProcessing_ShouldSetOkStatus()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"));
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);
        activity.ShouldNotBeNull();

        // Act
        OpenTelemetryInstrumentationMiddleware.Finally(activity, envelope);

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void Finally_WithFailure_ShouldSetErrorStatus()
    {
        // Arrange
        var envelope = new Envelope(new SimpleMessage("test"))
        {
            Failure = new InvalidOperationException("Something went wrong")
        };
        var activity = OpenTelemetryInstrumentationMiddleware.Before(_activitySource, envelope);
        activity.ShouldNotBeNull();

        // Act
        OpenTelemetryInstrumentationMiddleware.Finally(activity, envelope);

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Something went wrong");
        activity.GetTagItem("error.type").ShouldBe(nameof(InvalidOperationException));
    }
}
