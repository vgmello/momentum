import { readFileSync } from "fs";
import { parse } from "yaml";
import { DefaultTheme } from "vitepress";

interface TocItem {
    name: string;
    href?: string;
    items?: TocItem[];
}

const TocSidebar = (tocFilePath: string, basePath: string = "") => {
    const tocItems = parseTocYaml(tocFilePath);
    return convertTocToVitePressSidebar(tocItems, false, basePath);
}

export default TocSidebar;

function convertTocToVitePressSidebar(tocItems: TocItem[], collapsed: boolean = true, basePath: string = "")
    : DefaultTheme.SidebarItem[] {
    return tocItems.map((item) => {
        const navItem: DefaultTheme.SidebarItem = {
            text: item.name,
            collapsed: collapsed,
        };

        if (item.href) {
            let link = item.href.replace(/\.md$/, "");

            if (!link.startsWith("/")) {
                link = basePath + "/" + link;
            }

            navItem.link = link;
        }

        if (item.items && item.items.length > 0) {
            navItem.items = convertTocToVitePressSidebar(item.items, true, basePath);
        }

        return navItem;
    });
}

function parseTocYaml(tocFilePath: string): TocItem[] {
    try {
        const yamlContent = readFileSync(tocFilePath, "utf-8");
        const parsedYaml = parse(yamlContent);

        if (Array.isArray(parsedYaml)) {
            return parsedYaml;
        }

        if (typeof parsedYaml === "object" && parsedYaml !== null) {
            const possibleRoots = ["items", "toc", "content"];
            for (const root of possibleRoots) {
                if (parsedYaml[root] && Array.isArray(parsedYaml[root])) {
                    return parsedYaml[root];
                }
            }

            return [parsedYaml];
        }

        return [];
    } catch (error) {
        console.error(`Error parsing TOC file ${tocFilePath}:`, error);
        return [];
    }
}
