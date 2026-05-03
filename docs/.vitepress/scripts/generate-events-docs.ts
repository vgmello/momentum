import { execFileSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';
import { glob } from 'glob';

const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

function getGitHubUrl(): string | null {
    try {
        const remoteUrl = execFileSync('git', ['remote', 'get-url', 'origin'], {
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

async function getFirstPartyDlls(): Promise<string[]> {
    // Discover all csproj files under ../src to get first-party project names
    const csprojFiles = await glob('../src/**/*.csproj', { absolute: false });
    const projectNames = csprojFiles.map(f => path.basename(f, '.csproj'));

    if (projectNames.length === 0) {
        log('No .csproj files found under ../src');
        return [];
    }

    log(`Discovered ${projectNames.length} projects: ${projectNames.join(', ')}`);

    // For each project, find the matching DLL under its bin directory
    const allFiles: string[] = [];
    for (const name of projectNames) {
        const matches = await glob(`../src/**/bin/**/${name}.dll`, { absolute: true });
        allFiles.push(...matches);
    }

    // Deduplicate by assembly name - keeps one DLL per project (handles Debug/Release configs)
    const seenAssemblyNames = new Map<string, string>();
    for (const assemblyPath of allFiles) {
        const assemblyName = path.basename(assemblyPath);
        if (!seenAssemblyNames.has(assemblyName)) {
            seenAssemblyNames.set(assemblyName, assemblyPath);
        }
    }

    return Array.from(seenAssemblyNames.values());
}

try {
    const startTime = Date.now();

    log('Generating events documentation...');
    log('Scanning for first-party assembly files under ../src...');

    const assemblyPaths = await getFirstPartyDlls();

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
            execFileSync('events-docsgen', ['--version'], { stdio: 'pipe' });
        } catch {
            // Tool is not installed - provide installation instructions
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

        const args = ['generate', '--assemblies', assembliesArg, '--output', outputDir];
        if (githubUrl) {
            log(`Using GitHub URL: ${githubUrl}`);
            args.push('--github-url', githubUrl);
        } else {
            log('No GitHub URL found - links will use anchor references');
        }
        args.push('--verbose');

        execFileSync('events-docsgen', args, { stdio: 'inherit', env });
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

