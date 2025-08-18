import MarkdownIt from "markdown-it";

const VUE_SENSITIVE_LANGS = new Set(["csharp", "cs", "java", "typescript", "ts", "tsx", "cpp", "c", "h"]);

const SnippetPluginExt = (md: MarkdownIt) => {
    const originalSnippetRenderer = md.renderer.rules.fence!;

    md.renderer.rules.fence = (...args) => {
        const [tokens, idx] = args;
        const token = tokens[idx];

        // @ts-ignore
        const isSnippet = token.src && Array.isArray(token.src) && token.src[0];

        if (isSnippet && token.content) {
            const sliceMatch = token.info.match(/slice:(-?\d+(?:-\d+)?)/);
            if (sliceMatch) {
                const rangeStr = sliceMatch[1];
                const lines = token.content.split("\n");
                let start = 0;
                let end = undefined;

                if (rangeStr.startsWith("-")) {
                    // Format: slice:-10 (from start to line 10)
                    end = parseInt(rangeStr.substring(1), 10);
                } else if (rangeStr.includes("-")) {
                    // Format: slice:2-10 (from line 2 to line 10)
                    const [startStr, endStr] = rangeStr.split("-");
                    start = parseInt(startStr, 10) - 1;
                    end = parseInt(endStr, 10);
                } else {
                    // Format: slice:2 (from line 2 to the end)
                    start = parseInt(rangeStr, 10) - 1;
                }

                // Apply the slice if the numbers are valid.
                if (!isNaN(start)) {
                    token.content = lines.slice(start, end).join("\n");
                }
            }

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
