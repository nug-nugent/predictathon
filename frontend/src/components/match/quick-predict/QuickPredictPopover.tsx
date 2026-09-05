import { useRef, useState, type ChangeEvent, type FocusEvent, type MouseEvent, type ReactNode } from "react";
import { Button, HStack, Image, Input, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { matchWeekStart, type MatchPrediction } from "../../../services/prediction-service";
import { predictionStatusColor, predictionStatusText } from "../matchStatus";
import { usePredictionSave } from "../usePredictionSave";
import { crestUrl } from "../../../utils/crestUrl";
import { parseDigit } from "../../../utils/parseDigit";

type QuickPredictPopoverProps = {
    match: MatchPrediction;
    /** Minutes left to predict, for the deadline line - the caller has already computed it. */
    minutesToPredict: number;
    /** Called after a successful save, so the list behind the popover can catch up. */
    onSaved?: () => void;
    /** The match row that opens the popover - rendered as the trigger button's content. */
    children: ReactNode;
};

/// A match on the Home page's Today's Matches card that you can still predict, turned into its own
/// little prediction form: click the row and enter (or change) the score right there. The full
/// Predictions page is still a click away in the footer for anyone who wants the whole week, but
/// the common case on a matchday - one fixture, one score, before kick-off - no longer costs a page
/// load and a hunt for the right row.
///
/// Score entry deliberately behaves exactly like the Predictions page's rows: a digit in the home
/// box moves to the away box, and each completed pair saves itself. There's no Save button because
/// there'd be nothing for it to do.
export function QuickPredictPopover({ match, minutesToPredict, onSaved, children }: QuickPredictPopoverProps) {
    // Shown as the acronym, announced in full: "BEL goals" is no use to a screen reader, which has
    // all the room in the world for "Belgium goals".
    const homeName = teamName(match.homeTeamAcronym, match.homeTeamShortName, match.homeTeam);
    const awayName = teamName(match.awayTeamAcronym, match.awayTeamShortName, match.awayTeam);
    const homeSpokenName = teamName(null, match.homeTeamShortName, match.homeTeam);
    const awaySpokenName = teamName(null, match.awayTeamShortName, match.awayTeam);

    const [open, setOpen] = useState(false);
    const [homeInput, setHomeInput] = useState(match.homeTeamGoals !== null ? String(match.homeTeamGoals) : "");
    const [awayInput, setAwayInput] = useState(match.awayTeamGoals !== null ? String(match.awayTeamGoals) : "");
    const { saveState, save } = usePredictionSave(match.matchID);

    const homeInputRef = useRef<HTMLInputElement>(null);
    const awayInputRef = useRef<HTMLInputElement>(null);

    // The only way this row locks while the popover is open: the card stops offering quick predict
    // the moment the match ticks past its cutoff, but a save started just before it can still come
    // back refused.
    const locked = saveState === "cutoff";

    const onInputFocus = (event: FocusEvent<HTMLInputElement>) => {
        event.target.select();
    };

    // Same reason as MatchRow: without this the click's own mouseup collapses the selection back to
    // a caret, so replacing an existing digit would take a manual select first.
    const onInputMouseUp = (event: MouseEvent<HTMLInputElement>) => {
        event.preventDefault();
    };

    const onHomeChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) {
            return;
        }

        setHomeInput(value);
        save(value, awayInput, onSaved);
        if (value !== "") {
            awayInputRef.current?.focus();
        }
    };

    const onAwayChange = (event: ChangeEvent<HTMLInputElement>) => {
        const value = parseDigit(event.target.value);
        if (value === null) {
            return;
        }

        setAwayInput(value);
        save(homeInput, value, onSaved);
    };

    return (
        <Popover.Root
            open={open}
            onOpenChange={(e) => setOpen(e.open)}
            positioning={{ placement: "bottom" }}
            initialFocusEl={() => homeInputRef.current}
        >
            {/* Deliberately styled to match the link rows this replaces (LiveMatchRow) rather than to
                look like a button: within a list of matches, a row that opens a popover and a row
                that navigates should be the same thing to click. */}
            <Popover.Trigger asChild>
                <Button type="button" data-role="quick-predict" variant="plain" display="block" width="full" height="auto" p={0}
                    textAlign="left" fontWeight="normal" borderRadius="8px"
                    _hover={{ bg: "bg.muted" }}
                    _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus" }}>
                    {children}
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content width="auto" minW="260px" maxW="340px">
                        <Popover.Arrow />
                        <Popover.Body p={3}>
                            <Stack gap={2}>
                                <Popover.Title fontSize="sm" fontWeight="bold" textAlign="center">Your Prediction</Popover.Title>

                                <HStack gap={2} justify="center">
                                    <TeamSide name={homeName} image={match.homeTeamImage} crestPosition="after" />

                                    <HStack gap={1} flexShrink={0}>
                                        <Input ref={homeInputRef} value={homeInput} autoComplete="off" textAlign="center"
                                            inputMode="numeric" pattern="[0-9]*" size="sm" width="40px" readOnly={locked}
                                            aria-label={`${homeSpokenName} goals`} onFocus={onInputFocus} onMouseUp={onInputMouseUp} onChange={onHomeChange}
                                            bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                                        <Text>-</Text>
                                        <Input ref={awayInputRef} value={awayInput} autoComplete="off" textAlign="center"
                                            inputMode="numeric" pattern="[0-9]*" size="sm" width="40px" readOnly={locked}
                                            aria-label={`${awaySpokenName} goals`} onFocus={onInputFocus} onMouseUp={onInputMouseUp} onChange={onAwayChange}
                                            bg="input.bg" borderColor="input.border" _focusVisible={{ borderColor: "input.borderFocus" }} />
                                    </HStack>

                                    <TeamSide name={awayName} image={match.awayTeamImage} crestPosition="before" />
                                </HStack>

                                {/* aria-live so a save that lands or fails is announced - entering a score gives
                                    no other feedback, here or on the Predictions page. */}
                                <Text fontSize="xs" textAlign="center" aria-live="polite"
                                    color={predictionStatusColor("Pre", saveState, minutesToPredict)}>
                                    {predictionStatusText("Pre", saveState, minutesToPredict)}
                                </Text>

                                <Button asChild size="xs" variant="ghost">
                                    <RouterLink to={`/predictions?week=${encodeURIComponent(matchWeekStart(match.matchDateTime))}`}>
                                        All Matches This Week
                                    </RouterLink>
                                </Button>
                            </Stack>
                        </Popover.Body>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>
    );
}

// The three-letter code at every width, falling back through the longer names for a team that has
// none. Deliberately not TeamLabel's screen-width-driven naming: this popover is the same narrow
// box whatever it opens on, so keying the name off the *screen* would pick the longest name for
// the narrowest container it appears in. The crest beside it and the row it opened from both name
// the team in full, so an acronym here is read in plenty of context.
function teamName(acronym: string | null, shortName: string | null, name: string | null): string {
    return acronym || shortName || name || "TBC";
}

/// One side of the popover's scoreline: crest and name, facing the score. Plain text rather than
/// TeamName - that opens a popover of its own, and one nested inside another is a trap.
function TeamSide({ name, image, crestPosition }: {
    name: string;
    image: string | null;
    crestPosition: "before" | "after";
}) {
    const crest = crestUrl(image);
    const justify = crestPosition === "after" ? "flex-end" : "flex-start";

    return (
        <HStack gap={1.5} minW="0" flex="1" justify={justify}>
            {crestPosition === "before" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
            <Text fontSize="sm" truncate minW="0" textAlign={crestPosition === "after" ? "right" : "left"}>{name}</Text>
            {crestPosition === "after" && crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
        </HStack>
    );
}
