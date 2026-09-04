import { Box, HStack, Image, Link as ChakraLink, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MatchPrediction } from "../../../services/prediction-service";
import type { MatchStatusValue } from "../matchStatus";
import { crestUrl } from "../../../utils/crestUrl";
import { TeamLabel, type TeamNames } from "../../team/TeamLabel";

type LiveMatchLineProps = {
    match: MatchPrediction;
    status: MatchStatusValue;
    /** Larger type and crests for the focused match on the Live page. */
    size?: "sm" | "lg";
    /**
     * Makes each team's crest and name a link to its team page. Off by default: most callers wrap
     * the whole line in a single link, and an anchor nested inside another is both a confusing
     * target and invalid markup - so only a caller that leaves the line unwrapped may turn it on.
     */
    linkTeams?: boolean;
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
export function LiveMatchLine({ match, status, size = "sm", linkTeams = false }: LiveMatchLineProps) {
    const crestSize = size === "lg" ? "32px" : "20px";
    const nameSize = size === "lg" ? "md" : "sm";
    const centreSize = size === "lg" ? "xl" : "sm";

    return (
        <HStack gap={{ base: 1, md: 3 }} minW="0" flex="1">
            <TeamSide teamId={match.homeTeamID} name={match.homeTeam} shortName={match.homeTeamShortName}
                acronym={match.homeTeamAcronym} image={match.homeTeamImage}
                crestSize={crestSize} nameSize={nameSize} crestPosition="after" linked={linkTeams} />

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

            <TeamSide teamId={match.awayTeamID} name={match.awayTeam} shortName={match.awayTeamShortName}
                acronym={match.awayTeamAcronym} image={match.awayTeamImage}
                crestSize={crestSize} nameSize={nameSize} crestPosition="before" linked={linkTeams} />
        </HStack>
    );
}

type TeamSideProps = TeamNames & {
    /** Null for a not-yet-decided knockout placeholder, which has no team page to link to. */
    teamId: string | null;
    image: string | null;
    crestSize: string;
    nameSize: string;
    /** Home teams show the crest after the name, away teams before - so the two face the score. */
    crestPosition: "before" | "after";
    linked: boolean;
};

/// One side of the line: a team's crest and name, optionally as a link to its team page.
function TeamSide({ teamId, name, shortName, acronym, image, crestSize, nameSize, crestPosition, linked }: TeamSideProps) {
    const justify = crestPosition === "after" ? "flex-end" : "flex-start";

    const content = (
        <>
            {crestPosition === "before" && <Crest image={image} boxSize={crestSize} />}
            <Text fontSize={nameSize} truncate minW="0" textAlign={crestPosition === "after" ? "right" : "left"}>
                <TeamLabel name={name} shortName={shortName} acronym={acronym} />
            </Text>
            {crestPosition === "after" && <Crest image={image} boxSize={crestSize} />}
        </>
    );

    // A knockout placeholder ("Winner QF1") has no team behind it, so nowhere to go.
    if (!linked || !teamId) {
        return <HStack gap={2} minW="0" flex="1" justify={justify}>{content}</HStack>;
    }

    return (
        <ChakraLink asChild variant="plain" minW="0" flex="1" borderRadius="6px"
            _hover={{ textDecoration: "underline" }}
            _focusVisible={{ outline: "2px solid", outlineColor: "input.borderFocus", outlineOffset: "2px" }}>
            <RouterLink to={`/team/${teamId}`}>
                <HStack gap={2} minW="0" width="full" justify={justify}>{content}</HStack>
            </RouterLink>
        </ChakraLink>
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
