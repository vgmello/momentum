// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Major Code Smell", "S2139",
    Scope = "member",
    Target = "~M:Momentum.ServiceDefaults.ServiceDefaultsExtensions.RunAsync(Microsoft.AspNetCore.Builder.WebApplication,System.String[])~System.Threading.Tasks.Task",
    Justification = "Intentional: log fatal error before rethrowing to ensure capture before process termination")]
