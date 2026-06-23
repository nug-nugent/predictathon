import { Box, Container, Flex } from "@chakra-ui/react";
import { Outlet } from "react-router";
import { LoggedInHeader } from "./header/LoggedInHeader";
import { SideNavigation } from "./side-navigation/SideNavigation";

export function LoggedInLayout() {
    return (
        <Box>
            <LoggedInHeader />

            <Container maxW="6xl" paddingInline={{ base: 2, sm: 4, md: 6, lg: 8 }}>
                <Flex>
                    <Box display={{ base: "none", lg: "block" }} my={3} mr={6}>
                        <SideNavigation />
                    </Box>

                    <Box my={4} flexGrow={1}>
                        <Outlet />
                    </Box>
                </Flex>
            </Container>
        </Box>
    );
}
