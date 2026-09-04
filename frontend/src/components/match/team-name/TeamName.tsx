import { useState } from "react";
import { Button, HStack, Image, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { RecentResultsDialog } from "../../team/RecentResultsDialog";
import { TeamLabel, type TeamNames } from "../../team/TeamLabel";

type TeamNameProps = TeamNames & {
    /** Null for a not-yet-decided knockout placeholder - rendered as plain, non-clickable text. */
    teamId: string | null;
    crest: string | undefined;
    /** Home teams show the crest after the name, away teams before - matches MatchRow's layout either side of the score inputs. */
    crestPosition: "before" | "after";
};

export function TeamName({ teamId, name, shortName, acronym, crest, crestPosition }: TeamNameProps) {
    // The popover is controlled so picking "Recent Results" can close it as the dialog opens -
    // leaving it open behind a modal traps focus in a menu nobody can see.
    const [popoverOpen, setPopoverOpen] = useState(false);
    const [resultsOpen, setResultsOpen] = useState(false);
    const textAlign = crestPosition === "after" ? "right" : "left";

    const label = (
        <>
            {crestPosition === "before" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
            <Text fontSize="0.9em" textAlign={textAlign} truncate minW="0">
                <TeamLabel name={name} shortName={shortName} acronym={acronym} />
            </Text>
            {crestPosition === "after" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
        </>
    );

    if (!teamId) {
        return <HStack gap={2} minW="0">{label}</HStack>;
    }

    return (
        <>
            <Popover.Root open={popoverOpen} onOpenChange={(e) => setPopoverOpen(e.open)} positioning={{ placement: "bottom" }}>
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
                                <Stack gap={1} align="stretch">
                                    <Button size="xs" variant="ghost" justifyContent="flex-start"
                                        onClick={() => { setPopoverOpen(false); setResultsOpen(true); }}>
                                        Recent Results
                                    </Button>
                                    <Button asChild size="xs" variant="ghost" justifyContent="flex-start">
                                        <RouterLink to={`/team/${teamId}`}>View Team Detail</RouterLink>
                                    </Button>
                                </Stack>
                            </Popover.Body>
                        </Popover.Content>
                    </Popover.Positioner>
                </Portal>
            </Popover.Root>

            <RecentResultsDialog open={resultsOpen} onClose={() => setResultsOpen(false)} teamId={teamId} teamName={name || shortName || "TBC"} />
        </>
    );
}
