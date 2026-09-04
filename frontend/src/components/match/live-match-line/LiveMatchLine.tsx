import { Box, HStack, Image, Text } from "@chakra-ui/react";
import type { MatchPrediction } from "../../../services/prediction-service";
import type { MatchStatusValue } from "../matchStatus";
import { crestUrl } from "../../../utils/crestUrl";
import { TeamLabel } from "../../team/TeamLabel";

type LiveMatchLineProps = {
    match: MatchPrediction;
    status: MatchStatusValue;
    /** Larger type and crests for the focused match on the Live page. */
    size?: "sm" | "lg";
};

function kickoffTime(matchDateTime: string): string {
    return new Date(matchDateTime).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
}

// Both sides or neither: a half-populated scoreline would be worse than none.
function liveScore(match: MatchPrediction): string | null {
    if (match.liveHomeTeamGoals === null || match.liveAwayTeamGoals === null) {
        return null;
    }

    return `${match.liveHomeTeamGoals} - ${match.liveAwayTeamGoals}`;
}

/// The two teams either side of a centre slot holding whatever the match's stage calls for: its
/// kick-off time before it starts, the running score while it's in play, and the confirmed result
/// once it has one. A live match with no score yet shows "v" - either nobody has heard from the
/// provider about it, or it's genuinely goalless and the feed hasn't said so.
///
/// Deliberately non-interactive, unlike MatchRow's TeamName: every caller wraps the whole line in
/// a single link or button, and a clickable team name nested inside that would be both a confusing
/// target and invalid markup.
export function LiveMatchLine({ match, status, size = "sm" }: LiveMatchLineProps) {
    const crestSize = size === "lg" ? "32px" : "20px";
    const nameSize = size === "lg" ? "md" : "sm";
    const centreSize = size === "lg" ? "xl" : "sm";

    return (
        <HStack gap={{ base: 1, md: 3 }} minW="0" flex="1">
            <HStack gap={2} minW="0" flex="1" justify="flex-end">
                <Text fontSize={nameSize} truncate textAlign="right">
                    <TeamLabel name={match.homeTeam} shortName={match.homeTeamShortName} acronym={match.homeTeamAcronym} />
                </Text>
                <Crest image={match.homeTeamImage} boxSize={crestSize} />
            </HStack>

            <Box
                minW={size === "lg" ? "88px" : { base: "46px", md: "56px" }}
                textAlign="center"
                flexShrink={0}
                fontWeight="bold"
                fontSize={centreSize}
                color={status === "During" ? "status.live" : undefined}
            >
                {status === "Post" ? `${match.actualHomeTeamGoals} - ${match.actualAwayTeamGoals}`
                    : status === "During" ? liveScore(match) ?? "v"
                        : kickoffTime(match.matchDateTime)}
            </Box>

            <HStack gap={2} minW="0" flex="1">
                <Crest image={match.awayTeamImage} boxSize={crestSize} />
                <Text fontSize={nameSize} truncate>
                    <TeamLabel name={match.awayTeam} shortName={match.awayTeamShortName} acronym={match.awayTeamAcronym} />
                </Text>
            </HStack>
        </HStack>
    );
}

// A fixed-size placeholder keeps the two sides of the line symmetrical when only one team has a
// crest - common in knockout rounds, where one side is still a "Winner QF1" placeholder.
function Crest({ image, boxSize }: { image: string | null; boxSize: string }) {
    const url = crestUrl(image);

    if (!url) {
        return <Box boxSize={boxSize} flexShrink={0} />;
    }

    return <Image src={url} boxSize={boxSize} objectFit="contain" alt="" flexShrink={0} />;
}
