import { Box, Flex, IconButton, Spacer, Container } from "@chakra-ui/react";
import { Menu } from "lucide-react";
import { useUser } from "../../hooks/useUser";
import { UserMenu } from "./user-menu/UserMenu";
import { HeaderBrand } from "./header-brand/HeaderBrand";

export function SiteHeader({ onMenuButtonClick }: { onMenuButtonClick: () => void }) {
    const { user } = useUser();

    return (
        <Box bg="brand.headerBg" borderBottomWidth="1px" borderBottomColor="brand.headerBorder">
            <Container maxW="6xl">
                <Flex alignItems="center" h={{ base: "52px", md: "60px" }}>
                    {user && (
                        <Box
                            ml="-10px" mr={2}
                            display={{ base: "block", lg: "none" }}
                        >
                            <IconButton
                                aria-label="Open navigation menu"
                                variant="plain"
                                size="md"
                                color="brand.wordmarkFg"
                                onClick={onMenuButtonClick}>
                                <Menu />
                            </IconButton>
                        </Box>
                    )}

                    <HeaderBrand variant={user ? "loggedIn" : "loggedOut"} linkToHome />

                    <Spacer />
                    {user && <UserMenu />}
                </Flex>
            </Container>
        </Box>
    )
}