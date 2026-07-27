// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Momentum.ServiceDefaults.Api.Tests;

public class EndpointMappingExtensionsTests
{
    private static readonly Func<Type, bool> OnlyTestDefinitions =
        type => type == typeof(AlphaEndpoints) || type == typeof(BravoEndpoints);

    [Fact]
    public void MapEndpoints_MapsAllMatchedDefinitions()
    {
        var app = WebApplication.CreateBuilder().Build();

        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, OnlyTestDefinitions);

        var patterns = RoutePatterns(app);
        patterns.ShouldContain(p => p.Contains("alpha"));
        patterns.ShouldContain(p => p.Contains("bravo"));
    }

    [Fact]
    public void MapEndpoints_WithPredicate_MapsOnlyMatchingDefinitions()
    {
        var app = WebApplication.CreateBuilder().Build();

        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, type => type == typeof(AlphaEndpoints));

        var patterns = RoutePatterns(app);
        patterns.ShouldContain(p => p.Contains("alpha"));
        patterns.ShouldNotContain(p => p.Contains("bravo"));
    }

    [Fact]
    public void MapEndpoints_CanBeCalledMultipleTimes_PartitioningDefinitions()
    {
        var app = WebApplication.CreateBuilder().Build();

        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, type => type == typeof(AlphaEndpoints));
        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, type => type == typeof(BravoEndpoints));

        var patterns = RoutePatterns(app);
        patterns.ShouldContain(p => p.Contains("alpha"));
        patterns.ShouldContain(p => p.Contains("bravo"));
    }

    [Fact]
    public void MapEndpoints_ConventionOnReturnedGroup_AppliesToAllMappedEndpoints()
    {
        var app = WebApplication.CreateBuilder().Build();

        var group = app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, OnlyTestDefinitions);
        group.WithMetadata(new TestMarker());

        var endpoints = Endpoints(app);
        endpoints.Count.ShouldBe(2);
        endpoints.ShouldAllBe(e => e.Metadata.GetMetadata<TestMarker>() != null);
    }

    [Fact]
    public void MapEndpoints_SeparateCalls_ApplyConventionsIndependently()
    {
        var app = WebApplication.CreateBuilder().Build();

        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, type => type == typeof(AlphaEndpoints))
            .WithMetadata(new TestMarker());
        app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, type => type == typeof(BravoEndpoints));

        var marked = Endpoints(app).Where(e => e.Metadata.GetMetadata<TestMarker>() != null).ToList();
        marked.ShouldHaveSingleItem();
        ((RouteEndpoint)marked[0]).RoutePattern.RawText.ShouldNotBeNull().ShouldContain("alpha");
    }

    [Fact]
    public void MapEndpoints_WhenPredicateMatchesNothing_ReturnsGroupAndMapsNoEndpoints()
    {
        var app = WebApplication.CreateBuilder().Build();

        var group = app.MapEndpoints(typeof(EndpointMappingExtensionsTests).Assembly, _ => false);

        group.ShouldNotBeNull();
        Endpoints(app).ShouldBeEmpty();
    }

    private static List<Endpoint> Endpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints).ToList();

    private static List<string> RoutePatterns(WebApplication app) =>
        Endpoints(app).OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty).ToList();

    private sealed class TestMarker;

    private sealed class AlphaEndpoints : IEndpointDefinition
    {
        public static RouteGroupBuilder MapEndpoints(IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("alpha");
            group.MapGet("/", () => Results.Ok("alpha"));

            return group;
        }
    }

    private sealed class BravoEndpoints : IEndpointDefinition
    {
        public static RouteGroupBuilder MapEndpoints(IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("bravo");
            group.MapGet("/", () => Results.Ok("bravo"));

            return group;
        }
    }
}
