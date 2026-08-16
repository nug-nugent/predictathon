import { Center, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCompetitionWeeks, getMatchesForWeek, computeDefaultWeek, type MatchPrediction } from "../../../services/prediction-service";
import { WeekPicker } from "../../../components/match/week-picker/WeekPicker";
import { MatchList } from "../../../components/match/match-list/MatchList";
import { ApiError } from "../../../services/api";
import { PageHeading } from "../../../components/ui/page-heading";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function PredictionsPage() {
  const { currentCompetitionId, isLoading } = useCompetition();
  const [searchParams] = useSearchParams();
  const requestedWeek = searchParams.get("week");

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
  return <PredictionsWeekLoader key={currentCompetitionId} competitionId={currentCompetitionId} requestedWeek={requestedWeek} />;
}

function toApiErrorOrGeneric(err: unknown): ApiError {
  return err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]);
}

function PredictionsWeekLoader({ competitionId, requestedWeek }: { competitionId: string; requestedWeek: string | null }) {
  const [weeks, setWeeks] = useState<string[] | null>(null);
  const [selectedWeek, setSelectedWeek] = useState<string | null>(null);
  const [matches, setMatches] = useState<MatchPrediction[] | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  // Guards against a slower, older request's response landing after a newer one (e.g. clicking
  // through weeks quickly) - only the response matching the latest request is ever applied.
  const requestSeq = useRef(0);

  useEffect(() => {
    let cancelled = false;
    const seq = ++requestSeq.current;

    getCompetitionWeeks(competitionId)
      .then((fetchedWeeks) => {
        if (cancelled) return;
        setWeeks(fetchedWeeks);

        // Honour a ?week= link (e.g. from the Home page's "Next prediction due" link) if it names
        // a real week, otherwise fall back to the usual current-week default.
        const defaultWeek = requestedWeek && fetchedWeeks.includes(requestedWeek) ? requestedWeek : computeDefaultWeek(fetchedWeeks);
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

  if (weeks === null) {
    if (error) {
      return <ErrorState error={error} onRetry={() => { setError(null); setRetryCount((c) => c + 1); }} />;
    }
    return <LoadingSpinner />;
  }

  return (
    <>
      <PageHeading mb={4}>Predictions</PageHeading>
      <WeekPicker weeks={weeks} selectedWeek={selectedWeek ?? weeks[0]} onWeekChange={changeWeek} />

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
