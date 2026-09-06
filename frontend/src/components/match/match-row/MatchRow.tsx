import { Box, Flex, HStack, Input, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState, type ChangeEvent, type FocusEvent, type KeyboardEvent, type MouseEvent } from "react";
import { type MatchPrediction } from "../../../services/prediction-service";
import { computeMatchStatus, isPartialScoreline } from "../matchStatus";
import { usePredictionSave } from "../usePredictionSave";
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

export function MatchRow({ match, now, hasFocus, isFirstInGroup, onFocus, onSaved }: MatchRowProps) {
    const { user } = useUser();
    const { status, minutesToPredict } = computeMatchStatus(match, now);

    const [homeInput, setHomeInput] = useState(match.homeTeamGoals !== null ? String(match.homeTeamGoals) : "");
    const [awayInput, setAwayInput] = useState(match.awayTeamGoals !== null ? String(match.awayTeamGoals) : "");
    const { saveState, save: savePredictionFor, flush, retry } = usePredictionSave(match.matchID);

    const rowRef = useRef<HTMLDivElement>(null);
    const homeInputRef = useRef<HTMLInputElement>(null);
    const awayInputRef = useRef<HTMLInputElement>(null);

    const isPartial = isPartialScoreline(homeInput, awayInput);

    useEffect(() => {
        if (hasFocus && document.activeElement !== awayInputRef.current) {
            homeInputRef.current?.focus();
        }
    }, [hasFocus]);

    const locked = status !== "Pre" || saveState === "cutoff";

    const save = (homeValue: string, awayValue: string, focusNext: boolean) => {
        savePredictionFor(homeValue, awayValue, () => {
            onSaved(match.matchID);

            if (focusNext) {
                awayInputRef.current?.blur();
            }
        });
    };

    const onInputFocus = (event: FocusEvent<HTMLInputElement>) => {
        event.target.select();
        onFocus(match.matchID);
    };

    // Deliberately on the row rather than on each input: tabbing from the home box to the away box
    // blurs the home box, and flushing there would send the half-old pair the debounce exists to
    // suppress. Focus leaving the row entirely is the point at which the edit is finished.
    // A null relatedTarget (clicking onto nothing) counts as leaving.
    const onRowBlur = (event: FocusEvent<HTMLDivElement>) => {
        if (!rowRef.current?.contains(event.relatedTarget)) {
            flush();
        }
    };

    // Without this, the mouse-click's own mouseup handler runs after onFocus and collapses the
    // selection back to a caret at the click position - so a click only appears to select on
    // keyboard-driven focus (tab), not on the click that's the common case here.
    const onInputMouseUp = (event: MouseEvent<HTMLInputElement>) => {
        event.preventDefault();
    };

    // Left/Right step through every score input in DOM order (home, away, home, away, ...), so at
    // a row boundary they carry on into the next/previous match. Up/Down step by 2 to land on the
    // same side (home or away) one row up/down, since every row contributes exactly one pair.
    const onInputArrowNav = (event: KeyboardEvent<HTMLInputElement>) => {
        const step = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -2, ArrowDown: 2 }[event.key];
        if (step === undefined) return;

        const inputs = Array.from(document.querySelectorAll<HTMLInputElement>('input[data-role="score-input"]'));
        const currentIndex = inputs.indexOf(event.currentTarget);
        if (currentIndex === -1) return;

        const target = inputs[currentIndex + step];
        if (!target) return;

        event.preventDefault();
        target.focus();
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
        <Flex ref={rowRef} onBlur={onRowBlur} direction="column" borderTopWidth={isFirstInGroup ? "0" : "1px"} borderTopColor="border.hairline" py={2} px={{ base: 2, md: 4 }} gap={2}>
            <Flex align="center" gap={{ base: 2, md: 4 }} wrap="wrap">
                <Flex flex="1" minW="0" direction="column" gap={1}>
                    <Flex align="center" gap={{ base: 2, md: 4 }}>
                        <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                            <TeamName teamId={match.homeTeamID} name={match.homeTeam} shortName={match.homeTeamShortName}
                                acronym={match.homeTeamAcronym} crest={homeCrest} crestPosition="after" />
                        </HStack>

                        <HStack gap={1}>
                            <Input ref={homeInputRef} value={displayHome} autoComplete="off" textAlign="center" inputMode="numeric" pattern="[0-9]*"
                                data-role="score-input" size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onMouseUp={onInputMouseUp}
                                onChange={onHomeChange} onKeyDown={onInputArrowNav}
                                bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                            <Text>-</Text>
                            <Input ref={awayInputRef} value={displayAway} autoComplete="off" textAlign="center" inputMode="numeric" pattern="[0-9]*"
                                data-role="score-input" size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onMouseUp={onInputMouseUp}
                                onChange={onAwayChange} onKeyDown={onInputArrowNav}
                                bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                        </HStack>

                        <HStack flex="1" minW="0" gap={2}>
                            <TeamName teamId={match.awayTeamID} name={match.awayTeam} shortName={match.awayTeamShortName}
                                acronym={match.awayTeamAcronym} crest={awayCrest} crestPosition="before" />

                            {/* Referent for MatchList's "** Extra time excluded" note - keep the two in step. */}
                            {match.knockout && (
                                <Text as="span" fontWeight="bold" color="orange.500" flexShrink={0}
                                    title="Extra time excluded" aria-label="Extra time excluded">**</Text>
                            )}
                        </HStack>
                    </Flex>

                    {match.description && (
                        <Text fontSize="0.75em" color="fg.muted" textAlign="center">{match.description}</Text>
                    )}
                </Flex>

                <Box flexBasis={{ base: "100%", md: "auto" }}>
                    <MatchStatus matchId={match.matchID} myUserId={user?.id} status={status} minutesToPredict={minutesToPredict} saveState={saveState}
                        isPartial={isPartial} onRetry={retry}
                        actualHomeGoals={match.actualHomeTeamGoals} actualAwayGoals={match.actualAwayTeamGoals} score={match.score} />
                </Box>
            </Flex>
        </Flex>
    );
}
