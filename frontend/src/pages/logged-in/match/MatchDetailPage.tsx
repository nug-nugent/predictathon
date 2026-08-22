import { useState } from "react";
import { useParams } from "react-router";
import { Link as RouterLink } from "react-router";
import { Center, Heading, HStack, Image, Link as ChakraLink, SimpleGrid, Table, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getMatchDetail } from "../../../services/match-service";
import { getMatchPredictions, type MatchPredictionListItem } from "../../../services/prediction-service";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { PageHeading } from "../../../components/ui/page-heading";
import { Panel } from "../../../components/ui/panel";
import { TablePagination } from "../../../components/ui/table-pagination";
import { crestUrl } from "../../../utils/crestUrl";

const NO_PREDICTION_ID = "00000000-0000-0000-0000-000000000000";
const PAGE_SIZE = 20;

export function MatchDetailPage() {
    const { matchId } = useParams<{ matchId: string }>();
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!matchId || !currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <MatchDetail key={`${currentCompetitionId}-${matchId}`} competitionId={currentCompetitionId} matchId={matchId} />;
}

function MatchDetail({ competitionId, matchId }: { competitionId: string; matchId: string }) {
    const { data, error, reload } = useAsyncData(async () => {
        const [match, predictions] = await Promise.all([
            getMatchDetail(competitionId, matchId),
            getMatchPredictions(matchId),
        ]);
        return { match, predictions };
    }, [competitionId, matchId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const { match, predictions } = data;

    return <MatchDetailContent match={match} predictions={predictions} />;
}

function MatchDetailContent({ match, predictions }: {
    match: Awaited<ReturnType<typeof getMatchDetail>>;
    predictions: MatchPredictionListItem[];
}) {
    const [page, setPage] = useState(1);
    const pagePredictions = predictions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <>
            <PageHeading mb={4}>Match</PageHeading>
            <SimpleGrid columns={{ base: 1, lg: 2 }} gap={8} alignItems="start">
                <Panel accent hoverLift>
                    <VStack gap={1} mb={4}>
                        <Text color="fg.muted" fontSize="sm">
                            {new Date(match.matchDateTime).toLocaleString(undefined, { dateStyle: "full", timeStyle: "short" })}
                        </Text>
                        {match.knockout && <Text color="fg.muted" fontSize="xs">*After 90 minutes</Text>}
                    </VStack>

                    <HStack justify="space-around" mb={4}>
                        <TeamHeader teamId={match.homeTeamID} name={match.homeTeam} crest={crestUrl(match.homeTeamImage)} />
                        <Heading size="2xl">{match.homeTeamGoals ?? "?"} - {match.awayTeamGoals ?? "?"}</Heading>
                        <TeamHeader teamId={match.awayTeamID} name={match.awayTeam} crest={crestUrl(match.awayTeamImage)} />
                    </HStack>

                    {match.description && <Text textAlign="center" mb={4} color="fg.muted">{match.description}</Text>}

                    <Table.Root size="sm" variant="line">
                        <Table.Body>
                            <Table.Row>
                                <Table.Cell>Your prediction</Table.Cell>
                                <Table.Cell textAlign="end">{match.predictionHomeTeamGoals ?? "?"} - {match.predictionAwayTeamGoals ?? "?"}</Table.Cell>
                            </Table.Row>
                            <Table.Row>
                                <Table.Cell>Your score</Table.Cell>
                                <Table.Cell textAlign="end" color={`points.${match.yourPredictionScore}`} fontWeight="bold">{match.yourPredictionScore}</Table.Cell>
                            </Table.Row>
                            <Table.Row>
                                <Table.Cell>Average score</Table.Cell>
                                <Table.Cell textAlign="end">{match.averagePredictionScore.toFixed(2)}</Table.Cell>
                            </Table.Row>
                        </Table.Body>
                    </Table.Root>
                </Panel>

                <Panel accent hoverLift>
                    <Heading size="sm" mb={2}>All predictions</Heading>
                    <Table.Root size="sm" variant="line">
                        <Table.Header>
                            <Table.Row>
                                <Table.ColumnHeader>Predictor</Table.ColumnHeader>
                                <Table.ColumnHeader textAlign="center">Prediction</Table.ColumnHeader>
                                <Table.ColumnHeader textAlign="center">Score</Table.ColumnHeader>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {predictions.length === 0 ? (
                                <Table.Row>
                                    <Table.Cell colSpan={3}>
                                        <Text color="fg.muted">No predictions found</Text>
                                    </Table.Cell>
                                </Table.Row>
                            ) : pagePredictions.map((p) => <PredictionRow key={p.userID} prediction={p} />)}
                        </Table.Body>
                    </Table.Root>
                    <TablePagination count={predictions.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
                </Panel>
            </SimpleGrid>
        </>
    );
}

function TeamHeader({ teamId, name, crest }: { teamId: string | null; name: string | null; crest: string | undefined }) {
    const content = (
        <VStack gap={2} flex={1}>
            {crest && <Image src={crest} boxSize="48px" alt="" />}
            <Heading size="sm" textAlign="center">{name}</Heading>
        </VStack>
    );

    if (!teamId) {
        return content;
    }

    return (
        <ChakraLink asChild variant="plain" _hover={{ opacity: 0.75 }}>
            <RouterLink to={`/team/${teamId}`}>{content}</RouterLink>
        </ChakraLink>
    );
}

function PredictionRow({ prediction }: { prediction: MatchPredictionListItem }) {
    const madePrediction = prediction.predictionID !== NO_PREDICTION_ID;

    return (
        <Table.Row>
            <Table.Cell><ChakraLink asChild variant="underline"><RouterLink to={`/profile/${prediction.userID}`}>{prediction.username}</RouterLink></ChakraLink></Table.Cell>
            <Table.Cell textAlign="center">{madePrediction ? `${prediction.homeTeamGoals} - ${prediction.awayTeamGoals}` : "? - ?"}</Table.Cell>
            <Table.Cell textAlign="center" color={prediction.score !== null ? `points.${prediction.score}` : undefined} fontWeight="bold">{prediction.score ?? "-"}</Table.Cell>
        </Table.Row>
    );
}
