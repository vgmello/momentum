// Copyright (c) ABCDEG. All rights reserved.

using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using Operations.Extensions;
using Operations.Extensions.Abstractions.Messaging;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

internal class FluentValidationResultFrame(Type messageType, MethodCall validationMethodCall) : SyncFrame
{
    private Variable? _context;

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        var handlerResultResponseType = GetQueryOrCommandHandlerResponseType(messageType);

        if (handlerResultResponseType is not null)
        {
            var validationResultVariable = validationMethodCall.Creates.First();

            writer.WriteComment("Evaluate whether the execution should stop based on the ValidationFailure list");
            writer.Write($"BLOCK:if ({validationResultVariable.Usage}.Count is not 0)");

            // Extract Result<T> from the handler response type and cast the validation result to it
            var resultResponseType = handlerResultResponseType.GetGenericArguments()[0];
            var result = new CastVariable(validationResultVariable, resultResponseType);

            // Output the validation result as a cascading message
            var cascadingMessage = new CaptureCascadingMessages(result) { Target = _context! };
            cascadingMessage.GenerateCode(method, writer);

            writer.Write("return;");
            writer.FinishBlock();
        }

        Next?.GenerateCode(method, writer);
    }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        _context = chain.FindVariable(typeof(MessageContext));

        yield return _context;

        foreach (var variable in base.FindVariables(chain))
        {
            yield return variable;
        }
    }

    /// <summary>
    ///     Look for handlers that returns an ICommand Or IQuery of ResultOfT
    /// </summary>
    private static Type? GetQueryOrCommandHandlerResponseType(Type messageType)
    {
        static bool IsCommandOrQuery(Type i) => i.IsGenericType &&
                                                (i.GetGenericTypeDefinition() == typeof(ICommand<>) ||
                                                 i.GetGenericTypeDefinition() == typeof(IQuery<>));

        static bool HasResultTypeResponse(Type i) => i.GetGenericArguments()[0].IsGenericType &&
                                                     i.GetGenericArguments()[0].GetGenericTypeDefinition() ==
                                                     typeof(Result<>);

        return messageType.GetInterfaces().FirstOrDefault(i => IsCommandOrQuery(i) && HasResultTypeResponse(i));
    }
}
