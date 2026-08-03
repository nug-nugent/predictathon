import { Box, Container, Drawer, Flex, HStack, Portal, CloseButton, Center, Spinner } from "@chakra-ui/react";
import { Outlet, useLocation } from "react-router";
import { useUser } from "../../hooks/useUser";
import { SideNavigation } from "../side-navigation/SideNavigation";
import { SiteHeader } from "../site-header/SiteHeader";
import { HeaderBrand } from "../site-header/header-brand/HeaderBrand";
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
                            <Drawer.Header borderBottomWidth="1px" borderBottomColor="border.hairline">
                                <HStack alignItems={"center"}>
                                    <HeaderBrand variant="loggedIn" headingAs="h1" wordmarkColor="fg" subtitleColor="fg.muted" />
                                </HStack>
                            </Drawer.Header>
                            <Drawer.Body mt={0} ml={6}>
                                <SideNavigation onClick={() => setSideNavOpen(false)} />
                            </Drawer.Body>
                            <Drawer.CloseTrigger asChild>
                                <CloseButton size="lg" />
                            </Drawer.CloseTrigger>
                        </Drawer.Content>
                    </Drawer.Positioner>
                </Portal>
            </Drawer.Root>

            <Container maxW="6xl">
                <Flex>
                    {user && (
                        <Box w="180px" display={{ base: "none", lg: "block" }} my={3} mr={3} p={2} alignSelf="flex-start"
                            bg="surface.sidebar" borderWidth="1px" borderColor="border.hairline" borderRadius="card">
                            <SideNavigation />
                        </Box>
                    )}

                    <Box mt={3} flexGrow={1}>
                        {/* page content */}
                        <Outlet />
                    </Box>
                </Flex>
            </Container>
        </Box>
    )
}