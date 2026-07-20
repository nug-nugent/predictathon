import { Center, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCurrentCompetitionStatistics } from "../../../services/statistics-service";
import { PredictableTeamsTable } from "../../../components/statistics/PredictableTeamsTable";
import { PredictableMatchesTable } from "../../../components/statistics/PredictableMatchesTable";
import { BestPredictionsTable } from "../../../components/statistics/BestPredictionsTable";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function CurrentCompetitionStatisticsTab() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <CurrentCompetitionStats key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function CurrentCompetitionStats({ competitionId }: { competitionId: string }) {
    const { data: stats, error, reload } = useAsyncData(() => getCurrentCompetitionStatistics(competitionId), [competitionId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (stats === null) {
        return <LoadingSpinner />;
    }

    return (
        <VStack align="stretch" gap={6}>
            <BestPredictionsTable predictions={stats.bestPredictions} />
            <PredictableMatchesTable title="Most Predictable Matches" matches={stats.mostPredictableMatches} />
            <PredictableMatchesTable title="Least Predictable Matches" matches={stats.leastPredictableMatches} />
            <PredictableTeamsTable teams={stats.predictableTeams} />
        </VStack>
    );
}
