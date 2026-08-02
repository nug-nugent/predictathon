import { Box, Center, Heading, Link, SimpleGrid, Spinner, Text, VStack } from "@chakra-ui/react";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import { Link as RouterLink } from "react-router";
import { UserStatisticsCard } from "../../../components/home/UserStatisticsCard";
import { CompetitionRegistrationsCard } from "../../../components/home/CompetitionRegistrationsCard";
import { PredictionDeadlineCard } from "../../../components/home/PredictionDeadlineCard";
import { MiniLeagueTableCard } from "../../../components/home/MiniLeagueTableCard";
import { PredictionOfTheWeekCard } from "../../../components/home/PredictionOfTheWeekCard";
import { PersonalFormStripCard } from "../../../components/home/PersonalFormStripCard";
import { LoginForm } from "../../../components/site-header/login-button/LoginForm";
import { CompetitionSummaryCard } from "../../../components/registration/CompetitionSummaryCard";
import { getCompetitionsOpenForRegistration } from "../../../services/competition-registration-service";
import { AnnouncementFeed } from "../../../components/announcements/AnnouncementFeed";
import { PageHeading } from "../../../components/ui/page-heading";
import { useAsyncData } from "../../../hooks/useAsyncData";
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

function LoggedOutLanding() {
    const { data: competitions, error } = useAsyncData(getCompetitionsOpenForRegistration, []);

    return (
        <>
            <AnnouncementFeed audience="loginPage" />
            <SimpleGrid columns={{ base: 1, md: 2 }} gap={6} maxW="container.md" mx="auto">
                <LoginForm />

                <VStack align="stretch" gap={3}>
                    <Heading size="md">Register</Heading>

                    {error && <Text>{error.messages.join(" ")}</Text>}

                    {competitions === null && !error && (
                        <Center py={4}>
                            <Spinner />
                        </Center>
                    )}

                    {competitions !== null && competitions.length === 0 && (
                        <Text color="fg.muted">No competitions are currently open for registration.</Text>
                    )}

                    {competitions?.map((c) => (
                        <Box key={c.competitionID}>
                            <CompetitionSummaryCard competition={c} />
                            <Link asChild variant="underline">
                                <RouterLink to={`/register?competitionId=${c.competitionID}`}>Register for {c.competitionName}</RouterLink>
                            </Link>
                        </Box>
                    ))}
                </VStack>
            </SimpleGrid>
        </>
    );
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
