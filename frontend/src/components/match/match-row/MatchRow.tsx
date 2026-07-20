import { Flex, HStack, Input, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState, type ChangeEvent, type FocusEvent } from "react";
import { ApiError } from "../../../services/api";
import { savePrediction, type MatchPrediction } from "../../../services/prediction-service";
import { computeMatchStatus, type SaveState } from "../matchStatus";
import { MatchStatus } from "../match-status/MatchStatus";
import { TeamName } from "../team-name/TeamName";
import { useUser } from "../../../hooks/useUser";
import { crestUrl } from "../../../utils/crestUrl";
import { parseDigit } from "../../../utils/parseDigit";

type MatchRowProps = {
    match: MatchPrediction;
    now: Date;
    hasFocus: boolean;
    /** True for the first match in a date group, to add a bit of visual separation. */
    isFirstInGroup: boolean;
    onFocus: (matchId: string) => void;
    onSaved: (matchId: string) => void;
};

// Undecided future matches without a real team assigned should never render blank.
function teamName(name: string | null, shortName: string): string {
    return name || shortName || "TBC";
}

export function MatchRow({ match, now, hasFocus, isFirstInGroup, onFocus, onSaved }: MatchRowProps) {
    const { user } = useUser();
    const { status, minutesToPredict } = computeMatchStatus(match, now);

    const [homeInput, setHomeInput] = useState(match.homeTeamGoals !== null ? String(match.homeTeamGoals) : "");
    const [awayInput, setAwayInput] = useState(match.awayTeamGoals !== null ? String(match.awayTeamGoals) : "");
    const [saveState, setSaveState] = useState<SaveState>("idle");

    const homeInputRef = useRef<HTMLInputElement>(null);
    const awayInputRef = useRef<HTMLInputElement>(null);

    // Editing an existing prediction fires a save per digit change (home, then away), so two POSTs
    // can overlap. Chaining them guarantees the server processes them in entry order (last write
    // wins with the *latest* values), and the sequence number keeps a superseded save's outcome
    // from clobbering the UI state of the one that matters.
    const saveChain = useRef<Promise<void>>(Promise.resolve());
    const saveSeq = useRef(0);

    useEffect(() => {
        if (hasFocus && document.activeElement !== awayInputRef.current) {
            homeInputRef.current?.focus();
        }
    }, [hasFocus]);

    const locked = status !== "Pre" || saveState === "cutoff";

    const save = (homeValue: string, awayValue: string, focusNext: boolean) => {
        if (homeValue === "" || awayValue === "") return;

        const seq = ++saveSeq.current;
        setSaveState("saving");

        saveChain.current = saveChain.current.then(async () => {
            try {
                await savePrediction(match.matchID, Number(homeValue), Number(awayValue));
                if (saveSeq.current !== seq) return;

                setSaveState("saved");
                onSaved(match.matchID);

                if (focusNext) {
                    awayInputRef.current?.blur();
                }
            } catch (error) {
                if (saveSeq.current !== seq) return;
                setSaveState(error instanceof ApiError && error.status === 409 ? "cutoff" : "error");
            }
        });
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
        <Flex direction="column" borderTopWidth={isFirstInGroup ? "0" : "1px"} borderTopColor="border.hairline" py={2} px={{ base: 2, md: 4 }} gap={2}>
            <Flex align="center" gap={{ base: 2, md: 4 }} wrap="wrap">
                <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                    <TeamName teamId={match.homeTeamID} name={teamName(match.homeTeam, match.homeTeamShortName)} crest={homeCrest} crestPosition="after" />
                </HStack>

                <HStack gap={1}>
                    <Input ref={homeInputRef} value={displayHome} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onHomeChange}
                        bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                    <Text>-</Text>
                    <Input ref={awayInputRef} value={displayAway} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onAwayChange}
                        bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                </HStack>

                <HStack flex="1" minW="0" gap={2}>
                    <TeamName teamId={match.awayTeamID} name={teamName(match.awayTeam, match.awayTeamShortName)} crest={awayCrest} crestPosition="before" />
                </HStack>

                <MatchStatus matchId={match.matchID} myUserId={user?.id} status={status} minutesToPredict={minutesToPredict} saveState={saveState}
                    actualHomeGoals={match.actualHomeTeamGoals} actualAwayGoals={match.actualAwayTeamGoals} score={match.score} />
            </Flex>
        </Flex>
    );
}
