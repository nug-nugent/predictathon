import { Button, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import { ChevronDown } from "lucide-react";
import { useState } from "react";
import { useCompetition } from "../../../hooks/useCompetition";

const captionProps = {
    fontSize: { base: "xs", md: "xs" },
    lineHeight: "1",
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

    return (
        <Popover.Root open={open} onOpenChange={(e) => setOpen(e.open)}
            positioning={{ placement: "bottom-start" }}>
            <Popover.Trigger asChild>
                <Button {...captionProps} variant="plain" p="0" h="auto" fontWeight="normal" color="inherit">
                    {current?.competitionName} <ChevronDown size={12} />
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
                                    colorPalette="blue"
                                    onClick={() => setCurrentCompetitionId(c.competitionID)}
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
