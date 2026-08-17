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
                            >
                                <Stack gap={2}>
                                    <HStack alignItems={"center"}>
                                        <HeaderBrand variant="loggedIn" headingAs="h1" />
                                        <Spacer />
                                        {/* Overriding the default absolute corner placement so it sits inline
                                            with the ball/wordmark row - and it needs an explicit light color
                                            here, since the drawer default (near-black) is unreadable against
                                            this header's blue/dark gradient. */}
                                        <Drawer.CloseTrigger asChild>
                                            <CloseButton
                                                position="static"
                                                color="brand.wordmarkFg"
                                                _hover={{ bg: "rgba(255, 255, 255, 0.14)" }}
                                            />
                                        </Drawer.CloseTrigger>
                                    </HStack>
                                    {/* The drawer only ever opens below "lg", where HeaderBrand hides its
                                        own inline competition name - show it here on its own row instead. */}
                                    <CompetitionNameRow />
                                </Stack>
                            </Drawer.Header>
                            <Drawer.Body mt={0} ml={6}>
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