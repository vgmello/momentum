import { execSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';
import { glob } from 'glob';

const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

function getGitHubUrl(): string | null {
    try {
        const remoteUrl = execSync('git remote get-url origin', {
            encoding: 'utf8',
            cwd: path.resolve('..')
        }).trim();

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

        return `${githubUrl}/blob/main/src`;

    } catch (error) {
        log(`Could not get GitHub URL from git remote: ${error}`);

        return null;
    }
}

async function getAssemblyFiles(patterns: string[]): Promise<string[]> {
    const allFiles: string[] = [];

    for (const pattern of patterns) {
        const matches = await glob(pattern, {
            absolute: true
        });
        allFiles.push(...matches);
    }

    // Remove duplicates by full path
    const uniquePaths = [...new Set(allFiles)];

    // Deduplicate by assembly name - keep only the first match for each assembly
    // This prevents duplicate events when multiple builds exist (Debug/Release, different .NET versions)
    const seenAssemblyNames = new Map<string, string>();
    for (const assemblyPath of uniquePaths) {
        const assemblyName = path.basename(assemblyPath);
        if (!seenAssemblyNames.has(assemblyName)) {
            seenAssemblyNames.set(assemblyName, assemblyPath);
        }
    }

    return Array.from(seenAssemblyNames.values());
}

try {
    const startTime = Date.now();
    const args = process.argv.slice(2);
    const patternsArg = args[0];

    if (!patternsArg) {
        log('Usage: tsx generate-events-docs.ts <glob-patterns>');
        log('Example: tsx generate-events-docs.ts "../src/**/bin/**/Reservations*.dll"');
        log('Multiple: tsx generate-events-docs.ts "pattern1.dll,pattern2.dll"');
        process.exit(1);
    }

    log('Generating events documentation...');

    // Parse comma-delimited glob patterns
    const patterns = patternsArg
        .split(',')
        .map(p => p.trim())
        .filter(p => p.length > 0);

    log('Scanning for assembly files...');
    const assemblyPaths = await getAssemblyFiles(patterns);

    log(`Found ${assemblyPaths.length} assemblies:`);

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
        // Check if events-docsgen tool is available
        try {
            execSync('events-docsgen --version', { stdio: 'pipe' });
        } catch (checkError) {
            log('❌ The events-docsgen tool is not installed.');
            log('');
            log('Please install it using:');
            log('  dotnet tool install -g Momentum.Extensions.EventMarkdownGenerator --prerelease');
            log('');
            log('After installation, make sure the dotnet tools directory is in your PATH.');
            process.exit(1);
        }

        const env = { ...process.env, 'SkipLocalFeedPush': 'true' };
        const githubUrl = getGitHubUrl();

        let command = `events-docsgen generate --assemblies "${assembliesArg}" --output "${outputDir}"`;
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
