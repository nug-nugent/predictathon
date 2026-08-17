import { Box } from "@chakra-ui/react";
import { CompetitionSelector } from "./CompetitionSelector";

type CompetitionNameRowProps = {
    /** Depends on the background it sits on - default to the header's blue-block treatment. */
    color?: string;
};

/** Standard caption styling for the competition name, wherever it's shown alongside the wordmark. */
export function CompetitionNameRow({ color = "brand.subtitleFg" }: CompetitionNameRowProps) {
    return (
        <Box color={color} fontFamily="body" fontWeight="bold" fontSize="11px" textTransform="uppercase" minW="0">
            <CompetitionSelector />
        </Box>
    );
}
