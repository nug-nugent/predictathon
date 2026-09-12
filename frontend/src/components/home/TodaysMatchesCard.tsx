import { Button, Heading, HStack, Link as ChakraLink, Text, VStack } from "@chakra-ui/react";
import { Radio } from "lucide-react";
import { Link as RouterLink } from "react-router";
import type { LiveMatchGroups } from "../../utils/liveMatches";
import { LiveBadge } from "../match/live-badge/LiveBadge";
import { HomeMatchGroup } from "./HomeMatchGroup";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";

type TodaysMatchesCardProps = {
    /** Today's matches, already split into what's to come, what's in play and what's finished. */
    groups: LiveMatchGroups;
    now: Date;
    /** Refreshes the card once a quick prediction has been saved, so the row shows the new score. */
    onPredictionSaved: () => void;
};

/// Today's matches, split into what's still to come, what's in play, and what's finished. Only
/// shown on the days the competition is playing - HomeMatchesCard owns that decision, and puts
/// This Week's Matches here on the days it isn't.
export function TodaysMatchesCard({ groups, now, onPredictionSaved }: TodaysMatchesCardProps) {
    const isLive = groups.live.length > 0;

    return (
        <Panel p={3} accent mb={3}>
            <HStack gap={2} mb={3} justify="space-between">
                <HStack gap={2}>
                    <IconChip icon={Radio} color={isLive ? "status.live" : "brand.accent"} />
                    <Heading fontSize="17px" fontWeight="bold">Today's Matches</Heading>
                </HStack>

                {/* With something in play the pulsing badge doubles as the way in to the Live page,
                    so the thing drawing the eye is the thing you can click. With nothing in play
                    there's nothing to draw the eye to, so it steps back to the same quiet ghost
                    button the profile card uses for "Edit User". */}
                {isLive ? (
                    <ChakraLink asChild variant="underline" fontSize="sm" fontWeight="bold" flexShrink={0}
                        color="status.live">
                        <RouterLink to="/live" aria-label="Live page">
                            <HStack gap={1.5}>
                                <LiveBadge />
                                <Text as="span" aria-hidden="true">&rarr;</Text>
                            </HStack>
                        </RouterLink>
                    </ChakraLink>
                ) : (
                    <Button asChild size="xs" variant="ghost">
                        <RouterLink to="/live">View All</RouterLink>
                    </Button>
                )}
            </HStack>

            {/* What's happening right now leads, then what's still to come, then what's done -
                so the section is worth its place at the top of the page on a matchday. */}
            <VStack align="stretch" gap={4}>
                <HomeMatchGroup title="Live" matches={groups.live} now={now} live onPredictionSaved={onPredictionSaved} />
                <HomeMatchGroup title="Coming up" matches={groups.comingUp} now={now} onPredictionSaved={onPredictionSaved} />
                <HomeMatchGroup title="Completed" matches={groups.completed} now={now} onPredictionSaved={onPredictionSaved} />
            </VStack>
        </Panel>
    );
}
