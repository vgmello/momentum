import MarkdownIt from "markdown-it";

/**
 * Plugin to handle C# generic types and prevent Vue parser issues
 * Perfect hybrid solution: v-pre for C# code + protected minimal escaping
 */
const CSharpGenericsPlugin = (md: MarkdownIt) => {
    // Add v-pre to C# code blocks with generics
    const originalFenceRenderer = md.renderer.rules.fence!;
    
    md.renderer.rules.fence = (...args) => {
        const [tokens, idx] = args;
        const token = tokens[idx];
        const lang = token.info.split(/[\s{:]/)[0];
        
        // Apply v-pre to C# code blocks containing generic syntax
        if ((lang === "csharp" || lang === "cs") && token.content) {
            const hasGenerics = /<[A-Z][A-Za-z0-9]*>|<T[A-Za-z0-9]*>|\w+<\w+>/.test(token.content);
            
            if (hasGenerics) {
                const originalAttrs = token.attrs ? [...token.attrs] : [];
                token.attrPush(["v-pre", ""]);
                const result = originalFenceRenderer(...args);
                token.attrs = originalAttrs;
                return result;
            }
        }
        
        // Apply v-pre to XML code blocks to prevent Vue parsing issues
        if (lang === "xml" && token.content) {
            const originalAttrs = token.attrs ? [...token.attrs] : [];
            token.attrPush(["v-pre", ""]);
            const result = originalFenceRenderer(...args);
            token.attrs = originalAttrs;
            return result;
        }
        
        // Apply v-pre to bash code blocks that might contain XML-like syntax
        if (lang === "bash" && token.content) {
            // Check if bash content contains angle brackets that might confuse Vue
            if (/<[^>]*>/.test(token.content)) {
                const originalAttrs = token.attrs ? [...token.attrs] : [];
                token.attrPush(["v-pre", ""]);
                const result = originalFenceRenderer(...args);
                token.attrs = originalAttrs;
                return result;
            }
        }
        
        return originalFenceRenderer(...args);
    };
    
    // Apply only essential escaping outside code blocks
    md.core.ruler.before('normalize', 'csharp-generics-pre', (state) => {
        let src = state.src;
        const originalSrc = src;
        
        // Process files that have C# generics or XML content that might cause Vue parsing issues
        const hasCSharpGenerics = src.includes('[string](') || 
                                src.includes('Result<') || 
                                src.includes('<T>') ||
                                /\b\w+<[A-Z]\w*>/.test(src);
        
        const hasProblematicXML = src.includes('<?xml') || 
                                 src.includes('<databaseChangeLog') ||
                                 src.includes('<Project Sdk') ||
                                 src.includes('<changeSet') ||
                                 /<!--[^>]*-->/.test(src);
        
        // Ultra-aggressive mode: Apply to ANY file with potential Vue parsing issues
        const hasAnyAngleBrackets = /<[^>]*>/.test(src);
        const needsUltraAggressive = hasAnyAngleBrackets && 
                                   (hasProblematicXML || 
                                    src.includes('Migration Rollback Strategy') || 
                                    src.includes('adding-domains') ||
                                    src.length > 30000); // Large files are more likely to have issues
        
        if (needsUltraAggressive) {
            // For these files, escape EVERY angle bracket outside code blocks
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
                
                // Escape ALL angle brackets on this line, but preserve markdown anchor links
                lines[i] = line.replace(/<([^>]*?)>/g, (match, content) => {
                    // Keep markdown anchor IDs intact - they're needed for navigation
                    if (content.startsWith('a id=') || content.startsWith('/a')) {
                        return match;
                    }
                    // Escape everything else
                    return '&lt;' + content + '&gt;';
                });
            }
            
            src = lines.join('\n');
            state.src = src;
            console.log('[CSharpGenericsPlugin] Applied ultra-aggressive escaping to problematic file');
            return;
        }
        
        if (!hasCSharpGenerics && !hasProblematicXML) {
            return;
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
        
        // Escape specific C# generic patterns that cause Vue parsing issues
        // Only target patterns that are clearly generics, not HTML tags
        if (hasCSharpGenerics) {
            src = src.replace(/\bResult<([^>]+)>/g, 'Result&lt;$1&gt;');
            src = src.replace(/\bTask<([^>]+)>/g, 'Task&lt;$1&gt;');
            src = src.replace(/\b(\w+)<(T\w*|[A-Z]\w*)>/g, '$1&lt;$2&gt;');
            
            // Protect markdown links with nullable indicators
            src = src.replace(/(\[[^\]]+\]\([^)]+\))\?/g, '$1\\?');
        }
        
        // Escape problematic XML patterns outside of code blocks
        if (hasProblematicXML) {
            // Ultra-aggressive approach: escape ALL angle brackets outside of code blocks and markdown links
            // This will catch any XML content that might be causing Vue parsing issues
            
            // Split content into lines to process line by line
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
                
                // Escape angle brackets that are not part of markdown links
                lines[i] = line.replace(/<([^>]*?)>/g, (match, content) => {
                    // Don't escape if it's part of a markdown link pattern like [text](url)
                    const beforeMatch = line.substring(0, line.indexOf(match));
                    if (beforeMatch.includes('[') && beforeMatch.lastIndexOf('[') > beforeMatch.lastIndexOf(']')) {
                        return match; // Keep markdown links intact
                    }
                    
                    // Don't escape HTML anchor tags
                    if (content.startsWith('a id=') || content.startsWith('/a')) {
                        return match;
                    }
                    
                    // Escape everything else
                    return '&lt;' + content + '&gt;';
                });
            }
            
            src = lines.join('\n');
        }
        
        // Restore protected blocks unchanged
        for (let i = 0; i < protectedBlocks.length; i++) {
            src = src.replace(`__PROTECTED_${i}__`, protectedBlocks[i]);
        }
        
        // Log processed files (only when changes are made)
        if (src !== originalSrc) {
            console.log(`[CSharpGenericsPlugin] Applied Vue parsing fixes`);
        }
        
        state.src = src;
    });
};

export default CSharpGenericsPlugin;