import { useEffect, useState } from "react";
import { useParams } from "react-router";
import { Button, Center, Heading, HStack, Image, SimpleGrid, Spinner, Table, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getTeamDetail, type TeamDetail } from "../../../services/team-service";
import { PredictableMatchesTable } from "../../../components/statistics/PredictableMatchesTable";
import { crestUrl } from "../../../utils/crestUrl";
import { ApiError } from "../../../services/api";
import { Panel } from "../../../components/ui/panel";

export function TeamDetailPage() {
    const { teamId } = useParams<{ teamId: string }>();
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
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
    const [team, setTeam] = useState<TeamDetail | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        setError(null);
        getTeamDetail(competitionId, teamId)
            .then(setTeam)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [competitionId, teamId]);

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={reload}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    if (team === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    const crest = crestUrl(team.imageName);

    return (
        <SimpleGrid columns={{ base: 1, lg: 2 }} gap={8} alignItems="start">
            <Panel overflowX="auto">
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

            <PredictableMatchesTable title="Results" matches={team.results} />
        </SimpleGrid>
    );
}
