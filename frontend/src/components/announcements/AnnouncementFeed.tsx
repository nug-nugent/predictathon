import { Box, HStack, Text, VStack } from "@chakra-ui/react";
import { TriangleAlert } from "lucide-react";
import { getActiveAnnouncements, type ActiveAnnouncement } from "../../services/announcement-service";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { useAsyncData } from "../../hooks/useAsyncData";

// "all" (logged-in homepage) shows announcements flagged ShowOnHomepage; "loginPage" (logged-out
// landing page) shows only ones flagged ShowOnLoginPage. Renders nothing while loading, on error, or
// when there's nothing to show - this feed sits above the page's real content and shouldn't add
// clutter or jank.
export function AnnouncementFeed({ audience }: { audience: "all" | "loginPage" }) {
    const { data } = useAsyncData(getActiveAnnouncements, []);

    const announcements = (data ?? []).filter((a) => (audience === "all" ? a.showOnHomepage : a.showOnLoginPage));

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

// Warning reuses status.urgent - the same amber already used elsewhere for time-pressure/urgency
// cues (e.g. PredictionDeadlineCard) - rather than introducing a second "this matters" colour.
function AnnouncementItem({ announcement }: { announcement: ActiveAnnouncement }) {
    const isWarning = announcement.severity === "Warning";

    return (
        <Panel py={3} borderLeftWidth={isWarning ? "3px" : undefined} borderLeftColor={isWarning ? "status.urgent" : undefined}>
            <HStack align="flex-start" gap={3}>
                {isWarning && <IconChip icon={TriangleAlert} color="status.urgent" mt="1px" />}
                <Box flex={1}>
                    <Text whiteSpace="pre-wrap">{announcement.content}</Text>
                    <Text fontSize="xs" color="fg.muted" mt={1}>
                        {new Date(announcement.createdAtUtc).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}
                    </Text>
                </Box>
            </HStack>
        </Panel>
    );
}
