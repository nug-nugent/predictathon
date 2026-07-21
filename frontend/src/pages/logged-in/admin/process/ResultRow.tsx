import { Flex, HStack, Image, Input, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState, type ChangeEvent, type FocusEvent } from "react";
import { ApiError } from "../../../../services/api";
import { saveMatchResult } from "../../../../services/match-processing-service";
import { parseDigit } from "../../../../utils/parseDigit";
import { isResultEligible } from "./matchProcessingStatus";

type ResultRowProps = {
    matchId: string;
    matchDateTime: string;
    homeTeamName: string;
    awayTeamName: string;
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
    matchId, matchDateTime, homeTeamName, awayTeamName, homeCrest, awayCrest,
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
    // Locked once saved too - corrections after the fact belong on the fixture admin page, where
    // goals become editable once a match is marked played.
    const locked = !eligible || saveState === "saving" || saveState === "saved";

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

    const statusText = !eligible
        ? "Not yet available"
        : saveState === "saving"
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
                    <Text fontSize="0.9em" textAlign="right" truncate>{homeTeamName}</Text>
                    {homeCrest && <Image src={homeCrest} boxSize="20px" alt="" />}
                </HStack>

                <HStack gap={1}>
                    <Input ref={homeInputRef} value={homeInput} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onHomeChange} />
                    <Text>-</Text>
                    <Input ref={awayInputRef} value={awayInput} autoComplete="off" textAlign="center"
                        size="sm" width="40px" readOnly={locked} onFocus={onInputFocus} onChange={onAwayChange} />
                </HStack>

                <HStack flex="1" minW="0" gap={2}>
                    {awayCrest && <Image src={awayCrest} boxSize="20px" alt="" />}
                    <Text fontSize="0.9em" truncate>{awayTeamName}</Text>
                </HStack>

                <Text fontSize="xs" color={saveState === "error" ? "fg.error" : "fg.muted"} minW="90px" textAlign="right">
                    {statusText}
                </Text>
            </Flex>
        </Flex>
    );
}
