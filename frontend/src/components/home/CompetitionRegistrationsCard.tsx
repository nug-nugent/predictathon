import { useState } from "react";
import { Heading, Image, Table, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { useCompetition } from "../../hooks/useCompetition";
import {
    getAllCompetitionRegistrations, setDefaultCompetition, type UserCompetitionRegistration,
} from "../../services/competition-service";
import { competitionImageUrl } from "../../utils/competitionImageUrl";
import { formatDateOnly } from "../../utils/formatDateOnly";
import { ApiError } from "../../services/api";
import { Panel } from "../ui/panel";
import { ClickableRow } from "../ui/clickable-row";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

export function CompetitionRegistrationsCard() {
    const { setCurrentCompetitionId } = useCompetition();
    const navigate = useNavigate();
    const { data: registrations, error } = useAsyncData(getAllCompetitionRegistrations, []);
    // A failure switching to a competition, distinct from the list failing to load.
    const [switchError, setSwitchError] = useState<ApiError | null>(null);
    const [switching, setSwitching] = useState<string | null>(null);

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
            setSwitchError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
        } finally {
            setSwitching(null);
        }
    };

    if (error || switchError) {
        return <ErrorState error={(error ?? switchError)!} />;
    }

    if (registrations === null) {
        return <LoadingSpinner />;
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
                        <ClickableRow
                            key={r.competitionID}
                            opacity={switching === r.competitionID ? 0.6 : 1}
                            onActivate={() => selectCompetition(r)}
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
                        </ClickableRow>
                    ))}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
