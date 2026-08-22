import { Flex, HStack, Input, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState, type ChangeEvent, type FocusEvent, type MouseEvent } from "react";
import { ApiError } from "../../../../services/api";
import { saveMatchResult } from "../../../../services/match-processing-service";
import { TeamName } from "../../../../components/match/team-name/TeamName";
import { parseDigit } from "../../../../utils/parseDigit";
import { isResultEligible } from "./matchProcessingStatus";

type ResultRowProps = {
    matchId: string;
    matchDateTime: string;
    homeTeamId: string | null;
    homeTeamName: string;
    homeTeamShortName: string;
    awayTeamId: string | null;
    awayTeamName: string;
    awayTeamShortName: string;
    homeCrest: string | undefined;
    awayCrest: string | undefined;
    now: Date;
    hasFocus: boolean;
    /** True for the first match in a date group, to add a bit of visual separation. */
    isFirstInGroup: boolean;
    onFocus: (matchId: string) => void;
    onSaved: (matchId: string) => void;
};

type RowSaveState = "idle" | "saving" | "saved" | "error";

export function ResultRow({
    matchId, matchDateTime, homeTeamId, homeTeamName, homeTeamShortName, awayTeamId, awayTeamName, awayTeamShortName, homeCrest, awayCrest,
    now, hasFocus, isFirstInGroup, onFocus, onSaved,
}: ResultRowProps) {
    const [homeInput, setHomeInput] = useState("");
    const [awayInput, setAwayInput] = useState("");
    const [saveState, setSaveState] = useState<RowSaveState>("idle");
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const homeInputRef = useRef<HTMLInputElement>(null);
    const awayInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (hasFocus && document.activeElement !== awayInputRef.current) {
            homeInputRef.current?.focus();
        }
    }, [hasFocus]);

    const eligible = isResultEligible(matchDateTime, now);
    // Stays editable after a save so a mis-entered score can be corrected here, rather than
    // forcing a trip to the fixture admin page.
    const locked = !eligible || saveState === "saving";

    const save = async (homeValue: string, awayValue: string, focusNext: boolean) => {
        if (homeValue === "" || awayValue === "") return;

        setSaveState("saving");
        setErrorMessage(null);
        try {
            await saveMatchResult(matchId, Number(homeValue), Number(awayValue));
            setSaveState("saved");
            onSaved(matchId);

            if (focusNext) {
                awayInputRef.current?.blur();
            }
        } catch (error) {
            setErrorMessage(error instanceof ApiError ? error.messages.join(" ") : "Something went wrong.");
            setSaveState("error");
        }
    };

    const onInputFocus = (event: FocusEvent<HTMLInputElement>) => {
        event.target.select();
        onFocus(matchId);
    };

    // Without this, the mouse-click's own mouseup handler runs after onFocus and collapses the
    // selection back to a caret at the click position - so a click only appears to select on
    // keyboard-driven focus (tab), not on the click that's the common case here.
    const onInputMouseUp = (event: MouseEvent<HTMLInputElement>) => {
        event.preventDefault();
    };

    const onHomeChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) return;

        setHomeInput(value);
        void save(value, awayInput, false);
        if (value !== "") {
            awayInputRef.current?.focus();
        }
    };

    const onAwayChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) return;

        setAwayInput(value);
        void save(homeInput, value, true);
    };

    const statusText = saveState === "saving"
        ? "Saving..."
        : saveState === "saved"
            ? "Saved"
            : saveState === "error"
                ? errorMessage
                : "";

    return (
        <Flex direction="column" borderTopWidth={isFirstInGroup ? "0" : "1px"} py={2} px={{ base: 2, md: 4 }} gap={1}>
            <Flex align="center" gap={{ base: 2, md: 4 }} wrap="wrap">
                <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                    <TeamName teamId={homeTeamId} name={homeTeamName} shortName={homeTeamShortName} crest={homeCrest} crestPosition="after" />
                </HStack>

                <HStack gap={1}>
                    <Input ref={homeInputRef} value={homeInput} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onMouseUp={onInputMouseUp} onChange={onHomeChange} />
                    <Text>-</Text>
                    <Input ref={awayInputRef} value={awayInput} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onMouseUp={onInputMouseUp} onChange={onAwayChange} />
                </HStack>

                <HStack flex="1" minW="0" gap={2}>
                    <TeamName teamId={awayTeamId} name={awayTeamName} shortName={awayTeamShortName} crest={awayCrest} crestPosition="before" />
                </HStack>

                {statusText && (
                    <Text fontSize="xs" color={saveState === "error" ? "fg.error" : "fg.muted"} textAlign="right">
                        {statusText}
                    </Text>
                )}
            </Flex>
        </Flex>
    );
}
