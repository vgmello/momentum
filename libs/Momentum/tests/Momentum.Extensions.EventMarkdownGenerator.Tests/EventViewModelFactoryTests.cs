// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Covers <see cref="EventViewModelFactory"/>'s GitHub source-link generation specifically — the rest of
///     the view model is exercised via <see cref="FluidMarkdownGeneratorTests"/>.
/// </summary>
public class EventViewModelFactoryTests
{
    [EventTopic("test")]
    private sealed record MarkerEvent(Guid Id);

    private static EventMetadata CreateMetadata(string eventTypeName, string namespaceName, string assemblyName) =>
        new()
        {
            EventName = eventTypeName,
            EventNameKebab = eventTypeName,
            EventTypeName = eventTypeName,
            FullTypeName = $"{namespaceName}.{eventTypeName}",
            Namespace = namespaceName,
            AssemblyName = assemblyName,
            Topic = "tests",
            FullyQualifiedTopicName = "tests.v1",
            Domain = "Tests",
            Version = "v1",
            IsInternal = false,
            Entity = string.Empty,
            TopicAttribute = (Attribute)typeof(MarkerEvent).GetCustomAttributes(false).First(a => a.GetType().Name.Contains("EventTopic"))
        };

    private static EventDocumentation CreateDocumentation() => new() { Summary = "Test" };

    [Fact]
    public void CreateEventModel_WithAssemblyNameContainingDot_DoesNotSplitItAcrossFolders()
    {
        // Project folder "AppDomain.BackOffice" would otherwise get wrongly split into "AppDomain/BackOffice/..."
        // by a naive per-namespace-segment join. Also covers the no-SourceRootDirectory case: the link is
        // still emitted unverified (old, always-best-effort behavior), even though this guess happens to be
        // wrong in the real BusinessDayEnded case (see CreateEventModel_WithSourceRootAndGuessedFileMissing_FallsBackToAnchor).
        var metadata = CreateMetadata("BusinessDayEnded", "AppDomain.BackOffice.Messaging.AccountingInboxHandler",
            "AppDomain.BackOffice");
        var options = new GeneratorOptions
        {
            OutputDirectory = "unused",
            SidebarFileName = "unused.json",
            GitHubBaseUrl = "https://github.com/org/repo/blob/main/src"
        };

        var viewModel = EventViewModelFactory.CreateEventModel(metadata, CreateDocumentation(), options);

        viewModel.GithubUrl.ShouldBe(
            "https://github.com/org/repo/blob/main/src/AppDomain.BackOffice/Messaging/AccountingInboxHandler/BusinessDayEnded.cs");
    }

    [Fact]
    public void CreateEventModel_WithoutAssemblyName_FallsBackToSplittingFullNamespace()
    {
        // Hand-built EventMetadata (e.g. in tests) that never set AssemblyName must keep the old behavior.
        var metadata = CreateMetadata("CashierCreated", "AppDomain.Cashiers.Contracts.IntegrationEvents", assemblyName: "");
        var options = new GeneratorOptions
        {
            OutputDirectory = "unused",
            SidebarFileName = "unused.json",
            GitHubBaseUrl = "https://github.com/org/repo/blob/main/src"
        };

        var viewModel = EventViewModelFactory.CreateEventModel(metadata, CreateDocumentation(), options);

        viewModel.GithubUrl.ShouldBe(
            "https://github.com/org/repo/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierCreated.cs");
    }

    [Fact]
    public void CreateEventModel_WithoutGitHubBaseUrl_ReturnsAnchor()
    {
        var metadata = CreateMetadata("CashierCreated", "AppDomain.Cashiers.Contracts.IntegrationEvents", "AppDomain");

        var viewModel = EventViewModelFactory.CreateEventModel(metadata, CreateDocumentation());

        viewModel.GithubUrl.ShouldBe("#");
    }

    [Fact]
    public void CreateEventModel_WithSourceRootAndGuessedFileExists_ReturnsLink()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"momentum-sourceroot-{Guid.NewGuid()}");
        var projectDir = Path.Combine(tempRoot, "AppDomain", "Cashiers", "Contracts", "IntegrationEvents");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "CashierCreated.cs"), "// marker");

        try
        {
            var metadata = CreateMetadata("CashierCreated", "AppDomain.Cashiers.Contracts.IntegrationEvents", "AppDomain");
            var options = new GeneratorOptions
            {
                OutputDirectory = "unused",
                SidebarFileName = "unused.json",
                GitHubBaseUrl = "https://github.com/org/repo/blob/main/src",
                SourceRootDirectory = tempRoot
            };

            var viewModel = EventViewModelFactory.CreateEventModel(metadata, CreateDocumentation(), options);

            viewModel.GithubUrl.ShouldBe(
                "https://github.com/org/repo/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierCreated.cs");
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void CreateEventModel_WithSourceRootAndGuessedFileMissing_FallsBackToAnchor()
    {
        // Simulates BusinessDayEnded: declared in a file that doesn't match its own type name (colocated with
        // a handler), which reflection alone can never detect — the source-root existence check is what
        // catches it and suppresses the otherwise-confidently-wrong link.
        var tempRoot = Path.Combine(Path.GetTempPath(), $"momentum-sourceroot-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var metadata = CreateMetadata("BusinessDayEnded", "AppDomain.BackOffice.Messaging.AccountingInboxHandler",
                "AppDomain.BackOffice");
            var options = new GeneratorOptions
            {
                OutputDirectory = "unused",
                SidebarFileName = "unused.json",
                GitHubBaseUrl = "https://github.com/org/repo/blob/main/src",
                SourceRootDirectory = tempRoot
            };

            var viewModel = EventViewModelFactory.CreateEventModel(metadata, CreateDocumentation(), options);

            viewModel.GithubUrl.ShouldBe("#");
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}
