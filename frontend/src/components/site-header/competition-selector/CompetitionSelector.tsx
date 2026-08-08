import { Button, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import { ChevronDown } from "lucide-react";
import { useState } from "react";
import { useCompetition } from "../../../hooks/useCompetition";
import { setDefaultCompetition } from "../../../services/competition-service";

const captionProps = {
    fontSize: "13px",
    lineHeight: "1.3",
} as const;

export function CompetitionSelector() {
    const [open, setOpen] = useState(false);
    const { competitions, currentCompetitionId, setCurrentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return null;
    }

    if (competitions.length === 0) {
        return <Text {...captionProps}>No competitions</Text>;
    }

    const current = competitions.find((c) => c.competitionID === currentCompetitionId);

    if (competitions.length === 1) {
        return <Text {...captionProps}>{current?.competitionName}</Text>;
    }

    // Switches immediately (sessionStorage, via setCurrentCompetitionId) so the UI never waits on
    // the network; persisting the choice as the DB-backed default happens in the background and
    // is best-effort - a failure there shouldn't stop the user switching competitions.
    const selectCompetition = (competitionId: string) => {
        setCurrentCompetitionId(competitionId);
        setDefaultCompetition(competitionId).catch((err) => {
            console.error("Failed to persist default competition", err);
        });
    };

    return (
        <Popover.Root open={open} onOpenChange={(e) => setOpen(e.open)}
            positioning={{ placement: "bottom-start" }}>
            <Popover.Trigger asChild>
                <Button
                    {...captionProps}
                    variant="plain"
                    p="0"
                    h="auto"
                    gap="1"
                    fontWeight="semibold"
                    color="inherit"
                    cursor="pointer"
                    _hover={{ textDecoration: "underline" }}
                >
                    {current?.competitionName} <ChevronDown size={14} />
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content width="100%" rounded="sm" p="0">
                        <Stack gap="0" onClick={() => setOpen(false)}>
                            {competitions.map((c) => (
                                <Button
                                    key={c.competitionID}
                                    size="sm"
                                    p="6"
                                    justifyContent="flex-start"
                                    variant="ghost"
                                    colorPalette="action"
                                    onClick={() => selectCompetition(c.competitionID)}
                                >
                                    {c.competitionName}
                                </Button>
                            ))}
                        </Stack>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>
    )
}
