// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.EventMarkdownGenerator;
using Spectre.Console.Cli;

var app = new CommandApp<GenerateCommand>();
app.Configure(config => config.SetApplicationName("events-docsgen"));

await app.RunAsync(args);
