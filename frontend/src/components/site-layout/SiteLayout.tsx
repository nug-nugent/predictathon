import { Box, Container, Drawer, Flex, HStack, Portal, Stack, Image, Heading, Text, CloseButton, Center, Spinner } from "@chakra-ui/react";
import { Outlet } from "react-router";
import { useUser } from "../../providers/UserProvider";
import { SideNavigation } from "../side-navigation/SideNavigation";
import { SiteHeader } from "../site-header/SiteHeader";
import { useState } from "react";
import football from "../../assets/football.png";

export function SiteLayout() {
    const { user, isLoading } = useUser();
    const [sideNavOpen, setSideNavOpen] = useState(false);

    // Avoids a flash of logged-out content (or a redirect out of a protected route) while the
    // silent session refresh on app load is still in flight.
    if (isLoading) {
        return (
            <Center minH="100vh">
                <Spinner size="xl" />
            </Center>
        );
    }

    return (
        <Box>
            <SiteHeader onMenuButtonClick={() => setSideNavOpen(true)} />

            <Drawer.Root placement="start" open={sideNavOpen} onOpenChange={(e) => setSideNavOpen(e.open)}>
                <Portal>
                    <Drawer.Backdrop />
                    <Drawer.Positioner>
                        <Drawer.Content>
                            <Drawer.Header>
                                <HStack alignItems={"center"}>
                                    <Image src={football} mr={"2"} boxSize={{ base: "30px", md: "34px" }} />
                                    <Stack gap="0">
                                        <Heading as="h1" size={{ base: "xl", md: "2xl" }} lineHeight="1">
                                            Predictathon
                                        </Heading>
                                        <Text fontSize={{ base: "xs", md: "xs" }} lineHeight="1">
                                            {user?.currentCompetition}
                                        </Text>
                                    </Stack>
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
                        <Box w="180px" display={{ base: "none", lg: "block" }} my={3} mr={3}>
                            <SideNavigation />
                        </Box>
                    )}

                    <Box mt={4} flexGrow={1}>
                        {/* page content */}
                        <Outlet />
                    </Box>
                </Flex>
            </Container>
        </Box>
    )
}