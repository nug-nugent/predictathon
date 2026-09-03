import { createElement } from "react";
import { Box, HStack, Text, VStack } from "@chakra-ui/react";
import type { UserTrophy } from "../../services/trophy-service";
import { trophyColour, trophyIcon, trophyLabel } from "./trophy-icon";

// One trophy, spelled out: the badge, what it was won and how often, and the years it was won in.
// Shared by the profile's trophy cabinet and the popover behind a trophy stamp, so a win reads the
// same wherever it is explained rather than being restyled per surface.
export function TrophyBadge({ trophy }: { trophy: UserTrophy }) {
    const colour = trophyColour(trophy.badgeColour);

    return (
        <HStack
            gap={2}
            px={3}
            py={2}
            rounded="card"
            borderWidth="1px"
            borderColor="border.hairline"
            title={trophyLabel(trophy.name, trophy.winCount, trophy.years)}
        >
            <Box {...colour} display="flex" alignItems="center" flexShrink={0}>
                {/* createElement rather than binding the looked-up icon to a capitalised local:
                    that reads to the react-hooks lint as a component defined during render. */}
                {createElement(trophyIcon(trophy.badgeIcon), { size: 22, color: "currentColor" })}
            </Box>
            <VStack align="start" gap={0}>
                <HStack gap={1.5}>
                    <Text fontSize="sm" fontWeight="bold">{trophy.name}</Text>
                    {trophy.winCount > 1 && (
                        <Text fontSize="sm" fontWeight="bold" {...colour}>&times;{trophy.winCount}</Text>
                    )}
                </HStack>
                <Text fontSize="xs" color="fg.muted">{trophy.years}</Text>
            </VStack>
        </HStack>
    );
}
