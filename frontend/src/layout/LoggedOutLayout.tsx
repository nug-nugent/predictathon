import { Box, Container, Flex } from "@chakra-ui/react";
import { Outlet } from "react-router";
import { LoggedOutHeader } from "./header/LoggedOutHeader";

export function LoggedOutLayout() {
    return (
        <Box>
            <LoggedOutHeader />

            <Container maxW="6xl" paddingInline={{ base: 2, sm: 4, md: 6, lg: 8 }}>
                <Flex>
                    <Box my={4} flexGrow={1}>
                        <Outlet />
                    </Box>
                </Flex>
            </Container>
        </Box>
    );
}
