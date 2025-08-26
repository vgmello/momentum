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
};

export default CSharpGenericsPlugin;
