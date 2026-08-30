import { Box, Center, Heading, HStack, Link as ChakraLink, SimpleGrid, Table, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink, useParams } from "react-router";
import { useCompetition } from "../../../hooks/useCompetition";
import { useUser } from "../../../hooks/useUser";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { useMinuteTick } from "../../../hooks/useMinuteTick";
import { usePolling } from "../../../hooks/usePolling";
import { getTodaysMatches } from "../../../services/match-service";
import { getMatchPredictions, type MatchPrediction, type MatchPredictionListItem } from "../../../services/prediction-service";
import { computeMatchStatus, type MatchStatusValue } from "../../../components/match/matchStatus";
import { groupLiveMatches, hasLiveDayMatches, type LiveMatchGroups } from "../../../utils/liveMatches";
import { LiveMatchLine } from "../../../components/match/live-match-line/LiveMatchLine";
import { LiveMatchRow } from "../../../components/match/live-match-row/LiveMatchRow";
import { LiveBadge } from "../../../components/match/live-badge/LiveBadge";
import { PredictionsSummary } from "../../../components/match/predictions-summary/PredictionsSummary";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { PageHeading } from "../../../components/ui/page-heading";
import { Panel } from "../../../components/ui/panel";

// Faster than the Home card's minute: this is the page left open while a match plays, so the moment
// a result is confirmed it should turn into scores and points without a manual refresh.
const DAY_REFRESH_MS = 30000;
const PREDICTIONS_REFRESH_MS = 30000;

const NO_PREDICTION_ID = "00000000-0000-0000-0000-000000000000";

export function LivePage() {
    const { matchId } = useParams<{ matchId: string }>();
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <LiveDay key={currentCompetitionId} competitionId={currentCompetitionId} requestedMatchId={matchId} />;
}

function LiveDay({ competitionId, requestedMatchId }: { competitionId: string; requestedMatchId: string | undefined }) {
    const now = useMinuteTick();
    const { data: matches, error, reload } = useAsyncData(() => getTodaysMatches(competitionId), [competitionId]);

    usePolling(reload, DAY_REFRESH_MS);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (matches === null) {
        return <LoadingSpinner />;
    }

    const groups = groupLiveMatches(matches, now);

    // A match stays focused after its result lands (it moves out of `live` mid-visit), so watching
    // one through to full time doesn't yank the page out from under you. Arriving with no match
    // named - which is how the Home card's corner link gets here - or naming one that isn't on
    // today's card at all falls back to whichever match is in play.
    const selected = matches.find((m) => m.matchID === requestedMatchId) ?? groups.live[0] ?? null;

    if (selected === null) {
        return <NothingLive groups={groups} />;
    }

    const { status } = computeMatchStatus(selected, now);
    const others = groups.live.filter((m) => m.matchID !== selected.matchID);

    return (
        <>
            <PageHeading mb={4}>Live</PageHeading>
            <SimpleGrid columns={{ base: 1, lg: 3 }} gap={4} alignItems="start">
                <VStack align="stretch" gap={4} gridColumn={{ lg: "span 2" }}>
                    <FocusedMatch match={selected} status={status} />
                    <MatchPredictions key={selected.matchID} match={selected} status={status} />
                </VStack>

                <OtherLiveMatches matches={others} now={now} />
            </SimpleGrid>
        </>
    );
}

/// Shown when no match is in play: the rest of today's card if there is one - the groups say for
/// themselves that nothing is live, since there's no Live group among them - or somewhere to go
/// next on a day the competition isn't playing at all.
function NothingLive({ groups }: { groups: LiveMatchGroups }) {
    return (
        <>
            <PageHeading mb={4}>Live</PageHeading>
            <Panel accent maxW="640px" mx="auto">
                {hasLiveDayMatches(groups) ? (
                    <VStack align="stretch" gap={4}>
                        <TodayGroup title="Coming up" matches={groups.comingUp} status="Pre" />
                        <TodayGroup title="Completed" matches={groups.completed} status="Post" />
                    </VStack>
                ) : (
                    <VStack gap={3} py={4}>
                        <Heading size="md">No matches today</Heading>
                        <HStack gap={4}>
                            <ChakraLink asChild colorPalette="action" fontWeight="bold"><RouterLink to="/predictions">Predictions</RouterLink></ChakraLink>
                            <ChakraLink asChild colorPalette="action" fontWeight="bold"><RouterLink to="/results">Results</RouterLink></ChakraLink>
                        </HStack>
                    </VStack>
                )}
            </Panel>
        </>
    );
}

function TodayGroup({ title, matches, status }: { title: string; matches: MatchPrediction[]; status: MatchStatusValue }) {
    if (matches.length === 0) {
        return null;
    }

    return (
        <Box>
            <Text fontSize="xs" fontWeight="bold" letterSpacing="wide" textTransform="uppercase" color="fg.muted" mb={1} px={2}>
                {title}
            </Text>
            {matches.map((match) => (
                <LiveMatchRow key={match.matchID} match={match} status={status} />
            ))}
        </Box>
    );
}

function FocusedMatch({ match, status }: { match: MatchPrediction; status: MatchStatusValue }) {
    const predicted = match.homeTeamGoals !== null && match.awayTeamGoals !== null;

    return (
        <Panel accent>
            <HStack justify="space-between" mb={4}>
                <Text fontSize="sm" color="fg.muted">
                    {new Date(match.matchDateTime).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}
                </Text>
                {status === "During" ? <LiveBadge /> : <Text fontSize="xs" fontWeight="bold" color="fg.muted">FULL TIME</Text>}
            </HStack>

            <LiveMatchLine match={match} status={status} size="lg" />

            {match.description && <Text textAlign="center" mt={3} color="fg.muted" fontSize="sm">{match.description}</Text>}

            <HStack justify="center" gap={6} mt={4}>
                <VStack gap={0}>
                    <Text fontSize="xs" color="fg.muted" textTransform="uppercase" letterSpacing="wide">Your prediction</Text>
                    <Text fontWeight="bold">{predicted ? `${match.homeTeamGoals} - ${match.awayTeamGoals}` : "Not predicted"}</Text>
                </VStack>
                {status === "Post" && (
                    <VStack gap={0}>
                        <Text fontSize="xs" color="fg.muted" textTransform="uppercase" letterSpacing="wide">Points</Text>
                        <Text fontWeight="bold" color={`points.${match.score ?? 0}`}>{match.score ?? 0}</Text>
                    </VStack>
                )}
            </HStack>
        </Panel>
    );
}

/// Everyone's predictions for the focused match. The API only serves these from two minutes before
/// kick-off (PredictionService.GetMatchPredictionsAsync), which is exactly when a match becomes
/// live - so the only way to reach that refusal here is by typing a URL for a match that hasn't
/// started, and it is reported as the wait it is rather than as an error.
function MatchPredictions({ match, status }: { match: MatchPrediction; status: MatchStatusValue }) {
    const { user } = useUser();
    const { data: predictions, error, reload } = useAsyncData(() => getMatchPredictions(match.matchID), [match.matchID]);

    usePolling(reload, PREDICTIONS_REFRESH_MS);

    if (status === "Pre") {
        return (
            <Panel>
                <Text color="fg.muted">Everyone's predictions appear here once this match kicks off.</Text>
            </Panel>
        );
    }

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (predictions === null) {
        return <LoadingSpinner />;
    }

    const isPost = status === "Post";

    return (
        <Panel overflowX="auto">
            <Heading size="sm" mb={2}>All predictions</Heading>

            <PredictionsSummary predictions={predictions} isPost={isPost} />

            <Table.Root size="sm" variant="line">
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader>Predictor</Table.ColumnHeader>
                        <Table.ColumnHeader textAlign="center">Prediction</Table.ColumnHeader>
                        {isPost && <Table.ColumnHeader textAlign="center">Points</Table.ColumnHeader>}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {predictions.length === 0 ? (
                        <Table.Row>
                            <Table.Cell colSpan={isPost ? 3 : 2}><Text color="fg.muted">No predictions found.</Text></Table.Cell>
                        </Table.Row>
                    ) : predictions.map((p) => (
                        <PredictionRow key={p.userID} prediction={p} isPost={isPost} isMe={p.userID === user?.id} />
                    ))}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}

function PredictionRow({ prediction, isPost, isMe }: { prediction: MatchPredictionListItem; isPost: boolean; isMe: boolean }) {
    const madePrediction = prediction.predictionID !== NO_PREDICTION_ID;

    return (
        <Table.Row bg={isMe ? "surface.highlightRow" : undefined}>
            <Table.Cell fontWeight={isMe ? "bold" : "normal"}>
                <ChakraLink asChild variant="underline"><RouterLink to={`/profile/${prediction.userID}`}>{prediction.username}</RouterLink></ChakraLink>
            </Table.Cell>
            <Table.Cell textAlign="center">{madePrediction ? `${prediction.homeTeamGoals} - ${prediction.awayTeamGoals}` : "? - ?"}</Table.Cell>
            {isPost && (
                <Table.Cell textAlign="center" fontWeight="bold" color={`points.${prediction.score ?? 0}`}>{prediction.score ?? 0}</Table.Cell>
            )}
        </Table.Row>
    );
}

function OtherLiveMatches({ matches, now }: { matches: MatchPrediction[]; now: Date }) {
    return (
        <Panel accent>
            <HStack gap={2} mb={3}>
                <Heading size="sm">Also live</Heading>
                {matches.length > 0 && <LiveBadge size="xs" />}
            </HStack>

            {matches.length === 0 ? (
                <Text color="fg.muted" fontSize="sm">No other matches are in play right now.</Text>
            ) : (
                <VStack align="stretch" gap={1}>
                    {matches.map((match) => (
                        <ChakraLink key={match.matchID} asChild variant="plain" display="block" borderRadius="8px"
                            _hover={{ bg: "bg.muted", textDecoration: "none" }}
                            _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus" }}>
                            <RouterLink to={`/live/${match.matchID}`}>
                                <VStack align="stretch" gap={0} px={2} py={2}>
                                    <LiveMatchLine match={match} status={computeMatchStatus(match, now).status} />
                                    {match.description && <Text fontSize="xs" color="fg.muted" textAlign="center">{match.description}</Text>}
                                </VStack>
                            </RouterLink>
                        </ChakraLink>
                    ))}
                </VStack>
            )}
        </Panel>
    );
}
