// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.E2E.OpenApi.Generated;

namespace AppDomain.Tests.E2E;

/// <summary>
///     Base class for E2E tests that provides API client and configuration
/// </summary>
[Collection(nameof(End2EndTest))]
[Trait("Type", "E2E")]
public abstract class End2EndTest(End2EndTestFixture fixture)
{
    protected TestSettings TestSettings => fixture.TestSettings;

    protected HttpClient HttpClient => fixture.HttpClient;

    protected AppDomainApiClient ApiClient => fixture.ApiClient;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}

[CollectionDefinition(nameof(End2EndTest))]
public class End2EndTestCollection : ICollectionFixture<End2EndTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
