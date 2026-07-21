import { useMemo } from "react";
import { Table, Text, VStack } from "@chakra-ui/react";
import type { MatchPredictionListItem } from "../../../services/prediction-service";
import { PredictedWinnerBar } from "../predicted-winner-bar/PredictedWinnerBar";

type PredictionsSummaryProps = {
    predictions: MatchPredictionListItem[];
    isPost: boolean;
};

type Summary = {
    topScores: string[];
    scoreCounts: Record<string, number>;
    pointCounts: Record<number, number>;
    homeWin: number;
    draw: number;
    awayWin: number;
};

function computeSummary(predictions: MatchPredictionListItem[]): Summary {
    let homeWin = 0;
    let draw = 0;
    let awayWin = 0;
    const scoreCounts: Record<string, number> = {};
    const scoresList: string[] = [];
    const pointCounts: Record<number, number> = { 0: 0, 1: 0, 2: 0, 3: 0 };

    for (const p of predictions) {
        const points = p.score ?? 0;
        pointCounts[points] = (pointCounts[points] ?? 0) + 1;

        if (p.homeTeamGoals === null || p.awayTeamGoals === null) {
            continue;
        }

        if (p.homeTeamGoals > p.awayTeamGoals) homeWin++;
        else if (p.homeTeamGoals === p.awayTeamGoals) draw++;
        else awayWin++;

        const key = `${p.homeTeamGoals} - ${p.awayTeamGoals}`;
        scoreCounts[key] = (scoreCounts[key] ?? 0) + 1;
        if (scoreCounts[key] === 1) {
            scoresList.push(key);
        }
    }

    // Descending by count; the stable sort keeps ties in first-seen order.
    const topScores = [...scoresList].sort((a, b) => scoreCounts[b] - scoreCounts[a]).slice(0, 4);

    return { topScores, scoreCounts, pointCounts, homeWin, draw, awayWin };
}

export function PredictionsSummary({ predictions, isPost }: PredictionsSummaryProps) {
    const summary = useMemo(() => computeSummary(predictions), [predictions]);

    if (summary.topScores.length === 0) {
        return null;
    }

    return (
        <VStack gap={3} py={2} px={3} align="stretch">
            <VStack gap={1}>
                <Text fontWeight="bold" fontSize="xs">PREDICTED WINNER</Text>
                <PredictedWinnerBar homeWin={summary.homeWin} draw={summary.draw} awayWin={summary.awayWin} />
            </VStack>

            <Table.Root size="sm" variant="line">
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader fontSize="0.7em">TOP PREDICTIONS</Table.ColumnHeader>
                        {summary.topScores.map((score) => (
                            <Table.ColumnHeader key={score} fontSize="0.7em" textAlign="center">{score}</Table.ColumnHeader>
                        ))}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    <Table.Row>
                        <Table.Cell fontSize="0.8em"># of users</Table.Cell>
                        {summary.topScores.map((score) => (
                            <Table.Cell key={score} fontSize="0.8em" textAlign="center">{summary.scoreCounts[score]}</Table.Cell>
                        ))}
                    </Table.Row>
                </Table.Body>
            </Table.Root>

            {isPost && (
                <Table.Root size="sm" variant="line">
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader fontSize="0.7em">POINTS WON</Table.ColumnHeader>
                            {[3, 2, 1, 0].map((points) => (
                                <Table.ColumnHeader key={points} fontSize="0.7em" textAlign="center" color={`points.${points}`}>{points}</Table.ColumnHeader>
                            ))}
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        <Table.Row>
                            <Table.Cell fontSize="0.8em"># of users</Table.Cell>
                            {[3, 2, 1, 0].map((points) => (
                                <Table.Cell key={points} fontSize="0.8em" textAlign="center">{summary.pointCounts[points] ?? 0}</Table.Cell>
                            ))}
                        </Table.Row>
                    </Table.Body>
                </Table.Root>
            )}
        </VStack>
    );
}
