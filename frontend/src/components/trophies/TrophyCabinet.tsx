import { Box, Text, Wrap } from "@chakra-ui/react";
import type { UserTrophy } from "../../services/trophy-service";
import { TrophyBadge } from "./TrophyBadge";

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
                {trophies.map((trophy) => (
                    <TrophyBadge key={`${trophy.competitionSeriesID ?? trophy.name}-${trophy.years}`} trophy={trophy} />
                ))}
            </Wrap>
        </Box>
    );
}
