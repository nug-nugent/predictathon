import { Center, HStack, Link as ChakraLink, Text } from "@chakra-ui/react"
import { getLeagueTable } from "../../../services/league-service";
import { getCompetitionWeeks, computeDefaultWeek } from "../../../services/prediction-service";
import { useCompetition } from "../../../hooks/useCompetition";
import { Link } from "react-router";
import { useSearchParams } from "react-router";
import { weekEnd } from "../../../utils/matchWeek";
import { toDateOnly } from "../../../utils/toDateOnly";
import { PageHeading } from "../../../components/ui/page-heading";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";
import { LeagueTableView } from "../../../components/league/LeagueTableView";

// Optional ?date= filter, linked from the Home page's UserStatisticsCard rows.
type DateFilter = "ThisWeek" | "LastWeek";

export function LeaguePage() {
  const { currentCompetitionId, isLoading: competitionLoading } = useCompetition();
  const [searchParams] = useSearchParams();
  const rawDate = searchParams.get("date");
  const dateFilter: DateFilter | null = rawDate === "ThisWeek" || rawDate === "LastWeek" ? rawDate : null;

  if (competitionLoading) {
    return <LoadingSpinner />;
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
  // filterApplied: whether the requested ?date= filter actually applied - false means it couldn't
  // be resolved to a match week and the full table is shown instead.
  const { data, error, reload } = useAsyncData(async () => {
    const range = dateFilter ? await resolveFilterRange(competitionId, dateFilter) : null;
    // Position-change arrows only make sense against the full, unfiltered table - a date-filtered
    // view (?date=ThisWeek/LastWeek) already only covers a single match week.
    const dateForComparison = dateFilter ? undefined : toDateOnly(new Date());
    const items = await getLeagueTable(competitionId, range?.dateFrom, range?.dateTo, dateForComparison);
    return { items, filterApplied: range !== null };
  }, [competitionId, dateFilter]);

  if (error) {
    return <ErrorState error={error} onRetry={reload} />;
  }

  if (data === null) {
    return <LoadingSpinner />;
  }

  const { items, filterApplied } = data;

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
      <LeagueTableView items={items} />
    </>
  );
}
