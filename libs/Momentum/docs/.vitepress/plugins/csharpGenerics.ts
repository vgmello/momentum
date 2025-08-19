import MarkdownIt from "markdown-it";

/**
 * Plugin to handle C# generic types and escaped characters in markdown documentation
 * This plugin converts problematic HTML entities to safe characters to avoid Vue parser issues
 */
const CSharpGenericsPlugin = (md: MarkdownIt) => {
    // Process content before parsing to replace problematic HTML entities
    md.core.ruler.before('normalize', 'csharp-generics-pre', (state) => {
        // Replace HTML entities that cause Vue parser issues
        // The docs-dotnet.ts script double-escapes these entities, so we need to handle both forms
        
        // Handle double-escaped entities first
        state.src = state.src
            .replace(/&amp;lt;/g, '<')    // Replace &amp;lt; with actual less-than
            .replace(/&amp;gt;/g, '>');   // Replace &amp;gt; with actual greater-than
            
        // Handle regular HTML entities 
        state.src = state.src
            .replace(/&#92;/g, '\\\\')     // Replace &#92; with escaped backslash
            .replace(/&#63;/g, '?')       // Replace &#63; with actual question mark
            .replace(/&lt;/g, '<')       // Replace &lt; with actual less-than
            .replace(/&gt;/g, '>');      // Replace &gt; with actual greater-than
    });
};

export default CSharpGenericsPlugin;
