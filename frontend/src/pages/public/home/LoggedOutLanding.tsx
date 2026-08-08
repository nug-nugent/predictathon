import { Box, Flex, Heading, Text, VStack } from "@chakra-ui/react";
import { HeaderBrand } from "../../../components/site-header/header-brand/HeaderBrand";
import { LoginForm } from "../../../components/site-header/login-button/LoginForm";
import { CompetitionSpotlightCard } from "../../../components/home/CompetitionSpotlightCard";
import { AnnouncementFeed } from "../../../components/announcements/AnnouncementFeed";
import {
    getCompetitionsOpenForRegistration, getPublicStats, type PublicStats,
} from "../../../services/competition-registration-service";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState } from "../../../components/ui/async-state";

// The pre-login landing page - a full-bleed layout with its own brand rail rather than the
// standard SiteHeader/Container shell (see SiteLayout, which skips its chrome for this route).
export function LoggedOutLanding() {
    const { data: competitions, error: competitionsError } = useAsyncData(getCompetitionsOpenForRegistration, []);
    const { data: stats } = useAsyncData(getPublicStats, []);

    return (
        <Flex minH="100vh" direction={{ base: "column", lg: "row" }} bg="bg">
            <BrandRail stats={stats} />

            <Box flex={1} px={{ base: 4, md: "56px" }} py={{ base: 8, lg: 12 }} position="relative" overflow="hidden">
                <Box
                    position="absolute" top="-300px" right="-300px" boxSize="900px" borderRadius="full"
                    bg={{
                        _light: "radial-gradient(circle at 30% 30%, oklch(0.55 0.15 150 / 0.16), transparent 70%)",
                        _dark: "radial-gradient(circle at 30% 30%, oklch(0.75 0 0 / 0.10), transparent 70%)",
                    }}
                    pointerEvents="none" zIndex={0}
                />
                <Box
                    position="absolute" bottom="-260px" right="120px" boxSize="700px" borderRadius="full"
                    bg={{
                        _light: "radial-gradient(circle at 70% 70%, oklch(0.4 0.15 265 / 0.18), transparent 70%), {colors.bg}",
                        _dark: "radial-gradient(circle at 70% 70%, oklch(0.5 0 0 / 0.14), transparent 70%), {colors.bg}",
                    }}
                    pointerEvents="none" display={{ base: "none", lg: "block" }} zIndex={1}
                />

                <VStack align="stretch" gap={6} maxW="640px" position="relative" zIndex={2}>
                    <AnnouncementFeed audience="loginPage" />

                    <LoginForm />

                    <VStack align="stretch" gap={4}>
                        {competitionsError && <ErrorState error={competitionsError} />}

                        {competitions !== null && competitions.length === 0 && (
                            <Text color="fg.muted">No competitions are currently open for registration.</Text>
                        )}

                        {competitions?.map((c) => (
                            <CompetitionSpotlightCard key={c.competitionID} competition={c} />
                        ))}
                    </VStack>
                </VStack>
            </Box>
        </Flex>
    );
}

function BrandRail({ stats }: { stats: PublicStats | null }) {
    return (
        <Box
            w={{ base: "full", lg: "380px" }}
            flexShrink={0}
            position="relative"
            overflow="hidden"
            bg={{
                _light: "linear-gradient(165deg, #1E4FD1 0%, #0E4B3A 100%)",
                _dark: "linear-gradient(165deg, #000000 0%, {colors.input.border} 100%)",
            }}
            color="brand.wordmarkFg"
            display="flex"
            flexDirection="column"
            px={{ base: 6, md: 10 }}
            py={{ base: 8, lg: 12 }}
            gap={8}
        >
            <Box position="relative" zIndex={1}>
                <HeaderBrand variant="loggedOut" linkToHome headingAs="h1" />
            </Box>

            <VStack align="stretch" gap={4} position="relative" zIndex={1} mt={{ base: 0, lg: "auto" }}>
                <Heading size={{ base: "lg", md: "xl" }} lineHeight="1.25">
                    Predict the scores.<br />Beat your mates.
                </Heading>
                <Text fontSize="15px" color="whiteAlpha.900" lineHeight="1.6">
                    Prediction leagues for your favourite football tournaments - Premier League, World Cup, European
                    Championships. Giving you the bragging rights since 1998.
                </Text>

                {stats && (
                    <Flex gap={6}>
                        <Stat value={stats.predictionsMadeCount.toLocaleString()} label="Predictions made" />
                        <Stat value={stats.completedCompetitionsCount.toLocaleString()} label="Completed competitions since 1998" />
                    </Flex>
                )}
            </VStack>
        </Box>
    );
}

function Stat({ value, label }: { value: string; label: string }) {
    return (
        <VStack align="start" gap={0}>
            <Text fontFamily="heading" fontWeight="extrabold" fontSize="22px">{value}</Text>
            <Text fontSize="xs" color="whiteAlpha.800">{label}</Text>
        </VStack>
    );
}
