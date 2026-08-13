import { useState } from "react";
import { Heading, Table, Text, VStack } from "@chakra-ui/react";
import type { PredictableMatch } from "../../services/statistics-service";
import { ScoreComparisonIcon } from "./ScoreComparisonIcon";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";

const PAGE_SIZE = 5;

export function PredictableMatchesTable({ title, matches }: { title: string; matches: PredictableMatch[] }) {
    const [page, setPage] = useState(1);
    const pageMatches = matches.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto" accent hoverLift>
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>{title}</Heading>
                <Table.Root size="sm" variant="line">
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Date / time</Table.ColumnHeader>
                            <Table.ColumnHeader>Match</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Result</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Your prediction</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Your score</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Average score</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center"></Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageMatches.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={7}>
                                    <Text color="fg.muted">No matches found</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : pageMatches.map((m) => (
                            <Table.Row key={m.matchID}>
                                <Table.Cell>{new Date(m.matchDateTime).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" })}</Table.Cell>
                                <Table.Cell>{m.homeTeamShortName} vs {m.awayTeamShortName}</Table.Cell>
                                <Table.Cell textAlign="center">{m.homeTeamGoals ?? "?"}-{m.awayTeamGoals ?? "?"}</Table.Cell>
                                <Table.Cell textAlign="center">{m.predictionHomeTeamGoals ?? "?"}-{m.predictionAwayTeamGoals ?? "?"}</Table.Cell>
                                <Table.Cell textAlign="center" color={`points.${m.yourPredictionScore}`} fontWeight="bold">{m.yourPredictionScore}</Table.Cell>
                                <Table.Cell textAlign="center">{m.averagePredictionScore.toFixed(2)}</Table.Cell>
                                <Table.Cell textAlign="center"><ScoreComparisonIcon yours={m.yourPredictionScore} average={m.averagePredictionScore} /></Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>

                <TablePagination count={matches.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </VStack>
        </Panel>
    );
}
