import { useParams } from "react-router";
import { Center, Heading, HStack, Image, SimpleGrid, Stack, Table, Text } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getTeamDetail } from "../../../services/team-service";
import { PredictableMatchesTable } from "../../../components/statistics/PredictableMatchesTable";
import { TeamFixturesTable } from "../../../components/team/TeamFixturesTable";
import { LeagueStandingsTable } from "../../../components/team/LeagueStandingsTable";
import { crestUrl } from "../../../utils/crestUrl";
import { Panel } from "../../../components/ui/panel";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function TeamDetailPage() {
    const { teamId } = useParams<{ teamId: string }>();
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!teamId || !currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <TeamDetailLoader key={`${currentCompetitionId}-${teamId}`} competitionId={currentCompetitionId} teamId={teamId} />;
}

function TeamDetailLoader({ competitionId, teamId }: { competitionId: string; teamId: string }) {
    const { data: team, error, reload } = useAsyncData(() => getTeamDetail(competitionId, teamId), [competitionId, teamId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (team === null) {
        return <LoadingSpinner />;
    }

    const crest = crestUrl(team.imageName);

    return (
        <Stack gap={8}>
            <SimpleGrid columns={{ base: 1, lg: 2 }} gap={8} alignItems="start">
                <Panel overflowX="auto" accent hoverLift>
                    <HStack mb={2}>
                        {crest && <Image src={crest} boxSize="32px" alt="" />}
                        <Heading size="md">{team.teamName}</Heading>
                    </HStack>
                    <Text fontWeight="bold">Goals for: {team.goalsFor}</Text>
                    <Text fontWeight="bold" mb={2}>Goals against: {team.goalsAgainst}</Text>

                    <Table.Root size="sm" variant="line">
                        <Table.Body>
                            {team.averageGoalsForHome !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals for (home):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsForHome.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                            {team.averageGoalsAgainstHome !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals against (home):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsAgainstHome.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                            {team.averageGoalsForAway !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals for (away):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsForAway.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                            {team.averageGoalsAgainstAway !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals against (away):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsAgainstAway.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                            {team.averageGoalsForTotal !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals for (total):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsForTotal.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                            {team.averageGoalsAgainstTotal !== null && (
                                <Table.Row>
                                    <Table.Cell>Average goals against (total):</Table.Cell>
                                    <Table.Cell>{team.averageGoalsAgainstTotal.toFixed(2)}</Table.Cell>
                                </Table.Row>
                            )}
                        </Table.Body>
                    </Table.Root>
                </Panel>

                <TeamFixturesTable fixtures={team.fixtures} teamId={team.teamID} />
            </SimpleGrid>

            <PredictableMatchesTable title="Results" matches={team.results} />

            {team.leagueTable && <LeagueStandingsTable standings={team.leagueTable} highlightTeamId={team.teamID} />}
        </Stack>
    );
}
