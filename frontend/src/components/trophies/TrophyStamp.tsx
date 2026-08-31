import { HStack, Text } from "@chakra-ui/react";
import type { UserTrophy } from "../../services/trophy-service";
import { trophyColour, trophyIcon, trophyLabel } from "./trophy-icon";

// A compact row of a user's competition wins, for sitting beside their name in a list. Only wins
// earn a stamp - like the star on a shirt, it means "won this", not "did well at this".
//
// Trophies arrive already grouped and ordered by the server, so the first few are the ones worth
// showing; anything past `max` collapses into a count rather than crowding the name it sits next
// to. The full set is on the profile.
export function TrophyStamp({ trophies, max = 3 }: { trophies: UserTrophy[]; max?: number }) {
    if (trophies.length === 0) {
        return null;
    }

    const shown = trophies.slice(0, max);
    const remaining = trophies.length - shown.length;

    return (
        <HStack gap={1.5} flexShrink={0}>
            {shown.map((trophy) => {
                const Icon = trophyIcon(trophy.badgeIcon);
                const label = trophyLabel(trophy.name, trophy.winCount, trophy.years);

                return (
                    <HStack
                        key={`${trophy.competitionSeriesID ?? trophy.name}-${trophy.years}`}
                        gap={0.5}
                        role="img"
                        aria-label={label}
                        title={label}
                        {...trophyColour(trophy.badgeColour)}
                    >
                        <Icon size={13} color="currentColor" />
                        {trophy.winCount > 1 && (
                            <Text fontSize="2xs" fontWeight="bold" lineHeight="1">{trophy.winCount}</Text>
                        )}
                    </HStack>
                );
            })}
            {remaining > 0 && (
                <Text fontSize="2xs" color="fg.muted" lineHeight="1">+{remaining}</Text>
            )}
        </HStack>
    );
}
