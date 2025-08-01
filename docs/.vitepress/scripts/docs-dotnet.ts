import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { glob } from 'glob';
import { execSync } from 'node:child_process';
import { createReadStream } from 'node:fs';
import { pipeline } from 'node:stream/promises';

import docfxConfig from '../../docfx.json' with { type: 'json' };

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

interface DocfxConfig {
    metadata: Array<{
        src: Array<{
            src: string;
            files: string[];
        }>;
    }>;
}

const STATE_FILE_NAME = 'reference/.state';
const STATE_VERSION = '1.0';

const stateFilePath = path.join(process.cwd(), STATE_FILE_NAME);
const log = (message: string) => console.log(`[${new Date().toISOString()}] ${message}`);

try {
    const startTime = Date.now();

    log('Scanning for files...');

    const files = await getDocFxSrcFiles(docfxConfig);

    log(`Found ${files.length} files`);

    if (files.length === 0) {
        log('No files found. Build the project first.');
        process.exit(0);
    }

    const previousState = loadState();
    const { hasChanges, currentFiles } = await detectChanges(files, previousState);

    if (hasChanges) {
        runDocfx();

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

async function getDocFxSrcFiles(config: DocfxConfig): Promise<string[]> {
    const allFiles: string[] = [];

    for (const metadata of config.metadata) {
        for (const srcConfig of metadata.src) {
            const basePath = path.resolve(srcConfig.src);

            for (const pattern of srcConfig.files) {
                const matches = await glob(pattern, {
                    cwd: basePath,
                    absolute: true
                });
                allFiles.push(...matches);
            }
        }
    }

    return [...new Set(allFiles)]; // Remove duplicates
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
        if (!prevInfo || prevInfo.checksum !== info.checksum) {
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

function runDocfx(): void {
    log('Running docfx metadata...');
    try {
        execSync('docfx metadata', { stdio: 'inherit' });
        log('Documentation generated successfully');
    } catch (error) {
        log(`Error running docfx: ${error}`);
        process.exit(1);
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
