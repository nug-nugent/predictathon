import { Badge, Button, Dialog, Flex, HStack, Image, Portal, Stack, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { useCompetition } from "../../hooks/useCompetition";
import { useAsyncData } from "../../hooks/useAsyncData";
import { getTeamRecentResults, type TeamRecentResult } from "../../services/team-service";
import { crestUrl } from "../../utils/crestUrl";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { TeamLabel, type TeamNames } from "./TeamLabel";

const RESULT_COUNT = 6;

// Win/draw/loss colours are deliberately Chakra's own palettes rather than the `points` scale -
// that scale means prediction accuracy, and reusing it here would blur what a colour tells you.
const outcomePalette: Record<TeamRecentResult["outcome"], string> = {
    Win: "green",
    Draw: "gray",
    Loss: "red",
};

/// A team's last few results in the current competition, opened from the popover on any team name
/// (predictions, fixtures, the live page). Deliberately read-only and self-contained - the full
/// picture lives on the Team Detail page, linked from the footer.
export function RecentResultsDialog({ open, onClose, teamId, teamName }: {
    open: boolean;
    onClose: () => void;
    teamId: string;
    teamName: string;
}) {
    return (
        // lazyMount/unmountOnExit for the same reason as the popover that opens this - one of these
        // hangs off every team name, so a page listing a week's matches mounts dozens of them. The
        // fetch below was already deferred; this defers the dialog's own markup too.
        <Dialog.Root lazyMount unmountOnExit open={open} onOpenChange={(e) => { if (!e.open) onClose(); }} size="md">
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>{teamName} - Recent Results</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            {open && <RecentResultsList teamId={teamId} />}
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button asChild variant="ghost" onClick={onClose}>
                                <RouterLink to={`/team/${teamId}`}>View Team Detail</RouterLink>
                            </Button>
                            <Button variant="ghost" onClick={onClose}>Close</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

/// Split out so the fetch only starts when the dialog is actually opened, and starts afresh each
/// time it reopens (scores may have been entered since it was last looked at).
function RecentResultsList({ teamId }: { teamId: string }) {
    const { currentCompetitionId } = useCompetition();
    const { data: results, error, reload } = useAsyncData(
        () => currentCompetitionId ? getTeamRecentResults(currentCompetitionId, teamId, RESULT_COUNT) : Promise.resolve([]),
        [currentCompetitionId, teamId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (results === null) {
        return <LoadingSpinner />;
    }

    if (results.length === 0) {
        return <Text color="fg.muted">No results yet in this competition</Text>;
    }

    return (
        <Stack gap={0}>
            {results.map((result, index) => (
                <ResultRow key={result.matchID} result={result} teamId={teamId} isFirst={index === 0} />
            ))}
        </Stack>
    );
}

/// One played match, laid out like MatchRow - home team and crest, score, away crest and team -
/// with the team whose results these are picked out in bold and a win/draw/loss marker alongside.
function ResultRow({ result, teamId, isFirst }: { result: TeamRecentResult; teamId: string; isFirst: boolean }) {
    const kickoff = new Date(result.matchDateTime);

    return (
        <Flex direction="column" gap={1} py={2} borderTopWidth={isFirst ? "0" : "1px"} borderTopColor="border.hairline">
            <Flex align="center" gap={{ base: 2, md: 3 }}>
                <Badge colorPalette={outcomePalette[result.outcome]} size="sm" flexShrink={0} minW="52px" justifyContent="center">
                    {result.outcome}
                </Badge>

                <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                    <ResultTeamName name={result.homeTeam} shortName={result.homeTeamShortName} acronym={result.homeTeamAcronym}
                        image={result.homeTeamImage} isThisTeam={result.homeTeamID === teamId} crestPosition="after" />
                </HStack>

                <VStack gap={0} flexShrink={0} minW="52px">
                    <Text fontSize="0.9em" fontWeight="bold">{result.homeTeamGoals} - {result.awayTeamGoals}</Text>
                    <Text fontSize="0.7em" color="fg.muted">
                        {kickoff.toLocaleDateString(undefined, { day: "numeric", month: "short" })}
                    </Text>
                </VStack>

                <HStack flex="1" minW="0" gap={2}>
                    <ResultTeamName name={result.awayTeam} shortName={result.awayTeamShortName} acronym={result.awayTeamAcronym}
                        image={result.awayTeamImage} isThisTeam={result.awayTeamID === teamId} crestPosition="before" />
                </HStack>
            </Flex>

            {result.description && (
                <Text fontSize="0.75em" color="fg.muted" textAlign="center">{result.description}</Text>
            )}
        </Flex>
    );
}

/// Plain (non-clickable) team name and crest - a TeamName here would nest another popover inside
/// the dialog this one opened. Same width-dependent naming as TeamName, via the same TeamLabel.
function ResultTeamName({ name, shortName, acronym, image, isThisTeam, crestPosition }: TeamNames & {
    image: string | null;
    isThisTeam: boolean;
    crestPosition: "before" | "after";
}) {
    const crest = crestUrl(image);
    const textAlign = crestPosition === "after" ? "right" : "left";
    const fontWeight = isThisTeam ? "bold" : "normal";

    return (
        <HStack gap={2} minW="0">
            {crestPosition === "before" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
            <Text fontSize="0.9em" fontWeight={fontWeight} textAlign={textAlign} truncate minW="0">
                <TeamLabel name={name} shortName={shortName} acronym={acronym} />
            </Text>
            {crestPosition === "after" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
        </HStack>
    );
}
