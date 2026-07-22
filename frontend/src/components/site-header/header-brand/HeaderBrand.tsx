import { Heading, Image, Stack } from "@chakra-ui/react";
import { Link } from "react-router";
import football from "../../../assets/football.png";
import { CompetitionSelector } from "../competition-selector/CompetitionSelector";

type HeaderBrandProps = {
    /** Compact wordmark + competition subtitle (used once logged in), vs. a large standalone wordmark. */
    variant: "loggedIn" | "loggedOut";
    linkToHome?: boolean;
    headingAs?: "h1" | "h2";
    /** Wordmark/subtitle colors depend on the background they sit on - default to the header's blue-block treatment. */
    wordmarkColor?: string;
    subtitleColor?: string;
};

export function HeaderBrand({
    variant,
    linkToHome = false,
    headingAs,
    wordmarkColor = "brand.wordmarkFg",
    subtitleColor = "brand.subtitleFg",
}: HeaderBrandProps) {
    // alt="": decorative - it always sits beside the "Predictathon" wordmark text.
    const logo = <Image src={football} alt="" mr={2} boxSize={{ base: "36px", md: "44px" }} />;

    if (variant === "loggedOut") {
        const heading = (
            <Heading
                as={headingAs}
                size={{ base: "2xl", md: "3xl" }}
                color={wordmarkColor}
                fontWeight="extrabold"
                textTransform="uppercase"
            >
                Predictathon
            </Heading>
        );

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

    const heading = (
        <Heading
            as={headingAs}
            size={{ base: "2xl", md: "3xl" }}
            lineHeight="1"
            color={wordmarkColor}
            fontWeight="extrabold"
            textTransform="uppercase"
        >
            Predictathon
        </Heading>
    );

    return (
        <>
            {linkToHome ? <Link to="/">{logo}</Link> : logo}
            <Stack display={{ base: "none", sm: "block" }} gap="0">
                {/* Only the logo + wordmark link home - the CompetitionSelector below is itself
                    interactive, so it can't sit inside the same anchor. */}
                {linkToHome ? (
                    <Link to="/" style={{ textDecoration: "none", color: "inherit" }}>
                        {heading}
                    </Link>
                ) : (
                    heading
                )}
                <Stack
                    gap="0"
                    color={subtitleColor}
                    fontFamily="body"
                    fontWeight="bold"
                    fontSize="11px"
                    textTransform="uppercase"
                >
                    <CompetitionSelector />
                </Stack>
            </Stack>
        </>
    );
}
