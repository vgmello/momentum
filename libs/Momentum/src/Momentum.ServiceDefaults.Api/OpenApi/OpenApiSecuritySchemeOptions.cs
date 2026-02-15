// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.Api.OpenApi;

/// <summary>
///     Configuration for a single OpenAPI security scheme, bound from
///     <c>OpenApi:SecuritySchemes:{SchemeName}</c>.
/// </summary>
public class OpenApiSecuritySchemeOptions
{
    /// <summary>The security scheme type (e.g., "Http", "ApiKey", "OAuth2", "OpenIdConnect").</summary>
    public string? Type { get; set; }

    /// <summary>The name of the HTTP authentication scheme (e.g., "bearer").</summary>
    public string Scheme { get; set; } = string.Empty;

    /// <summary>The bearer token format hint (e.g., "JWT").</summary>
    public string? BearerFormat { get; set; }

    /// <summary>A short description for the security scheme.</summary>
    public string? Description { get; set; }
}
