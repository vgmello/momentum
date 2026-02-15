import MarkdownIt from "markdown-it";

const hasAngleBracketPair = (content: string): boolean => {
    const openIdx = content.indexOf("<");
    return openIdx !== -1 && content.includes(">", openIdx + 2);
};

/**
 * Plugin to handle C# generic types and prevent Vue parser issues.
 * Adds v-pre to fenced code blocks and inline code containing angle brackets,
 * preventing Vue from interpreting C#/XML generics as component tags.
 */
const CSharpGenericsPlugin = (md: MarkdownIt) => {
    // Override fence renderer for code blocks with angle brackets
    const originalFenceRenderer = md.renderer.rules.fence!;

    md.renderer.rules.fence = (...args) => {
        const [tokens, idx] = args;
        const token = tokens[idx];

        // Apply v-pre to any code fence that might contain content confusing to Vue parser
        if (token.content && hasAngleBracketPair(token.content)) {
            const originalAttrs = token.attrs ? [...token.attrs] : [];
            token.attrPush(["v-pre", ""]);
            const result = originalFenceRenderer(...args);
            token.attrs = originalAttrs;
            return result;
        }

        return originalFenceRenderer(...args);
    };

    // Override code_inline renderer for inline code with angle brackets
    const originalCodeInlineRenderer = md.renderer.rules.code_inline;

    md.renderer.rules.code_inline = (tokens, idx, options, env, slf) => {
        const token = tokens[idx];

        if (token.content && hasAngleBracketPair(token.content)) {
            return `<code v-pre>${md.utils.escapeHtml(token.content)}</code>`;
        }

        if (originalCodeInlineRenderer) {
            return originalCodeInlineRenderer(tokens, idx, options, env, slf);
        }

        return `<code${slf.renderAttrs(token)}>${md.utils.escapeHtml(token.content)}</code>`;
    };
};

export default CSharpGenericsPlugin;
