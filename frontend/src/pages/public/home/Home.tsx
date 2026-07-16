import { Center, Link, SimpleGrid, Spinner, Text, VStack } from "@chakra-ui/react";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import { Link as RouterLink } from "react-router";
import { UserStatisticsCard } from "../../../components/home/UserStatisticsCard";
import { CompetitionRegistrationsCard } from "../../../components/home/CompetitionRegistrationsCard";
import { HomeMatchWeeksSection } from "../../../components/home/HomeMatchWeeksSection";

export function HomePage() {
  const { user, isLoading: userLoading } = useUser();

  if (userLoading) {
    return (
      <Center mt={4}>
        <Spinner />
      </Center>
    );
  }

  if (!user) {
    return (
      <Text>Welcome! Login above or <Link asChild variant={"underline"}><RouterLink to={"/register"}>register here</RouterLink></Link>.</Text>
    );
  }

  return <Dashboard />;
}

function Dashboard() {
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

  return (
    <SimpleGrid key={currentCompetitionId} columns={{ base: 1, lg: 2 }} gap={6}>
      <VStack align="stretch" gap={4}>
        <UserStatisticsCard competitionId={currentCompetitionId} />
        <CompetitionRegistrationsCard />
      </VStack>
      <HomeMatchWeeksSection competitionId={currentCompetitionId} />
    </SimpleGrid>
  );
}
