import { Box, Collapsible, Heading, HStack, Text } from "@chakra-ui/react";
import { ChevronDown } from "lucide-react";
import { getLiveLeagueTable } from "../../services/league-service";
import { useAsyncData } from "../../hooks/useAsyncData";
import { usePolling } from "../../hooks/usePolling";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { Panel } from "../ui/panel";
import { LeagueTableView } from "./LeagueTableView";

// In step with the Live page's own refresh: the points below move when a live score does, so a
// table that lagged behind the scoreline it's derived from would be worse than no table.
const REFRESH_MS = 30000;

/// The league table as it stands, with what each match in play is worth to each user so far. Open by
/// default - while a match is being played this is the thing people are actually here to watch - and
/// foldable for when the match in front of you is all you want.
///
/// Rows stay in their real standing order rather than being re-sorted by projected total - a table
/// that reshuffled itself every time a goal went in would be unreadable at exactly the moment it's
/// most worth reading. Where the projection would put someone is shown as an arrow beside their
/// current position instead.
///
/// The table itself is the same LeagueTableView the League page uses, so the two stay recognisably
/// the same table.
export function LiveLeagueTable({ competitionId }: { competitionId: string }) {
    const { data: table, error, reload } = useAsyncData(() => getLiveLeagueTable(competitionId), [competitionId]);

    usePolling(reload, REFRESH_MS);

    return (
        <Panel accent p={3}>
            <Collapsible.Root defaultOpen>
                <Collapsible.Trigger width="full" cursor="pointer">
                    <HStack justify="space-between" width="full">
                        <Heading size="sm">Live League Table</Heading>
                        {/* The chevron is the only affordance, so it turns to say which way this
                            goes - "open" and "shut" shouldn't look identical. Selecting on an
                            ancestor's state rather than _open, because data-state sits on the
                            trigger button, not on this box inside it. */}
                        <Box transition="transform 0.15s" color="fg.muted"
                            css={{ "[data-state=open] &": { transform: "rotate(180deg)" } }}>
                            <ChevronDown size={18} />
                        </Box>
                    </HStack>
                </Collapsible.Trigger>

                <Collapsible.Content>
                    <Box pt={3}>
                        {error ? <ErrorState error={error} onRetry={reload} />
                            : table === null ? <LoadingSpinner />
                                : table.length === 0
                                    ? <Text color="fg.muted" fontSize="sm">Nobody has registered for this competition yet.</Text>
                                    : <LeagueTableView items={table} showLivePoints bare />}
                    </Box>
                </Collapsible.Content>
            </Collapsible.Root>
        </Panel>
    );
}
