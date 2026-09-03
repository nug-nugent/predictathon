import { Button, HStack, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import type { UserTrophy } from "../../services/trophy-service";
import { TrophyBadge } from "./TrophyBadge";
import { trophyColour, trophyIcon, trophyLabel } from "./trophy-icon";

// A compact row of a user's competition wins, for sitting beside their name in a list. Only wins
// earn a stamp - like the star on a shirt, it means "won this", not "did well at this".
//
// Trophies arrive already grouped and ordered by the server, so the first few are the ones worth
// showing; anything past `max` collapses into a count rather than crowding the name it sits next
// to. The full set is on the profile.
//
// `interactive` makes the stamp a button that opens the same set spelled out. The hover tooltip
// alone was desktop-only - on a phone the icons are unexplained decoration, with no way to ask
// what they are short of visiting the owner's profile.
export function TrophyStamp({ trophies, max = 3, interactive = false, ownerName }: {
    trophies: UserTrophy[];
    max?: number;
    interactive?: boolean;
    ownerName?: string;
}) {
    if (trophies.length === 0) {
        return null;
    }

    const shown = trophies.slice(0, max);
    const remaining = trophies.length - shown.length;

    const stamp = (
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
                        title={interactive ? undefined : label}
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

    if (!interactive) {
        return stamp;
    }

    return (
        // lazyMount/unmountOnExit for the same reason the reaction pills use them: one of these per
        // post adds up to a portalled subtree per message on a page where the user usually opens
        // none.
        <Popover.Root positioning={{ placement: "bottom-start" }} lazyMount unmountOnExit>
            <Popover.Trigger asChild>
                {/* Sized off its contents rather than the button recipe, so turning the stamp into
                    a control doesn't push the name line taller than it was. */}
                <Button
                    variant="plain"
                    h="auto"
                    minH={0}
                    minW={0}
                    px={1}
                    py={0.5}
                    mx={-1}
                    flexShrink={0}
                    rounded="sm"
                    _hover={{ bg: "bg.muted" }}
                    aria-label={ownerName ? `Show ${ownerName}'s trophies` : "Show trophies"}
                >
                    {stamp}
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content width="auto" minW="220px" maxW="300px">
                        <Popover.Arrow />
                        <Popover.Body p={3}>
                            <Stack gap={2} align="stretch">
                                <Text fontSize="sm" fontWeight="semibold">
                                    {ownerName ? `${ownerName}'s Trophies` : "Trophies"}
                                </Text>
                                {/* Every trophy, not just the ones that fit beside the name: the
                                    "+2" is exactly what someone is clicking to find out about. */}
                                <Stack gap={2} align="stretch" maxH="240px" overflowY="auto">
                                    {trophies.map((trophy) => (
                                        <TrophyBadge
                                            key={`${trophy.competitionSeriesID ?? trophy.name}-${trophy.years}`}
                                            trophy={trophy}
                                        />
                                    ))}
                                </Stack>
                            </Stack>
                        </Popover.Body>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>
    );
}
