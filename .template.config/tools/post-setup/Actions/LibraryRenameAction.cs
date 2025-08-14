using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PostSetup.Actions;

public class LibraryRenameAction
{
    public class LibraryRenameResult
    {
        public bool Enabled { get; set; }
        public string LibPrefix { get; set; } = string.Empty;
        public List<string> ImportedTokens { get; set; } = new();
        public int ProcessedFiles { get; set; }
        public int ChangedFiles { get; set; }
        public string SkipReason { get; set; } = string.Empty;
    }

    public LibraryRenameResult ProcessMomentumLibImport(string projectDir, JsonElement config)
    {
        var result = new LibraryRenameResult();

        if (!config.TryGetProperty("momentumLibImport", out var importConfig))
        {
            result.SkipReason = "No momentumLibImport configuration found";
            return result;
        }

        if (!importConfig.TryGetProperty("enabled", out var enabledElement) ||
            !enabledElement.GetBoolean())
        {
            result.SkipReason = "Momentum library import is disabled";
            return result;
        }

        result.Enabled = true;

        if (!importConfig.TryGetProperty("libName", out var libNameElement))
        {
            result.SkipReason = "libName not specified in momentumLibImport config";
            return result;
        }

        var libPrefix = libNameElement.GetString() ?? "";
        if (string.IsNullOrEmpty(libPrefix) || libPrefix == "Momentum")
        {
            result.SkipReason = "Library prefix is empty or unchanged (Momentum)";
            return result;
        }

        result.LibPrefix = libPrefix;

        Console.WriteLine($"Processing Momentum library imports with prefix: {libPrefix}");

        var importedTokens = BuildImportedTokensWhitelist(projectDir, libPrefix);
        result.ImportedTokens = importedTokens;

        if (importedTokens.Count == 0)
        {
            result.SkipReason = "No imported Momentum libraries found";
            return result;
        }

        Console.WriteLine($"  → Found {importedTokens.Count} imported library tokens: {string.Join(", ", importedTokens)}");

        var processedFiles = 0;
        var changedFiles = 0;
        var regex = BuildMomentumLibraryRegex(importedTokens);

        var scanRoots = GetStringArrayFromConfig(importConfig, "scanRoots", new[] { "src", "infra", "libs", "tests" });
        var fileExtensions = GetStringArrayFromConfig(importConfig, "fileExtensions", new[] { ".cs", ".csproj" });
        var excludeDirs = GetStringArrayFromConfig(importConfig, "excludeDirs", new[] { ".git", "bin", "obj" });
        var maxBytes = GetIntFromConfig(importConfig, "maxBytes", 2097152);

        foreach (var root in scanRoots)
        {
            var rootPath = Path.Combine(projectDir, root);
            if (!Directory.Exists(rootPath)) continue;

            var files = FindFilesToProcess(rootPath, fileExtensions, excludeDirs, maxBytes);
            foreach (var filePath in files)
            {
                processedFiles++;
                if (ProcessFileContent(filePath, regex, libPrefix, projectDir))
                {
                    changedFiles++;
                }
            }
        }

        result.ProcessedFiles = processedFiles;
        result.ChangedFiles = changedFiles;

        return result;
    }

    private List<string> BuildImportedTokensWhitelist(string projectDir, string libPrefix)
    {
        var tokens = new List<string>();
        var libsPath = Path.Combine(projectDir, "libs");

        if (!Directory.Exists(libsPath))
            return tokens;

        var prefixedLibsPath = Path.Combine(libsPath, libPrefix);
        var searchPath = Directory.Exists(prefixedLibsPath) ? prefixedLibsPath : libsPath;

        var directories = Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories)
            .Where(d => Path.GetFileName(d).StartsWith($"{libPrefix}."))
            .ToList();

        var projectFiles = Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith($"{libPrefix}."))
            .ToList();

        var allItems = directories.Concat(projectFiles).Select(Path.GetFileNameWithoutExtension).Distinct();

        foreach (var item in allItems)
        {
            if (item.StartsWith($"{libPrefix}.") && item.Length > libPrefix.Length + 1)
            {
                var token = item.Substring(libPrefix.Length + 1);

                // Exclude Extensions.Abstractions - it should never be renamed
                if (token == "Extensions.Abstractions" || token.StartsWith("Extensions.Abstractions."))
                {
                    continue;
                }

                if (!tokens.Contains(token))
                {
                    tokens.Add(token);
                }
            }
        }

        return tokens.OrderByDescending(t => t.Length).ToList(); // Longer tokens first for proper matching
    }

    private Regex BuildMomentumLibraryRegex(List<string> tokens)
    {
        var escapedTokens = tokens.Select(t => Regex.Escape(t));
        var alternation = string.Join("|", escapedTokens);
        // Use negative lookahead to exclude .Abstractions
        // This ensures we don't match Momentum.Extensions.Abstractions when we want to rename Momentum.Extensions
        var pattern = $@"\bMomentum\.(?<token>{alternation})(?!\.Abstractions)";
        return new Regex(pattern, RegexOptions.Compiled);
    }

    private List<string> FindFilesToProcess(string rootPath, string[] extensions, string[] excludeDirs, int maxBytes)
    {
        var files = new List<string>();

        foreach (var ext in extensions)
        {
            try
            {
                var pattern = "*" + ext;
                var foundFiles = Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories)
                    .Where(f => !IsExcludedFile(f, excludeDirs) && new FileInfo(f).Length <= maxBytes)
                    .ToArray();
                files.AddRange(foundFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Warning: Error searching for {ext} files: {ex.Message}");
            }
        }

        return files;
    }

    private bool IsExcludedFile(string filePath, string[] excludeDirs)
    {
        return excludeDirs.Any(dir => filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}") ||
                                     filePath.Contains($"{Path.DirectorySeparatorChar}{dir}"));
    }

    private bool ProcessFileContent(string filePath, Regex regex, string libPrefix, string projectDir)
    {
        try
        {
            string originalContent;
            Encoding encoding = Encoding.UTF8;

            // Detect BOM and preserve it
            var bytes = File.ReadAllBytes(filePath);
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(true); // UTF-8 with BOM
            }

            originalContent = encoding.GetString(bytes);
            var modifiedContent = regex.Replace(originalContent, match =>
            {
                var token = match.Groups["token"].Value;
                return $"{libPrefix}.{token}";
            });

            // Also handle path renaming in solution files (libs\Momentum\ -> libs\libPrefix\)
            if (Path.GetExtension(filePath).Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(filePath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                modifiedContent = modifiedContent.Replace($"libs\\Momentum\\", $"libs\\{libPrefix}\\");
                modifiedContent = modifiedContent.Replace($"libs/Momentum/", $"libs/{libPrefix}/");
            }

            if (modifiedContent != originalContent)
            {
                // Write atomically: write to temp file, then replace
                var tempFile = filePath + ".tmp";
                File.WriteAllText(tempFile, modifiedContent, encoding);

                // Delete the original file, then move temp file to original location
                File.Delete(filePath);
                File.Move(tempFile, filePath);
                Console.WriteLine($"  → Updated library references in: {Path.GetRelativePath(projectDir, filePath)}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Could not process file {filePath}: {ex.Message}");
        }

        return false;
    }

    private string[] GetStringArrayFromConfig(JsonElement config, string propertyName, string[] defaultValue)
    {
        if (config.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
        {
            return arrayElement.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        }
        return defaultValue;
    }

    private int GetIntFromConfig(JsonElement config, string propertyName, int defaultValue)
    {
        if (config.TryGetProperty(propertyName, out var intElement) && intElement.TryGetInt32(out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
