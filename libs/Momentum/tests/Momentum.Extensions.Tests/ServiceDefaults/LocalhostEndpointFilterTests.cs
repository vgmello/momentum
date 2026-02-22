// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Momentum.ServiceDefaults.Api.EndpointFilters;
using System.Net;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class LocalhostEndpointFilterTests
{
    private readonly FakeLogCollector _logCollector = FakeLogCollector.Create(new FakeLogCollectorOptions());
    private readonly LocalhostEndpointFilter _filter;

    public LocalhostEndpointFilterTests()
    {
        var logger = new FakeLogger(_logCollector);
        _filter = new LocalhostEndpointFilter(logger);
    }

    private static DefaultEndpointFilterInvocationContext CreateContext(
        IPAddress? remoteIpAddress,
        Dictionary<string, string>? headers = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIpAddress;

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                httpContext.Request.Headers[header.Key] = header.Value;
            }
        }

        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    private static EndpointFilterDelegate CreateNextDelegate(object? result = null)
    {
        return _ => new ValueTask<object?>(result ?? "OK");
    }

    [Fact]
    public async Task InvokeAsync_WithLoopbackAddress_ShouldCallNext()
    {
        // Arrange
        var context = CreateContext(IPAddress.Loopback);
        var next = CreateNextDelegate("NextResult");

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBe("NextResult");
    }

    [Fact]
    public async Task InvokeAsync_WithNonLoopbackAddress_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateContext(IPAddress.Parse("192.168.1.100"));
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
    }

    [Fact]
    public async Task InvokeAsync_WithNonLoopbackAddress_ShouldLogRemoteRequest()
    {
        // Arrange
        var context = CreateContext(IPAddress.Parse("10.0.0.5"));
        var next = CreateNextDelegate();

        // Act
        await _filter.InvokeAsync(context, next);

        // Assert
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Debug);
        _logCollector.LatestRecord.Message.ShouldContain("10.0.0.5");
    }

    [Fact]
    public async Task InvokeAsync_WithNullRemoteIp_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateContext(remoteIpAddress: null);
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
    }

    [Fact]
    public async Task InvokeAsync_WithNullRemoteIp_ShouldLogRemoteRequest()
    {
        // Arrange
        var context = CreateContext(remoteIpAddress: null);
        var next = CreateNextDelegate();

        // Act
        await _filter.InvokeAsync(context, next);

        // Assert
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Debug);
        _logCollector.LatestRecord.Message.ShouldContain("local-only endpoint");
    }

    [Theory]
    [InlineData("X-Forwarded-For")]
    [InlineData("X-Real-IP")]
    [InlineData("Forwarded")]
    [InlineData("X-Original-Forwarded-For")]
    public async Task InvokeAsync_WithProxyHeader_ShouldReturnUnauthorized(string headerName)
    {
        // Arrange
        var context = CreateContext(
            IPAddress.Loopback,
            new Dictionary<string, string> { [headerName] = "192.168.1.1" });
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
    }

    [Theory]
    [InlineData("X-Forwarded-For")]
    [InlineData("X-Real-IP")]
    [InlineData("Forwarded")]
    [InlineData("X-Original-Forwarded-For")]
    public async Task InvokeAsync_WithProxyHeader_ShouldLogWarning(string headerName)
    {
        // Arrange
        var context = CreateContext(
            IPAddress.Loopback,
            new Dictionary<string, string> { [headerName] = "192.168.1.1" });
        var next = CreateNextDelegate();

        // Act
        await _filter.InvokeAsync(context, next);

        // Assert
        _logCollector.LatestRecord.Level.ShouldBe(LogLevel.Warning);
        _logCollector.LatestRecord.Message.ShouldContain(headerName);
    }

    [Fact]
    public async Task InvokeAsync_WithIPv6MappedLoopback_ShouldCallNext()
    {
        // Arrange - ::ffff:127.0.0.1 is an IPv6 mapped IPv4 loopback
        var ipv6MappedLoopback = IPAddress.Parse("::ffff:127.0.0.1");
        var context = CreateContext(ipv6MappedLoopback);
        var next = CreateNextDelegate("NextResult");

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBe("NextResult");
    }

    [Fact]
    public async Task InvokeAsync_WithIPv6Loopback_ShouldCallNext()
    {
        // Arrange
        var context = CreateContext(IPAddress.IPv6Loopback);
        var next = CreateNextDelegate("NextResult");

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBe("NextResult");
    }

    [Fact]
    public async Task InvokeAsync_WithProxyHeaderCaseInsensitive_ShouldReturnUnauthorized()
    {
        // Arrange - headers are case-insensitive per the FrozenSet with OrdinalIgnoreCase
        var context = CreateContext(
            IPAddress.Loopback,
            new Dictionary<string, string> { ["x-forwarded-for"] = "10.0.0.1" });
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
    }

    [Fact]
    public async Task InvokeAsync_WithIPv6MappedNonLoopback_ShouldReturnUnauthorized()
    {
        // Arrange - ::ffff:192.168.1.1 is an IPv6 mapped non-loopback IPv4 address
        var ipv6MappedNonLoopback = IPAddress.Parse("::ffff:192.168.1.1");
        var context = CreateContext(ipv6MappedNonLoopback);
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
    }

    [Fact]
    public async Task InvokeAsync_WithLoopbackAddress_ShouldNotLog()
    {
        // Arrange
        var context = CreateContext(IPAddress.Loopback);
        var next = CreateNextDelegate("NextResult");

        // Act
        await _filter.InvokeAsync(context, next);

        // Assert
        _logCollector.Count.ShouldBe(0);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleProxyHeaders_ShouldReturnUnauthorized()
    {
        // Arrange - multiple forwarded headers present
        var context = CreateContext(
            IPAddress.Loopback,
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "10.0.0.1",
                ["X-Real-IP"] = "10.0.0.2"
            });
        var next = CreateNextDelegate();

        // Act
        var result = await _filter.InvokeAsync(context, next);

        // Assert
        result.ShouldBeAssignableTo<IResult>();
        _logCollector.Count.ShouldBe(1);
    }
}
