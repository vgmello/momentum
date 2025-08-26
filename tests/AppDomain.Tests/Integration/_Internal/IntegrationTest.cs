// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Tests.Integration._Internal;

[Collection(nameof(IntegrationTest))]
[Trait("Type", "Integration")]
public class IntegrationTest
{
    protected IntegrationTestFixture Fixture { get; }

    protected IntegrationTest(IntegrationTestFixture fixture)
    {
        fixture.TestOutput = TestContext.Current.TestOutputHelper;
        Fixture = fixture;
    }
}

[CollectionDefinition(nameof(IntegrationTest))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
