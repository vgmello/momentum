// Copyright (c) OrgName. All rights reserved.

using Microsoft.AspNetCore.TestHost;
using Wolverine;

namespace AppDomain.Tests.Unit.Common;

public abstract class EndpointTest : IAsyncLifetime
{
    protected static readonly WebApplicationOptions TestOptions = new() { EnvironmentName = "Test" };
    protected static readonly Guid FakeTenantId = Guid.Parse("12345678-0000-0000-0000-000000000000");

    private Action<WebApplication>? _configureApp;

    protected static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    protected IMessageBus MockBus { get; }
    protected WebApplication App { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    protected EndpointTest()
    {
        MockBus = Substitute.For<IMessageBus>();
    }

    protected virtual WebApplicationBuilder CreateAppBuilder()
    {
        var builder = WebApplication.CreateEmptyBuilder(TestOptions);
        builder.WebHost.UseTestServer();
        builder.Services.AddRoutingCore();
        builder.Services.AddSingleton(MockBus);
        return builder;
    }

    protected void ConfigureApp(Action<WebApplication> configure)
    {
        _configureApp = configure;
    }

    public async ValueTask InitializeAsync()
    {
        var builder = CreateAppBuilder();
        App = builder.Build();
        _configureApp?.Invoke(App);
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        Client.Dispose();
        await App.DisposeAsync();
    }
}
