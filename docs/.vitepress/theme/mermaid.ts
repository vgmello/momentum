import mermaid, { type MermaidConfig } from "mermaid";
import elkLayouts from "@mermaid-js/layout-elk";

mermaid.registerLayoutLoaders(elkLayouts);

mermaid.registerIconPacks([
    {
        name: "logos",
        loader: () => fetch("https://unpkg.com/@iconify-json/logos/icons.json").then((res) => res.json()),
    },
]);

export const render = async (id: string, code: string, config: MermaidConfig): Promise<string> => {
    mermaid.initialize(config);
    const { svg } = await mermaid.render(id, code);
    return svg;
};
