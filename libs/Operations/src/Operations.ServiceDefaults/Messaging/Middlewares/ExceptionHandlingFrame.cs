// Copyright (c) ABCDEG. All rights reserved.

using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using Wolverine;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Wolverine code generation frame that wraps message handlers in try-catch blocks for exception handling.
/// </summary>
/// <remarks>
///     This frame generates code that:
///     <list type="bullet">
///         <item>Wraps the handler execution in a try-catch block</item>
///         <item>Captures any exceptions in the envelope's Failure property</item>
///         <item>Re-throws the exception for Wolverine's error handling pipeline</item>
///     </list>
///     This ensures that exceptions are properly tracked in the message envelope for
///     logging and metrics purposes while still allowing Wolverine to handle retries and DLQ.
/// </remarks>
public class ExceptionHandlingFrame : SyncFrame
{
    private Variable? _envelope;

    /// <summary>
    ///     Generates the try-catch code wrapping for exception handling.
    /// </summary>
    /// <param name="method">The generated method being built.</param>
    /// <param name="writer">The source code writer.</param>
    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write("BLOCK:try");
        Next?.GenerateCode(method, writer);
        writer.FinishBlock();

        writer.Write("BLOCK:catch (System.Exception ex)");

        writer.Write($"{_envelope?.Usage}.Failure = ex;");

        writer.Write("throw;");

        writer.FinishBlock();
    }

    /// <summary>
    ///     Identifies required variables for code generation.
    /// </summary>
    /// <param name="chain">The method variables available in the handler chain.</param>
    /// <returns>The envelope variable needed for exception tracking.</returns>
    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        _envelope = chain.FindVariable(typeof(Envelope));

        yield return _envelope;
    }
}
