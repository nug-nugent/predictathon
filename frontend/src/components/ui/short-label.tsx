import { Box } from "@chakra-ui/react";

/**
 * A label with a shorter wording for small screens. Table column headers are usually what pushes a
 * table past the edge of a phone - "Average score" needs more room than the two-digit numbers
 * underneath it - so the narrow form is shown below `md` and the full wording from `md` up.
 *
 * Only one of the two is rendered visible at a time, and the other is `display: none`, so a screen
 * reader announces exactly the label that is on screen rather than both.
 */
export function ShortLabel({ short, full }: { short: string; full: string }) {
    return (
        <>
            <Box as="span" hideFrom="md">{short}</Box>
            <Box as="span" hideBelow="md">{full}</Box>
        </>
    );
}
