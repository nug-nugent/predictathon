import { Box, HStack, Image, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MatchPrediction } from "../../../services/prediction-service";
import { computeMatchStatus } from "../matchStatus";
import { QuickPredictPopover } from "../quick-predict/QuickPredictPopover";
import { YourPrediction } from "../your-prediction/YourPrediction";
import { liveMatchHref } from "../../../utils/liveMatches";
import { crestUrl } from "../../../utils/crestUrl";
import { TeamLabel } from "../../team/TeamLabel";

/// One tie in the bracket: the two teams stacked with the score between them, and what the match is
/// worth to you underneath.
///
/// The card is deliberately not a second prediction form. A match still open goes through the same
/// QuickPredictPopover the Home page uses, so entering a score from the bracket behaves exactly like
/// entering it anywhere else - same debounce, same deadline handling, same retry - and there is only
/// one implementation of score entry to keep right. Anything past its deadline is a link to wherever
/// the match is best followed, as it is in every other list.
export function BracketMatchCard({ match, now, onPredictionSaved }: {
    match: MatchPrediction;
    now: Date;
    onPredictionSaved?: () => void;
}) {
    const { status, minutesToPredict } = computeMatchStatus(match, now);

    const content = (
        <VStack align="stretch" gap={0} width="full" px={2} py={1.5}>
            <BracketTeamLine
                teamId={match.homeTeamID} name={match.homeTeam} shortName={match.homeTeamShortName}
                acronym={match.homeTeamAcronym} image={match.homeTeamImage}
                goals={match.actualHomeTeamGoals ?? match.liveHomeTeamGoals}
            />
            <BracketTeamLine
                teamId={match.awayTeamID} name={match.awayTeam} shortName={match.awayTeamShortName}
                acronym={match.awayTeamAcronym} image={match.awayTeamImage}
                goals={match.actualAwayTeamGoals ?? match.liveAwayTeamGoals}
            />
            <Box borderTopWidth="1px" borderTopColor="border.hairline" mt={1} pt={1} textAlign="right">
                <YourPrediction match={match} status={status} />
            </Box>
        </VStack>
    );

    const card = (
        <Box
            borderWidth="1px" borderColor="border.card" borderRadius="8px" bg="surface.card"
            width="full" _hover={{ bg: "bg.muted" }}
        >
            {content}
        </Box>
    );

    if (status === "Pre") {
        return (
            <QuickPredictPopover match={match} minutesToPredict={minutesToPredict} onSaved={onPredictionSaved}>
                {card}
            </QuickPredictPopover>
        );
    }

    return (
        <RouterLink to={liveMatchHref(match, status)} style={{ display: "block", width: "100%" }}>
            {card}
        </RouterLink>
    );
}

/// One team's line within a card: crest, name, and its goals once there are any to show.
function BracketTeamLine({ teamId, name, shortName, acronym, image, goals }: {
    teamId: string | null;
    name: string | null;
    shortName: string;
    acronym: string | null;
    image: string | null;
    goals: number | null;
}) {
    const crest = crestUrl(image);

    return (
        <HStack gap={1.5} minW="0" py={0.5}>
            {crest
                ? <Image src={crest} boxSize="14px" objectFit="contain" alt="" flexShrink={0} />
                : <Box boxSize="14px" flexShrink={0} />}
            {/* Wraps rather than truncates: a card in an unresolved round says "Winner Group A",
                and clipped to one narrow line every one of them reads "Winner ..." instead.
                whiteSpace is set explicitly because a card that can still be predicted is wrapped in
                QuickPredictPopover's trigger, and Chakra's Button recipe puts white-space: nowrap on
                that - which would otherwise push these names straight out of the card. */}
            <Box flex="1" minW="0" fontSize="xs" lineHeight="1.25" whiteSpace="normal" wordBreak="break-word"
                color={teamId === null ? "fg.muted" : undefined}>
                {/* An undecided slot names itself in full at every width. TeamLabel would drop to the
                    acronym tier on a phone, and a placeholder has no acronym behind it - so a whole
                    unplayed bracket came out as sixteen identical cards reading "TBC". A real team
                    still abbreviates: there the crest and the round say which tie you are looking at. */}
                {teamId === null
                    ? (name ?? shortName)
                    : <TeamLabel name={name} shortName={shortName} acronym={acronym} />}
            </Box>
            <Text fontSize="xs" fontWeight="bold" minW="10px" textAlign="right">
                {goals ?? ""}
            </Text>
        </HStack>
    );
}
