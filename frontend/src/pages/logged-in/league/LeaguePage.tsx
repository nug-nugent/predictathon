import { Button, Center, HStack, Link as ChakraLink, Spinner, Table, Text, VStack } from "@chakra-ui/react"
import { useEffect, useState } from "react"
import { getLeagueTable, type LeagueTableItem } from "../../../services/league-service";
import { getCompetitionWeeks, computeDefaultWeek } from "../../../services/prediction-service";
import { useCompetition } from "../../../hooks/useCompetition";
import { ApiError } from "../../../services/api";
import { Link } from "react-router";
import { useSearchParams } from "react-router";
import { weekEnd } from "../../../utils/matchWeek";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";

// Optional ?date= filter, linked from the Home page's UserStatisticsCard rows.
type DateFilter = "ThisWeek" | "LastWeek";

export function LeaguePage() {
  const { currentCompetitionId, isLoading: competitionLoading } = useCompetition();
  const [searchParams] = useSearchParams();
  const rawDate = searchParams.get("date");
  const dateFilter: DateFilter | null = rawDate === "ThisWeek" || rawDate === "LastWeek" ? rawDate : null;

  if (competitionLoading) {
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

  // Keyed by competitionId (and filter) so switching either remounts this fresh (a new loading
  // spinner, not a flash of the previous stale table) instead of needing to reset state.
  return <LeagueTable key={`${currentCompetitionId}-${dateFilter ?? "all"}`} competitionId={currentCompetitionId} dateFilter={dateFilter} />;
}

// Resolves a ?date= filter to the [dateFrom, dateTo] range of the relevant match week, using the
// same current-week bucketing as the Home page cards. Null when the filter can't apply (e.g.
// "LastWeek" during the competition's first week).
async function resolveFilterRange(competitionId: string, dateFilter: DateFilter): Promise<{ dateFrom: string; dateTo: string } | null> {
  const weeks = await getCompetitionWeeks(competitionId);
  const currentWeek = computeDefaultWeek(weeks);

  const week = dateFilter === "ThisWeek"
    ? currentWeek
    : weeks.indexOf(currentWeek) > 0 ? weeks[weeks.indexOf(currentWeek) - 1] : "";

  return week ? { dateFrom: week, dateTo: weekEnd(week) } : null;
}

function LeagueTable({ competitionId, dateFilter }: { competitionId: string; dateFilter: DateFilter | null }) {
  const [items, setItems] = useState<LeagueTableItem[] | null>(null);
  // Whether the requested ?date= filter actually applied - false means it couldn't be resolved
  // to a match week and the full table is shown instead.
  const [filterApplied, setFilterApplied] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const range = dateFilter ? await resolveFilterRange(competitionId, dateFilter) : null;
        const data = await getLeagueTable(competitionId, range?.dateFrom, range?.dateTo);

        if (!cancelled) {
          setFilterApplied(range !== null);
          setItems(data);
        }
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
      }
    };

    load();
    return () => { cancelled = true; };
  }, [competitionId, dateFilter, retryCount]);

  if (error) {
    return (
      <Center mt={4}>
        <VStack gap={3}>
          <Text>{error.messages.join(" ")}</Text>
          <Button onClick={() => { setError(null); setRetryCount((c) => c + 1); }}>Try again</Button>
        </VStack>
      </Center>
    );
  }

  if (items === null) {
    return (
      <Center mt={4}>
        <Spinner />
      </Center>
    );
  }

  return (
    <>
      <PageHeading mb={4}>League Table</PageHeading>
      {dateFilter && (
        <HStack justify="center" mb={3} gap={2} wrap="wrap">
          <Text fontSize="sm" color="fg.muted">
            {filterApplied
              ? `Showing the ${dateFilter === "ThisWeek" ? "current" : "last"} match week only.`
              : `There's no ${dateFilter === "ThisWeek" ? "current" : "last"} match week yet - showing the full table.`}
          </Text>
          {filterApplied && (
            <ChakraLink asChild variant="underline" fontSize="sm">
              <Link to="/league">Show full table</Link>
            </ChakraLink>
          )}
        </HStack>
      )}
      <Panel overflowX="auto">
        <Table.Root size="sm" variant="line" striped showColumnBorder stickyHeader>
          <Table.ColumnGroup>
            <Table.Column htmlWidth="20px" />
            <Table.Column htmlWidth="50%" />
            <Table.Column />
            <Table.Column />
            <Table.Column />
            <Table.Column />
            <Table.Column />
            <Table.Column />
            <Table.Column />
          </Table.ColumnGroup>
          <Table.Header>
            <Table.Row>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POS</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>NAME</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>3</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>2</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>1</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>0</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>L</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POINTS</Table.ColumnHeader>
              <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>AGD</Table.ColumnHeader>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {items.map((item) => (
              <Table.Row key={item.userID}>
                <Table.Cell fontSize={"0.9em"} textAlign={"right"}>{item.leaguePosition}</Table.Cell>
                <Table.Cell fontSize={"0.9em"}><Link to={`/profile/${item.userID}`}>{item.username}</Link></Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.3"} display={{ base: "none", sm: "table-cell" }}>{item.threePointers}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.2"} display={{ base: "none", sm: "table-cell" }}>{item.twoPointers}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.1"} display={{ base: "none", sm: "table-cell" }}>{item.onePointers}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPointers}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPredictions}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.score}</Table.Cell>
                <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.averageGoalDifference}</Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table.Root>
      </Panel>
    </>
  );
}
