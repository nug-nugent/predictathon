import { Grid, GridItem, Heading, Image } from "@chakra-ui/react";
import { Link } from "react-router";
import football from "../../../assets/football.png";
import { CompetitionSelector } from "../competition-selector/CompetitionSelector";

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
// nav drawer) - previously "loggedIn" and "loggedOut" each had their own, larger scale, which made
// the post-login site header read as a hero banner instead of a compact utility strip.
const LOGO_SIZE = "32px";
const WORDMARK_FONT_SIZE = "21px";
// Below the "sm" breakpoint the logged-in header also has to fit a hamburger button and the user
// menu on the same row, so the wordmark shrinks there rather than being hidden entirely, and the
// ball shrinks to sit on the same line as the wordmark instead of spanning the competition row too.
const WORDMARK_FONT_SIZE_MOBILE = "13px";
const LOGO_SIZE_MOBILE = "18px";

export function HeaderBrand({
    variant,
    linkToHome = false,
    headingAs,
    wordmarkColor = "brand.wordmarkFg",
    subtitleColor = "brand.subtitleFg",
}: HeaderBrandProps) {
    // alt="": decorative - it always sits beside the "Predictathon" wordmark text. The loggedIn
    // variant spaces the ball via the wrapping Grid's columnGap instead, so no margin here.
    const logo = (
        <Image
            src={football}
            alt=""
            mr={variant === "loggedIn" ? undefined : 2}
            boxSize={variant === "loggedIn" ? { base: LOGO_SIZE_MOBILE, sm: LOGO_SIZE } : LOGO_SIZE}
        />
    );

    const heading = (
        <Heading
            as={headingAs}
            fontSize={variant === "loggedIn" ? { base: WORDMARK_FONT_SIZE_MOBILE, sm: WORDMARK_FONT_SIZE } : WORDMARK_FONT_SIZE}
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
        // Named grid areas (rather than a fixed nested Stack) so the ball can sit beside just the
        // wordmark on mobile - its own row, competition on a row of its own beneath - while still
        // spanning both rows on wider screens, where there's room for the original tall-logo lockup.
        <Grid
            templateColumns="auto minmax(0, 1fr)"
            templateAreas={{
                base: `"logo heading" "competition competition"`,
                sm: `"logo heading" "logo competition"`,
            }}
            columnGap={{ base: "6px", sm: "8px" }}
            rowGap="0"
            alignItems="center"
            minW="0"
        >
            <GridItem area="logo">
                {linkToHome ? <Link to="/">{logo}</Link> : logo}
            </GridItem>
            <GridItem area="heading" minW="0">
                {/* Only the logo + wordmark link home - the CompetitionSelector below is itself
                    interactive, so it can't sit inside the same anchor. */}
                {linkToHome ? (
                    <Link to="/" style={{ textDecoration: "none", color: "inherit" }}>
                        {heading}
                    </Link>
                ) : (
                    heading
                )}
            </GridItem>
            <GridItem
                area="competition"
                minW="0"
                color={subtitleColor}
                fontFamily="body"
                fontWeight="bold"
                fontSize="11px"
                textTransform="uppercase"
            >
                <CompetitionSelector />
            </GridItem>
        </Grid>
    );
}
