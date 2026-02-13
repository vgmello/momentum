// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Momentum.Extensions.EventMarkdownGenerator;

public sealed class CopyTemplatesCommand : AsyncCommand<CopyTemplatesCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-o|--output")]
        [Description("Output directory for template files (defaults to ./templates)")]
        [DefaultValue("./templates")]
        public string Output { get; init; } = "./templates";

        [CommandOption("-f|--force")]
        [Description("Overwrite existing template files if they exist")]
        public bool Force { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var outputPath = Path.GetFullPath(settings.Output);

            if (Directory.Exists(outputPath) && !settings.Force)
            {
                var files = Directory.GetFiles(outputPath, "*.liquid");

                if (files.Length > 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] Template directory already contains .liquid files.");
                    AnsiConsole.MarkupLine("Use --force to overwrite existing files.");

                    return 1;
                }
            }

            await FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(outputPath, cancellationToken);

            AnsiConsole.MarkupLine($"[green]✓[/] Successfully copied default templates to: {outputPath}");
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("You can now customize these templates and use them with:");
            AnsiConsole.MarkupLine($"  [yellow]events-docsgen generate --templates {settings.Output} [other options][/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");

            return 1;
        }
    }
}
