import { Center, Spinner, Text } from "@chakra-ui/react";
import { useEffect, useState } from "react";
import { useCompetition } from "../../../providers/CompetitionProvider";
import { getCompetitionWeeks, getMatchesForWeek, computeDefaultWeek, type MatchPrediction } from "../../../services/prediction-service";
import { WeekPicker } from "../../../components/match/week-picker/WeekPicker";
import { MatchList } from "../../../components/match/match-list/MatchList";

export function PredictionsPage() {
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

  // Keyed by competitionId so switching competitions remounts this fresh instead of needing to
  // reset state - same pattern as LeaguePage.
  return <PredictionsWeekLoader key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function PredictionsWeekLoader({ competitionId }: { competitionId: string }) {
  const [weeks, setWeeks] = useState<string[] | null>(null);
  const [selectedWeek, setSelectedWeek] = useState<string | null>(null);
  const [matches, setMatches] = useState<MatchPrediction[] | null>(null);

  useEffect(() => {
    getCompetitionWeeks(competitionId).then((fetchedWeeks) => {
      setWeeks(fetchedWeeks);

      const defaultWeek = computeDefaultWeek(fetchedWeeks);
      setSelectedWeek(defaultWeek);

      if (!defaultWeek) {
        setMatches([]);
        return;
      }

      getMatchesForWeek(competitionId, defaultWeek).then(setMatches);
    });
  }, [competitionId]);

  const changeWeek = (dateFrom: string) => {
    setSelectedWeek(dateFrom);
    setMatches(null);
    getMatchesForWeek(competitionId, dateFrom).then(setMatches);
  };

  if (weeks === null || selectedWeek === null || matches === null) {
    return (
      <Center mt={4}>
        <Spinner />
      </Center>
    );
  }

  return (
    <>
      <WeekPicker weeks={weeks} selectedWeek={selectedWeek} onWeekChange={changeWeek} />
      <MatchList key={selectedWeek} matches={matches} />
    </>
  );
}
