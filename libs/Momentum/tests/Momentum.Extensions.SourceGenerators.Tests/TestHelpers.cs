// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.CodeAnalysis.Diagnostics;
using Momentum.Extensions.Abstractions.Dapper;
using System.ComponentModel.DataAnnotations.Schema;

namespace Momentum.Extensions.SourceGenerators.Tests;

internal static class TestHelpers
{
    private static readonly TestAnalyzerConfigOptionsProvider EmptyOptions = new(new TestAnalyzerConfigOptions([]));

    public static (string[] Output, Diagnostic[] Diagnostics) GetGeneratedSources<T>(
        string source, Dictionary<string, string?>? options = null)
        where T : IIncrementalGenerator, new()
    {
        var driver = CreateAndRunGenerator<T>(source, options);
        var generationResults = driver.GetRunResult();

        var output = generationResults.GeneratedTrees
            .Select(tree => tree.ToString())
            .ToArray();

        return (output, generationResults.Diagnostics.ToArray());
    }

    public static GeneratorDriver CreateAndRunGenerator<T>(string source, Dictionary<string, string?>? options = null)
        where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation<T>(source);

        return RunGenerator<T>(compilation, options);
    }

    private static CSharpCompilation CreateCompilation<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbCommandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ColumnAttribute).Assembly.Location)
            ]);

        return CSharpCompilation.Create(
            "Platform.TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriver RunGenerator<T>(CSharpCompilation compilation, Dictionary<string, string?>? options)
        where T : IIncrementalGenerator, new()
    {
        var generator = new T().AsSourceGenerator();

        var opts = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);

        var optProvider = options is not null
            ? new TestAnalyzerConfigOptionsProvider(new TestAnalyzerConfigOptions(options))
            : EmptyOptions;

        driver = driver
            .WithUpdatedAnalyzerConfigOptions(optProvider)
            .RunGenerators(compilation);

        return driver;
    }
}

internal class TestAnalyzerConfigOptionsProvider(TestAnalyzerConfigOptions globalOptions) : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions => globalOptions;
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => globalOptions;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => globalOptions;
}

internal class TestAnalyzerConfigOptions(Dictionary<string, string?> options) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, out string value)
    {
        if (options.TryGetValue(key, out var result))
        {
            value = result ?? string.Empty;

            return true;
        }

        value = string.Empty;

        return false;
    }
}
