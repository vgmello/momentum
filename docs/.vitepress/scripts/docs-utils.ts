import path from 'node:path';
import { glob } from 'glob';

export const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

export async function getFirstPartyDlls(): Promise<string[]> {
    // Discover all csproj files under ../src to get first-party project names.
    // The script runs from the docs/ directory, so ../src always points to the repo src/.
    const csprojFiles = await glob('../src/**/*.csproj', { absolute: false });
    const projectNames = csprojFiles.map(f => path.basename(f, '.csproj'));

    if (projectNames.length === 0) {
        log('No .csproj files found under ../src');
        return [];
    }

    log(`Discovered ${projectNames.length} projects: ${projectNames.join(', ')}`);

    // For each project, find the matching DLL under its bin directory.
    // Matching by exact project name ensures only first-party assemblies are picked up.
    const allFiles: string[] = [];
    for (const name of projectNames) {
        const matches = await glob(`../src/**/bin/**/${name}.dll`, { absolute: true });
        allFiles.push(...matches);
    }

    // Deduplicate by assembly name - keeps one DLL per project.
    // Multiple matches arise from Debug/Release configs or different framework versions;
    // any copy is equally valid for API doc extraction, so first match wins.
    const seen = new Map<string, string>();
    for (const assemblyPath of allFiles) {
        const name = path.basename(assemblyPath);
        if (!seen.has(name)) seen.set(name, assemblyPath);
    }

    return Array.from(seen.values());
}
