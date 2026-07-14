import { useEffect, useState } from "react";
import { Button, Center, SimpleGrid, Spinner, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCurrentCompetitionStatistics, type CurrentCompetitionStatistics } from "../../../services/statistics-service";
import { PredictableTeamsTable } from "../../../components/statistics/PredictableTeamsTable";
import { PredictableMatchesTable } from "../../../components/statistics/PredictableMatchesTable";
import { BestPredictionsTable } from "../../../components/statistics/BestPredictionsTable";
import { ApiError } from "../../../services/api";

export function CurrentCompetitionStatisticsTab() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
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
    const [stats, setStats] = useState<CurrentCompetitionStatistics | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        getCurrentCompetitionStatistics(competitionId)
            .then(setStats)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [competitionId]);

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    if (stats === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <SimpleGrid columns={{ base: 1, lg: 2 }} gap={8}>
            <VStack align="stretch" gap={6}>
                <BestPredictionsTable predictions={stats.bestPredictions} />
                <PredictableMatchesTable title="Most Predictable Matches" matches={stats.mostPredictableMatches} />
                <PredictableMatchesTable title="Least Predictable Matches" matches={stats.leastPredictableMatches} />
            </VStack>
            <VStack align="stretch" gap={6}>
                <PredictableTeamsTable teams={stats.predictableTeams} />
            </VStack>
        </SimpleGrid>
    );
}
