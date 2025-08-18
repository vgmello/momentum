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
                        text: "CQRS",
                        collapsed: false,
                        items: [
                            { text: "Commands", link: "/cqrs/commands" },
                            { text: "Queries", link: "/cqrs/queries" },
                            { text: "Handlers", link: "/cqrs/handlers" },
                            { text: "Validation", link: "/cqrs/validation" },
                        ],
                    },
                    {
                        text: "Messaging & Events",
                        collapsed: false,
                        items: [
                            { text: "Integration Events", link: "/messaging/integration-events" },
                            { text: "Domain Events", link: "/messaging/domain-events" },
                            { text: "Kafka Configuration", link: "/messaging/kafka" },
                            { text: "Wolverine", link: "/messaging/wolverine" },
                        ],
                    },
                    {
                        text: "Database",
                        collapsed: false,
                        items: [
                            { text: "Data Access", link: "/rdbms" },
                            { text: "DbCommand Pattern", link: "/database/dbcommand" },
                            { text: "Entity Mapping", link: "/database/entity-mapping" },
                            { text: "Transactions", link: "/database/transactions" },
                            { text: "Database Migrations", link: "/rdbms-migrations" },
                        ],
                    },
                    {
                        text: "Service Configuration",
                        collapsed: false,
                        items: [
                            { text: "Service Defaults", link: "/service-configuration/service-defaults" },
                            { text: "API Setup", link: "/service-configuration/api-setup" },
                            { text: "Observability", link: "/service-configuration/observability" },
                        ],
                    },
                    {
                        text: "Testing",
                        collapsed: false,
                        items: [
                            { text: "Unit Tests", link: "/testing/unit-tests" },
                            { text: "Integration Tests", link: "/testing/integration-tests" },
                            { text: "Testing Overview", link: "/testing" },
                        ],
                    },
                    {
                        text: "Production",
                        collapsed: false,
                        items: [
                            { text: "Best Practices", link: "/best-practices" },
                            { text: "Troubleshooting", link: "/troubleshooting" },
                            { text: "Error Handling", link: "/error-handling" },
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
