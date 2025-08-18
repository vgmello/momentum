// Copyright (c) ORG_NAME. All rights reserved.

/// <summary>
/// Entry point for the Orleans stateful processing service.
/// This service provides actor-based stateful processing using Microsoft Orleans,
/// handling long-running business processes and maintaining distributed state.
/// </summary>

using AppDomain.BackOffice.Orleans;
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
using AppDomain.BackOffice.Orleans.Invoices.Grains;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
//#if (USE_KAFKA)
builder.AddKafkaMessagingExtensions();
//#endif
builder.AddOrleans();

// Application Services
builder.AddApplicationServices();

var app = builder.Build();

app.MapOrleansDashboard();
app.MapDefaultHealthCheckEndpoints();

app.MapPost("/invoices/{id:guid}/pay", async (Guid id, decimal amount, IGrainFactory grains) =>
{
    var grain = grains.GetGrain<IInvoiceGrain>(id);
    await grain.MarkAsPaidAsync(amount, DateTime.UtcNow);

    return Results.Accepted();
});

app.MapGet("/invoices/{id:guid}", async (Guid id, IGrainFactory grains) =>
{
    var grain = grains.GetGrain<IInvoiceGrain>(id);

    return Results.Ok(await grain.GetInvoiceAsync());
});

await app.RunAsync(args);
