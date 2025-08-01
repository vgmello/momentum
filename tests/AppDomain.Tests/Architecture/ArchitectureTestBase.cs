// Copyright (c) ABCDEG. All rights reserved.

using NetArchTest.Rules;

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

/// <summary>
///     Base class for all architecture tests providing common functionality.
/// </summary>
public abstract class ArchitectureTestBase
{
    /// <summary>
    ///     Gets all types from the AppDomain assemblies for architecture testing.
    /// </summary>
    protected static Types GetAppDomainTypes() => Types
        .InAssemblies([typeof(IAppDomainAssembly).Assembly, typeof(Api.DependencyInjection).Assembly]);
}
