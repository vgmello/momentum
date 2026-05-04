import type { MermaidConfig } from "mermaid";

// Lazy-load mermaid so it becomes a separate chunk, keeping the main bundle small
// and reducing peak Rollup memory during the VitePress build.
let iconsRegistered = false;

export const render = async (id: string, code: string, config: MermaidConfig): Promise<string> => {
    const { default: mermaid } = await import("mermaid");

    if (!iconsRegistered) {
        mermaid.registerIconPacks([
            {
                name: "logos",
                loader: () => fetch("https://unpkg.com/@iconify-json/logos/icons.json").then((res) => res.json()),
            },
        ]);
        iconsRegistered = true;
    }

    mermaid.initialize(config);
    const { svg } = await mermaid.render(id, code);
    return svg;
};
