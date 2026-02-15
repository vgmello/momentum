// Copyright (c) OrgName. All rights reserved.

using NetArchTest.Rules;

namespace AppDomain.Tests.Architecture;
/// <summary>
///     Base class for all architecture tests providing common functionality.
/// </summary>
[Trait("Type", "Architecture")]
public abstract class ArchitectureTestBase
{
    /// <summary>
    ///     Gets all types from the AppDomain assemblies for architecture testing.
    /// </summary>
    protected static Types GetAppDomainTypes() => Types
#if INCLUDE_API
        .InAssemblies([typeof(IAppDomainAssembly).Assembly, typeof(Api.DependencyInjection).Assembly]);
#else
        .InAssemblies([typeof(IAppDomainAssembly).Assembly]);
#endif
}
