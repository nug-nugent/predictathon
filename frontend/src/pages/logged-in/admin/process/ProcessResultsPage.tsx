import { Center, Text } from "@chakra-ui/react";
import { useCompetition } from "../../../../hooks/useCompetition";
import { getMatchesForProcessing } from "../../../../services/match-processing-service";
import { getTeamsForCompetition } from "../../../../services/team-service";
import { ResultsList } from "./ResultsList";
import { PageHeading } from "../../../../components/ui/page-heading";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

export function ProcessResultsPage() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <ProcessResultsLoader key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function ProcessResultsLoader({ competitionId }: { competitionId: string }) {
    const { data, error, reload } = useAsyncData(async () => {
        const [matches, teams] = await Promise.all([
            getMatchesForProcessing(competitionId),
            getTeamsForCompetition(competitionId),
        ]);
        return { matches, teams };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    return (
        <>
            <PageHeading mb={4}>Process Results</PageHeading>
            <ResultsList matches={data.matches} teams={data.teams} />
        </>
    );
}
