import MarkdownIt from "markdown-it";

const VUE_SENSITIVE_LANGS = new Set(["csharp", "cs", "java", "typescript", "ts", "tsx", "cpp", "c", "h"]);

function parseSliceRange(rangeStr: string): { start: number; end: number | undefined } {
    let start = 0;
    let end: number | undefined = undefined;

    if (rangeStr.startsWith("-")) {
        // Format: slice:-10 (from start to line 10)
        end = Number.parseInt(rangeStr.substring(1), 10);
    } else if (rangeStr.includes("-")) {
        // Format: slice:2-10 (from line 2 to line 10)
        const [startStr, endStr] = rangeStr.split("-");
        start = Number.parseInt(startStr, 10) - 1;
        end = Number.parseInt(endStr, 10);
    } else {
        // Format: slice:2 (from line 2 to the end)
        start = Number.parseInt(rangeStr, 10) - 1;
    }

    return { start, end };
}

function applySlice(token: any): void {
    const sliceMatch = token.info.match(/slice:(-?\d+(?:-\d+)?)/);
    if (!sliceMatch) return;

    const { start, end } = parseSliceRange(sliceMatch[1]);
    const lines = token.content.split("\n");

    // Apply the slice if the numbers are valid.
    if (!Number.isNaN(start)) {
        token.content = lines.slice(start, end).join("\n");
    }
}

const SnippetPluginExt = (md: MarkdownIt) => {
    const originalSnippetRenderer = md.renderer.rules.fence!;

    md.renderer.rules.fence = (...args) => {
        const [tokens, idx] = args;
        const token = tokens[idx];

        // @ts-ignore
        const isSnippet = token.src && Array.isArray(token.src) && token.src[0];

        if (isSnippet && token.content) {
            applySlice(token);

            const lang = token.info.split(/[\s{:]/)[0];
            if (VUE_SENSITIVE_LANGS.has(lang)) {
                const originalAttrs = token.attrs ? [...token.attrs] : [];
                token.attrPush(["v-pre", ""]);
                const result = originalSnippetRenderer(...args);
                token.attrs = originalAttrs;
                return result;
            }
        }

        return originalSnippetRenderer(...args);
    };
}

export default SnippetPluginExt;
