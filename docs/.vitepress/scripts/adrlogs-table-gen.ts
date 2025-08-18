import { writeFileSync } from "node:fs";
import { join } from "node:path";
import { AdrParser } from "../plugins/adr/adrParser";

const ADR_DIR_DEFAULT = "../../arch/adr";

const adrDir = join(import.meta.dirname, ADR_DIR_DEFAULT);
const outputPath = join(adrDir, "table.md");

const tableContent = generateAdrTable(adrDir);

writeFileSync(outputPath, tableContent, "utf-8");

console.log(`✅ ADR table generated at: ${outputPath}`);

function generateAdrTable(adrDir: string): string {
    const metadata = AdrParser.getMetadata(adrDir);

    if (metadata.length === 0) {
        return "### ⚠️ No ADRs found or an error occurred.";
    }

    const header = `| ID | Title | Status | Date |\n|:-------|:------|:-------|:-----|`;

    const rows = metadata
        .map((meta) => {
            const idLink = `[${meta.number}](${meta.fileName})`;
            const statusBadge = `<Badge type="${getStatusBadgeType(meta.status)}" text="${meta.status}" />`;
            const formattedDate = meta.date ? formatDate(meta.date) : "–";
            return `| ${idLink} | ${meta.title} | ${statusBadge} | ${formattedDate} |`;
        })
        .join("\n");

    return `${header}\n${rows}`;
}

function getStatusBadgeType(status: string): string {
    switch (status.toLowerCase()) {
        case "accepted":
            return "tip";
        case "proposed":
            return "info";
        case "deprecated":
            return "danger";
        case "superseded":
            return "warning";
        default:
            return "info";
    }
}

function formatDate(date: string | Date): string {
    try {
        const dateObj = new Date(date);
        if (isNaN(dateObj.getTime())) return date.toString(); // Invalid date
        return dateObj.toISOString().split("T")[0]; // YYYY-MM-DD
    } catch {
        return date.toString();
    }
}
