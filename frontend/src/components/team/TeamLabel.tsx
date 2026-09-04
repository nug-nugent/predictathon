import { Box, VisuallyHidden } from "@chakra-ui/react";

/**
 * The names a team can be shown by, widest first. Every field is nullable: a not-yet-decided
 * knockout slot ("Winner QF1") has no team record behind it, and a team added in a hurry may not
 * have an acronym yet.
 */
export type TeamNames = {
    /** The full name, e.g. "Brighton & Hove Albion". */
    name: string | null;
    /** The everyday short form, e.g. "Brighton". */
    shortName: string | null;
    /** The three-letter code, e.g. "BHA" - see the Acronym column in 01_Teams.sql. */
    acronym: string | null;
};

const FALLBACK = "TBC";

/**
 * A team's name at whatever length the screen has room for: its acronym on phones, its short name
 * on large phones and small tablets, and its full name from tablets up. Each tier falls back to the
 * next widest one where the narrower name is missing, so a team with no acronym still reads
 * sensibly on a phone.
 *
 * Only one of the three is on screen at a time; the other two are `display: none`. All three are
 * hidden from assistive technology and the full name is exposed once alongside them, so a screen
 * reader always announces "Brighton & Hove Albion" rather than "BHA" - the abbreviation buys space
 * on a phone, which is not a constraint a screen reader has.
 *
 * Width-dependent text like this only belongs where the surrounding row gives the reader enough
 * context to decode it - a scoreline, a league table, a fixture list, usually beside a crest.
 * Prose ("Brighton v Arsenal closes in 2h") should use `shortName` directly at every width.
 */
export function TeamLabel({ name, shortName, acronym }: TeamNames) {
    const full = name || shortName || FALLBACK;
    const short = shortName || name || FALLBACK;
    const abbreviated = acronym || short;

    return (
        <>
            <Box as="span" aria-hidden="true" display={{ base: "inline", sm: "none" }}>{abbreviated}</Box>
            <Box as="span" aria-hidden="true" display={{ base: "none", sm: "inline", md: "none" }}>{short}</Box>
            <Box as="span" aria-hidden="true" display={{ base: "none", md: "inline" }}>{full}</Box>
            <VisuallyHidden data-role="team-full-name">{full}</VisuallyHidden>
        </>
    );
}
