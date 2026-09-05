import { Box, HStack, Link as ChakraLink } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MatchPrediction } from "../../../services/prediction-service";
import type { MatchStatusValue } from "../matchStatus";
import { liveMatchHref } from "../../../utils/liveMatches";
import { LiveMatchLine } from "../live-match-line/LiveMatchLine";
import { YourPrediction } from "../your-prediction/YourPrediction";
import { QuickPredictPopover } from "../quick-predict/QuickPredictPopover";

type LiveMatchRowProps = {
    match: MatchPrediction;
    status: MatchStatusValue;
    /**
     * Opt in to entering the prediction here rather than going to the Predictions page. Only has
     * any effect on a match still open for predictions; the row is a link at every other stage.
     * Off by default - a list of matches you're following (the Live page) is a different thing
     * from a list of matches you can still act on.
     */
    quickPredict?: boolean;
    /** Minutes left to predict, needed by the quick-predict popover's deadline line. */
    minutesToPredict?: number;
    /** Called after a quick-predict save, so the caller can refresh what the row shows. */
    onPredictionSaved?: () => void;
};

/// One match as a whole-row target: the two teams, and what the match is worth to you on the right.
/// Usually a link to wherever the match is best followed; with `quickPredict` set, a match you can
/// still predict opens its prediction popover in place instead.
export function LiveMatchRow({ match, status, quickPredict, minutesToPredict, onPredictionSaved }: LiveMatchRowProps) {
    const content = (
        <HStack gap={{ base: 1, md: 2 }} px={2} py={2} width="full">
            <LiveMatchLine match={match} status={status} />
            <Box minW={{ base: "54px", md: "78px" }} textAlign="right" flexShrink={0}>
                <YourPrediction match={match} status={status} />
            </Box>
        </HStack>
    );

    if (quickPredict && status === "Pre") {
        return (
            <QuickPredictPopover match={match} minutesToPredict={minutesToPredict ?? 0} onSaved={onPredictionSaved}>
                {content}
            </QuickPredictPopover>
        );
    }

    return (
        <ChakraLink asChild variant="plain" display="block" borderRadius="8px"
            _hover={{ bg: "bg.muted", textDecoration: "none" }}
            _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus" }}>
            <RouterLink to={liveMatchHref(match, status)}>
                {content}
            </RouterLink>
        </ChakraLink>
    );
}
