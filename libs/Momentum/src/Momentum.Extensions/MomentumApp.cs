// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;

namespace Momentum.Extensions;

/// <summary>
///     Provides shared application-level properties for the Momentum platform.
/// </summary>
public static class MomentumApp
{
    private static readonly Lock EntryAssemblyLock = new();

    /// <summary>
    ///     Gets or sets the entry assembly for the application.
    /// </summary>
    public static Assembly EntryAssembly
    {
        get
        {
            var assembly = Volatile.Read(ref field);

            if (assembly is not null)
                return assembly;

            lock (EntryAssemblyLock)
            {
                assembly = field;

                if (assembly is not null)
                    return assembly;

                assembly = Assembly.GetEntryAssembly() ??
                    throw new InvalidOperationException(
                        "Unable to identify entry assembly. Please provide an assembly via the MomentumApp.EntryAssembly property.");

                Volatile.Write(ref field, assembly);

                return assembly;
            }
        }
        set => Volatile.Write(ref field, value);
    }
}
