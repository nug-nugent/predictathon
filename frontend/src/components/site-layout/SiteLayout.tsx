import { Box, Drawer, Flex, HStack, Portal, CloseButton, Center, Spacer, Spinner, Stack, Text } from "@chakra-ui/react";
import { Outlet, useLocation } from "react-router";
import { useUser } from "../../hooks/useUser";
import { SideNavigation } from "../side-navigation/SideNavigation";
import { SiteHeader } from "../site-header/SiteHeader";
import { HeaderBrand } from "../site-header/header-brand/HeaderBrand";
import { CompetitionNameRow } from "../site-header/competition-selector/CompetitionNameRow";
import { useState } from "react";

export function SiteLayout() {
    const { user, isLoading } = useUser();
    const [sideNavOpen, setSideNavOpen] = useState(false);
    const location = useLocation();

    // Avoids a flash of logged-out content (or a redirect out of a protected route) while the
    // silent session refresh on app load is still in flight.
    if (isLoading) {
        return (
            <Center minH="100vh">
                <Spinner size="xl" />
            </Center>
        );
    }

    // The logged-out landing page (LoggedOutLanding) owns its own full-bleed layout with its own
    // brand rail, rather than sitting inside the standard header/container chrome.
    if (!user && location.pathname === "/") {
        return <Outlet />;
    }

    return (
        <Box>
            <SiteHeader onMenuButtonClick={() => setSideNavOpen(true)} />

            <Drawer.Root placement="start" open={sideNavOpen} onOpenChange={(e) => setSideNavOpen(e.open)}>
                <Portal>
                    <Drawer.Backdrop />
                    <Drawer.Positioner>
                        <Drawer.Content bg="surface.sidebar">
                            <Drawer.Header
                                bg={{ _light: "linear-gradient(135deg, #1E4FD1 0%, #1948C4 100%)", _dark: "{colors.bg}" }}
                                borderBottomWidth="0"
                                pt="14px"
                                pl="16px"
                                pr="16px"
                            >
                                {/* flex="1": Drawer.Header is itself a row flex container, so without this
                                    the Stack shrinks to its own content width instead of filling the header
                                    - leaving the Spacer below nothing to push against, and the close button
                                    sitting right next to the wordmark instead of on the far right. */}
                                <Stack gap={0} flex="1" minW="0">
                                    {/* gap=0: HStack's own default gap was stacking on top of the ball's
                                        own mr, doubling the ball-to-wordmark spacing versus the closed
                                        header (which uses a plain Flex, with no gap of its own). */}
                                    <HStack alignItems={"center"} gap={0}>
                                        <HeaderBrand variant="loggedIn" headingAs="h1" showCompetition={false} />
                                        <Spacer />
                                        {/* Overriding the default absolute corner placement so it sits inline
                                            with the ball/wordmark row - and it needs an explicit light color
                                            here, since the drawer default (near-black) is unreadable against
                                            this header's blue/dark gradient. size="sm" keeps it close to the
                                            ball's own 32px, so it doesn't stretch the row taller than the text
                                            needs and open up extra space below the wordmark. */}
                                        <Drawer.CloseTrigger asChild>
                                            <CloseButton
                                                size="sm"
                                                position="static"
                                                color="brand.wordmarkFg"
                                                _hover={{ bg: "rgba(255, 255, 255, 0.14)" }}
                                            />
                                        </Drawer.CloseTrigger>
                                    </HStack>
                                    {/* The drawer only ever opens below "lg", where HeaderBrand hides its
                                        own inline competition name - show it here on its own row instead.
                                        mt pulls it up against the ball/wordmark row, which - like the row
                                        above it - is taller than the wordmark text alone (driven by the
                                        ball/close-button heights), leaving slack under the text otherwise. */}
                                    <Box mt="-6px">
                                        <CompetitionNameRow />
                                    </Box>
                                </Stack>
                            </Drawer.Header>
                            {/* p=0: nav rows need to reach the drawer's edges themselves (full-width active
                                background + flush left border), so the inset that used to live here as a
                                margin now lives inside NavItem's own padding instead. */}
                            <Drawer.Body mt={0} p={0} pt={2}>
                                <SideNavigation onClick={() => setSideNavOpen(false)} />
                            </Drawer.Body>
                        </Drawer.Content>
                    </Drawer.Positioner>
                </Portal>
            </Drawer.Root>

            <Box px={{ base: 4, md: 8, xl: 14 }}>
                <Flex>
                    {user && (
                        <Box w="224px" display={{ base: "none", lg: "block" }} my={3} mr={3} p={2} alignSelf="flex-start"
                            bg="surface.sidebar" borderWidth="1px" borderColor="border.hairline" borderRadius="card">
                            <SideNavigation />
                        </Box>
                    )}

                    <Box mt={3} flexGrow={1} minW={0}>
                        {/* page content */}
                        <Outlet />
                    </Box>
                </Flex>

                <Text textAlign="center" fontSize="xs" color="fg.muted" mt={6} mb={3}>
                    Predictathon v{__APP_VERSION__} &middot; &copy; David Huggett 1998&ndash;{new Date().getFullYear()}
                </Text>
            </Box>
        </Box>
    )
}