import { DefaultTheme } from 'vitepress';
import eventsSidebarData from '../../events/events-sidebar.json' with { type: 'json' };

const EventsSidebar = () => {
    const eventItems: DefaultTheme.SidebarItem[] = eventsSidebarData.map((e) => ({
        text: e.text,
        items: e.items.map((i) => ({
            text: i.text === "Domain Events" ? "<b>Domain Events</b>" : i.text,
            items: i.items,
            link: i.link ?? undefined,
            collapsed: i.collapsed
        })),
        link: e.link ?? undefined,
        collapsed: e.collapsed
    }) as DefaultTheme.SidebarItem);

    return eventItems
}

export default EventsSidebar;
