import { useState } from "react";
import { Heading, Link, Table, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { BestPrediction } from "../../services/statistics-service";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";

const PAGE_SIZE = 10;

export function BestPredictionsTable({ predictions }: { predictions: BestPrediction[] }) {
    const [page, setPage] = useState(1);
    const pagePredictions = predictions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto">
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>Best Predictions</Heading>
                <Table.Root size="sm" variant="line">
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>User</Table.ColumnHeader>
                            <Table.ColumnHeader>Match</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Result</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Prediction</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Score</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Average score</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Difference</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pagePredictions.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={7}>
                                    <Text color="fg.muted">No matches found</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : pagePredictions.map((p) => (
                            <Table.Row key={`${p.matchID}-${p.userID}`}>
                                <Table.Cell>
                                    <Link asChild><RouterLink to={`/profile/${p.userID}`}>{p.username}</RouterLink></Link>
                                </Table.Cell>
                                <Table.Cell>{p.homeTeamShortName} vs {p.awayTeamShortName}</Table.Cell>
                                <Table.Cell textAlign="center">{p.homeTeamGoals ?? "?"}-{p.awayTeamGoals ?? "?"}</Table.Cell>
                                <Table.Cell textAlign="center">{p.predictionHomeTeamGoals ?? "?"}-{p.predictionAwayTeamGoals ?? "?"}</Table.Cell>
                                <Table.Cell textAlign="center" color={`points.${p.predictionScore}`} fontWeight="bold">{p.predictionScore}</Table.Cell>
                                <Table.Cell textAlign="center">{p.averagePredictionScore.toFixed(2)}</Table.Cell>
                                <Table.Cell textAlign="center">{p.scoreDifference.toFixed(2)}</Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>

                <TablePagination count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </VStack>
        </Panel>
    );
}
