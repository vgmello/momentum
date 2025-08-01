// Copyright (c) ABCDEG. All rights reserved.

using Wolverine;

namespace Operations.ServiceDefaults.Messaging.Wolverine;

/// <summary>
///     Provides extension methods for Wolverine messaging components.
/// </summary>
public static class WolverineExtensions
{
    /// <summary>
    ///     Gets the message name from a Wolverine envelope.
    /// </summary>
    /// <param name="envelope">The Wolverine envelope containing the message.</param>
    /// <param name="fullName">
    ///     If <c>true</c>, returns the full type name including namespace;
    ///     if <c>false</c>, returns only the type name. Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    ///     The message type name. If the message type cannot be determined,
    ///     returns the envelope's MessageType property or "UnknownMessage" as a fallback.
    /// </returns>
    /// <remarks>
    ///     This method is useful for logging and debugging purposes to identify
    ///     the type of message being processed without requiring direct access
    ///     to the message instance.
    /// </remarks>
    public static string GetMessageName(this Envelope envelope, bool fullName = false)
    {
        if (envelope.Message?.GetType() is { } messageType)
        {
            if (fullName)
                return messageType.FullName ?? messageType.Name;

            return messageType.Name;
        }

        return envelope.MessageType ?? "UnknownMessage";
    }
}
