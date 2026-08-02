import { Button, HStack, Image, Popover, Portal, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";

type TeamNameProps = {
    /** Null for a not-yet-decided knockout placeholder - rendered as plain, non-clickable text. */
    teamId: string | null;
    name: string;
    /** Shown instead of `name` on mobile, where full names like "Brighton & Hove Albion" don't fit. */
    shortName: string;
    crest: string | undefined;
    /** Home teams show the crest after the name, away teams before - matches MatchRow's layout either side of the score inputs. */
    crestPosition: "before" | "after";
};

export function TeamName({ teamId, name, shortName, crest, crestPosition }: TeamNameProps) {
    const textAlign = crestPosition === "after" ? "right" : "left";

    const label = (
        <>
            {crestPosition === "before" && crest && <Image src={crest} boxSize="20px" alt="" flexShrink={0} />}
            <Text fontSize="0.9em" textAlign={textAlign} truncate minW="0" hideFrom="md">{shortName}</Text>
            <Text fontSize="0.9em" textAlign={textAlign} truncate minW="0" hideBelow="md">{name}</Text>
            {crestPosition === "after" && crest && <Image src={crest} boxSize="20px" alt="" flexShrink={0} />}
        </>
    );

    if (!teamId) {
        return <HStack gap={2} minW="0">{label}</HStack>;
    }

    return (
        <Popover.Root positioning={{ placement: "bottom" }}>
            <Popover.Trigger asChild>
                <Button variant="plain" size="sm" p={0} h="auto" minW="0" fontWeight="normal">
                    <HStack gap={2} minW="0">{label}</HStack>
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content width="auto">
                        <Popover.Arrow />
                        <Popover.Body p={2}>
                            <Button asChild size="xs" variant="ghost">
                                <RouterLink to={`/team/${teamId}`}>View team detail</RouterLink>
                            </Button>
                        </Popover.Body>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>
    );
}
