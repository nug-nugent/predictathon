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
    const logo = <Image src={football} mr={2} boxSize={{ base: "30px", md: "34px" }} />;

    if (variant === "loggedOut") {
        return (
            <>
                {linkToHome ? <Link to="/">{logo}</Link> : logo}
                <Heading
                    as={headingAs}
                    size={{ base: "2xl", md: "3xl" }}
                    color={wordmarkColor}
                    fontWeight="extrabold"
                    textTransform="uppercase"
                >
                    Predictathon
                </Heading>
            </>
        );
    }

    return (
        <>
            {linkToHome ? <Link to="/">{logo}</Link> : logo}
            <Stack display={{ base: "none", sm: "block" }} gap="0">
                <Heading
                    as={headingAs}
                    size={{ base: "xl", md: "2xl" }}
                    lineHeight="1"
                    color={wordmarkColor}
                    fontWeight="extrabold"
                    textTransform="uppercase"
                >
                    Predictathon
                </Heading>
                <Stack
                    gap="0"
                    color={subtitleColor}
                    fontFamily="body"
                    fontWeight="bold"
                    fontSize="10px"
                    textTransform="uppercase"
                >
                    <CompetitionSelector />
                </Stack>
            </Stack>
        </>
    );
}
