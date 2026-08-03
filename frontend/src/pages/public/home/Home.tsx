import { Center, SimpleGrid, Text, VStack } from "@chakra-ui/react";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import { UserStatisticsCard } from "../../../components/home/UserStatisticsCard";
import { CompetitionRegistrationsCard } from "../../../components/home/CompetitionRegistrationsCard";
import { PredictionDeadlineCard } from "../../../components/home/PredictionDeadlineCard";
import { MiniLeagueTableCard } from "../../../components/home/MiniLeagueTableCard";
import { PredictionOfTheWeekCard } from "../../../components/home/PredictionOfTheWeekCard";
import { PersonalFormStripCard } from "../../../components/home/PersonalFormStripCard";
import { LoggedOutLanding } from "./LoggedOutLanding";
import { AnnouncementFeed } from "../../../components/announcements/AnnouncementFeed";
import { PageHeading } from "../../../components/ui/page-heading";
import { LoadingSpinner } from "../../../components/ui/async-state";

export function HomePage() {
    const { user, isLoading: userLoading } = useUser();

    if (userLoading) {
        return <LoadingSpinner />;
    }

    if (!user) {
        return <LoggedOutLanding />;
    }

    return <Dashboard />;
}

function Dashboard() {
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

    return (
        <>
            <AnnouncementFeed audience="all" />
            <PageHeading mb={4}>Home</PageHeading>
            <SimpleGrid key={currentCompetitionId} columns={{ base: 1, lg: 2 }} gap={3}>
                <VStack align="stretch" gap={3}>
                    <UserStatisticsCard competitionId={currentCompetitionId} />
                    <PredictionDeadlineCard competitionId={currentCompetitionId} />
                    <CompetitionRegistrationsCard />
                </VStack>
                <VStack align="stretch" gap={3}>
                    <MiniLeagueTableCard competitionId={currentCompetitionId} />
                    <PredictionOfTheWeekCard competitionId={currentCompetitionId} />
                    <PersonalFormStripCard competitionId={currentCompetitionId} />
                </VStack>
            </SimpleGrid>
        </>
    );
}
