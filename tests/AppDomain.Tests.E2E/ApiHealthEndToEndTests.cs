// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Tests.E2E;

/// <summary>
/// End-to-end tests for API health and basic functionality
/// </summary>
public class ApiHealthEndToEndTests : ApiEndToEndTestsBase
{
    [Fact]
    public async Task ApiIsRunning_BasicEndpointsRespond()
    {
        // Act & Assert
        var cashiersResponse = await HttpClient.GetAsync("/cashiers");
        cashiersResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var invoicesResponse = await HttpClient.GetAsync("/invoices");
        invoicesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CashiersEndpoint_ReturnsJsonContent()
    {
        // Act
        var response = await HttpClient.GetAsync("/cashiers");
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvoicesEndpoint_ReturnsJsonContent()
    {
        // Act
        var response = await HttpClient.GetAsync("/invoices");
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ApiEndpoints_HandleInvalidRoutes()
    {
        // Act
        var response = await HttpClient.GetAsync("/nonexistent");
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApiEndpoints_HandleInvalidHttpMethods()
    {
        // Act
        var response = await HttpClient.PostAsync("/cashiers/invalid", null);
        
        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}