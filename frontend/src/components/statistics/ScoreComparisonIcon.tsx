import { Box } from "@chakra-ui/react";
import { Minus, TrendingDown, TrendingUp } from "lucide-react";

// Compares a user's own prediction score against the average for that match/team - mirrors the
// legacy UpArrow/DownArrow/NoChange.gif icons used across the Statistics user controls.
export function ScoreComparisonIcon({ yours, average }: { yours: number; average: number }) {
    if (yours > average) return <Box as="span" color="green.500" display="inline-flex"><TrendingUp size={16} /></Box>;
    if (yours < average) return <Box as="span" color="red.500" display="inline-flex"><TrendingDown size={16} /></Box>;
    return <Box as="span" color="fg.muted" display="inline-flex"><Minus size={16} /></Box>;
}
