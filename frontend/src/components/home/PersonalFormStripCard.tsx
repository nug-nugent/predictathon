import { Box, Heading, HStack, Text, VStack } from "@chakra-ui/react";
import { Activity } from "lucide-react";
import { useUser } from "../../hooks/useUser";
import { getUserRecentForm } from "../../services/league-service";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

const BAR_MAX_HEIGHT = 60;

// `week` is one of the ISO datetime strings returned by getCompetitionWeeks (see matchWeekStartDate
// in prediction-service.ts, which is parsed the same direct way) - not a bare "yyyy-MM-dd" DateOnly.
function formatWeekLabel(week: string): string {
    return new Date(week).toLocaleDateString(undefined, { day: "numeric", month: "short" });
}

export function PersonalFormStripCard({ competitionId }: { competitionId: string }) {
    const { user } = useUser();
    const userId = user?.id;

    const { data: form, error } = useAsyncData(async () => {
        if (!userId) return [];
        return getUserRecentForm(competitionId, userId);
    }, [competitionId, userId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (form === null) {
        return <LoadingSpinner />;
    }

    if (form.length === 0) {
        return (
            <Panel accent hoverLift>
                <HStack gap={2} mb={2}>
                    <IconChip icon={Activity} color="action.fg" />
                    <Heading size="md">Recent Form</Heading>
                </HStack>
                <Text color="fg.muted">No completed match weeks yet.</Text>
            </Panel>
        );
    }

    const maxPoints = Math.max(1, ...form.map((w) => w.points));

    return (
        <Panel p={3} accent hoverLift>
            <HStack gap={2} mb={2}>
                <IconChip icon={Activity} color="action.fg" />
                <Heading fontSize="17px" fontWeight="semibold">Recent Form</Heading>
            </HStack>
            <HStack align="flex-end" gap={3} justify="space-around">
                {form.map((week) => (
                    <VStack key={week.week} gap={1}>
                        <Text fontSize="xs" fontWeight="bold">{week.points}</Text>
                        <Box
                            width="24px"
                            height={`${Math.max(4, (week.points / maxPoints) * BAR_MAX_HEIGHT)}px`}
                            bg="blue.500"
                            borderRadius="sm"
                        />
                        <Text fontSize="xs" color="fg.muted">{formatWeekLabel(week.week)}</Text>
                    </VStack>
                ))}
            </HStack>
        </Panel>
    );
}
