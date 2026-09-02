import { Box, SimpleGrid, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { TodaysMatchesCard } from "../../../components/home/TodaysMatchesCard";
import { UserStatisticsCard } from "../../../components/home/UserStatisticsCard";
import { CompetitionRegistrationsCard } from "../../../components/home/CompetitionRegistrationsCard";
import { CompetitionSpotlightCard } from "../../../components/home/CompetitionSpotlightCard";
import { PredictionDeadlineCard } from "../../../components/home/PredictionDeadlineCard";
import { MiniLeagueTableCard } from "../../../components/home/MiniLeagueTableCard";
import { PredictionOfTheWeekCard } from "../../../components/home/PredictionOfTheWeekCard";
import { PersonalFormStripCard } from "../../../components/home/PersonalFormStripCard";
import { AnnouncementFeed } from "../../../components/announcements/AnnouncementFeed";
import { PageHeading } from "../../../components/ui/page-heading";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { getCompetitionsOpenForRegistration } from "../../../services/competition-registration-service";

// Split out of Home.tsx so it can be lazy()'d separately from the logged-out landing page - see
// the comment on Home.tsx's import of this file.
export function Dashboard() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return <CompetitionSignUpPrompt />;
    }

    return (
        <>
            <AnnouncementFeed audience="all" />
            {/* Full width above the two-column grid, and only present on days the competition
                actually has matches - see TodaysMatchesCard. */}
            <TodaysMatchesCard key={`live-${currentCompetitionId}`} competitionId={currentCompetitionId} />
            {/* Two stacked columns at lg and up. Below that the grid collapses to one column, and
                the columns would otherwise interleave as "all of the left, then all of the right",
                burying the league table below the competition list. display:contents dissolves the
                column wrappers on mobile so every card is a direct grid item, which in turn lets
                `order` decide where a card lands in the single-column stack. */}
            <SimpleGrid key={currentCompetitionId} columns={{ base: 1, lg: 2 }} gap={3}>
                <VStack align="stretch" gap={3} display={{ base: "contents", lg: "flex" }}>
                    <UserStatisticsCard competitionId={currentCompetitionId} />
                    <PredictionDeadlineCard competitionId={currentCompetitionId} />
                    {/* Housekeeping rather than competition news, so it goes to the bottom of the
                        mobile stack. _empty keeps the wrapper from leaving a phantom grid gap on
                        the (common) days the card itself renders nothing. */}
                    <Box order={{ base: 1, lg: 0 }} _empty={{ display: "none" }}>
                        <CompetitionRegistrationsCard />
                    </Box>
                </VStack>
                <VStack align="stretch" gap={3} display={{ base: "contents", lg: "flex" }}>
                    <MiniLeagueTableCard competitionId={currentCompetitionId} />
                    <PredictionOfTheWeekCard competitionId={currentCompetitionId} />
                    <PersonalFormStripCard competitionId={currentCompetitionId} />
                </VStack>
            </SimpleGrid>
        </>
    );
}

// Shown in place of the dashboard for a logged-in user who isn't registered for any competition
// yet. Other pages redirect back here for such users (see ProtectedRoute's requireCompetition),
// so this is effectively the only page (besides login) they can reach.
function CompetitionSignUpPrompt() {
    const { data: competitions, error } = useAsyncData(getCompetitionsOpenForRegistration, []);

    return (
        <VStack align="stretch" gap={4} maxW="640px" mx="auto" mt={4}>
            <PageHeading>Join a Competition</PageHeading>
            <Text color="fg.muted">You're not registered for any competitions yet. Sign up to one below to get started.</Text>

            {error && <ErrorState error={error} />}

            {competitions !== null && competitions.length === 0 && (
                <Text color="fg.muted">No competitions are currently open for registration.</Text>
            )}

            {competitions?.map((c) => (
                <CompetitionSpotlightCard
                    key={c.competitionID}
                    competition={c}
                    registerHref={`/competition/${c.competitionID}/register`}
                />
            ))}
        </VStack>
    );
}
