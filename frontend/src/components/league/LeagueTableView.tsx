import { Box, HStack, IconButton, Popover, Portal, Table, Text } from "@chakra-ui/react";
import { CircleHelp, TrendingUp } from "lucide-react";
import { Link } from "react-router";
import { Panel } from "../ui/panel";
import { LeaguePositionChangeIcon } from "./LeaguePositionChangeIcon";
import { PlayerAvatar } from "./PlayerAvatar";
import { ShortLabel } from "../ui/short-label";
import { breakableCellText, compactCellsOnSmallScreens } from "../ui/table-density";
import { useUser } from "../../hooks/useUser";
import type { LeagueTableItem, LiveLeagueTableItem } from "../../services/league-service";

type LeagueTableViewProps = {
    items: LeagueTableItem[];
    /**
     * Adds an AVG column (score / predictions made) - only meaningful for the all-time table, where
     * competitions of differing lengths make the raw points total hard to compare.
     */
    showAveragePointsPerPrediction?: boolean;
    /**
     * Adds an IN PLAY column showing how much of each row's points are still provisional. For the
     * Live page, whose rows are LiveLeagueTableItems. Nothing else changes: those rows already
     * carry the live scores in every column, and their previousLeaguePosition is the confirmed
     * standing, so the position-change arrow below works on them unaltered.
     */
    showLivePoints?: boolean;
    /** Renders without the surrounding Panel, for embedding inside one (see LiveLeagueTable). */
    bare?: boolean;
};

// Shared table markup for any ranked list of LeagueTableItem rows - a single competition's league
// table (LeaguePage), the all-time table aggregated across every competition (Statistics page), or
// the Live page's standings. One definition of what a league table looks like, so the three can't
// drift into looking like different things.
export function LeagueTableView({ items, showAveragePointsPerPrediction = false, showLivePoints = false, bare = false }: LeagueTableViewProps) {
    const { user } = useUser();
    const Wrapper = bare ? Box : Panel;

    // The Live table carries an extra IN PLAY column - the thing people are on that page to watch,
    // and so the one column that never stands down. Everything optional beside it waits an extra
    // breakpoint to make the room: the points breakdown appears from `md` rather than `sm`, and
    // AGD, the tie-break detail, hides outright below `md`. Neither applies to the League page or
    // the all-time table, which have the room and keep both at every width.
    const breakdownDisplay = showLivePoints
        ? { base: "none", md: "table-cell" }
        : { base: "none", sm: "table-cell" };
    const agdDisplay = showLivePoints ? { base: "none", md: "table-cell" } : undefined;

    return (
        <Wrapper overflowX="auto" {...(bare ? {} : { accent: true })}>
            {/* A phone is around 40px short of the room this table needs at the default cell
                padding, which is the whole difference between it fitting and it scrolling sideways
                inside its own card. Trimmed below `md` only; the full padding is back from there. */}
            <Table.Root size="sm" variant="line" showColumnBorder stickyHeader
                css={compactCellsOnSmallScreens}>
                <Table.ColumnGroup>
                    <Table.Column htmlWidth="20px" />
                    <Table.Column htmlWidth="20px" />
                    <Table.Column htmlWidth="50%" />
                    <Table.Column />
                    <Table.Column />
                    <Table.Column />
                    <Table.Column />
                    <Table.Column />
                    <Table.Column />
                    {showAveragePointsPerPrediction && <Table.Column />}
                    <Table.Column />
                    {showLivePoints && <Table.Column />}
                </Table.ColumnGroup>
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POS</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}></Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>NAME</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={breakdownDisplay}>3</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={breakdownDisplay}>2</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={breakdownDisplay}>1</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={breakdownDisplay}>0</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={breakdownDisplay}>-</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}><ShortLabel short="PTS" full="POINTS" /></Table.ColumnHeader>
                        {showAveragePointsPerPrediction && (
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>AVG</Table.ColumnHeader>
                        )}
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={agdDisplay}>
                            <HStack justify="center" gap={0.5}>
                                <Text>AGD</Text>
                                <Popover.Root positioning={{ placement: "bottom" }}>
                                    <Popover.Trigger asChild>
                                        <IconButton aria-label="What is AGD?" size="2xs" variant="ghost" p={0} minW="auto" h="auto">
                                            <CircleHelp size={12} />
                                        </IconButton>
                                    </Popover.Trigger>
                                    <Portal>
                                        <Popover.Positioner>
                                            <Popover.Content maxW="280px">
                                                <Popover.Arrow />
                                                <Popover.Body>
                                                    <Text fontSize="sm" fontWeight="normal" textAlign="left">
                                                        Average goal difference. The <Text as="span" fontStyle="italic">total</Text> goal difference is what separates users on equal points, followed by number of 3 pointers. See the{" "}
                                                        <Link to="/rules">rules page</Link>.
                                                    </Text>
                                                </Popover.Body>
                                            </Popover.Content>
                                        </Popover.Positioner>
                                    </Portal>
                                </Popover.Root>
                            </HStack>
                        </Table.ColumnHeader>
                        {showLivePoints && (
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>IN PLAY</Table.ColumnHeader>
                        )}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {items.map((item) => {
                        const predictionsMade = item.threePointers + item.twoPointers + item.onePointers + item.noPointers;
                        const averagePointsPerPrediction = predictionsMade > 0 ? item.score / predictionsMade : 0;
                        const isCurrentUser = item.userID === user?.id;

                        return (
                            <Table.Row key={item.userID} bg={isCurrentUser ? "surface.highlightRow" : undefined} fontWeight={isCurrentUser ? "bold" : "normal"}>
                                <Table.Cell fontSize={"0.9em"} textAlign={"right"}>{item.leaguePosition}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>
                                    <LeaguePositionChangeIcon current={item.leaguePosition} previous={item.previousLeaguePosition} />
                                </Table.Cell>
                                <Table.Cell fontSize={"0.9em"} css={breakableCellText}>
                                    <HStack gap={2}>
                                        <PlayerAvatar username={item.username} avatarUrl={item.avatarUrl} />
                                        <Link to={`/profile/${item.userID}`}>{item.username}</Link>
                                    </HStack>
                                </Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.3"} display={breakdownDisplay}>{item.threePointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.2"} display={breakdownDisplay}>{item.twoPointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.1"} display={breakdownDisplay}>{item.onePointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={breakdownDisplay}>{item.noPointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={breakdownDisplay}>{item.noPredictions}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.score}</Table.Cell>
                                {showAveragePointsPerPrediction && (
                                    <Table.Cell fontSize={"0.9em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>{averagePointsPerPrediction.toFixed(3)}</Table.Cell>
                                )}
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} display={agdDisplay}>{item.averageGoalDifference}</Table.Cell>
                                {showLivePoints && (
                                    <Table.Cell fontSize={"0.9em"} textAlign={"center"}><LivePoints points={liveItem(item).livePoints} /></Table.Cell>
                                )}
                            </Table.Row>
                        );
                    })}
                </Table.Body>
            </Table.Root>
        </Wrapper>
    );
}

// showLivePoints is the caller promising these rows carry the live fields; this is where that
// promise is cashed in, in one place, rather than casting at each use.
function liveItem(item: LeagueTableItem): LiveLeagueTableItem {
    return item as LiveLeagueTableItem;
}

/// What the matches in play are worth to one user so far. A gain gets an arrow and a sign so it
/// reads as movement rather than as another total sitting beside the real one; nothing gained is a
/// dash rather than a "+0", which would look like a score.
function LivePoints({ points }: { points: number }) {
    if (points === 0) {
        return <Text as="span" color="fg.muted">&ndash;</Text>;
    }

    return (
        <HStack gap={1} justify="center" color="status.relaxed" fontWeight="bold">
            <TrendingUp size={14} aria-hidden="true" />
            <Text as="span">+{points}</Text>
        </HStack>
    );
}
