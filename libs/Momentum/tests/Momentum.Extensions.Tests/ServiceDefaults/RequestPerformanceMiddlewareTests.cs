// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Momentum.ServiceDefaults.Messaging.Middlewares;
using Momentum.ServiceDefaults.Messaging.Telemetry;
using Wolverine;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public sealed class RequestPerformanceMiddlewareTests : IDisposable
{
    private record TestMessage(string Value);

    private readonly Meter _meter = new("TestMeter.PerfMiddleware");
    private readonly MessagingMeterStore _meterStore;
    private readonly MeterListener _meterListener;
    private readonly List<string> _recordedInstruments = [];
    private readonly FakeLogCollector _logCollector = FakeLogCollector.Create(new FakeLogCollectorOptions());
    private readonly FakeLogger _fakeLogger;

    public RequestPerformanceMiddlewareTests()
    {
        _meterStore = new MessagingMeterStore(_meter);
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == _meter)
                listener.EnableMeasurementEvents(instrument);
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
        {
            _recordedInstruments.Add(instrument.Name);
        });
        _meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            _recordedInstruments.Add(instrument.Name);
        });
        _meterListener.Start();

        _fakeLogger = new FakeLogger(_logCollector);
    }

    public void Dispose()
    {
        _meterListener.Dispose();
        _meter.Dispose();
    }

    [Fact]
    public void Before_ShouldRecordMessageReceived()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"));

        // Act
        middleware.Before(_fakeLogger, envelope, _meterStore);
        _meterListener.RecordObservableInstruments();

        // Assert
        _recordedInstruments.ShouldContain(name => name.EndsWith(".count"));
    }

    [Fact]
    public void Before_ShouldLogRequestReceived()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"));

        // Act
        middleware.Before(_fakeLogger, envelope, _meterStore);

        // Assert
        _logCollector.Count.ShouldBe(1);
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Debug);
        _logCollector.LatestRecord.Message.ShouldContain("TestMessage");
        _logCollector.LatestRecord.Message.ShouldContain("received");
    }

    [Fact]
    public void Finally_WithNoFailure_ShouldCompleteSuccessfully()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"));

        middleware.Before(_fakeLogger, envelope, _meterStore);

        // Act & Assert
        Should.NotThrow(() => middleware.Finally(_fakeLogger, envelope));
    }

    [Fact]
    public void Finally_WithNoFailure_ShouldLogRequestCompleted()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"));

        middleware.Before(_fakeLogger, envelope, _meterStore);
        _logCollector.Clear();

        // Act
        middleware.Finally(_fakeLogger, envelope);

        // Assert
        _logCollector.Count.ShouldBe(1);
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Debug);
        _logCollector.LatestRecord.Message.ShouldContain("TestMessage");
        _logCollector.LatestRecord.Message.ShouldContain("completed");
    }

    [Fact]
    public void Finally_WithNoFailure_ShouldRecordProcessingTime()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"));

        middleware.Before(_fakeLogger, envelope, _meterStore);
        _recordedInstruments.Clear();

        // Act
        middleware.Finally(_fakeLogger, envelope);
        _meterListener.RecordObservableInstruments();

        // Assert
        _recordedInstruments.ShouldContain(name => name.EndsWith(".duration"));
    }

    [Fact]
    public void Finally_WithFailure_ShouldRecordException()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"))
        {
            Failure = new InvalidOperationException("Processing failed")
        };

        middleware.Before(_fakeLogger, envelope, _meterStore);
        _recordedInstruments.Clear();

        // Act
        middleware.Finally(_fakeLogger, envelope);
        _meterListener.RecordObservableInstruments();

        // Assert
        _recordedInstruments.ShouldContain(name => name.EndsWith(".exceptions"));
    }

    [Fact]
    public void Finally_WithFailure_ShouldLogRequestFailed()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var exception = new InvalidOperationException("Processing failed");
        var envelope = new Envelope(new TestMessage("test"))
        {
            Failure = exception
        };

        middleware.Before(_fakeLogger, envelope, _meterStore);
        _logCollector.Clear();

        // Act
        middleware.Finally(_fakeLogger, envelope);

        // Assert
        _logCollector.Count.ShouldBe(1);
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Error);
        _logCollector.LatestRecord.Message.ShouldContain("TestMessage");
        _logCollector.LatestRecord.Message.ShouldContain("failed");
        _logCollector.LatestRecord.Exception.ShouldBe(exception);
    }

    [Fact]
    public void Finally_WithFailure_ShouldRecordProcessingTime()
    {
        // Arrange
        var middleware = new RequestPerformanceMiddleware();
        var envelope = new Envelope(new TestMessage("test"))
        {
            Failure = new InvalidOperationException("Processing failed")
        };

        middleware.Before(_fakeLogger, envelope, _meterStore);
        _recordedInstruments.Clear();

        // Act
        middleware.Finally(_fakeLogger, envelope);
        _meterListener.RecordObservableInstruments();

        // Assert
        _recordedInstruments.ShouldContain(name => name.EndsWith(".duration"));
        _recordedInstruments.ShouldContain(name => name.EndsWith(".exceptions"));
    }
}
