import { Box, Container, Flex } from "@chakra-ui/react";

export function HeaderContainer({ children }: { children: React.ReactNode }) {
    return (
        <Box bg="blue.fg" color="blue.contrast">
            <Container maxW="6xl">
                <Flex alignItems="center" h={{ base: "50px", md: "60px" }}>
                    {children}
                </Flex>
            </Container>
        </Box>
    );
}
