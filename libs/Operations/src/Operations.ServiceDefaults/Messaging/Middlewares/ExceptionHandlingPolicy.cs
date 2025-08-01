// Copyright (c) ABCDEG. All rights reserved.

using JasperFx.CodeGeneration;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Wolverine policy that ensures exception handling middleware is applied to all message handlers.
/// </summary>
/// <remarks>
///     This policy automatically adds the ExceptionHandlingFrame to all handler chains that don't
///     already have it, ensuring consistent exception handling across the messaging system.
///     The policy is registered during Wolverine configuration and applies to all discovered handlers.
/// </remarks>
public class ExceptionHandlingPolicy : IHandlerPolicy
{
    /// <summary>
    ///     Applies the exception handling policy to handler chains.
    /// </summary>
    /// <param name="chains">The collection of handler chains to process.</param>
    /// <param name="rules">The code generation rules.</param>
    /// <param name="container">The service container.</param>
    /// <remarks>
    ///     This method iterates through all handler chains and adds an ExceptionHandlingFrame
    ///     to those that don't already have one, ensuring comprehensive exception handling.
    /// </remarks>
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var middleware in chains.Select(c => c.Middleware))
        {
            if (middleware.OfType<ExceptionHandlingFrame>().Any())
                continue;

            middleware.Add(new ExceptionHandlingFrame());
        }
    }
}
