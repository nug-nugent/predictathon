import { Box, Heading, Stack, Text } from "@chakra-ui/react";
import type { KnockoutBracket as KnockoutBracketData, KnockoutRound } from "../../../services/prediction-service";
import { BracketMatchCard } from "./BracketMatchCard";

/// How wide a round's column gets before the tree is too cramped to read, and the gap between one
/// round's column and the next - which is also where the connector elbows live, so the two have to
/// agree.
///
/// A bracket is seven columns wide and only ever as tall as its first round, so horizontal space is
/// the scarce one and vertical space is going spare. The cards are therefore narrow, and anything
/// that would widen one is stacked onto another line instead - see BracketCardFooter. The design
/// draws 200px cards, which is 1700px across for a sixteen-team bracket and wider than this site's
/// content column on anything short of a large monitor.
///
/// This is a floor, not a width: the columns are `1fr` above it and take whatever room there is. It
/// only has to fit the widest thing that cannot wrap - a crest, a three-letter acronym and a goal
/// tally on one line, or "You: 2 - 2" in the footer. The longest text on a card is a placeholder
/// like "Runner-up Group B", and those wrap onto a second line quite happily, so there is no reason
/// to reserve a line's worth of width for them.
const COLUMN_MINIMUM_WIDTH = 100;
const BRACKET_GAP = 24;

/// Breathing room between one tie and the next down a column. Each tie is a `flex: 1` box with its
/// card centred, so without this the cards in a busy first round end up all but touching.
const MATCH_GAP = 10;

/// The width below which a tree is not worth drawing at all - phones and small tablets, where the
/// stacked list is the only readable form. Deliberately far narrower than the tree's own minimum, so
/// a desktop that can nearly fit the bracket still gets a bracket.
const STACKED_BELOW_WIDTH = 760;

/// The connector stroke, matching the design. At a hairline the elbows all but vanish against the
/// page and the bracket stops reading as a tree at all.
const CONNECTOR_WIDTH = "2px";

/// A competition's knockout stage drawn as a bracket: the first round down each side, working in to
/// the final in the middle, with the third-place play-off beneath it.
///
/// The geometry is one CSS grid on the whole thing plus a subgrid per round, following
/// https://ishadeed.com/article/fifa-layout/. What that buys is markup that matches the tournament
/// rather than the picture: a round is one element holding its two halves, instead of a
/// "quarter-finals left" and a "quarter-finals right" that only the stylesheet knows are the same
/// round. Round n counting inwards spans `grid-column: n+1 / -(n+1)`, so each round insets itself by
/// one column at each end and the final lands in the middle column on its own - no round needs to
/// know how many rounds there are.
///
/// The vertical alignment falls out rather than being calculated: each half is a flex column whose
/// matches are `flex: 1`, so a round with half as many matches as the one outside it puts each of
/// them across two of that round's rows, centred - which is exactly opposite the pair it is fed by.
///
/// The design lays the same picture out with absolute coordinates, which a static artboard can
/// afford and a page that has to survive a phone cannot; the grid reproduces its geometry and
/// collapses when it runs out of room.
export function KnockoutBracket({ bracket, now, onPredictionSaved }: {
    bracket: KnockoutBracketData;
    now: Date;
    onPredictionSaved?: () => void;
}) {
    const { rounds, thirdPlacePlayOff } = bracket;

    if (rounds.length === 0) {
        return <Text textAlign="center" py={4}>This competition has no knockout bracket.</Text>;
    }

    // One column per round down each side, less one for the middle they share: 4 rounds is 7
    // columns. Rows are the first round's matches per side, which every deeper round divides into.
    const columnCount = rounds.length * 2 - 1;
    const rowCount = Math.ceil(rounds[0].matches.length / 2);
    const treeMinimumWidth = columnCount * COLUMN_MINIMUM_WIDTH + (columnCount - 1) * BRACKET_GAP;
    const collapsed = `@container bracket (width < ${STACKED_BELOW_WIDTH}px)`;

    const columns = {
        display: "grid",
        gridTemplateColumns: `repeat(${columnCount}, minmax(${COLUMN_MINIMUM_WIDTH}px, 1fr))`,
        columnGap: "var(--bracket-gap)",
        minWidth: `${treeMinimumWidth}px`,
    };

    return (
        <Stack gap={6}>
            <Box
                css={{
                    "--bracket-gap": `${BRACKET_GAP}px`,
                    "--bracket-match-gap": `${MATCH_GAP}px`,
                    containerType: "inline-size",
                    containerName: "bracket",
                    overflowX: "auto",
                }}
            >
                {/* The round names run across the top of the bracket rather than beside each tie.
                    Its own grid on the same track sizes as the tree below, so the labels line up
                    with the columns without having to be woven into the subgrid's rows. */}
                <Box
                    css={{
                        ...columns,
                        marginBottom: "var(--chakra-spacing-2)",
                        [collapsed]: { display: "none" },
                    }}
                >
                    {rounds.map((round, index) => (
                        <RoundLabels
                            key={round.knockoutRound}
                            roundName={round.roundName}
                            startColumn={index + 1}
                            endColumn={columnCount - index}
                        />
                    ))}
                </Box>

                <Box
                    css={{
                        ...columns,
                        gridTemplateRows: `repeat(${rowCount}, minmax(64px, auto))`,

                        // Collapsed, it becomes what a narrow screen can read: each round a headed
                        // block, in order, first round to final. Same markup, no second component.
                        [collapsed]: {
                            display: "flex",
                            flexDirection: "column",
                            gap: "var(--chakra-spacing-5)",
                            minWidth: 0,
                        },
                    }}
                >
                    {rounds.map((round, index) => (
                        <BracketRound
                            key={round.knockoutRound}
                            round={round}
                            insetFromEdge={index + 1}
                            isFirstRound={index === 0}
                            isFinalRound={index === rounds.length - 1}
                            collapsed={collapsed}
                            now={now}
                            onPredictionSaved={onPredictionSaved}
                        />
                    ))}

                    {/* Sits in the final's own column, in the empty rows underneath it. The final is
                        centred - the semi-final connectors run to its middle, so it has to be - which
                        leaves half a column of nothing below it; dropping the play-off beneath the
                        whole tree instead (as the design does) put a screen's worth of gap between
                        the two cards it belongs next to. One row past the middle is as close as it
                        can go without colliding with the final, and on a bracket too short to have a
                        spare row the grid simply adds one underneath, which is where it used to be. */}
                    {thirdPlacePlayOff && (
                        <Box
                            css={{
                                gridColumn: Math.ceil(columnCount / 2),
                                gridRow: Math.floor(rowCount / 2) + 2,
                                alignSelf: "start",
                                minWidth: 0,
                                // alignSelf governs the cross axis, which is horizontal once the
                                // grid has become a flex column - left as "start" the play-off
                                // would be the one card in the stacked list narrower than the rest.
                                [collapsed]: { display: "block", alignSelf: "stretch" },
                            }}
                        >
                            <Text mb={2} textAlign="center" fontSize="10px" fontWeight="bold" letterSpacing="0.4px"
                                textTransform="uppercase" color="fg.muted">
                                3rd place playoff
                            </Text>
                            <BracketMatchCard match={thirdPlacePlayOff} now={now} onPredictionSaved={onPredictionSaved} />
                        </Box>
                    )}
                </Box>
            </Box>
        </Stack>
    );
}

/// A round's name over each of its two columns - left-aligned over the top half of the draw and
/// right-aligned over the bottom - or once, centred, for the final, which has only one column.
function RoundLabels({ roundName, startColumn, endColumn }: {
    roundName: string;
    startColumn: number;
    endColumn: number;
}) {
    const label = (column: number, align: "left" | "center" | "right") => (
        <Box gridColumn={column} textAlign={align} fontSize="10px" fontWeight="bold" letterSpacing="0.4px"
            textTransform="uppercase" color="fg.muted" lineHeight="1.3">
            {roundName}
        </Box>
    );

    if (startColumn === endColumn) {
        return label(startColumn, "center");
    }

    return (
        <>
            {label(startColumn, "left")}
            {label(endColumn, "right")}
        </>
    );
}

/// One round, spanning the whole grid as a subgrid and placing its two halves at either end of its
/// own span. `insetFromEdge` is how many columns in from each edge the round sits: 1 for the first
/// round, rising by one each round, so the last lands on the middle column alone.
function BracketRound({ round, insetFromEdge, isFirstRound, isFinalRound, collapsed, now, onPredictionSaved }: {
    round: KnockoutRound;
    insetFromEdge: number;
    isFirstRound: boolean;
    isFinalRound: boolean;
    /** The container query at which the tree unwinds into a stacked list. */
    collapsed: string;
    now: Date;
    onPredictionSaved?: () => void;
}) {
    // Slots 1..n/2 are the top half of the draw and the rest the bottom, which is what decides the
    // side of the view a tie appears on. The final has a single match and only a top half.
    const halfway = Math.ceil(round.matches.length / 2);
    const topHalf = round.matches.slice(0, halfway);
    const bottomHalf = round.matches.slice(halfway);

    return (
        <Box
            css={{
                gridColumn: `${insetFromEdge} / -${insetFromEdge}`,
                gridRow: "1 / -1",
                display: "grid",
                gridTemplateColumns: "subgrid",
                gridTemplateRows: "subgrid",

                [collapsed]: {
                    display: "block",
                },
            }}
        >
            <BracketHalf
                matches={topHalf} side="start" isFirstRound={isFirstRound} isFinalRound={isFinalRound}
                collapsed={collapsed} roundName={round.roundName} now={now} onPredictionSaved={onPredictionSaved}
            />

            {bottomHalf.length > 0 && (
                <BracketHalf
                    matches={bottomHalf} side="end" isFirstRound={isFirstRound} isFinalRound={isFinalRound}
                    collapsed={collapsed} roundName={round.roundName} now={now} onPredictionSaved={onPredictionSaved}
                />
            )}
        </Box>
    );
}

/// One half of a round's draw - the ties down the left of the bracket, or down the right.
function BracketHalf({ matches, side, isFirstRound, isFinalRound, collapsed, roundName, now, onPredictionSaved }: {
    matches: KnockoutRound["matches"];
    side: "start" | "end";
    isFirstRound: boolean;
    isFinalRound: boolean;
    /** The container query at which the tree unwinds into a stacked list. */
    collapsed: string;
    roundName: string;
    now: Date;
    onPredictionSaved?: () => void;
}) {
    // An elbow comes in three pieces, following the design's SVG paths: each tie runs a stub out to
    // the middle of the gap, the first of each pair carries the vertical joining the two stubs, and
    // the tie they feed runs a stub back from its own edge to meet that vertical. Because every tie
    // in a half is `flex: 1`, adjacent centres are exactly one match-box apart, so the vertical is
    // `top: 50%; height: 100%` and nothing has to be measured.
    const towards = side === "start" ? "right" : "left";
    const from = side === "start" ? "left" : "right";
    const halfGap = "calc(var(--bracket-gap) / 2)";

    // Not border.card, which is deliberately transparent in dark mode - a card there is read by its
    // fill, but a connector is only ever a line.
    const lineColour = "border.divider";

    return (
        <Box
            css={{
                gridColumn: side === "start" ? "1" : "-2",
                gridRow: "1 / -1",
                display: "flex",
                flexDirection: "column",
                rowGap: "var(--bracket-match-gap)",
                minWidth: 0,

                [collapsed]: {
                    display: "block",
                },
            }}
        >
            {/* The round's name only earns a line of its own once the tree has collapsed into a
                stacked list; drawn out, the labels across the top of the bracket say it instead. */}
            <Box
                css={{
                    display: "none",
                    [collapsed]: { display: side === "start" ? "block" : "none" },
                }}
            >
                <Heading size="xs" mb={2}>{roundName}</Heading>
            </Box>

            {matches.map((match, index) => (
                <Box
                    key={match.matchID}
                    css={{
                        flex: "1",
                        display: "flex",
                        alignItems: "center",
                        position: "relative",
                        minWidth: 0,

                        // The stub out towards the round this tie feeds.
                        ...(isFinalRound ? {} : {
                            "&::after": {
                                content: '""',
                                position: "absolute",
                                [towards]: `calc(${halfGap} * -1)`,
                                top: "50%",
                                width: halfGap,
                                borderTopWidth: CONNECTOR_WIDTH,
                                borderTopStyle: "solid",
                                borderTopColor: lineColour,
                            },
                        }),

                        // The vertical joining this tie to the one below it, drawn once per pair.
                        // Its height is one tie box plus the gap between them, because that gap is
                        // exactly what the row gap added to the distance between the two centres it
                        // has to span - at a plain 100% it stops short of the tie below.
                        ...(isFinalRound || index % 2 !== 0 || index + 1 >= matches.length ? {} : {
                            "&::before": {
                                content: '""',
                                position: "absolute",
                                [towards]: `calc(${halfGap} * -1)`,
                                top: "50%",
                                height: "calc(100% + var(--bracket-match-gap))",
                                borderLeftWidth: CONNECTOR_WIDTH,
                                borderLeftStyle: "solid",
                                borderLeftColor: lineColour,
                            },
                        }),

                        [collapsed]: {
                            display: "block",
                            marginBottom: "var(--chakra-spacing-2)",
                            "&::before": { display: "none" },
                            "&::after": { display: "none" },
                        },
                    }}
                >
                    {/* The stub back from this tie to the vertical of the pair that feeds it. Its own
                        element because the wrapper's two pseudo-elements are already spoken for by
                        the outgoing stub and the vertical, and a middle round needs all three. */}
                    {!isFirstRound && (
                        <Box
                            aria-hidden="true"
                            css={{
                                position: "absolute",
                                [from]: `calc(${halfGap} * -1)`,
                                top: "50%",
                                width: halfGap,
                                borderTopWidth: CONNECTOR_WIDTH,
                                borderTopStyle: "solid",
                                borderTopColor: lineColour,
                                [collapsed]: { display: "none" },
                            }}
                        />
                    )}

                    <BracketMatchCard
                        match={match}
                        now={now}
                        isFinal={isFinalRound}
                        onPredictionSaved={onPredictionSaved}
                    />
                </Box>
            ))}
        </Box>
    );
}
