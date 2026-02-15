/**
 * Resolves dotnet template conditional directives (<!--#if-->, <!--#else-->, <!--#endif-->)
 * in markdown frontmatter blocks. These HTML-comment directives break YAML parsing when
 * embedded inside --- frontmatter delimiters.
 *
 * In the template repo, INCLUDE_SAMPLE is always true since the sample app exists.
 * For generated projects, `dotnet new` resolves these before users see them.
 *
 * Usage:
 *   bun resolve-conditionals.ts          # resolve (save originals to .originals/)
 *   bun resolve-conditionals.ts restore  # restore originals
 */

import { readFileSync, writeFileSync, mkdirSync, existsSync, unlinkSync, rmSync } from "node:fs";
import { dirname, relative, join } from "node:path";
import { glob } from "glob";

const ORIGINALS_DIR = join(process.cwd(), ".vitepress", ".originals");

const SYMBOLS: Record<string, boolean> = {
    INCLUDE_SAMPLE: true,
    INCLUDE_API: true,
    INCLUDE_ORLEANS: true,
    USE_KAFKA: true,
};

function evaluateCondition(condition: string): boolean {
    const symbol = condition.replace(/[()]/g, "").trim();
    return SYMBOLS[symbol] ?? true;
}

function resolveConditionals(content: string): string {
    // Only process frontmatter (between --- delimiters)
    const frontmatterMatch = content.match(/^(---\n)([\s\S]*?\n)(---\n?)/);
    if (!frontmatterMatch) return content;

    const [fullMatch, openDelim, frontmatter, closeDelim] = frontmatterMatch;

    // Check if frontmatter contains template directives
    if (!frontmatter.includes("<!--#if")) return content;

    // Resolve <!--#if (CONDITION) --> ... <!--#else --> ... <!--#endif --> blocks
    let resolved = frontmatter;
    const pattern =
        /<!--#if\s*\(([^)]+)\)\s*-->\n([\s\S]*?)(?:<!--#else\s*-->\n([\s\S]*?))?<!--#endif\s*-->\n?/g;

    resolved = resolved.replace(pattern, (_match, condition, ifBlock, elseBlock) => {
        return evaluateCondition(condition) ? ifBlock : (elseBlock ?? "");
    });

    return content.replace(fullMatch, openDelim + resolved + closeDelim);
}

function resolve() {
    const files = glob.sync("**/*.md", { cwd: process.cwd(), absolute: true });
    let processed = 0;

    for (const file of files) {
        const content = readFileSync(file, "utf-8");
        const resolved = resolveConditionals(content);
        if (resolved !== content) {
            // Save original
            const relPath = relative(process.cwd(), file);
            const backupPath = join(ORIGINALS_DIR, relPath);
            mkdirSync(dirname(backupPath), { recursive: true });
            writeFileSync(backupPath, content);

            // Write resolved version
            writeFileSync(file, resolved);
            processed++;
            console.log(`  Resolved: ${relPath}`);
        }
    }

    if (processed > 0) {
        console.log(`Resolved template conditionals in ${processed} file(s)`);
    } else {
        console.log("No template conditionals to resolve in frontmatter");
    }
}

function restore() {
    if (!existsSync(ORIGINALS_DIR)) {
        return;
    }

    const backups = glob.sync("**/*.md", { cwd: ORIGINALS_DIR, absolute: true });
    let restored = 0;

    for (const backup of backups) {
        const relPath = relative(ORIGINALS_DIR, backup);
        const originalPath = join(process.cwd(), relPath);
        const content = readFileSync(backup, "utf-8");
        writeFileSync(originalPath, content);
        unlinkSync(backup);
        restored++;
        console.log(`  Restored: ${relPath}`);
    }

    rmSync(ORIGINALS_DIR, { recursive: true, force: true });

    if (restored > 0) {
        console.log(`Restored ${restored} file(s)`);
    }
}

// Main
const action = process.argv[2];
if (action === "restore") {
    restore();
} else {
    resolve();
}
