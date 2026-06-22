import { Box, Heading, Text } from "@chakra-ui/react";
import { useUser } from "../../../providers/UserProvider";

export function HomePage() {
    const { user } = useUser();

    return (
        <>
            <Box>
                <Heading>Welcome, {user!.name}!</Heading>
                <Text>Your role is: {user!.role}</Text>
            </Box>
        </>
    );
}
