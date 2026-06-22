import { Box, Container, Flex } from "@chakra-ui/react";
import { Outlet } from "react-router";
import { LoggedOutHeader } from "./header/LoggedOutHeader";

export function LoggedOutLayout() {
    return (
        <Box>
            <LoggedOutHeader />

            <Container maxW="6xl">
                <Flex>
                    <Box my={4} flexGrow={1}>
                        <Outlet />
                    </Box>
                </Flex>
            </Container>
        </Box>
    );
}
