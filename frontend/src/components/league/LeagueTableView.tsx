import { HStack, IconButton, Popover, Portal, Table, Text } from "@chakra-ui/react";
import { CircleHelp } from "lucide-react";
import { Link } from "react-router";
import { Panel } from "../ui/panel";
import { LeaguePositionChangeIcon } from "./LeaguePositionChangeIcon";
import { PlayerAvatar } from "./PlayerAvatar";
import { useUser } from "../../hooks/useUser";
import type { LeagueTableItem } from "../../services/league-service";

// Shared table markup for any ranked list of LeagueTableItem rows - a single competition's league
// table (LeaguePage) or the all-time table aggregated across every competition (Statistics page).
// showAveragePointsPerPrediction adds an AVG column (score / predictions made) - only meaningful for
// the all-time table, where competitions of differing lengths make the raw points total hard to compare.
export function LeagueTableView({ items, showAveragePointsPerPrediction = false }: { items: LeagueTableItem[]; showAveragePointsPerPrediction?: boolean }) {
    const { user } = useUser();

    return (
        <Panel overflowX="auto" accent>
            <Table.Root size="sm" variant="line" showColumnBorder stickyHeader>
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
                </Table.ColumnGroup>
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POS</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}></Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>NAME</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>3</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>2</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>1</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>0</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>-</Table.ColumnHeader>
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POINTS</Table.ColumnHeader>
                        {showAveragePointsPerPrediction && (
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>AVG</Table.ColumnHeader>
                        )}
                        <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>
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
                                <Table.Cell fontSize={"0.9em"}>
                                    <HStack gap={2}>
                                        <PlayerAvatar username={item.username} avatarUrl={item.avatarUrl} />
                                        <Link to={`/profile/${item.userID}`}>{item.username}</Link>
                                    </HStack>
                                </Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.3"} display={{ base: "none", sm: "table-cell" }}>{item.threePointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.2"} display={{ base: "none", sm: "table-cell" }}>{item.twoPointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.1"} display={{ base: "none", sm: "table-cell" }}>{item.onePointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPointers}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPredictions}</Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.score}</Table.Cell>
                                {showAveragePointsPerPrediction && (
                                    <Table.Cell fontSize={"0.9em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>{averagePointsPerPrediction.toFixed(3)}</Table.Cell>
                                )}
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.averageGoalDifference}</Table.Cell>
                            </Table.Row>
                        );
                    })}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
