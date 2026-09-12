import { getTodaysMatches } from "../../services/match-service";
import { groupLiveMatches, hasLiveDayMatches } from "../../utils/liveMatches";
import { useAsyncData } from "../../hooks/useAsyncData";
import { useMinuteTick } from "../../hooks/useMinuteTick";
import { usePolling } from "../../hooks/usePolling";
import { TodaysMatchesCard } from "./TodaysMatchesCard";
import { ThisWeeksMatchesCard } from "./ThisWeeksMatchesCard";
import { ErrorState } from "../ui/async-state";

// A minute is plenty: the only things that change under this card are an admin confirming a result
// and matches crossing their kick-off, and useMinuteTick already re-buckets the latter for free.
const REFRESH_MS = 60000;

/// The football at the top of the Home page. On a matchday that's Today's Matches; on the days in
/// between - which is most of them - it's This Week's Matches instead, so the section always has
/// something to say rather than leaving the page to open on statistics.
///
/// Which of the two it is turns on the one question today's matches answer, so this owns that fetch
/// and hands the answer down rather than having both cards ask.
export function HomeMatchesCard({ competitionId }: { competitionId: string }) {
    const now = useMinuteTick();
    const { data: matches, error, reload } = useAsyncData(() => getTodaysMatches(competitionId), [competitionId]);

    usePolling(reload, REFRESH_MS);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    // No spinner while the first load is in flight, and no fallback to the week either: a card that
    // appeared as one thing and then became another would be worse than a moment of nothing.
    if (matches === null) {
        return null;
    }

    const groups = groupLiveMatches(matches, now);

    if (!hasLiveDayMatches(groups)) {
        return <ThisWeeksMatchesCard competitionId={competitionId} />;
    }

    return <TodaysMatchesCard groups={groups} now={now} onPredictionSaved={reload} />;
}
