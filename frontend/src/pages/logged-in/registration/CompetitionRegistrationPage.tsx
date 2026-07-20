import { useNavigate, useParams } from "react-router";
import { Heading, VStack } from "@chakra-ui/react";
import { CompetitionSummaryCard } from "../../../components/registration/CompetitionSummaryCard";
import { PaymentStep } from "../../../components/registration/PaymentStep";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCompetitionForRegistration } from "../../../services/competition-registration-service";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function CompetitionRegistrationPage() {
    const { id } = useParams<{ id: string }>();

    if (!id) {
        return null;
    }

    return <CompetitionRegistrationLoader key={id} id={id} />;
}

function CompetitionRegistrationLoader({ id }: { id: string }) {
    const { setCurrentCompetitionId, refreshCompetitions } = useCompetition();
    const navigate = useNavigate();

    const { data: competition, error } = useAsyncData(() => getCompetitionForRegistration(id), [id]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (competition === null) {
        return <LoadingSpinner />;
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
