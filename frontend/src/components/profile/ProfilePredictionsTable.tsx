import { useState } from "react";
import { ButtonGroup, Heading, HStack, IconButton, Image, Pagination, Table, Text } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type { MatchPrediction } from "../../services/prediction-service";
import { crestUrl } from "../../utils/crestUrl";
import { Panel } from "../ui/panel";

const PAGE_SIZE = 10;

export function ProfilePredictionsTable({ predictions }: { predictions: MatchPrediction[] }) {
    const [page, setPage] = useState(1);
    const pagePredictions = predictions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto">
            <Heading size="md" mb={2}>Predictions</Heading>
            {predictions.length === 0 ? (
                <Text color="fg.muted">No predictions found</Text>
            ) : (
                <>
                    <Table.Root size="sm" variant="line">
                        <Table.Body>
                            {pagePredictions.map((m) => (
                                <Table.Row key={m.matchID}>
                                    <Table.Cell>
                                        <HStack justify="flex-end" gap={2}>
                                            <Text>{m.homeTeamShortName}</Text>
                                            {crestUrl(m.homeTeamImage) && <Image src={crestUrl(m.homeTeamImage)} h="16px" alt="" />}
                                        </HStack>
                                    </Table.Cell>
                                    <Table.Cell textAlign="center" whiteSpace="nowrap">
                                        {m.homeTeamGoals ?? "?"}-{m.awayTeamGoals ?? "?"}
                                    </Table.Cell>
                                    <Table.Cell>
                                        <HStack gap={2}>
                                            {crestUrl(m.awayTeamImage) && <Image src={crestUrl(m.awayTeamImage)} h="16px" alt="" />}
                                            <Text>{m.awayTeamShortName}</Text>
                                        </HStack>
                                    </Table.Cell>
                                    <Table.Cell textAlign="center" color={m.score !== null ? `points.${m.score}` : undefined} fontWeight="bold">
                                        {m.score ?? ""}
                                    </Table.Cell>
                                </Table.Row>
                            ))}
                        </Table.Body>
                    </Table.Root>

                    {predictions.length > PAGE_SIZE && (
                        <Pagination.Root count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={(e) => setPage(e.page)}>
                            <ButtonGroup variant="ghost" size="sm" justifyContent="center" mt={2}>
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
                </>
            )}
        </Panel>
    );
}
