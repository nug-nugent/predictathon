import { useEffect, useState } from "react";
import { Center, Heading, Image, Spinner, Table, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { useCompetition } from "../../hooks/useCompetition";
import {
    getAllCompetitionRegistrations, setDefaultCompetition, type UserCompetitionRegistration,
} from "../../services/competition-service";
import { competitionImageUrl } from "../../utils/competitionImageUrl";
import { formatDateOnly } from "../../utils/formatDateOnly";
import { ApiError } from "../../services/api";
import { Panel } from "../ui/panel";

export function CompetitionRegistrationsCard() {
    const { setCurrentCompetitionId } = useCompetition();
    const navigate = useNavigate();
    const [registrations, setRegistrations] = useState<UserCompetitionRegistration[] | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [switching, setSwitching] = useState<string | null>(null);

    useEffect(() => {
        getAllCompetitionRegistrations()
            .then(setRegistrations)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    }, []);

    const selectCompetition = async (registration: UserCompetitionRegistration) => {
        if (!registration.registered) {
            navigate(`/competition/${registration.competitionID}/register`);
            return;
        }

        setSwitching(registration.competitionID);
        try {
            await setDefaultCompetition(registration.competitionID);
            setCurrentCompetitionId(registration.competitionID);
        } catch (err) {
            setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
        } finally {
            setSwitching(null);
        }
    };

    if (error) {
        return (
            <Center py={4}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (registrations === null) {
        return (
            <Center py={4}>
                <Spinner />
            </Center>
        );
    }

    // Matches legacy: hide the whole widget when there's nothing worth choosing between.
    if (registrations.length <= 1) {
        return null;
    }

    return (
        <Panel>
            <Heading size="md" mb={2}>Competitions</Heading>
            <Table.Root size="sm" variant="line">
                <Table.Body>
                    {registrations.map((r) => (
                        <Table.Row
                            key={r.competitionID}
                            cursor="pointer"
                            opacity={switching === r.competitionID ? 0.6 : 1}
                            _hover={{ bg: "bg.muted" }}
                            onClick={() => selectCompetition(r)}
                        >
                            <Table.Cell width="48px">
                                {competitionImageUrl(r.imageFilename) && (
                                    <Image src={competitionImageUrl(r.imageFilename)} maxH="32px" maxW="32px" alt="" />
                                )}
                            </Table.Cell>
                            <Table.Cell>{r.competitionName}</Table.Cell>
                            <Table.Cell textAlign="right">
                                {r.registered ? (
                                    <VStack align="end" gap={0}>
                                        <Text color="green.600" fontWeight="bold">Registered</Text>
                                        <Text fontSize="xs" color="fg.muted">Click here to select</Text>
                                    </VStack>
                                ) : (
                                    <VStack align="end" gap={0}>
                                        <Text fontSize="xs">
                                            {new Date(r.startDate) <= new Date() ? "Started" : "Starts"} {formatDateOnly(r.startDate.slice(0, 10))}
                                        </Text>
                                        <Text fontSize="xs">Entry fee: £{r.entranceFee.toFixed(2)}</Text>
                                        <Text fontSize="xs" color="fg.muted">Click here to register</Text>
                                    </VStack>
                                )}
                            </Table.Cell>
                        </Table.Row>
                    ))}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
