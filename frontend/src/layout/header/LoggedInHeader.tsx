import {
    Box,
    CloseButton,
    Drawer,
    Heading,
    HStack,
    IconButton,
    Image,
    Portal,
    Spacer,
    Stack,
    Text
} from "@chakra-ui/react";
import { Menu } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router";
import football from "../../assets/football.png";
import { useUser } from "../../providers/UserProvider";
import { SideNavigation } from "../side-navigation/SideNavigation";
import { HeaderContainer } from "./header-container/HeaderContainer";
import { UserMenu } from "./user-menu/UserMenu";

export function LoggedInHeader() {
    const [sideNavOpen, setSideNavOpen] = useState(false);
    const { user } = useUser();

    return (
        <HeaderContainer>
            <Box ml="-10px" mr={2} display={{ base: "block", lg: "none" }}>
                <IconButton variant="plain" size="md" color="blue.contrast" onClick={() => setSideNavOpen(true)}>
                    <Menu />
                </IconButton>
            </Box>

            <Link to="/">
                <Image src={football} mr={2} boxSize={{ base: "30px", md: "34px" }} />
            </Link>

            <Stack display={{ base: "none", sm: "block" }} gap="0">
                <Heading size={{ base: "xl", md: "2xl" }} lineHeight="1">
                    <Link to="/">Predictathon</Link>
                </Heading>
                <Text fontSize={{ base: "xs", md: "xs" }} lineHeight="1">
                    {user!.currentCompetition}
                </Text>
            </Stack>

            <Spacer />
            <UserMenu />

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
                                            {user!.currentCompetition}
                                        </Text>
                                    </Stack>
                                </HStack>
                            </Drawer.Header>
                            <Drawer.Body>
                                <SideNavigation onClick={() => setSideNavOpen(false)} />
                            </Drawer.Body>
                            <Drawer.CloseTrigger asChild>
                                <CloseButton size="lg" />
                            </Drawer.CloseTrigger>
                        </Drawer.Content>
                    </Drawer.Positioner>
                </Portal>
            </Drawer.Root>
        </HeaderContainer>
    );
}
