// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Specifies the default domain for distributed events in an assembly.
/// </summary>
/// <remarks>
///     Apply this attribute at the assembly level to define a default domain name
///     that will be used for all distributed events in the assembly. This domain
///     is typically used as a prefix or namespace for event topics in message brokers.
///     Individual events can override this default by specifying their own domain.
/// </remarks>
/// <example>
///     <code>
/// [assembly: DefaultDomain(Domain = "AppDomain")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public class DefaultDomainAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the default domain name for distributed events.
    /// </summary>
    /// <value>The domain name to use as a default for all distributed events in the assembly.</value>
    public string? Domain { get; set; }
}
