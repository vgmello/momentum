// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Represents a command that can be sent through the message bus and produces a result.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the command.</typeparam>
/// <remarks>
///     This is a marker interface used by the messaging infrastructure to identify command messages.
///     Commands typically represent actions that change the state of the system and return a result.
/// </remarks>
public interface ICommand<out TResult>
{
    /// <summary>
    ///     Gets an empty result for this command type.
    /// </summary>
    /// <value>
    ///     The default value for the result type, typically <c>null</c> for reference types
    ///     or the default value for value types.
    /// </value>
    TResult? Empty => default;
}
