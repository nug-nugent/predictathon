import { Box, Heading, Image, Stack } from "@chakra-ui/react";
import { Link } from "react-router";
import football from "../../../assets/football.png";
import { CompetitionNameRow } from "../competition-selector/CompetitionNameRow";

type HeaderBrandProps = {
    /** Compact wordmark + competition subtitle (used once logged in), vs. a standalone wordmark. */
    variant: "loggedIn" | "loggedOut";
    linkToHome?: boolean;
    headingAs?: "h1" | "h2";
    /** Wordmark/subtitle colors depend on the background they sit on - default to the header's blue-block treatment. */
    wordmarkColor?: string;
    subtitleColor?: string;
};

// One fixed scale for the wordmark lockup everywhere it appears (top bar, login brand rail, mobile
// nav drawer, mobile header row) - previously "loggedIn" and "loggedOut" each had their own, larger
// scale, which made the post-login site header read as a hero banner instead of a compact utility strip.
const LOGO_SIZE = "32px";
const WORDMARK_FONT_SIZE = "21px";

export function HeaderBrand({
    variant,
    linkToHome = false,
    headingAs,
    wordmarkColor = "brand.wordmarkFg",
    subtitleColor = "brand.subtitleFg",
}: HeaderBrandProps) {
    // alt="": decorative - it always sits beside the "Predictathon" wordmark text.
    const logo = <Image src={football} alt="" mr={2} boxSize={LOGO_SIZE} />;

    const heading = (
        <Heading
            as={headingAs}
            fontSize={WORDMARK_FONT_SIZE}
            lineHeight="1"
            letterSpacing="-0.01em"
            color={wordmarkColor}
            fontWeight="extrabold"
            textTransform="uppercase"
            truncate
        >
            Predictathon
        </Heading>
    );

    if (variant === "loggedOut") {
        return linkToHome ? (
            <Link to="/" style={{ display: "flex", alignItems: "center", textDecoration: "none", color: "inherit" }}>
                {logo}
                {heading}
            </Link>
        ) : (
            <>
                {logo}
                {heading}
            </>
        );
    }

    return (
        <>
            {linkToHome ? <Link to="/">{logo}</Link> : logo}
            <Stack gap="0" minW="0">
                {/* Only the logo + wordmark link home - the CompetitionSelector below is itself
                    interactive, so it can't sit inside the same anchor. */}
                {linkToHome ? (
                    <Link to="/" style={{ textDecoration: "none", color: "inherit" }}>
                        {heading}
                    </Link>
                ) : (
                    heading
                )}
                {/* Below "sm" there isn't room for the ball, wordmark, hamburger and user menu to all
                    share a row with the competition name too - callers show a CompetitionNameRow of
                    its own, full-width, beneath the whole header row instead. */}
                <Box display={{ base: "none", sm: "block" }}>
                    <CompetitionNameRow color={subtitleColor} />
                </Box>
            </Stack>
        </>
    );
}
