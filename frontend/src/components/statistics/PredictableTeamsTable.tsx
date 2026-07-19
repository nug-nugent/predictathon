import { Heading, HStack, Image, Table, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import type { PredictableTeam } from "../../services/statistics-service";
import { crestUrl } from "../../utils/crestUrl";
import { Panel } from "../ui/panel";

export function PredictableTeamsTable({ teams }: { teams: PredictableTeam[] }) {
    const navigate = useNavigate();

    return (
        <Panel overflowX="auto">
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>Average Prediction Score By Team</Heading>
                <Table.Root size="sm" variant="line">
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Team</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Average score</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {teams.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={2}>
                                    <Text color="fg.muted">No teams found</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : teams.map((t) => (
                            <Table.Row key={t.teamID} onClick={() => navigate(`/team/${t.teamID}`)} cursor="pointer" _hover={{ bg: "bg.muted" }}>
                                <Table.Cell>
                                    <HStack gap={2}>
                                        {crestUrl(t.teamImage) && <Image src={crestUrl(t.teamImage)} h="16px" alt="" />}
                                        <Text>{t.shortName}</Text>
                                    </HStack>
                                </Table.Cell>
                                <Table.Cell textAlign="center">{t.averageScore.toFixed(2)}</Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>
            </VStack>
        </Panel>
    );
}
