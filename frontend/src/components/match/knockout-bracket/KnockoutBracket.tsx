import { Box, Heading, Stack, Text } from "@chakra-ui/react";
import type { KnockoutBracket as KnockoutBracketData, KnockoutRound } from "../../../services/prediction-service";
import { BracketMatchCard } from "./BracketMatchCard";

/// How wide a round's column gets before the tree is too cramped to read, and the gap between one
/// round's column and the next - which is also the length of the connector stubs, so the two have to
/// agree. Both feed the breakpoint at which the tree gives up and stacks.
///
/// The column width is deliberately tight. A 16-team bracket is seven columns, and seven columns at
/// this width plus their gaps is what has to fit beside the sidebar on a 1280-wide screen for the
/// tree to be drawn at all - a few pixels wider and every desktop falls back to the stacked list.
const COLUMN_MINIMUM_WIDTH = 108;
const BRACKET_GAP = 18;

/// A competition's knockout stage drawn as a bracket: the first round down each side, working in to
/// the final in the middle, with the third-place play-off beneath it.
///
/// The geometry is one CSS grid on the whole thing plus a subgrid per round, following
/// https://ishadeed.com/article/fifa-layout/. What that buys is markup that matches the tournament
/// rather than the picture: a round is one element containing its two halves, instead of a
/// "quarter-finals left" and a "quarter-finals right" that only the stylesheet knows are the same
/// round. Round n counting inwards spans `grid-column: n+1 / -(n+1)`, so each round insets itself by
/// one column at each end and the final lands in the middle column on its own - no round needs to
/// know how many rounds there are.
///
/// The vertical alignment falls out rather than being calculated: each half is a flex column whose
/// matches are `flex: 1`, so a round with half as many matches as the one outside it puts each of
/// them across two of that round's rows, centred - which is exactly opposite the pair it is fed by.
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

    // Narrower than the tree's own minimum and it stops being a tree, so the breakpoint is that
    // minimum rather than a round number: the view is either a whole bracket or a stacked list,
    // never a bracket you have to scroll sideways to find the other half of. Passed down because
    // every part of the layout has to unwind at the same width.
    const collapsed = `@container bracket (width < ${treeMinimumWidth}px)`;

    return (
        <Stack gap={6}>
            <Box
                css={{
                    "--bracket-gap": `${BRACKET_GAP}px`,
                    containerType: "inline-size",
                    containerName: "bracket",
                    overflowX: "auto",
                }}
            >
                <Box
                    css={{
                        display: "grid",
                        gridTemplateColumns: `repeat(${columnCount}, minmax(${COLUMN_MINIMUM_WIDTH}px, 1fr))`,
                        gridTemplateRows: `repeat(${rowCount}, minmax(64px, auto))`,
                        columnGap: "var(--bracket-gap)",
                        minWidth: `${treeMinimumWidth}px`,

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
                            isFinalRound={index === rounds.length - 1}
                            collapsed={collapsed}
                            now={now}
                            onPredictionSaved={onPredictionSaved}
                        />
                    ))}
                </Box>
            </Box>

            {thirdPlacePlayOff && (
                <Box maxW="320px" mx="auto" width="full">
                    <Heading size="xs" mb={2} textAlign="center">Third place play-off</Heading>
                    <BracketMatchCard match={thirdPlacePlayOff} now={now} onPredictionSaved={onPredictionSaved} />
                </Box>
            )}
        </Stack>
    );
}

/// One round, spanning the whole grid as a subgrid and placing its two halves at either end of its
/// own span. `insetFromEdge` is how many columns in from each edge the round sits: 1 for the first
/// round, rising by one each round, so the last lands on the middle column alone.
function BracketRound({ round, insetFromEdge, isFinalRound, collapsed, now, onPredictionSaved }: {
    round: KnockoutRound;
    insetFromEdge: number;
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
                matches={topHalf} side="start" isFinalRound={isFinalRound} collapsed={collapsed}
                roundName={round.roundName} now={now} onPredictionSaved={onPredictionSaved}
            />

            {bottomHalf.length > 0 && (
                <BracketHalf
                    matches={bottomHalf} side="end" isFinalRound={isFinalRound} collapsed={collapsed}
                    roundName={round.roundName} now={now} onPredictionSaved={onPredictionSaved}
                />
            )}
        </Box>
    );
}

/// One half of a round's draw - the matches down the left of the bracket, or down the right.
function BracketHalf({ matches, side, isFinalRound, collapsed, roundName, now, onPredictionSaved }: {
    matches: KnockoutRound["matches"];
    side: "start" | "end";
    isFinalRound: boolean;
    /** The container query at which the tree unwinds into a stacked list. */
    collapsed: string;
    roundName: string;
    now: Date;
    onPredictionSaved?: () => void;
}) {
    // Connectors are drawn from the feeding side: each match gets a stub out towards the round it
    // feeds, and the first of each pair carries the vertical that joins the pair's two stubs. Since
    // every match in a half is `flex: 1`, adjacent centres are exactly one match-box apart, so the
    // vertical is `top: 50%; height: 100%` and needs no measuring.
    const towards = side === "start" ? "right" : "left";

    return (
        <Box
            css={{
                gridColumn: side === "start" ? "1" : "-2",
                gridRow: "1 / -1",
                display: "flex",
                flexDirection: "column",
                minWidth: 0,

                [collapsed]: {
                    display: "block",
                },
            }}
        >
            {/* The round's name only earns a line of its own once the tree has collapsed into a
                stacked list, where nothing else says which round you are looking at. */}
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

                        // Stub out towards the next round.
                        ...(isFinalRound ? {} : {
                            "&::after": {
                                content: '""',
                                position: "absolute",
                                [towards]: "calc(var(--bracket-gap) * -1)",
                                top: "50%",
                                width: "var(--bracket-gap)",
                                borderTopWidth: "1px",
                                borderTopStyle: "solid",
                                // Not border.card, which is deliberately transparent in dark mode -
                                // a card there is read by its fill, but a connector is only a line.
                                borderTopColor: "border.divider",
                            },
                        }),

                        // The vertical joining this match to the one below it, drawn once per pair.
                        ...(isFinalRound || index % 2 !== 0 || index + 1 >= matches.length ? {} : {
                            "&::before": {
                                content: '""',
                                position: "absolute",
                                [towards]: "calc(var(--bracket-gap) * -1)",
                                top: "50%",
                                height: "100%",
                                borderLeftWidth: "1px",
                                borderLeftStyle: "solid",
                                borderLeftColor: "border.divider",
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
                    <BracketMatchCard match={match} now={now} onPredictionSaved={onPredictionSaved} />
                </Box>
            ))}
        </Box>
    );
}
