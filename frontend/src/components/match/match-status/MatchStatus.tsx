import { useState } from "react";
import { Button, HStack, Popover, Portal, Spinner, Table, Text, VStack } from "@chakra-ui/react";
import { ChevronDown } from "lucide-react";
import type { MatchStatusValue, SaveState } from "../matchStatus";
import { getMatchPredictions, type MatchPredictionListItem } from "../../../services/prediction-service";
import { PredictionsSummary } from "../predictions-summary/PredictionsSummary";

type MatchStatusProps = {
    matchId: string;
    myUsername: string | undefined;
    status: MatchStatusValue;
    minutesToPredict: number;
    saveState: SaveState;
    actualHomeGoals: number | null;
    actualAwayGoals: number | null;
    score: number | null;
};

function formatCountdown(minutes: number): string {
    if (minutes < 60) {
        return `${minutes}m left`;
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
        const remainingMinutes = minutes % 60;
        return `${hours}h${remainingMinutes ? ` ${remainingMinutes}m` : ""} left`;
    }

    const days = Math.floor(hours / 24);
    const remainingHours = hours % 24;
    return `${days}d${remainingHours ? ` ${remainingHours}h` : ""} left`;
}

function getColor(status: MatchStatusValue, saveState: SaveState, minutesToPredict: number): string {
    if (status !== "Pre") return "fg.muted";
    if (saveState === "cutoff" || saveState === "error") return "fg.error";
    if (saveState === "saving") return "fg.info";
    if (saveState === "saved") return "fg.success";

    return minutesToPredict < 1440 ? "status.urgent" : "status.relaxed";
}

function getText(status: MatchStatusValue, saveState: SaveState, minutesToPredict: number): string {
    if (status !== "Pre") return "Awaiting result";
    if (saveState === "cutoff") return "Predictions are closed for this match";
    if (saveState === "error") return "Failed to save prediction!";
    if (saveState === "saving") return "Saving...";
    if (saveState === "saved") return "Prediction saved!";

    return formatCountdown(minutesToPredict);
}

export function MatchStatus({ matchId, myUsername, status, minutesToPredict, saveState, actualHomeGoals, actualAwayGoals, score }: MatchStatusProps) {
    const [open, setOpen] = useState(false);
    const [predictions, setPredictions] = useState<MatchPredictionListItem[] | null>(null);
    const [loadFailed, setLoadFailed] = useState(false);

    const handleOpenChange = (isOpen: boolean) => {
        setOpen(isOpen);

        if (isOpen && predictions === null && !loadFailed) {
            getMatchPredictions(matchId)
                .then(setPredictions)
                .catch(() => setLoadFailed(true));
        }
    };

    return (
        <VStack gap={1} align={{ base: "flex-start", md: "flex-end" }} width={{ base: "auto", md: "140px" }} flexShrink={0}>
            {status === "Post" ? (
                <VStack gap={0} width="full" fontSize="0.85em">
                    <Text>Result: {actualHomeGoals} - {actualAwayGoals}</Text>
                    <Text color={`points.${score ?? 0}`} fontWeight="bold">Points: {score ?? 0}</Text>
                </VStack>
            ) : (
                <Text fontSize="0.85em" color={getColor(status, saveState, minutesToPredict)} width="full" textAlign={{ base: "left", md: "right" }}>
                    {getText(status, saveState, minutesToPredict)}
                </Text>
            )}

            {status !== "Pre" && (
                <Popover.Root open={open} onOpenChange={(e) => handleOpenChange(e.open)} positioning={{ placement: "bottom-end" }}>
                    <Popover.Trigger asChild>
                        <Button size="2xs" variant="ghost">
                            All predictions <ChevronDown size={12} />
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
                                                {predictions.map((p) => (
                                                    <Table.Row key={p.userID}>
                                                        <Table.Cell fontSize="0.8em" fontWeight={p.username === myUsername ? "bold" : "normal"}>{p.username}</Table.Cell>
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
                                    </VStack>
                                )}
                            </Popover.Content>
                        </Popover.Positioner>
                    </Portal>
                </Popover.Root>
            )}
        </VStack>
    );
}
