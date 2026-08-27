import { Center, Text } from "@chakra-ui/react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCompetitionWeekSummaries, getMatchesForWeek, computePredictionsLandingWeek, type CompetitionWeekSummary, type MatchPrediction } from "../../../services/prediction-service";
import { WeekPicker } from "../../../components/match/week-picker/WeekPicker";
import { MatchList } from "../../../components/match/match-list/MatchList";
import { ApiError } from "../../../services/api";
import { PageHeading } from "../../../components/ui/page-heading";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function PredictionsPage() {
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

  // Keyed by competitionId so switching competitions remounts this fresh instead of needing to
  // reset state - same pattern as LeaguePage.
  return <PredictionsWeekLoader key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function toApiErrorOrGeneric(err: unknown): ApiError {
  return err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]);
}

function PredictionsWeekLoader({ competitionId }: { competitionId: string }) {
  const [searchParams, setSearchParams] = useSearchParams();
  const [summaries, setSummaries] = useState<CompetitionWeekSummary[] | null>(null);
  const [selectedWeek, setSelectedWeek] = useState<string | null>(null);
  const [matches, setMatches] = useState<MatchPrediction[] | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  // Read once, on arrival. The selected week gets stamped back into ?week= below, so reading it
  // live here (and depending on it) would re-run the initial load against our own write.
  const requestedWeek = useRef(searchParams.get("week")).current;

  // Guards against a slower, older request's response landing after a newer one (e.g. clicking
  // through weeks quickly) - only the response matching the latest request is ever applied.
  const requestSeq = useRef(0);

  useEffect(() => {
    let cancelled = false;
    const seq = ++requestSeq.current;

    getCompetitionWeekSummaries(competitionId)
      .then((fetchedSummaries) => {
        if (cancelled) return;
        setSummaries(fetchedSummaries);

        // Honour a ?week= link (e.g. from the Home page's "Next prediction due" link) if it names a
        // real week, otherwise land on whichever week still has something to do.
        const weeks = fetchedSummaries.map((s) => s.weekStart);
        const defaultWeek = requestedWeek && weeks.includes(requestedWeek)
          ? requestedWeek
          : computePredictionsLandingWeek(fetchedSummaries, new Date());
        setSelectedWeek(defaultWeek);

        if (!defaultWeek) {
          setMatches([]);
          return;
        }

        return getMatchesForWeek(competitionId, defaultWeek).then((data) => {
          if (!cancelled && requestSeq.current === seq) setMatches(data);
        });
      })
      .catch((err) => {
        if (!cancelled) setError(toApiErrorOrGeneric(err));
      });

    return () => { cancelled = true; };
  }, [competitionId, retryCount, requestedWeek]);

  // Keep ?week= pointing at whatever's on screen, so a refresh or a back-navigation returns to the
  // same week rather than silently re-resolving to a different one once a deadline passes.
  useEffect(() => {
    if (!selectedWeek || searchParams.get("week") === selectedWeek) {
      return;
    }

    const updated = new URLSearchParams(searchParams);
    updated.set("week", selectedWeek);
    setSearchParams(updated, { replace: true });
  }, [selectedWeek, searchParams, setSearchParams]);

  const changeWeek = (dateFrom: string) => {
    setSelectedWeek(dateFrom);
    setMatches(null);
    setError(null);

    const seq = ++requestSeq.current;
    getMatchesForWeek(competitionId, dateFrom)
      .then((data) => {
        if (requestSeq.current === seq) setMatches(data);
      })
      .catch((err) => {
        if (requestSeq.current === seq) setError(toApiErrorOrGeneric(err));
      });
  };

  const outstanding = useMemo(
    () => new Map((summaries ?? []).filter((s) => s.openUnpredictedCount > 0).map((s) => [s.weekStart, s.openUnpredictedCount])),
    [summaries]
  );

  if (summaries === null) {
    if (error) {
      return <ErrorState error={error} onRetry={() => { setError(null); setRetryCount((c) => c + 1); }} />;
    }
    return <LoadingSpinner />;
  }

  const weeks = summaries.map((s) => s.weekStart);

  return (
    <>
      <PageHeading mb={4}>Predictions</PageHeading>
      <WeekPicker weeks={weeks} selectedWeek={selectedWeek ?? weeks[0]} onWeekChange={changeWeek} outstanding={outstanding} />

      {error ? (
        <ErrorState error={error} onRetry={() => selectedWeek && changeWeek(selectedWeek)} />
      ) : matches === null ? (
        <LoadingSpinner />
      ) : (
        <MatchList key={selectedWeek} matches={matches} />
      )}
    </>
  );
}
