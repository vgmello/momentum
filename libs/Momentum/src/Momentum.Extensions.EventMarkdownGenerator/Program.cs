// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("events-docsgen");
    
    config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generate markdown documentation from event assemblies")
        .IsDefault();
    
    config.AddCommand<CopyTemplatesCommand>("copy-templates")
        .WithDescription("Copy default templates to a local directory for customization");
});

await app.RunAsync(args);
