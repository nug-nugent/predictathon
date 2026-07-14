import { useEffect, useState } from "react";
import { Button, Center, Spinner, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../../hooks/useCompetition";
import { getMatchesForProcessing } from "../../../../services/match-processing-service";
import { getTeamsForCompetition, type Team } from "../../../../services/team-service";
import { ApiError } from "../../../../services/api";
import { ResultsList } from "./ResultsList";
import type { MatchAdmin } from "../../../../services/match-admin-service";

export function ProcessResultsPage() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
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
    const [matches, setMatches] = useState<MatchAdmin[] | null>(null);
    const [teams, setTeams] = useState<Team[]>([]);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        Promise.all([
            getMatchesForProcessing(competitionId),
            getTeamsForCompetition(competitionId),
        ])
            .then(([matchList, teamList]) => {
                setMatches(matchList);
                setTeams(teamList);
            })
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [competitionId]);

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    if (matches === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return <ResultsList matches={matches} teams={teams} />;
}
