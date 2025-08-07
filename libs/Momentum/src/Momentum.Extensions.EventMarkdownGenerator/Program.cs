// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator;
using Spectre.Console.Cli;

var app = new CommandApp<GenerateCommand>();
app.Configure(config => config.SetApplicationName("events-docsgen"));

await app.RunAsync(args);
