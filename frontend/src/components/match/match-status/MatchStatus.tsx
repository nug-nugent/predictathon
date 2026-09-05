import { useState } from "react";
import { Box, Button, HStack, Popover, Portal, Spinner, Stack, Table, Text, VStack } from "@chakra-ui/react";
import { ChevronDown } from "lucide-react";
import { predictionStatusColor, predictionStatusText, type MatchStatusValue, type SaveState } from "../matchStatus";
import { getMatchPredictions, type MatchPredictionListItem } from "../../../services/prediction-service";
import { PredictionsSummary } from "../predictions-summary/PredictionsSummary";
import { TablePagination } from "../../ui/table-pagination";

const PAGE_SIZE = 20;

type MatchStatusProps = {
    matchId: string;
    myUserId: string | undefined;
    status: MatchStatusValue;
    minutesToPredict: number;
    saveState: SaveState;
    actualHomeGoals: number | null;
    actualAwayGoals: number | null;
    score: number | null;
};

export function MatchStatus({ matchId, myUserId, status, minutesToPredict, saveState, actualHomeGoals, actualAwayGoals, score }: MatchStatusProps) {
    const [open, setOpen] = useState(false);
    const [predictions, setPredictions] = useState<MatchPredictionListItem[] | null>(null);
    const [loadFailed, setLoadFailed] = useState(false);
    const [page, setPage] = useState(1);

    // Refetches on every open (not just the first): a match can move During -> Post while this
    // row stays mounted, and the cached list's scores would be stale. Already-loaded data stays
    // visible while the silent refresh is in flight, and a previous failure doesn't block retrying.
    const handleOpenChange = (isOpen: boolean) => {
        setOpen(isOpen);

        if (isOpen) {
            setPage(1);
            getMatchPredictions(matchId)
                .then((loaded) => {
                    setPredictions(loaded);
                    setLoadFailed(false);
                })
                .catch(() => setLoadFailed(true));
        }
    };

    // Live matches have no fixed-width results block, so on mobile the status text and the
    // "All predictions" trigger can sit side by side in one row instead of costing two.
    const isDuring = status === "During";

    return (
        <Stack gap={1} direction={{ base: isDuring ? "row" : "column", md: "column" }}
            align={{ base: "center", md: "flex-end" }} justify="center" width={{ base: "full", md: "140px" }} flexShrink={0} pt={{ base: 1, md: 0 }}>
            {status === "Post" ? (
                <VStack gap={0} width="full" fontSize="0.85em">
                    <Text textAlign={{ base: "center", md: "right" }} width="full">Result: {actualHomeGoals} - {actualAwayGoals}</Text>
                    <Text textAlign={{ base: "center", md: "right" }} width="full" color={`points.${score ?? 0}`} fontWeight="bold">Points: {score ?? 0}</Text>
                </VStack>
            ) : (
                <Text fontSize="0.85em" color={predictionStatusColor(status, saveState, minutesToPredict)} width={{ base: isDuring ? "auto" : "full", md: "full" }} textAlign={{ base: "center", md: "right" }}>
                    {predictionStatusText(status, saveState, minutesToPredict)}
                </Text>
            )}

            {status !== "Pre" && (
                <Popover.Root open={open} onOpenChange={(e) => handleOpenChange(e.open)} positioning={{ placement: "bottom-end" }}>
                    <Popover.Trigger asChild>
                        <Button size="2xs" variant="ghost">
                            All Predictions <ChevronDown size={12} />
                        </Button>
                    </Popover.Trigger>
                    <Portal>
                        <Popover.Positioner>
                            <Popover.Content minW="280px" maxW="360px">
                                {predictions === null ? (
                                    loadFailed ? (
                                        <Text fontSize="sm" p={3}>Failed to load predictions.</Text>
                                    ) : (
                                        <HStack justify="center" p={3}>
                                            <Spinner size="sm" />
                                        </HStack>
                                    )
                                ) : (
                                    <VStack gap={0} align="stretch">
                                        <PredictionsSummary predictions={predictions} isPost={status === "Post"} />

                                        <Table.Root size="sm" variant="line">
                                            <Table.Header>
                                                <Table.Row>
                                                    <Table.ColumnHeader fontSize="0.7em">User</Table.ColumnHeader>
                                                    <Table.ColumnHeader fontSize="0.7em" textAlign="center">Prediction</Table.ColumnHeader>
                                                    {status === "Post" && <Table.ColumnHeader fontSize="0.7em" textAlign="center">Points</Table.ColumnHeader>}
                                                </Table.Row>
                                            </Table.Header>
                                            <Table.Body>
                                                {predictions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE).map((p) => (
                                                    <Table.Row key={p.userID}>
                                                        <Table.Cell fontSize="0.8em" fontWeight={p.userID === myUserId ? "bold" : "normal"}>{p.username}</Table.Cell>
                                                        <Table.Cell fontSize="0.8em" textAlign="center">
                                                            {p.homeTeamGoals ?? "L"} - {p.awayTeamGoals ?? "L"}
                                                        </Table.Cell>
                                                        {status === "Post" && (
                                                            <Table.Cell fontSize="0.8em" textAlign="center" color={`points.${p.score ?? 0}`}>{p.score ?? 0}</Table.Cell>
                                                        )}
                                                    </Table.Row>
                                                ))}
                                            </Table.Body>
                                        </Table.Root>
                                        <Box pb={2}>
                                            <TablePagination count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
                                        </Box>
                                    </VStack>
                                )}
                            </Popover.Content>
                        </Popover.Positioner>
                    </Portal>
                </Popover.Root>
            )}
        </Stack>
    );
}
