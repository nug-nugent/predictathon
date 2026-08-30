import { useState } from "react";
import { Box, Button, Center, Heading, HStack, Input, Link as ChakraLink, SimpleGrid, Table, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink, useParams } from "react-router";
import { useCompetition } from "../../../hooks/useCompetition";
import { useUser } from "../../../hooks/useUser";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { useMinuteTick } from "../../../hooks/useMinuteTick";
import { usePolling } from "../../../hooks/usePolling";
import { getTodaysMatches, saveLiveScore } from "../../../services/match-service";
import { getMatchPredictions, type MatchPrediction, type MatchPredictionListItem } from "../../../services/prediction-service";
import { computeMatchStatus, type MatchStatusValue } from "../../../components/match/matchStatus";
import { groupLiveMatches, hasLiveDayMatches, type LiveMatchGroups } from "../../../utils/liveMatches";
import { LiveMatchLine } from "../../../components/match/live-match-line/LiveMatchLine";
import { LiveMatchRow } from "../../../components/match/live-match-row/LiveMatchRow";
import { LiveBadge } from "../../../components/match/live-badge/LiveBadge";
import { LiveLeagueTable } from "../../../components/league/LiveLeagueTable";
import { PredictionsSummary } from "../../../components/match/predictions-summary/PredictionsSummary";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { PageHeading } from "../../../components/ui/page-heading";
import { Role } from "../../../constants/roles";
import { ApiError } from "../../../services/api";
import { parseDigit } from "../../../utils/parseDigit";
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

    // A list of one, whose only entry is the match already filling the rest of the page, tells the
    // reader nothing - so it only earns its column when there's somewhere else to go. Without it the
    // focused match takes the full width rather than leaving a third of the page empty.
    const showAllLiveMatches = groups.live.length > 1;

    return (
        <>
            <PageHeading mb={4}>Live</PageHeading>
            <SimpleGrid columns={{ base: 1, lg: 3 }} gap={4} alignItems="start">
                <VStack align="stretch" gap={4} gridColumn={{ lg: showAllLiveMatches ? "span 2" : "span 3" }}>
                    <FocusedMatch match={selected} status={status} />
                    <AdminLiveScore key={`admin-${selected.matchID}`} match={selected} status={status} onSaved={reload} />
                    <MatchPredictions key={selected.matchID} match={selected} status={status} />
                </VStack>

                {showAllLiveMatches && <AllLiveMatches matches={groups.live} selectedMatchId={selected.matchID} now={now} />}
            </SimpleGrid>

            {/* Below the grid rather than in it: the standings are about the competition, not about
                the match the rest of the page is showing. */}
            <Box mt={4}>
                <LiveLeagueTable competitionId={competitionId} />
            </Box>
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
                        <Heading size="md">No Matches Today</Heading>
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
                // A finished match goes to its own page rather than the Results list: you arrived
                // here looking at one match at a time, so that's what the next click should give you.
                <LiveMatchRow key={match.matchID} match={match} status={status} completedTarget="match" />
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

            {/* The feed is delayed, so say when the score we're showing was last actually current
                rather than letting it read as this second's score. */}
            {status === "During" && match.liveScoreUpdatedDateTime && (
                <Text textAlign="center" mt={1} color="fg.muted" fontSize="xs">
                    Score as at {new Date(match.liveScoreUpdatedDateTime).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
                </Text>
            )}

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

/// Lets a match administrator put a score in directly - to get ahead of the provider's delayed feed,
/// or to correct it. Unlike the feed an admin may lower a score, which is the only way to take back
/// a goal the provider reported and a VAR review then chalked off.
///
/// Renders nothing for everyone else, and nothing once the match is over: a finished match's score
/// is the Process Results page's business, and that one counts.
function AdminLiveScore({ match, status, onSaved }: { match: MatchPrediction; status: MatchStatusValue; onSaved: () => void }) {
    const { user } = useUser();

    // Seeded once from whatever the score was when this match came into focus, and deliberately not
    // resynced afterwards - a background poll landing mid-edit shouldn't rewrite what's being typed.
    const [homeInput, setHomeInput] = useState(match.liveHomeTeamGoals !== null ? String(match.liveHomeTeamGoals) : "");
    const [awayInput, setAwayInput] = useState(match.liveAwayTeamGoals !== null ? String(match.liveAwayTeamGoals) : "");
    const [saveState, setSaveState] = useState<"idle" | "saving" | "saved" | "error">("idle");
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    if (!user?.roles.includes(Role.MatchAdministrator) || status !== "During") {
        return null;
    }

    const canSave = homeInput !== "" && awayInput !== "" && saveState !== "saving";

    // Not async: an onClick handler's return value is ignored, so handing it a promise leaves
    // rejections unhandled. The state updates below are the only "result" this needs.
    const save = () => {
        setSaveState("saving");
        setErrorMessage(null);

        saveLiveScore(match.matchID, Number(homeInput), Number(awayInput))
            .then(() => {
                setSaveState("saved");
                onSaved();
            })
            .catch((error: unknown) => {
                setSaveState("error");
                setErrorMessage(error instanceof ApiError ? error.messages.join(" ") : "Couldn't save the score.");
            });
    };

    return (
        <Panel>
            <HStack justify="space-between" wrap="wrap" gap={3}>
                <VStack align="flex-start" gap={0}>
                    <Heading size="sm">Update the Live Score</Heading>
                    <Text fontSize="xs" color="fg.muted">Shown to everyone straight away. Doesn't score any predictions.</Text>
                </VStack>

                <HStack gap={2}>
                    <ScoreInput value={homeInput} onChange={setHomeInput} label="Home goals" />
                    <Text>-</Text>
                    <ScoreInput value={awayInput} onChange={setAwayInput} label="Away goals" />
                    <Button size="sm" colorPalette="action" onClick={save} disabled={!canSave} ml={2}>
                        {saveState === "saving" ? "Saving..." : "Save"}
                    </Button>
                </HStack>
            </HStack>

            {saveState === "saved" && <Text fontSize="sm" color="fg.success" mt={2}>Live score saved.</Text>}
            {saveState === "error" && <Text fontSize="sm" color="fg.error" mt={2}>{errorMessage}</Text>}
        </Panel>
    );
}

function ScoreInput({ value, onChange, label }: { value: string; onChange: (value: string) => void; label: string }) {
    return (
        <Input value={value} aria-label={label} autoComplete="off" textAlign="center" inputMode="numeric" pattern="[0-9]*"
            size="sm" width="44px" bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }}
            onChange={(event) => {
                // Same single-digit gate the Predictions page's score inputs use, so a pasted value
                // can't put something unsendable in the box.
                const parsed = parseDigit(event.target.value);
                if (parsed !== null) {
                    onChange(parsed);
                }
            }} />
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
            <Heading size="sm" mb={2}>All Predictions</Heading>

            <PredictionsSummary predictions={predictions} isPost={isPost} />

            <Table.Root size="sm" variant="line">
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader>Predictor</Table.ColumnHeader>
                        <Table.ColumnHeader textAlign="center">Prediction</Table.ColumnHeader>
                        {isPost
                            ? <Table.ColumnHeader textAlign="center">Points</Table.ColumnHeader>
                            : <Table.ColumnHeader textAlign="center">Projected score</Table.ColumnHeader>}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {predictions.length === 0 ? (
                        <Table.Row>
                            <Table.Cell colSpan={3}><Text color="fg.muted">No predictions found.</Text></Table.Cell>
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
            {isPost ? (
                <Table.Cell textAlign="center" fontWeight="bold" color={`points.${prediction.score ?? 0}`}>{prediction.score ?? 0}</Table.Cell>
            ) : (
                // Coloured on the same points scale the real scores use, so a live 3-pointer reads
                // the same way it will once the result is confirmed. A dash where there's nothing to
                // project from - no live score yet, or no prediction to project.
                <Table.Cell textAlign="center" fontWeight="bold"
                    color={prediction.projectedScore === null ? "fg.muted" : `points.${prediction.projectedScore}`}>
                    {prediction.projectedScore ?? "–"}
                </Table.Cell>
            )}
        </Table.Row>
    );
}

/// Every match in play, the focused one included and marked rather than left out. Keeping the list
/// complete and in one order means it doesn't reshuffle as you click between matches - the highlight
/// moves instead, so you can see where you are without having to re-find everything else.
///
/// Only rendered when more than one match is in play - see the caller.
function AllLiveMatches({ matches, selectedMatchId, now }: { matches: MatchPrediction[]; selectedMatchId: string; now: Date }) {
    return (
        <Panel accent>
            <HStack gap={2} mb={3}>
                <Heading size="sm">All Live Matches</Heading>
                <LiveBadge size="xs" />
            </HStack>

            <VStack align="stretch" gap={1}>
                {matches.map((match) => {
                    const isSelected = match.matchID === selectedMatchId;

                    return (
                        <ChakraLink key={match.matchID} asChild variant="plain" display="block" borderRadius="8px"
                            bg={isSelected ? "surface.highlightRow" : undefined}
                            _hover={{ bg: "bg.muted", textDecoration: "none" }}
                            _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus" }}>
                            {/* aria-current carries the same "you are here" the highlight does,
                                for anyone who can't see the highlight. */}
                            <RouterLink to={`/live/${match.matchID}`} aria-current={isSelected ? "true" : undefined}>
                                <VStack align="stretch" gap={0} px={2} py={2}>
                                    <LiveMatchLine match={match} status={computeMatchStatus(match, now).status} />
                                    {match.description && <Text fontSize="xs" color="fg.muted" textAlign="center">{match.description}</Text>}
                                </VStack>
                            </RouterLink>
                        </ChakraLink>
                    );
                })}
            </VStack>
        </Panel>
    );
}
