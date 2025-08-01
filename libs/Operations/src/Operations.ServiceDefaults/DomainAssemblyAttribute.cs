// Copyright (c) ABCDEG. All rights reserved.

using System.Collections.Immutable;
using System.Reflection;

namespace Operations.ServiceDefaults;

/// <summary>
///     Marks an assembly to identify domain assemblies that should be scanned for various components.
/// </summary>
/// <param name="typeMarkers">
///     One or more types from the domain assemblies to be registered.
///     The assemblies containing these types will be included in domain assembly discovery.
/// </param>
/// <remarks>
///     Apply this attribute at the assembly level to register domain assemblies for:
///     <list type="bullet">
///         <item>FluentValidation validator discovery</item>
///         <item>Command and query handler discovery</item>
///         <item>Integration event discovery</item>
///     </list>
///     Multiple instances of this attribute can be applied to register multiple domain assemblies.
/// </remarks>
/// <example>
///     <code>
/// [assembly: DomainAssembly(typeof(User), typeof(Order))]
/// [assembly: DomainAssembly(typeof(Invoice))]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DomainAssemblyAttribute(params Type[] typeMarkers) : Attribute
{
    private Type[] DomainAssemblyTypeMarkers => typeMarkers;

    /// <summary>
    ///     Gets all domain assemblies registered via this attribute.
    /// </summary>
    /// <param name="applicationAssembly">
    ///     The assembly to scan for attributes. If null, uses the entry assembly.
    /// </param>
    /// <returns>
    ///     A read-only list of assemblies marked as domain assemblies.
    /// </returns>
    internal static IReadOnlyList<Assembly> GetDomainAssemblies(Assembly? applicationAssembly = null)
    {
        var targetAssembly = applicationAssembly ?? ServiceDefaultsExtensions.EntryAssembly;

        return targetAssembly.GetCustomAttribute<DomainAssemblyAttribute>()?
            .DomainAssemblyTypeMarkers
            .Select(t => t.Assembly)
            .ToImmutableList() ?? [];
    }
}
