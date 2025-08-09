// Copyright (c) ABCDEG. All rights reserved.

using System.Reflection;
using NetArchTest.Rules;

namespace AppDomain.Tests.Architecture;

/// <summary>
/// Base class for architecture tests providing common functionality.
/// </summary>
[PublicAPI]
public abstract class ArchitectureTestBase
{
    /// <summary>
    /// Gets the AppDomain domain assembly.
    /// </summary>
    protected static Assembly DomainAssembly => typeof(AppDomain.IAppDomainAssembly).Assembly;

    /// <summary>
    /// Gets the AppDomain API assembly.
    /// </summary>
    protected static Assembly ApiAssembly => typeof(Api.Program).Assembly;

    /// <summary>
    /// Gets the AppDomain Contracts assembly.
    /// </summary>
    protected static Assembly ContractsAssembly => typeof(Contracts.GlobalUsings).Assembly;

    /// <summary>
    /// Gets all AppDomain assemblies.
    /// </summary>
    protected static Assembly[] AllAppDomainAssemblies => new[]
    {
        DomainAssembly,
        ApiAssembly,
        ContractsAssembly
    };

    /// <summary>
    /// Creates a types predicate for the domain assembly.
    /// </summary>
    /// <returns>A types predicate for architecture testing</returns>
    protected static Types DomainTypes() => Types.InAssembly(DomainAssembly);

    /// <summary>
    /// Creates a types predicate for the API assembly.
    /// </summary>
    /// <returns>A types predicate for architecture testing</returns>
    protected static Types ApiTypes() => Types.InAssembly(ApiAssembly);

    /// <summary>
    /// Creates a types predicate for the contracts assembly.
    /// </summary>
    /// <returns>A types predicate for architecture testing</returns>
    protected static Types ContractsTypes() => Types.InAssembly(ContractsAssembly);

    /// <summary>
    /// Creates a types predicate for all AppDomain assemblies.
    /// </summary>
    /// <returns>A types predicate for architecture testing</returns>
    protected static Types AllAppDomainTypes() => Types.InAssemblies(AllAppDomainAssemblies);
}