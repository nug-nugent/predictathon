import { useState } from "react";
import { Heading, Table, Text, VStack } from "@chakra-ui/react";
import type { MatchListItem } from "../../services/statistics-service";
import { ScoreComparisonIcon } from "./ScoreComparisonIcon";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";
import { ClickableRow } from "../ui/clickable-row";
import { ShortLabel } from "../ui/short-label";
import { compactCellsOnSmallScreens } from "../ui/table-density";
import { TeamLabel } from "../team/TeamLabel";

const DEFAULT_PAGE_SIZE = 5;

// How your score compared with everyone else's - the average and the icon reading it - is the pair
// of columns that stands down on a phone, where the table would otherwise run off the screen.
const COMPARISON_DISPLAY = { base: "none", md: "table-cell" };

/**
 * A list of played matches with the result, your prediction and what it scored. The result is part
 * of the match itself - "Ipswich Town 0 - 2 Liverpool" - rather than a column of its own, so the
 * scoreline reads as the match's name and the row spends its width on the comparison instead.
 */
export function PredictableMatchesTable({ title, matches, onRowClick, pageSize = DEFAULT_PAGE_SIZE }: { title: string; matches: MatchListItem[]; onRowClick?: (matchId: string) => void; pageSize?: number }) {
    const [page, setPage] = useState(1);
    const pageMatches = matches.slice((page - 1) * pageSize, page * pageSize);

    return (
        <Panel overflowX="auto" accent hoverLift>
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>{title}</Heading>
                {/* Columns of dates, scores and averages are more than a phone has room for: below
                    `md` the kick-off time, the field average and the icon comparing the two step
                    aside, leaving what the row is actually about - the match and its score, your
                    prediction and what it scored. The full set is a tap away on the match. */}
                <Table.Root size="sm" variant="line"
                    css={compactCellsOnSmallScreens}>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader><ShortLabel short="Date" full="Date / time" /></Table.ColumnHeader>
                            <Table.ColumnHeader>Match</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center"><ShortLabel short="You" full="Your prediction" /></Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center"><ShortLabel short="Pts" full="Your score" /></Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center" display={COMPARISON_DISPLAY}>Average score</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center" display={COMPARISON_DISPLAY}></Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageMatches.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={6}>
                                    <Text color="fg.muted">No matches found</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : pageMatches.map((m) => {
                            const cells = (
                                <>
                                    <Table.Cell whiteSpace="nowrap">
                                        <ShortLabel
                                            short={new Date(m.matchDateTime).toLocaleDateString(undefined, { dateStyle: "short" })}
                                            full={new Date(m.matchDateTime).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" })}
                                        />
                                    </Table.Cell>
                                    <Table.Cell whiteSpace="nowrap">
                                        <TeamLabel name={m.homeTeam} shortName={m.homeTeamShortName} acronym={m.homeTeamAcronym} />
                                        {` ${m.homeTeamGoals ?? "?"} - ${m.awayTeamGoals ?? "?"} `}
                                        <TeamLabel name={m.awayTeam} shortName={m.awayTeamShortName} acronym={m.awayTeamAcronym} />
                                    </Table.Cell>
                                    <Table.Cell textAlign="center">{m.predictionHomeTeamGoals ?? "?"}-{m.predictionAwayTeamGoals ?? "?"}</Table.Cell>
                                    <Table.Cell textAlign="center" color={`points.${m.yourPredictionScore}`} fontWeight="bold">{m.yourPredictionScore}</Table.Cell>
                                    <Table.Cell textAlign="center" display={COMPARISON_DISPLAY}>{m.averagePredictionScore.toFixed(2)}</Table.Cell>
                                    <Table.Cell textAlign="center" display={COMPARISON_DISPLAY}><ScoreComparisonIcon yours={m.yourPredictionScore} average={m.averagePredictionScore} /></Table.Cell>
                                </>
                            );

                            return onRowClick ? (
                                <ClickableRow key={m.matchID} onActivate={() => onRowClick(m.matchID)}>{cells}</ClickableRow>
                            ) : (
                                <Table.Row key={m.matchID}>{cells}</Table.Row>
                            );
                        })}
                    </Table.Body>
                </Table.Root>

                <TablePagination count={matches.length} pageSize={pageSize} page={page} onPageChange={setPage} />
            </VStack>
        </Panel>
    );
}
