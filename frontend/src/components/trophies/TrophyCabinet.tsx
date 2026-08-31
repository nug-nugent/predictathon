import { Box, HStack, Text, VStack, Wrap } from "@chakra-ui/react";
import type { UserTrophy } from "../../services/trophy-service";
import { trophyColour, trophyIcon, trophyLabel } from "./trophy-icon";

// The full set of a user's competition wins, shown on their profile. Wins only, and nothing at all
// when there are none - most people have never won one, and a permanently empty trophy cabinet on
// your own profile is the opposite of celebratory.
export function TrophyCabinet({ trophies }: { trophies: UserTrophy[] }) {
    if (trophies.length === 0) {
        return null;
    }

    return (
        <Box mt={4} pt={3} borderTopWidth="1px">
            <Text fontSize="sm" fontWeight="bold" mb={2}>Trophies</Text>
            <Wrap gap={3}>
                {trophies.map((trophy) => {
                    const Icon = trophyIcon(trophy.badgeIcon);
                    const colour = trophyColour(trophy.badgeColour);

                    return (
                        <HStack
                            key={`${trophy.competitionSeriesID ?? trophy.name}-${trophy.years}`}
                            gap={2}
                            px={3}
                            py={2}
                            rounded="card"
                            borderWidth="1px"
                            borderColor="border.hairline"
                            title={trophyLabel(trophy.name, trophy.winCount, trophy.years)}
                        >
                            <Box {...colour} display="flex" alignItems="center" flexShrink={0}>
                                <Icon size={22} color="currentColor" />
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
                })}
            </Wrap>
        </Box>
    );
}
