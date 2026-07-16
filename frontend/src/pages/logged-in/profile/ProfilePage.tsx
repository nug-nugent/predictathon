import { useEffect, useState } from "react";
import { Button, Center, SimpleGrid, Spinner, Text, VStack } from "@chakra-ui/react";
import { useParams } from "react-router";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import {
    getUserProfile, getUserPredictionHistory, getUserLeagueTable, getUserLeagueHistory,
    type UserProfile, type CompetitionUserLeagueTableItem, type UserCompetitionLeagueHistoryItem,
} from "../../../services/profile-service";
import { getCompetitionDetails } from "../../../services/competition-service";
import { getLeagueTable } from "../../../services/league-service";
import type { MatchPrediction } from "../../../services/prediction-service";
import { ProfileCard } from "../../../components/profile/ProfileCard";
import { ProfileStatisticsCard } from "../../../components/profile/ProfileStatisticsCard";
import { ProfilePredictionsTable } from "../../../components/profile/ProfilePredictionsTable";
import { ProfileLeagueTable } from "../../../components/profile/ProfileLeagueTable";
import { ProfileLeagueHistoryChart } from "../../../components/profile/ProfileLeagueHistoryChart";
import { ApiError } from "../../../services/api";

export function ProfilePage() {
    const { id } = useParams<{ id: string }>();
    const { user } = useUser();
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        if (!id) return;
        getUserProfile(id)
            .then(setProfile)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [id]);

    if (!id) {
        return null;
    }

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

    if (profile === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={4}>
            <ProfileCard profile={profile} isOwnProfile={profile.username === user?.name} />
            <ProfileCompetitionContent userId={id} />
        </VStack>
    );
}

function ProfileCompetitionContent({ userId }: { userId: string }) {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    if (!currentCompetitionId) {
        return null;
    }

    return <ProfileCompetitionData key={`${userId}-${currentCompetitionId}`} userId={userId} competitionId={currentCompetitionId} />;
}

function ProfileCompetitionData({ userId, competitionId }: { userId: string; competitionId: string }) {
    const [predictions, setPredictions] = useState<MatchPrediction[] | null>(null);
    const [leagueTable, setLeagueTable] = useState<CompetitionUserLeagueTableItem[] | "hidden" | null>(null);
    const [history, setHistory] = useState<UserCompetitionLeagueHistoryItem[] | null>(null);
    const [worstPosition, setWorstPosition] = useState<number>(1);
    const [username, setUsername] = useState<string | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const [profile, matchHistory, competition, leagueHistory, table] = await Promise.all([
                    getUserProfile(userId),
                    getUserPredictionHistory(competitionId, userId),
                    getCompetitionDetails(competitionId),
                    getUserLeagueHistory(competitionId, userId),
                    getLeagueTable(competitionId),
                ]);

                if (cancelled) return;
                setUsername(profile.username);
                setPredictions(matchHistory);
                setHistory(leagueHistory);
                setWorstPosition(table.reduce((max, r) => Math.max(max, r.leaguePosition), 1));

                if (competition.duplicateFixturesAllowed) {
                    setLeagueTable("hidden");
                } else {
                    setLeagueTable(await getUserLeagueTable(competitionId, userId));
                }
            } catch (err) {
                if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
            }
        };

        load();
        return () => { cancelled = true; };
    }, [userId, competitionId]);

    if (error) {
        return (
            <Center mt={4}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (predictions === null || leagueTable === null || username === null || history === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <SimpleGrid columns={{ base: 1, lg: 2 }} gap={6}>
            <VStack align="stretch" gap={4}>
                <ProfileStatisticsCard competitionId={competitionId} userId={userId} />
                <ProfileLeagueHistoryChart history={history} worstPosition={worstPosition} />
                {leagueTable !== "hidden" && <ProfileLeagueTable username={username} table={leagueTable} />}
            </VStack>
            <ProfilePredictionsTable predictions={predictions} />
        </SimpleGrid>
    );
}
