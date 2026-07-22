import { useState } from "react";
import { Heading, HStack, Image, Table, Text } from "@chakra-ui/react";
import type { MatchPrediction } from "../../services/prediction-service";
import { crestUrl } from "../../utils/crestUrl";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";

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

                    <TablePagination count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
                </>
            )}
        </Panel>
    );
}
