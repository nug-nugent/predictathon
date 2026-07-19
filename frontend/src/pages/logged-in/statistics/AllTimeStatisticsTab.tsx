import { useEffect, useState } from "react";
import { Button, Center, Spinner, Text, VStack } from "@chakra-ui/react";
import { getAllTimeStatistics, type AllTimeStatistics } from "../../../services/statistics-service";
import { LeaderboardTable } from "../../../components/statistics/LeaderboardTable";
import { ApiError } from "../../../services/api";

export function AllTimeStatisticsTab() {
    const [stats, setStats] = useState<AllTimeStatistics | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        getAllTimeStatistics()
            .then(setStats)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, []);

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
        <VStack align="stretch" gap={6}>
            <LeaderboardTable
                title="Most Points"
                items={stats.highestAllTimeScores}
                columns={[{ header: "Total Points", render: (i) => i.totalScore }]}
            />
            <LeaderboardTable
                title="Best Average Per Prediction"
                items={stats.highestAverageScores}
                columns={[{ header: "Average Points", render: (i) => i.averageScore.toFixed(3) }]}
            />
            <LeaderboardTable
                title="Prediction - % Correct (1, 2, or 3 points)"
                items={stats.highestPercentageCorrect}
                columns={[{ header: "Percentage Correct", render: (i) => `${i.correctPredictionPercentage.toFixed(2)}%` }]}
            />
            <LeaderboardTable
                title="Competition Winners"
                items={stats.competitionWinners}
                columns={[
                    { header: "Wins", render: (i) => i.wins },
                    { header: "2nds", render: (i) => i.secondPlaces },
                    { header: "3rds", render: (i) => i.thirdPlaces },
                ]}
            />
            <LeaderboardTable
                title="Prolific Predictors - Most Predictions In Total"
                items={stats.mostPredictions}
                columns={[{ header: "Total Predictions", render: (i) => i.totalPredictions }]}
            />
        </VStack>
    );
}
