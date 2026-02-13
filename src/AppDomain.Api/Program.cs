// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_ORLEANS)
using AppDomain.Api.Infrastructure.Extensions;
//#endif
//#if (INCLUDE_SAMPLE)
using AppDomain.Api.Cashiers;
using AppDomain.Api.Invoices;
//#endif
using AppDomain.Infrastructure;
//#if (USE_KAFKA)
using Momentum.Extensions.Messaging.Kafka;
//#endif
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
//#if (INCLUDE_BFF)
using Momentum.ServiceDefaults.Api.FrontendIntegration;
//#endif
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults(requireAuth: false);
//#if (INCLUDE_BFF)
builder.AddFrontendIntegration();
//#endif

// Configure OpenAPI with .NET 10 native support
// NOTE: AddOpenApi must be called directly in the API project (not in a library)
// for the .NET 10 source generator to intercept it and generate XML documentation
// comment transformers. This enables automatic inclusion of XML docs in the OpenAPI document.
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults());

//#if (USE_KAFKA)
builder.AddKafkaMessagingExtensions();
//#endif
//#if (INCLUDE_ORLEANS)
builder.AddOrleansClient();
//#endif

builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.ConfigureApiUsingDefaults();
//#if (INCLUDE_BFF)
app.UseFrontendIntegration();
//#endif
//#if (INCLUDE_SAMPLE)
app.MapCashierEndpoints();
app.MapInvoiceEndpoints();
//#endif
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
