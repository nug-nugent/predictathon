import { Box, Flex, IconButton, Spacer } from "@chakra-ui/react";
import { Menu } from "lucide-react";
import { useUser } from "../../hooks/useUser";
import { UserMenu } from "./user-menu/UserMenu";
import { HeaderBrand } from "./header-brand/HeaderBrand";
import { CompetitionNameRow } from "./competition-selector/CompetitionNameRow";

export function SiteHeader({ onMenuButtonClick }: { onMenuButtonClick: () => void }) {
    const { user } = useUser();

    return (
        <Box bg="brand.headerBg" borderBottomWidth="1px" borderBottomColor="brand.headerBorder">
            <Box px={{ base: 4, md: 8, xl: 14 }}>
                <Flex alignItems="center" h={{ base: "52px", md: "60px" }}>
                    {user && (
                        <Box
                            ml="-10px" mr={0}
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

                {/* Below "sm" HeaderBrand hides its own inline competition name (no room alongside
                    the hamburger/wordmark/user menu) - show it here instead, on a row of its own,
                    left-aligned under the hamburger. mt pulls it up against the row above: the Flex
                    is a fixed 52px tall for the hamburger/avatar touch targets, which leaves the
                    wordmark's own text sitting well clear of the row's bottom edge. Measured against
                    the ball's own bottom edge (the tallest thing in that row), not the wordmark text. */}
                {user && (
                    <Box display={{ base: "block", sm: "none" }} pb={2} mt="-8px">
                        <CompetitionNameRow />
                    </Box>
                )}
            </Box>
        </Box>
    )
}