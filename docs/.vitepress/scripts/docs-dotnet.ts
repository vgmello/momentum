import fs, { createReadStream } from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { glob } from 'glob';
import { execFileSync } from 'node:child_process';
import { pipeline } from 'node:stream/promises';

import docfxBaseConfig from '../../docfx.json' with { type: 'json' };

interface FileInfo {
    checksum: string;
    mtime: number;
    size: number;
}

interface State {
    version: string;
    lastCheck: string;
    files: Record<string, FileInfo>;
}

const STATE_FILE_NAME = 'reference/.state';
const GENERATED_CONFIG_NAME = '.docfx.generated.json';
const STATE_VERSION = '1.0';

const stateFilePath = path.join(process.cwd(), STATE_FILE_NAME);
const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

try {
    const startTime = Date.now();

    log('Scanning for first-party assembly files under ../src...');

    const dllPaths = await getFirstPartyDlls();

    log(`Found ${dllPaths.length} assemblies`);

    if (dllPaths.length === 0) {
        log('No assemblies found. Build the project first.');
        process.exit(0);
    }

    const previousState = loadState();
    const { hasChanges, currentFiles } = await detectChanges(dllPaths, previousState);

    if (hasChanges) {
        runDocfx(dllPaths);

        const newState: State = {
            version: STATE_VERSION,
            lastCheck: new Date().toISOString(),
            files: currentFiles
        };

        await saveState(newState);

        log('State saved');
    } else {
        log('No changes detected');
    }

    log(`Completed in ${Date.now() - startTime}ms`);

} catch (error) {
    log(`Error: ${error}`);
    process.exit(1);
}

async function getFirstPartyDlls(): Promise<string[]> {
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

async function detectChanges(files: string[], previousState: State | null)
    : Promise<{ hasChanges: boolean; currentFiles: Record<string, FileInfo> }> {
    const previousFiles = previousState?.files || {};
    const filesToCheck: string[] = [];
    const currentFiles: Record<string, FileInfo> = {};

    // First pass: check which files need rechecking
    for (const file of files) {
        if (needsRecheck(file, previousFiles[file])) {
            filesToCheck.push(file);
        } else {
            currentFiles[file] = previousFiles[file];
        }
    }

    log(`Checking ${filesToCheck.length} of ${files.length} files`);

    // Process only files that need checking
    if (filesToCheck.length > 0) {
        const checkedFiles = await processFiles(filesToCheck);
        Object.assign(currentFiles, checkedFiles);
    }

    let hasChanges = false;

    // Check for new or modified files
    for (const [filePath, info] of Object.entries(currentFiles)) {
        const prevInfo = previousFiles[filePath];
        if (prevInfo?.checksum !== info.checksum) {
            log(`Changed: ${filePath}`);
            hasChanges = true;
        }
    }

    // Check for deleted files
    for (const filePath of Object.keys(previousFiles)) {
        if (!currentFiles[filePath]) {
            log(`Deleted: ${filePath}`);
            hasChanges = true;
        }
    }

    return { hasChanges, currentFiles };
}

function needsRecheck(filePath: string, previousInfo: FileInfo | undefined): boolean {
    if (!previousInfo) return true;

    try {
        const stats = fs.statSync(filePath);
        // Only recheck if modification time or size changed
        return stats.mtimeMs !== previousInfo.mtime || stats.size !== previousInfo.size;
    } catch {
        return true;
    }
}

async function processFiles(files: string[]): Promise<Record<string, FileInfo>> {
    const fileInfoMap: Record<string, FileInfo> = {};
    const batchSize = 5;

    for (let i = 0; i < files.length; i += batchSize) {
        const batch = files.slice(i, i + batchSize);
        const results = await Promise.all(
            batch.map(async (file) => ({
                file,
                info: await getFileInfo(file)
            }))
        );

        for (const { file, info } of results) {
            if (info) {
                fileInfoMap[file] = info;
            }
        }
    }

    return fileInfoMap;
}

async function getFileInfo(filePath: string): Promise<FileInfo | null> {
    try {
        const stats = await fs.promises.stat(filePath);
        const checksum = await calculateChecksum(filePath);
        return {
            checksum,
            mtime: stats.mtimeMs,
            size: stats.size
        };
    } catch (error) {
        log(`Error reading ${filePath}: ${error}`);
        return null;
    }
}

function runDocfx(dllPaths: string[]): void {
    log('Running docfx metadata...');

    // Build a dynamic docfx config that merges the base settings with the discovered assemblies.
    // Paths are made relative to the docs/ directory (cwd when the script runs).
    const configDir = process.cwd();
    const relativePaths = dllPaths.map(p => path.relative(configDir, p));

    const generatedConfig = {
        ...docfxBaseConfig,
        metadata: [{
            src: [{ files: relativePaths }],
            output: 'reference/',
            outputFormat: 'markdown',
            namespaceLayout: 'nested',
            categoryLayout: 'nested'
        }]
    };

    const configPath = path.join(configDir, GENERATED_CONFIG_NAME);
    fs.writeFileSync(configPath, JSON.stringify(generatedConfig, null, 2));

    try {
        execFileSync('docfx', ['metadata', configPath], { stdio: 'inherit' });
        log('Documentation generated successfully');
    } catch (error) {
        log(`Error running docfx: ${error}`);
        process.exit(1);
    } finally {
        if (fs.existsSync(configPath)) {
            try {
                fs.unlinkSync(configPath);
            } catch (error) {
                log(`Error cleaning up generated docfx config ${configPath}: ${error}`);
            }
        }
    }
}

function loadState(): State | null {
    try {
        if (fs.existsSync(stateFilePath)) {
            const stateContent = fs.readFileSync(stateFilePath, 'utf-8');
            const state = JSON.parse(stateContent) as State;

            if (state.version !== STATE_VERSION) {
                log('State file version mismatch, starting fresh');
                return null;
            }

            return state;
        }
    } catch (error) {
        log(`Error loading state: ${error}`);
    }
    return null;
}

async function saveState(state: State): Promise<void> {
    const stateDir = path.dirname(stateFilePath);
    if (!fs.existsSync(stateDir)) {
        await fs.promises.mkdir(stateDir, { recursive: true });
    }
    await fs.promises.writeFile(stateFilePath, JSON.stringify(state, null, 2));
}

async function calculateChecksum(filePath: string): Promise<string> {
    const hash = crypto.createHash('sha256');
    const stream = createReadStream(filePath);
    await pipeline(stream, hash);
    return hash.digest('hex');
}
