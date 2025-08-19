import MarkdownIt from "markdown-it";

/**
 * Plugin to handle C# generic types and prevent Vue parser issues
 * Perfect hybrid solution: v-pre for C# code + protected minimal escaping
 */
const CSharpGenericsPlugin = (md: MarkdownIt) => {
    const originalFenceRenderer = md.renderer.rules.fence!;

    md.renderer.rules.fence = (...args) => {
        const [tokens, idx] = args;
        const token = tokens[idx];

        // Apply v-pre to any code fence that might contain content confusing to Vue parser
        if (token.content && /<[^>]*>/.test(token.content)) {
            const originalAttrs = token.attrs ? [...token.attrs] : [];
            token.attrPush(["v-pre", ""]);
            const result = originalFenceRenderer(...args);
            token.attrs = originalAttrs;
            return result;
        }

        return originalFenceRenderer(...args);
    };

    // Apply escaping to any file with angle brackets that could confuse Vue
    md.core.ruler.before('normalize', 'escape-vue-conflicts', (state) => {
        let src = state.src;

        // Simple detection: does this file have any angle brackets that could be problematic?
        if (!/<[^>]*>/.test(src)) {
            return;
        }

        // Special handling for files that cause explosive over-escaping in the refined approach
        // These are files where protection/restoration breaks down, so we use ultra-aggressive processing
        const hasComplexPatterns = /\b\w+<[^>]*<[^>]*>[^>]*>/.test(src);

        if (hasComplexPatterns) {
            // Use the old ultra-aggressive approach: direct line-by-line processing
            const lines = src.split('\n');
            let inCodeBlock = false;

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];

                // Check if we're entering or leaving a code block
                if (line.trim().startsWith('```')) {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                // Skip processing if we're inside a code block
                if (inCodeBlock) {
                    continue;
                }

                // Escape ALL angle brackets on this line, but preserve anchor tags
                lines[i] = line.replace(/<([^>]*?)>/g, (match, content) => {
                    // Keep markdown anchor IDs intact
                    if (content.startsWith('a id=') || content.startsWith('/a')) {
                        return match;
                    }
                    // Escape everything else
                    return '&lt;' + content + '&gt;';
                });
            }

            src = lines.join('\n');
            state.src = src;
            console.log('[CSharpGenericsPlugin] Applied ultra-aggressive escaping (old method)');
            return; // Early return - bypass all other processing
        }

        // Protect fenced code blocks and inline code from escaping
        const protectedBlocks: string[] = [];
        let blockIndex = 0;

        src = src.replace(/```[\s\S]*?```/g, (match) => {
            const placeholder = `__PROTECTED_${blockIndex++}__`;
            protectedBlocks.push(match);
            return placeholder;
        });

        src = src.replace(/`[^`\r\n]+`/g, (match) => {
            const placeholder = `__PROTECTED_${blockIndex++}__`;
            protectedBlocks.push(match);
            return placeholder;
        });

        // Escape ALL angle brackets outside code blocks, preserving navigation anchors and markdown links
        const lines = src.split('\n');
        let inCodeBlock = false;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];

            // Check if we're entering or leaving a code block
            if (line.trim().startsWith('```')) {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            // Skip processing if we're inside a code block
            if (inCodeBlock) {
                continue;
            }

            // Escape angle brackets, preserving anchor tags and markdown links
            lines[i] = line.replace(/<([^>]*?)>/g, (match, content) => {
                // Preserve navigation anchor tags
                if (content.startsWith('a id=') || content.startsWith('/a')) {
                    return match;
                }

                // Preserve markdown links - don't escape if inside [text](url) pattern
                const beforeMatch = line.substring(0, line.indexOf(match));
                if (beforeMatch.includes('[') && beforeMatch.lastIndexOf('[') > beforeMatch.lastIndexOf(']')) {
                    return match;
                }

                // Escape everything else
                return '&lt;' + content + '&gt;';
            });
        }

        src = lines.join('\n');

        // Only escape nullable markdown links for files that actually have C# generics
        // This prevents corrupting other content that might have similar patterns
        const hasCSharpGenerics = src.includes('[string](') ||
            src.includes('Result<') ||
            src.includes('<T>') ||
            /\b\w+<[A-Z]\w*>/.test(src);

        if (hasCSharpGenerics) {
            src = src.replace(/(\[[^\]]+\]\([^)]+\))\?/g, '$1\\?');
        }

        // Restore protected blocks unchanged
        for (let i = 0; i < protectedBlocks.length; i++) {
            src = src.replace(`__PROTECTED_${i}__`, protectedBlocks[i]);
        }

        state.src = src;
    });
};

export default CSharpGenericsPlugin;
