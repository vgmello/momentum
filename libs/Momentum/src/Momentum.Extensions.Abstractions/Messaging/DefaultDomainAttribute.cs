// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Specifies the default domain for distributed events in an assembly.
/// </summary>
/// <remarks>
///     Apply this attribute at the assembly level to define a default domain name
///     that will be used as a fallback for distributed events that do not specify
///     their own domain via <see cref="EventTopicAttribute"/>.
///
///     <para>
///         Topic name resolution follows this precedence:
///         <list type="number">
///             <item><see cref="EventTopicAttribute.Domain"/> on the event class (if non-empty)</item>
///             <item><see cref="Domain"/> from this assembly-level attribute</item>
///             <item>First segment of the assembly name (e.g., <c>AppDomain.Contracts</c> → <c>AppDomain</c>)</item>
///         </list>
///         Use <see cref="GetDomainName"/> to resolve the effective domain for an assembly
///         following this precedence chain.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// [assembly: DefaultDomain("AppDomain")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public class DefaultDomainAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultDomainAttribute"/> class
    ///     with the specified domain name.
    /// </summary>
    /// <param name="domain">The default domain name for distributed events.</param>
    public DefaultDomainAttribute(string domain = "")
    {
        Domain = domain;
    }

    /// <summary>
    ///     Gets or sets the default domain name for distributed events.
    /// </summary>
    /// <value>
    ///     The domain name used as a fallback when <see cref="EventTopicAttribute.Domain"/>
    ///     is not set on an individual event.
    /// </value>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    ///     Resolves the effective domain name for the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to resolve the domain for.</param>
    /// <returns>
    ///     The <see cref="Domain"/> value from the assembly's <see cref="DefaultDomainAttribute"/>
    ///     if present; otherwise, the first segment of the assembly name
    ///     (e.g., <c>AppDomain.Contracts</c> → <c>AppDomain</c>).
    /// </returns>
    public static string GetDomainName(Assembly assembly)
    {
        var attr = assembly.GetCustomAttribute<DefaultDomainAttribute>();

        if (!string.IsNullOrWhiteSpace(attr?.Domain))
            return attr.Domain;

        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var dotIndex = assemblyName.IndexOf('.');
        return dotIndex > 0 ? assemblyName[..dotIndex] : assemblyName;
    }
}
