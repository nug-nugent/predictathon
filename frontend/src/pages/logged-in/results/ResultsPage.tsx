import { Center, Text } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { useCompetition } from "../../../hooks/useCompetition";
import { getMatchResults } from "../../../services/match-service";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { PageHeading } from "../../../components/ui/page-heading";
import { PredictableMatchesTable } from "../../../components/statistics/PredictableMatchesTable";

export function ResultsPage() {
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

    return <Results key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function Results({ competitionId }: { competitionId: string }) {
    const navigate = useNavigate();
    const { data: results, error, reload } = useAsyncData(() => getMatchResults(competitionId), [competitionId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (results === null) {
        return <LoadingSpinner />;
    }

    return (
        <>
            <PageHeading mb={4}>Results</PageHeading>
            <PredictableMatchesTable title="Results" matches={results} pageSize={20} onRowClick={(matchId) => { void navigate(`/match/${matchId}`); }} />
        </>
    );
}
