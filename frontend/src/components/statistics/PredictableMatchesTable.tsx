import { useState } from "react";
import { ButtonGroup, Heading, IconButton, Pagination, Table, Text, VStack } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type { PredictableMatch } from "../../services/statistics-service";
import { ScoreComparisonIcon } from "./ScoreComparisonIcon";
import { Panel } from "../ui/panel";

const PAGE_SIZE = 5;

export function PredictableMatchesTable({ title, matches }: { title: string; matches: PredictableMatch[] }) {
    const [page, setPage] = useState(1);
    const pageMatches = matches.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto">
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

                {matches.length > PAGE_SIZE && (
                    <Pagination.Root count={matches.length} pageSize={PAGE_SIZE} page={page} onPageChange={(e) => setPage(e.page)}>
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
        </Panel>
    );
}
