import { defineConfig, type MarkdownOptions } from "vitepress";

import { MermaidPlugin } from "./plugins/mermaid/mermaid";
import SnippetPluginExt from "./plugins/snippet";
import TocSidebar from "./plugins/tocSidebar";
import AdrSidebar from "./plugins/adr/adrSidebar";

const REF_DIR = "reference/toc.yml";
const ADR_DIR = "guide/arch/adr";

const markdownOptions: MarkdownOptions = {
    theme: {
        light: "github-light",
        dark: "github-dark",
    },

    preConfig: (md) => {
        SnippetPluginExt(md);
    },

    config: (md) => {
        MermaidPlugin(md);
    },
};

export default defineConfig({
    title: "Momentum .NET",
    description: "Momentum .NET, It's not a framework, it's a highly opinionated .NET template with pre-configured .NET libraries and tools, for scalable, distributed .NET services.",
    markdown: markdownOptions,
    themeConfig: {
        nav: [
            { text: "Home", link: "/" },
            { text: "Guide", link: "/guide/getting-started" },
            { text: "Reference", link: "/reference/Momentum" },
        ],

        editLink: {
            pattern: 'https://github.com/vgmello/momentum/edit/main/docs/:path'
        },

        search: {
            provider: "local",
        },

        sidebar: {
            "/guide/": {
                base: "/guide",
                items: [
                    {
                        text: "Introduction",
                        collapsed: false,
                        items: [
                            { text: "Getting Started", link: "/getting-started" },
                        ],
                    },
                    {
                        text: "Data",
                        collapsed: false,
                        items: [
                            { text: "Data Access", link: "/rdbms" },
                            { text: "Database Migrations", link: "/rdbms-migrations" },
                        ],
                    },
                    {
                        text: "Messaging / Eventing",
                        collapsed: false,
                        items: [],
                    },
                    {
                        text: "Testing",
                        collapsed: false,
                        items: [
                            { text: "Unit Tests", link: "/unit-tests" },
                            { text: "Integration Tests", link: "/integration-tests" }
                        ],
                    },
                    {
                        text: "Architecture",
                        collapsed: false,
                        items: [
                            { text: "Overview", link: "/arch/" },
                            { text: "ADRs", base: "/guide/arch/adr", collapsed: true, items: AdrSidebar(ADR_DIR) }
                        ],
                    },
                ],
            },
            "/reference": {
                base: "/reference",
                items: TocSidebar(REF_DIR),
            },
        },

        socialLinks: [{ icon: "github", link: "https://github.com/vgmello/momentum" }],

        footer: {
            copyright: "Momentum .NET",
        },
    },
    lastUpdated: true,
    cleanUrls: true,
});
