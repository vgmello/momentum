import { DefaultTheme } from "vitepress";
import { AdrParser } from "./adrParser";

const AdrSidebar = (adrDir: string) => {
    const adrData = AdrParser.getMetadata(adrDir);

    const adrItems: DefaultTheme.SidebarItem[] = adrData.map((meta) => ({
        text: `${meta.number}: ${meta.title}`,
        link: `/${meta.fileName}`,
    }));

    return [{ text: "Index", link: "/" }, ...adrItems]
}

export default AdrSidebar;
