import type { Theme } from "vitepress";
import DefaultTheme from "vitepress/theme";
import Mermaid from "../plugins/mermaid/Mermaid.vue";

export default {
    extends: DefaultTheme,
    enhanceApp({ app }) {
        app.component("Mermaid", Mermaid);
    },
} satisfies Theme;
