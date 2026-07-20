import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { Center, Heading, Spinner, Text, VStack } from "@chakra-ui/react";
import { CompetitionSummaryCard } from "../../../components/registration/CompetitionSummaryCard";
import { PaymentStep } from "../../../components/registration/PaymentStep";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCompetitionForRegistration, type CompetitionRegistrationDetails } from "../../../services/competition-registration-service";
import { ApiError } from "../../../services/api";

export function CompetitionRegistrationPage() {
    const { id } = useParams<{ id: string }>();
    const { setCurrentCompetitionId, refreshCompetitions } = useCompetition();
    const navigate = useNavigate();

    const [competition, setCompetition] = useState<CompetitionRegistrationDetails | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        if (!id) return;

        // Error is cleared in the success callback (not synchronously here) so this stays safe to
        // run directly in an effect - see react-hooks/set-state-in-effect.
        getCompetitionForRegistration(id)
            .then((c) => { setCompetition(c); setError(null); })
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    }, [id]);

    if (!id) {
        return null;
    }

    if (error) {
        return (
            <Center mt={4}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (competition === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={6} maxW="container.sm" mx="auto">
            <Heading size="lg">Join a competition</Heading>
            <CompetitionSummaryCard competition={competition} />
            <PaymentStep
                competitionId={competition.competitionID}
                entranceFee={competition.entranceFee}
                payPalPaymentAvailable={competition.payPalPaymentAvailable}
                onRegistered={async () => {
                    // Refresh the shared competitions list before switching to the new one, so it
                    // shows up in the header's CompetitionSelector rather than staying stuck on
                    // the snapshot fetched before this registration happened.
                    await refreshCompetitions();
                    setCurrentCompetitionId(competition.competitionID);
                    navigate("/");
                }}
            />
        </VStack>
    );
}
