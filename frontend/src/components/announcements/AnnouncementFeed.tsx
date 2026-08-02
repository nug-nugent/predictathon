import { Text, VStack } from "@chakra-ui/react";
import { getActiveAnnouncements, type ActiveAnnouncement } from "../../services/announcement-service";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";

// "all" (logged-in homepage) shows every active announcement; "loginPage" (logged-out landing page)
// shows only ones flagged ShowOnLoginPage. Renders nothing while loading, on error, or when there's
// nothing to show - this feed sits above the page's real content and shouldn't add clutter or jank.
export function AnnouncementFeed({ audience }: { audience: "all" | "loginPage" }) {
    const { data } = useAsyncData(getActiveAnnouncements, []);

    const announcements = (data ?? []).filter((a) => audience === "all" || a.showOnLoginPage);

    if (announcements.length === 0) {
        return null;
    }

    return (
        <VStack align="stretch" gap={2} mb={4}>
            {announcements.map((a) => (
                <AnnouncementItem key={a.announcementID} announcement={a} />
            ))}
        </VStack>
    );
}

function AnnouncementItem({ announcement }: { announcement: ActiveAnnouncement }) {
    return (
        <Panel py={3}>
            <Text whiteSpace="pre-wrap">{announcement.content}</Text>
            <Text fontSize="xs" color="fg.muted" mt={1}>
                {new Date(announcement.createdAtUtc).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}
            </Text>
        </Panel>
    );
}
