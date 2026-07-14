import { useState } from "react";
import { ButtonGroup, Heading, IconButton, Link, Pagination, Table, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type { BestPrediction } from "../../services/statistics-service";

const PAGE_SIZE = 10;

export function BestPredictionsTable({ predictions }: { predictions: BestPrediction[] }) {
    const [page, setPage] = useState(1);
    const pagePredictions = predictions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={1}>
            <Heading size="sm">Best Predictions</Heading>
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

            {predictions.length > PAGE_SIZE && (
                <Pagination.Root count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={(e) => setPage(e.page)}>
                    <ButtonGroup variant="ghost" size="sm" justifyContent="center">
                        <Pagination.PrevTrigger asChild>
                            <IconButton aria-label="Previous page"><ChevronLeft /></IconButton>
                        </Pagination.PrevTrigger>
                        <Pagination.Items
                            render={(p) => (
                                <IconButton variant={{ base: "ghost", _selected: "outline" }} onClick={() => setPage(p.value)}>
                                    {p.value}
                                </IconButton>
                            )}
                        />
                        <Pagination.NextTrigger asChild>
                            <IconButton aria-label="Next page"><ChevronRight /></IconButton>
                        </Pagination.NextTrigger>
                    </ButtonGroup>
                </Pagination.Root>
            )}
        </VStack>
    );
}
