import MarkdownIt from "markdown-it";

const CSharpGenericsPlugin = (md: MarkdownIt) => {
    md.core.ruler.before('normalize', 'csharp-generics', (state) => {

        const genericPattern = /(?<!`)(\b[A-Z][a-zA-Z0-9]*(?:\.[A-Z][a-zA-Z0-9]*)*<[^>]+>)(?!`)/g;
        state.src = state.src.replace(genericPattern, (m: string) => {
            return md.utils.escapeHtml(m);
        });

    });
};

export default CSharpGenericsPlugin;
