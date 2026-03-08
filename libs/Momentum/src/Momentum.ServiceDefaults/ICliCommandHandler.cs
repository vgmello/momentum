// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;

namespace Momentum.ServiceDefaults;

/// <summary>
///     Handles CLI commands for messaging frameworks (e.g., Wolverine's codegen, db-apply).
/// </summary>
public interface ICliCommandHandler
{
    /// <summary>
    ///     Determines whether the given argument is a recognized CLI command.
    /// </summary>
    bool IsCommand(string arg);

    /// <summary>
    ///     Executes the CLI command.
    /// </summary>
    Task ExecuteAsync(WebApplication app, string[] args);
}
