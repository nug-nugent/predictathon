import { Box } from "@chakra-ui/react";
import { MoveRight, TrendingDown, TrendingUp } from "lucide-react";

// Shows how a user's league position has moved since the comparison date - mirrors the legacy
// UpArrow/DownArrow/NoChange.gif icons from the League Table page. Note a lower position number is
// better, so a decrease is an improvement (up) and an increase is a decline (down). Renders nothing
// when there's no previous position to compare against (e.g. the user's first week, or the table's
// been date-filtered so no comparison was requested).
export function LeaguePositionChangeIcon({ current, previous }: { current: number; previous: number | null }) {
    if (previous === null) return null;
    if (previous === current) return <Box as="span" color="fg.muted" display="inline-flex"><MoveRight size={16} /></Box>;
    if (previous > current) return <Box as="span" color="green.500" display="inline-flex"><TrendingUp size={16} /></Box>;
    return <Box as="span" color="red.500" display="inline-flex"><TrendingDown size={16} /></Box>;
}
