using System;
using System.IO;

namespace PostSetup;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Running Momentum .NET post-setup tasks...");
            
            // Get the target project directory (where template was instantiated)
            var projectDir = Environment.CurrentDirectory;
            Console.WriteLine($"Project directory: {projectDir}");
            
            // Post-setup tasks
            SetupGitIgnore(projectDir);
            SetupScriptPermissions(projectDir);
            SetupDevEnvironment(projectDir);
            
            Console.WriteLine("Momentum .NET post-setup completed successfully!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Review and customize the generated configuration files");
            Console.WriteLine("2. Update connection strings in appsettings.json files");
            Console.WriteLine("3. Run 'dotnet build' to verify the solution builds correctly");
            Console.WriteLine("4. For Aspire projects, run 'dotnet run --project src/{YourProjectName}.AppHost'");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Post-setup failed: {ex.Message}");
            return 1;
        }
    }
    
    private static void SetupGitIgnore(string projectDir)
    {
        var gitIgnorePath = Path.Combine(projectDir, ".gitignore");
        if (!File.Exists(gitIgnorePath))
        {
            Console.WriteLine("Creating .gitignore...");
            File.WriteAllText(gitIgnorePath, """
                # Build results
                [Dd]ebug/
                [Dd]ebugPublic/
                [Rr]elease/
                [Rr]eleases/
                x64/
                x86/
                [Aa][Rr][Mm]/
                [Aa][Rr][Mm]64/
                bld/
                [Bb]in/
                [Oo]bj/
                [Ll]og/
                
                # Visual Studio
                .vs/
                *.user
                *.suo
                *.userosscache
                *.sln.docstates
                
                # VS Code
                .vscode/
                
                # JetBrains
                .idea/
                *.sln.iml
                
                # Environment variables
                .env
                .env.local
                .env.development.local
                .env.test.local
                .env.production.local
                
                # Docker
                .docker/
                
                # Temporary files
                *.tmp
                *.temp
                
                # Package files
                *.nupkg
                *.snupkg
                
                # ASP.NET Scaffolding
                ScaffoldingReadMe.txt
                
                # Logs
                logs/
                *.log
                npm-debug.log*
                yarn-debug.log*
                yarn-error.log*
                
                # Coverage directory used by tools like istanbul
                coverage/
                
                # Test results
                TestResults/
                [Tt]est[Rr]esult*/
                *.trx
                *.coverage
                *.coveragexml
                
                # Database
                *.db
                *.sqlite
                *.sqlite3
                
                # OS generated files
                .DS_Store
                .DS_Store?
                ._*
                .Spotlight-V100
                .Trashes
                ehthumbs.db
                Thumbs.db
                
                """);
        }
    }
    
    private static void SetupScriptPermissions(string projectDir)
    {
        if (!OperatingSystem.IsWindows())
        {
            var scriptsDir = Path.Combine(projectDir, "scripts");
            if (Directory.Exists(scriptsDir))
            {
                Console.WriteLine("Setting executable permissions on scripts...");
                var shellFiles = Directory.GetFiles(scriptsDir, "*.sh");
                foreach (var file in shellFiles)
                {
                    try
                    {
                        var chmod = System.Diagnostics.Process.Start("chmod", $"+x \"{file}\"");
                        chmod?.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not set permissions on {file}: {ex.Message}");
                    }
                }
            }
        }
    }
    
    private static void SetupDevEnvironment(string projectDir)
    {
        // Create .env.example for environment variable documentation
        var envExamplePath = Path.Combine(projectDir, ".env.example");
        if (!File.Exists(envExamplePath))
        {
            Console.WriteLine("Creating .env.example...");
            File.WriteAllText(envExamplePath, """
                # Database Configuration
                # ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=app_domain;Username=postgres;Password=postgres
                
                # Kafka Configuration  
                # Kafka__BootstrapServers=localhost:9092
                
                # Service Configuration
                # ASPNETCORE_ENVIRONMENT=Development
                # ASPNETCORE_URLS=https://localhost:7100;http://localhost:5100
                
                # Observability
                # OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
                
                """);
        }
        
        // Ensure directories exist for development
        var directories = new[]
        {
            "logs",
            "scripts"
        };
        
        foreach (var dir in directories)
        {
            var dirPath = Path.Combine(projectDir, dir);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                Console.WriteLine($"Created directory: {dir}/");
            }
        }
    }
}