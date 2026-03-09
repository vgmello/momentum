// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx;
using Microsoft.Extensions.Hosting;
using System.Collections.Frozen;

namespace Momentum.Extensions.Messaging.Wolverine;

internal class WolverineCliCommandHandler : ICliCommandHandler
{
    private static readonly FrozenSet<string> Commands = new[]
    {
        "check-env",
        "codegen",
        "db-apply",
        "db-assert",
        "db-dump",
        "db-patch",
        "describe",
        "help",
        "resources",
        "storage"
    }.ToFrozenSet();

    public bool IsCommand(string arg) => Commands.Contains(arg);

    public async Task ExecuteAsync(IHost host, string[] args) => await host.RunJasperFxCommands(args);
}
