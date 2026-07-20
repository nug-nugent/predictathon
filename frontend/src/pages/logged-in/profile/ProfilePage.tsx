import { SimpleGrid, VStack } from "@chakra-ui/react";
import { useParams } from "react-router";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import {
    getUserProfile, getUserPredictionHistory, getUserLeagueTable, getUserLeagueHistory,
    type CompetitionUserLeagueTableItem,
} from "../../../services/profile-service";
import { getCompetitionDetails } from "../../../services/competition-service";
import { getLeagueTable } from "../../../services/league-service";
import { ProfileCard } from "../../../components/profile/ProfileCard";
import { ProfileStatisticsCard } from "../../../components/profile/ProfileStatisticsCard";
import { ProfilePredictionsTable } from "../../../components/profile/ProfilePredictionsTable";
import { ProfileLeagueTable } from "../../../components/profile/ProfileLeagueTable";
import { ProfileLeagueHistoryChart } from "../../../components/profile/ProfileLeagueHistoryChart";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function ProfilePage() {
    const { id } = useParams<{ id: string }>();

    if (!id) {
        return null;
    }

    return <ProfileLoader key={id} userId={id} />;
}

function ProfileLoader({ userId }: { userId: string }) {
    const { user } = useUser();
    const { data: profile, error, reload } = useAsyncData(() => getUserProfile(userId), [userId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (profile === null) {
        return <LoadingSpinner />;
    }

    return (
        <VStack align="stretch" gap={4}>
            <ProfileCard profile={profile} isOwnProfile={profile.userID === user?.id} />
            <ProfileCompetitionContent userId={userId} username={profile.username} />
        </VStack>
    );
}

function ProfileCompetitionContent({ userId, username }: { userId: string; username: string }) {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return null;
    }

    return <ProfileCompetitionData key={`${userId}-${currentCompetitionId}`} userId={userId} username={username} competitionId={currentCompetitionId} />;
}

function ProfileCompetitionData({ userId, username, competitionId }: { userId: string; username: string; competitionId: string }) {
    const { data, error } = useAsyncData(async () => {
        const [predictions, competition, history, table] = await Promise.all([
            getUserPredictionHistory(competitionId, userId),
            getCompetitionDetails(competitionId),
            getUserLeagueHistory(competitionId, userId),
            getLeagueTable(competitionId),
        ]);

        // The what-if league table is meaningless when duplicate fixtures are allowed (the same
        // fixture can occur twice), so it's hidden for those competitions.
        const leagueTable: CompetitionUserLeagueTableItem[] | "hidden" = competition.duplicateFixturesAllowed
            ? "hidden"
            : await getUserLeagueTable(competitionId, userId);

        return {
            predictions,
            history,
            leagueTable,
            worstPosition: table.reduce((max, r) => Math.max(max, r.leaguePosition), 1),
        };
    }, [userId, competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    return (
        <SimpleGrid columns={{ base: 1, lg: 2 }} gap={6}>
            <VStack align="stretch" gap={4}>
                <ProfileStatisticsCard competitionId={competitionId} userId={userId} />
                <ProfileLeagueHistoryChart history={data.history} worstPosition={data.worstPosition} />
                {data.leagueTable !== "hidden" && <ProfileLeagueTable username={username} table={data.leagueTable} />}
            </VStack>
            <ProfilePredictionsTable predictions={data.predictions} />
        </SimpleGrid>
    );
}
