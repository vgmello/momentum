import { execSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';

const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

function getGitHubUrl(): string | null {
    try {
        // Get the git remote URL
        const remoteUrl = execSync('git remote get-url origin', {
            encoding: 'utf8',
            cwd: path.resolve('..') // Run from the project root
        }).trim();

        // Convert SSH or HTTPS git URL to GitHub web URL
        let githubUrl: string;
        if (remoteUrl.startsWith('git@github.com:')) {
            // SSH format: git@github.com:user/repo.git
            const repoPath = remoteUrl.replace('git@github.com:', '').replace('.git', '');
            githubUrl = `https://github.com/${repoPath}`;
        } else if (remoteUrl.startsWith('https://github.com/')) {
            // HTTPS format: https://github.com/user/repo.git
            githubUrl = remoteUrl.replace('.git', '');
        } else {
            log(`Unsupported remote URL format: ${remoteUrl}`);
            return null;
        }

        // Add the blob/main/src path for source code links
        return `${githubUrl}/blob/main/src`;

    } catch (error) {
        log(`Could not get GitHub URL from git remote: ${error}`);
        return null;
    }
}

try {
    const startTime = Date.now();

    // Get assembly paths from command line arguments
    const args = process.argv.slice(2);
    const assemblyPathsArg = args[0];

    if (!assemblyPathsArg) {
        log('Usage: tsx generate-events-docs.ts <path-to-assemblies>');
        log('Example: tsx generate-events-docs.ts ../src/AppDomain/bin/Debug/net9.0/AppDomain.dll');
        log('Multiple: tsx generate-events-docs.ts "assembly1.dll,assembly2.dll"');
        process.exit(1);
    }

    // Look for the pre-built DLL in both local and Docker environments
    const possibleDllPaths = [
        '/generator/libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/bin/Debug/net9.0/Momentum.Extensions.EventMarkdownGenerator.dll',
        path.resolve('..', 'libs', 'Momentum', 'src', 'Momentum.Extensions.EventMarkdownGenerator', 'bin', 'Debug', 'net9.0', 'Momentum.Extensions.EventMarkdownGenerator.dll')
    ];

    const toolDllPath = possibleDllPaths.find(dllPath => fs.existsSync(dllPath));

    if (!toolDllPath) {
        log('Tool DLL not found. Tried paths:');
        possibleDllPaths.forEach(p => log(`  - ${p}`));
        log('Build the tool first.');
        process.exit(1);
    }

    log(`Using tool at: ${toolDllPath}`);
    log('Generating events documentation...');

    // Parse comma-delimited assembly paths
    const assemblyPaths = assemblyPathsArg
        .split(',')
        .map(p => p.trim())
        .filter(p => p.length > 0)
        .map(p => path.resolve(p));

    log(`Processing ${assemblyPaths.length} assemblies:`);

    // Check if assemblies exist
    const existingAssemblies = assemblyPaths.filter(assemblyPath => {
        const exists = fs.existsSync(assemblyPath);
        if (exists) {
            log(`  ✓ ${assemblyPath}`);
        } else {
            log(`  ✗ Assembly not found: ${assemblyPath}`);
        }
        return exists;
    });

    if (existingAssemblies.length === 0) {
        log('No assemblies found. Build the project first.');
        process.exit(0);
    }

    // Generate events documentation
    const outputDir = path.resolve('events');
    const assembliesArg = existingAssemblies.join(',');

    try {
        // Run the pre-built DLL directly
        const env = { ...process.env, 'SkipLocalFeedPush': 'true' };
        const githubUrl = getGitHubUrl();

        let command = `dotnet "${toolDllPath}" --assemblies "${assembliesArg}" --output "${outputDir}"`;
        if (githubUrl) {
            log(`Using GitHub URL: ${githubUrl}`);
            command += ` --github-url "${githubUrl}"`;
        } else {
            log('No GitHub URL found - links will use anchor references');
        }
        command += ' --verbose';

        execSync(command, { stdio: 'inherit', env });
    } catch (toolError) {
        log(`Tool execution failed: ${toolError}`);
        throw toolError;
    }

    log(`Events documentation generated in ${outputDir}`);
    log(`Completed in ${Date.now() - startTime}ms`);

} catch (error) {
    log(`Error: ${error}`);
    process.exit(1);
}
