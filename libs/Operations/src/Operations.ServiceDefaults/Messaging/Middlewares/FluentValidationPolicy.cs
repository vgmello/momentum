// Copyright (c) ABCDEG. All rights reserved.

using FluentValidation;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

public class FluentValidationPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var chain in chains)
            Apply(chain, container);
    }

    private static void Apply(HandlerChain chain, IServiceContainer container)
    {
        var validatorInterface = typeof(IValidator<>).MakeGenericType(chain.MessageType);
        var registeredValidators = container.RegistrationsFor(validatorInterface);

        var methodName = registeredValidators.Count == 1
            ? nameof(FluentValidationExecutor.ExecuteOne)
            : nameof(FluentValidationExecutor.ExecuteMany);

        var validationMethodInfo = typeof(FluentValidationExecutor)
            .GetMethod(methodName)!
            .MakeGenericMethod(chain.MessageType);

        var validationMethodCall = new MethodCall(typeof(FluentValidationExecutor), validationMethodInfo);

        chain.Middleware.Add(validationMethodCall);
        chain.Middleware.Add(new FluentValidationResultFrame(chain.MessageType, validationMethodCall));
    }
}
