import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import matter from "gray-matter";

const ADR_PREFIX = "adr-";
const MD_EXTENSION = ".md";

export interface AdrMetadata {
    number: string;
    title: string;
    status: string;
    date: string;
    fileName: string;
}

const adrFileRegex = RegExp(/adr-(\d+)-(.*?)\.md/);

export class AdrParser {
    /**
     * Fetches and parses all ADRs from a directory, applying fallback logic.
     * This is the single source of truth for ADR data.
     */
    static getMetadata(adrDir: string): AdrMetadata[] {
        try {
            const files = readdirSync(adrDir)
                .filter((file) => file.startsWith(ADR_PREFIX) && file.endsWith(MD_EXTENSION))
                .sort();

            return files.map((file) => {
                const filePath = join(adrDir, file);
                const content = readFileSync(filePath, "utf-8");
                const { data: frontmatter } = matter(content);

                let number, title;

                if (frontmatter.number && frontmatter.title) {
                    number = frontmatter.number;
                    title = frontmatter.title;
                } else {
                    const fromFilename = this.parseAdrFromFilename(file);
                    number = fromFilename.number || "ADR-???";
                    title = fromFilename.title || "Untitled";
                }

                return {
                    number,
                    title,
                    status: frontmatter.status || "Unknown",
                    date: frontmatter.date || "",
                    fileName: `${file.replace(MD_EXTENSION, "")}`,
                };
            });
        } catch (error) {
            console.error(`🚨 Failed to get ADR data from ${adrDir}:`, error);
            return [];
        }
    }

    /**
     * Parses ADR metadata from a filename as a fallback.
     * Example: "adr-001-some-title.md" -> { number: "ADR-001", title: "Some Title" }
     */
    private static parseAdrFromFilename(filename: string): Partial<AdrMetadata> {
        const match = adrFileRegex.exec(filename);
        if (!match) return {};

        const number = `ADR-${match[1]}`;
        const title = match[2]
            .split("-")
            .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
            .join(" ");

        return { number, title };
    }
}
