import { getAllTimeLeagueTable } from "../../../services/statistics-service";
import { LeagueTableView } from "../../../components/league/LeagueTableView";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function AllTimeLeagueTableTab() {
    const { data: items, error, reload } = useAsyncData(getAllTimeLeagueTable, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (items === null) {
        return <LoadingSpinner />;
    }

    return <LeagueTableView items={items} showAveragePointsPerPrediction />;
}
