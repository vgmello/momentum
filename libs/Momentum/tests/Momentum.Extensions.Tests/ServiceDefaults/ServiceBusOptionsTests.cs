// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Messaging;
using NSubstitute;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class ServiceBusOptionsTests
{
    private readonly ServiceBusOptions.Configurator _configurator;

    public ServiceBusOptionsTests()
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ApplicationName.Returns("MyDomain.Api");
        _configurator = new ServiceBusOptions.Configurator(hostEnvironment);
    }

    [Fact]
    public void PostConfigure_WithValidOptions_ShouldSetServiceUrn()
    {
        var options = new ServiceBusOptions
        {
            Domain = "MyDomain",
            PublicServiceName = "my-service"
        };

        _configurator.PostConfigure(name: null, options);

        options.ServiceUrn.ShouldNotBeNull();
        options.ServiceUrn.ToString().ShouldBe("/my_domain/my-service");
    }

    [Fact]
    public void PostConfigure_WithEmptyServiceName_ShouldDeriveFromAppName()
    {
        var options = new ServiceBusOptions
        {
            Domain = "MyDomain",
            PublicServiceName = string.Empty
        };

        _configurator.PostConfigure(name: null, options);

        options.PublicServiceName.ShouldBe("mydomain-api");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PostConfigure_WithEmptyDomain_ShouldThrow(string domain)
    {
        var options = new ServiceBusOptions
        {
            Domain = domain,
            PublicServiceName = "my-service"
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            _configurator.PostConfigure(name: null, options));

        exception.Message.ShouldContain("Domain cannot be empty");
    }

    [Theory]
    [InlineData("123invalid")]
    [InlineData("has-hyphen")]
    [InlineData("has.dot")]
    public void PostConfigure_WithInvalidDomainFormat_ShouldThrow(string domain)
    {
        var options = new ServiceBusOptions
        {
            Domain = domain,
            PublicServiceName = "my-service"
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            _configurator.PostConfigure(name: null, options));

        exception.Message.ShouldContain("Invalid ServiceBus Domain format");
    }

    [Theory]
    [InlineData("HasUpperCase")]
    [InlineData("has_underscore")]
    public void PostConfigure_WithInvalidServiceNameFormat_ShouldThrow(string serviceName)
    {
        var options = new ServiceBusOptions
        {
            Domain = "MyDomain",
            PublicServiceName = serviceName
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            _configurator.PostConfigure(name: null, options));

        exception.Message.ShouldContain("Invalid ServiceBus PublicServiceName format");
    }

    [Fact]
    public void PostConfigure_WithValidServiceName_ShouldBuildCorrectUrn()
    {
        var options = new ServiceBusOptions
        {
            Domain = "ECommerce",
            PublicServiceName = "order-service"
        };

        _configurator.PostConfigure(name: null, options);

        options.ServiceUrn.ToString().ShouldBe("/e_commerce/order-service");
    }

    [Fact]
    public void PostConfigure_ServiceUrn_ShouldUseSnakeCaseDomain()
    {
        var options = new ServiceBusOptions
        {
            Domain = "MyDomain",
            PublicServiceName = "test-service"
        };

        _configurator.PostConfigure(name: null, options);

        options.ServiceUrn.ToString().ShouldStartWith("/my_domain/");
    }

    [Fact]
    public void PostConfigure_ReliableMessaging_ShouldDefaultToTrue()
    {
        var options = new ServiceBusOptions();

        options.ReliableMessaging.ShouldBeTrue();
    }

    [Fact]
    public void PostConfigure_WithEmptyServiceName_AfterDerivation_ShouldThrow()
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ApplicationName.Returns("   ");
        var configurator = new ServiceBusOptions.Configurator(hostEnvironment);

        var options = new ServiceBusOptions
        {
            Domain = "MyDomain",
            PublicServiceName = string.Empty
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            configurator.PostConfigure(name: null, options));

        exception.Message.ShouldContain("PublicServiceName");
    }

    [Fact]
    public void PostConfigure_ShouldHandleMultiSegmentDomain()
    {
        var options = new ServiceBusOptions
        {
            Domain = "ECommerce",
            PublicServiceName = "catalog-service"
        };

        _configurator.PostConfigure(name: null, options);

        options.ServiceUrn.ToString().ShouldBe("/e_commerce/catalog-service");
    }
}
