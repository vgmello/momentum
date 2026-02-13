import { DefaultTheme } from 'vitepress';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import eventsSidebarData from '../../events/events-sidebar.json' with { type: 'json' };

interface EventSidebarItem {
    text: string;
    items?: EventSidebarItem[];
    link?: string | null;
    collapsed?: boolean;
}

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const generateFallbackSidebar = (): DefaultTheme.SidebarItem[] => {
    const eventsDir = path.resolve(__dirname, '../../events');

    if (!fs.existsSync(eventsDir)) {
        return [{ text: 'Events', link: '/' }];
    }

    const files = fs.readdirSync(eventsDir)
        .filter(f => f.endsWith('.md') && !f.startsWith('index') && !f.startsWith('domain_events') && !f.startsWith('integration_events'));

    const integrationEvents: DefaultTheme.SidebarItem[] = [];
    const domainEvents: DefaultTheme.SidebarItem[] = [];

    for (const file of files) {
        const name = file.replace('.md', '');
        const parts = name.split('.');
        const eventName = parts[parts.length - 1];
        const isIntegration = name.includes('IntegrationEvents');

        const item: DefaultTheme.SidebarItem = {
            text: eventName,
            link: `/${name}`
        };

        if (isIntegration) {
            integrationEvents.push(item);
        } else if (name.includes('DomainEvents')) {
            domainEvents.push(item);
        }
    }

    const items: DefaultTheme.SidebarItem[] = [
        { text: 'Overview', link: '/' }
    ];

    if (integrationEvents.length > 0) {
        items.push({
            text: 'Integration Events',
            link: '/integration_events',
            collapsed: false,
            items: integrationEvents
        });
    }

    if (domainEvents.length > 0) {
        items.push({
            text: 'Domain Events',
            link: '/domain_events',
            collapsed: false,
            items: domainEvents
        });
    }

    return items;
};

const EventsSidebar = (): DefaultTheme.SidebarItem[] => {
    // If the sidebar JSON is empty, generate a fallback from existing files
    if (!eventsSidebarData || eventsSidebarData.length === 0) {
        return generateFallbackSidebar();
    }

    const eventItems: DefaultTheme.SidebarItem[] = (eventsSidebarData as EventSidebarItem[]).map((e) => ({
        text: e.text,
        items: e.items?.map((i) => ({
            text: i.text === "Domain Events" ? "<b>Domain Events</b>" : i.text,
            items: i.items,
            link: i.link ?? undefined,
            collapsed: i.collapsed
        })),
        link: e.link ?? undefined,
        collapsed: e.collapsed
    }) as DefaultTheme.SidebarItem);

    return eventItems;
};

export default EventsSidebar;
