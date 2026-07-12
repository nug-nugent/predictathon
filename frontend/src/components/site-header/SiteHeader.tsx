import { Box, Flex, Heading, IconButton, Spacer, Container, Image, Stack } from "@chakra-ui/react";
import { Menu } from "lucide-react";
import { useUser } from "../../providers/UserProvider";
import { Link } from "react-router";
import football from "../../assets/football.png";
import { LoginButton } from "./login-button/LoginButton";
import { UserMenu } from "./user-menu/UserMenu";
import { CompetitionSelector } from "./competition-selector/CompetitionSelector";

export function SiteHeader({ onMenuButtonClick }: { onMenuButtonClick: () => void }) {
    const { user } = useUser();

    return (
        <Box bg="blue.fg" color="blue.contrast">
            <Container maxW="6xl">
                <Flex alignItems="center" h={{ base: "50px", md: "60px" }}>
                    {user && (
                        <Box
                            ml="-10px" mr={2}
                            display={{ base: "block", lg: "none" }}
                        >
                            <IconButton
                                variant="plain"
                                size="md"
                                color="blue.contrast"
                                onClick={onMenuButtonClick}>
                                <Menu />
                            </IconButton>
                        </Box>
                    )}

                    <Link to="/">
                        <Image src={football} mr={2} boxSize={{ base: "30px", md: "34px" }} />
                    </Link> 

                    {user ? (
                        <Stack display={{ base: "none", sm: "block" }} gap="0">
                            <Heading  size={{ base: "xl", md: "2xl" }}  lineHeight="1">
                                Predictathon
                            </Heading>
                            <CompetitionSelector />
                        </Stack>
                    ) : (
                        <Heading size={{ base: "2xl", md: "3xl" }}>
                            Predictathon
                        </Heading>
                    )}
                    <Spacer />
                    {user ? (
                        <UserMenu />
                    ) : (
                        <LoginButton />
                    )}
                </Flex>
            </Container>
        </Box>
    )
}