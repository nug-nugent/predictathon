import { VStack } from "@chakra-ui/react";
import { getAllTimeStatistics } from "../../../services/statistics-service";
import { LeaderboardTable } from "../../../components/statistics/LeaderboardTable";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function AllTimeStatisticsTab() {
    const { data: stats, error, reload } = useAsyncData(getAllTimeStatistics, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (stats === null) {
        return <LoadingSpinner />;
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
