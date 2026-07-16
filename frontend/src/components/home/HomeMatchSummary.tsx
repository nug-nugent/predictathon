import { Box, Heading, HStack, Image, Table, Text } from "@chakra-ui/react";
import type { MatchPrediction } from "../../services/prediction-service";
import { crestUrl } from "../../utils/crestUrl";

// Compact, read-only match summary - used for the Home page's "Current/Last Matches" and "Future
// Matches" widgets. Unlike the full Predictions page's MatchList/MatchRow, there's no inline
// editing here; it's just a preview (click through to Predictions to actually predict).
export function HomeMatchSummary({ title, matches }: { title: string; matches: MatchPrediction[] }) {
    return (
        <Box borderWidth="1px" rounded="md" p={4}>
            <Heading size="md" mb={2}>{title}</Heading>
            {matches.length === 0 ? (
                <Text color="fg.muted">No matches found</Text>
            ) : (
                <Table.Root size="sm" variant="line">
                    <Table.Body>
                        {matches.map((m) => (
                            <Table.Row key={m.matchID}>
                                <Table.Cell>
                                    <HStack justify="flex-end" gap={2}>
                                        <Text>{m.homeTeamShortName}</Text>
                                        {crestUrl(m.homeTeamImage) && <Image src={crestUrl(m.homeTeamImage)} h="16px" alt="" />}
                                    </HStack>
                                </Table.Cell>
                                <Table.Cell textAlign="center" whiteSpace="nowrap">
                                    {m.actualHomeTeamGoals ?? ""}-{m.actualAwayTeamGoals ?? ""}
                                </Table.Cell>
                                <Table.Cell>
                                    <HStack gap={2}>
                                        {crestUrl(m.awayTeamImage) && <Image src={crestUrl(m.awayTeamImage)} h="16px" alt="" />}
                                        <Text>{m.awayTeamShortName}</Text>
                                    </HStack>
                                </Table.Cell>
                                <Table.Cell textAlign="center" fontSize="xs">
                                    {new Date(m.matchDateTime).toLocaleDateString(undefined, { day: "numeric", month: "short" })}
                                </Table.Cell>
                                <Table.Cell textAlign="center" whiteSpace="nowrap">
                                    {m.homeTeamGoals ?? "?"}-{m.awayTeamGoals ?? "?"}
                                </Table.Cell>
                                <Table.Cell textAlign="center" color={m.score !== null ? `points.${m.score}` : undefined} fontWeight="bold">
                                    {m.score ?? ""}
                                </Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>
            )}
        </Box>
    );
}
