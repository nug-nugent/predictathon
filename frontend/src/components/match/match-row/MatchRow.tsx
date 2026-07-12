import { Flex, HStack, Image, Input, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState, type ChangeEvent, type FocusEvent } from "react";
import { ApiError } from "../../../services/api";
import { savePrediction, type MatchPrediction } from "../../../services/prediction-service";
import { computeMatchStatus, type SaveState } from "../matchStatus";
import { MatchStatus } from "../match-status/MatchStatus";
import { useUser } from "../../../hooks/useUser";

type MatchRowProps = {
    match: MatchPrediction;
    now: Date;
    hasFocus: boolean;
    /** True for the first match in a date group, to add a bit of visual separation. */
    isFirstInGroup: boolean;
    onFocus: (matchId: string) => void;
    onSaved: (matchId: string) => void;
};

function crestUrl(image: string | null): string | undefined {
    return image ? `/team-crests/${image}` : undefined;
}

// Undecided future matches without a real team assigned should never render blank.
function teamName(name: string | null, shortName: string): string {
    return name || shortName || "TBC";
}

function parseDigit(raw: string): string | null {
    if (raw === "") return "";
    return /^[0-9]$/.test(raw) ? raw : null;
}

export function MatchRow({ match, now, hasFocus, isFirstInGroup, onFocus, onSaved }: MatchRowProps) {
    const { user } = useUser();
    const { status, minutesToPredict } = computeMatchStatus(match, now);

    const [homeInput, setHomeInput] = useState(match.homeTeamGoals !== null ? String(match.homeTeamGoals) : "");
    const [awayInput, setAwayInput] = useState(match.awayTeamGoals !== null ? String(match.awayTeamGoals) : "");
    const [saveState, setSaveState] = useState<SaveState>("idle");

    const homeInputRef = useRef<HTMLInputElement>(null);
    const awayInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (hasFocus && document.activeElement !== awayInputRef.current) {
            homeInputRef.current?.focus();
        }
    }, [hasFocus]);

    const locked = status !== "Pre" || saveState === "cutoff";

    const save = async (homeValue: string, awayValue: string, focusNext: boolean) => {
        if (homeValue === "" || awayValue === "") return;

        setSaveState("saving");
        try {
            await savePrediction(match.matchID, Number(homeValue), Number(awayValue));
            setSaveState("saved");
            onSaved(match.matchID);

            if (focusNext) {
                awayInputRef.current?.blur();
            }
        } catch (error) {
            setSaveState(error instanceof ApiError && error.status === 409 ? "cutoff" : "error");
        }
    };

    const onInputFocus = (event: FocusEvent<HTMLInputElement>) => {
        event.target.select();
        onFocus(match.matchID);
    };

    const onHomeChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) return;

        setHomeInput(value);
        save(value, awayInput, false);
        if (value !== "") {
            awayInputRef.current?.focus();
        }
    };

    const onAwayChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) return;

        setAwayInput(value);
        save(homeInput, value, true);
    };

    const displayHome = homeInput !== "" ? homeInput : (status !== "Pre" ? "L" : "");
    const displayAway = awayInput !== "" ? awayInput : (status !== "Pre" ? "L" : "");

    const homeCrest = crestUrl(match.homeTeamImage);
    const awayCrest = crestUrl(match.awayTeamImage);

    return (
        <Flex direction="column" borderTopWidth={isFirstInGroup ? "0" : "1px"} py={2} px={{ base: 2, md: 4 }} gap={2}>
            <Flex align="center" gap={{ base: 2, md: 4 }} wrap="wrap">
                <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                    <Text fontSize="0.9em" textAlign="right" truncate>{teamName(match.homeTeam, match.homeTeamShortName)}</Text>
                    {homeCrest && <Image src={homeCrest} boxSize="20px" alt="" />}
                </HStack>

                <HStack gap={1}>
                    <Input ref={homeInputRef} value={displayHome} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onHomeChange} />
                    <Text>-</Text>
                    <Input ref={awayInputRef} value={displayAway} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onAwayChange} />
                </HStack>

                <HStack flex="1" minW="0" gap={2}>
                    {awayCrest && <Image src={awayCrest} boxSize="20px" alt="" />}
                    <Text fontSize="0.9em" truncate>{teamName(match.awayTeam, match.awayTeamShortName)}</Text>
                </HStack>

                <MatchStatus matchId={match.matchID} myUsername={user?.name} status={status} minutesToPredict={minutesToPredict} saveState={saveState}
                    actualHomeGoals={match.actualHomeTeamGoals} actualAwayGoals={match.actualAwayTeamGoals} score={match.score} />
            </Flex>
        </Flex>
    );
}
