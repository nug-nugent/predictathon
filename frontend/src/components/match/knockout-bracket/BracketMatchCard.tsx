import { Box, HStack, Image, Text, VisuallyHidden } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MatchPrediction } from "../../../services/prediction-service";
import { computeMatchStatus } from "../matchStatus";
import { QuickPredictPopover } from "../quick-predict/QuickPredictPopover";
import { liveMatchHref } from "../../../utils/liveMatches";
import { crestUrl } from "../../../utils/crestUrl";

/// The stripe along the top of a card. Every tie gets the same one the rest of the site puts on a
/// card - green in light, the standard turquoise in dark - rather than a colour of its own per
/// state: the footer already says in words whether a tie is predicted, and a bracket of sixteen
/// cards in three different colours read as decoration rather than information.
///
/// The final is the exception, and only because it is the one tie everybody looks for.
/// <param name="isFinal">Whether it is the competition's final.</param>
function accentFor(isFinal: boolean): string {
    return isFinal ? "bracket.finalAccent" : "card.accentStripe";
}

/// One tie in the bracket: the two teams, their goals once there are any, and what the tie is worth
/// to you along the bottom.
///
/// The card is deliberately not a second prediction form. A tie still open goes through the same
/// QuickPredictPopover the Home page uses, so entering a score from the bracket behaves exactly like
/// entering it anywhere else - same debounce, same deadline handling, same retry - and there is only
/// one implementation of score entry to keep right. Anything past its deadline is a link to wherever
/// the tie is best followed, as it is in every other list.
export function BracketMatchCard({ match, now, isFinal = false, onPredictionSaved }: {
    match: MatchPrediction;
    now: Date;
    /// Marks the one tie the whole bracket runs towards, which the design colours apart.
    isFinal?: boolean;
    onPredictionSaved?: () => void;
}) {
    const { status, minutesToPredict } = computeMatchStatus(match, now);
    const predicted = match.homeTeamGoals !== null && match.awayTeamGoals !== null;
    const isOpen = status === "Pre";

    const card = (
        <Box
            borderWidth="1px" borderColor="border.card" borderTopWidth="3px" borderTopColor={accentFor(isFinal)}
            borderRadius="6px" bg="surface.card" width="full" overflow="hidden"
            boxShadow="0 1px 2px rgba(20, 20, 30, 0.06)"
            _hover={{ bg: "bg.muted" }}
        >
            {isFinal && (
                <Box bg="bracket.finalAccent" color="bracket.finalAccentFg" fontSize="10px" fontWeight="extrabold"
                    letterSpacing="0.6px" textAlign="center" py="3px">
                    FINAL
                </Box>
            )}

            <Box px={3} pt={2.5} pb={2}>
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
            </Box>

            <BracketCardFooter match={match} predicted={predicted} isOpen={isOpen} isSettled={status === "Post"} />
        </Box>
    );

    if (isOpen) {
        return (
            <QuickPredictPopover match={match} minutesToPredict={minutesToPredict} onSaved={onPredictionSaved} showWeekLink={false}>
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

/// The strip along the bottom of a card: what you predicted, or that you still can.
function BracketCardFooter({ match, predicted, isOpen, isSettled }: {
    match: MatchPrediction;
    predicted: boolean;
    isOpen: boolean;
    isSettled: boolean;
}) {
    return (
        <Box borderTopWidth="1px" borderTopColor="border.hairline" px={3} py={1.5}
            display="flex" justifyContent={predicted ? "flex-start" : "flex-end"} alignItems="baseline" gap={2}>
            {!predicted && (
                isOpen
                    ? <Text fontSize="xs" fontWeight="bold" color="status.urgent">Predict</Text>
                    : <Text fontSize="xs" color="fg.muted">No prediction</Text>
            )}

            {predicted && (
                <>
                    {/* Muted, with the points beside it carrying the colour, following
                        YourPrediction on the Home and Live pages. The points are the part worth
                        looking at and they read on the functional points scale, so colouring the
                        scoreline too left two greens competing. Bold rather than plain because a
                        bracket card has no other line of its own to anchor on. */}
                    <Text fontSize="xs" fontWeight="bold" color="fg.muted">
                        You: {match.homeTeamGoals} - {match.awayTeamGoals}
                    </Text>
                    {/* Kept even though the design has no room for it: the points scale is functional,
                        and a settled tie whose prediction scored is when it matters most. */}
                    {isSettled && (
                        <Text fontSize="xs" fontWeight="bold" color={`points.${match.score ?? 0}`}>
                            {match.score ?? 0} {match.score === 1 ? "point" : "points"}
                        </Text>
                    )}
                </>
            )}
        </Box>
    );
}

/// One team's line within a card: the team, and its goals once there are any to show.
///
/// A decided team reads as its crest and its acronym at every width. A bracket column is narrow -
/// far narrower than a fixture list's row - and three letters beside a flag identify a team there
/// better than a name that has to wrap to fit. An undecided slot has neither, so it keeps its
/// placeholder in full: "Winner Group A" is the only thing that distinguishes it from fifteen
/// otherwise identical cards.
function BracketTeamLine({ teamId, name, shortName, acronym, image, goals }: {
    teamId: string | null;
    name: string | null;
    shortName: string;
    acronym: string | null;
    image: string | null;
    goals: number | null;
}) {
    const crest = crestUrl(image);
    const decided = teamId !== null;

    return (
        <HStack gap={1.5} minW="0" py="1px">
            {decided && (crest
                ? <Image src={crest} boxSize="16px" objectFit="contain" alt="" flexShrink={0} />
                : <Box boxSize="16px" flexShrink={0} />)}
            {/* whiteSpace is explicit because a tie that can still be predicted sits inside
                QuickPredictPopover's trigger, and Chakra's Button recipe sets white-space: nowrap on
                that, which would push a placeholder straight out of the card. */}
            <Box flex="1" minW="0" fontSize="13px" lineHeight="1.35" whiteSpace="normal" wordBreak="break-word"
                fontWeight={decided ? "medium" : "normal"} color={decided ? undefined : "fg.muted"}>
                {decided ? (
                    <>
                        {/* Announced in full: "ECU" is no use to a screen reader, which has all the
                            room in the world for "Ecuador". Mirrors what TeamLabel does elsewhere. */}
                        <Box as="span" aria-hidden="true">{acronym || shortName || name}</Box>
                        <VisuallyHidden>{name ?? shortName}</VisuallyHidden>
                    </>
                ) : (name ?? shortName)}
            </Box>
            {/* A fixed column rather than a shrink-to-fit one, so a tie's two figures line up under
                each other however long the names beside them are - and so a 10 sits under a 9. */}
            {goals !== null && (
                <Text fontSize="13px" fontWeight="bold" width="16px" textAlign="right" flexShrink={0}
                    fontVariantNumeric="tabular-nums">
                    {goals}
                </Text>
            )}
        </HStack>
    );
}
