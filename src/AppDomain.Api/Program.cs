// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_ORLEANS)
using AppDomain.Api.Infrastructure.Extensions;
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

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddApiServiceDefaults(requireAuth: false);
builder.AddServiceBus(bus => bus.UseWolverine());
//#if (INCLUDE_BFF)
builder.AddFrontendIntegration();
//#endif

// Configure OpenAPI with .NET 10 native support
// NOTE: AddOpenApi must be called directly in the API project (not in a library)
// for the .NET 10 source generator to intercept it and generate XML documentation
// comment transformers. This enables automatic inclusion of XML docs in the OpenAPI document.
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults(builder.Configuration));

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

await app.RunAsync(args);
