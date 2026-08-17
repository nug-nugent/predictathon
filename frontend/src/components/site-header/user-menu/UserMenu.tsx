import { Avatar, Button, Popover, Portal, Text, Stack } from "@chakra-ui/react";
import { useUser } from "../../../hooks/useUser";
import { ChevronDown, LogOut, Moon, Sun, UserPen } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { useState } from "react";
import { logoutUser } from "../../../services/user-service";
import { useColorMode } from "../../ui/color-mode-context";

export function UserMenu() {
    const [open, setOpen] = useState(false);
    const { user, setUser } = useUser();
    const { colorMode, toggleColorMode } = useColorMode();
    const navigate = useNavigate();

    const handleLogout = async () => {
        // Best-effort - clear local state regardless of whether the network call succeeds, so
        // the user isn't stuck "logged in" in the UI just because a request failed.
        try {
            await logoutUser();
        } catch {
            // Ignore - the refresh cookie will simply expire naturally if this didn't revoke it.
        }

        setUser(null);
        void navigate("/");
    }

    return user && (
        <Popover.Root open={open} onOpenChange={(e) => setOpen(e.open)}
            positioning={{ placement: "bottom-end", offset: { crossAxis: -10, mainAxis: 2 } }}>
            <Popover.Trigger asChild>
                <Button
                    variant="plain" size={{ base: "xs", md: "md" }} gap="8px"
                    bg="rgba(255, 255, 255, 0.14)" _hover={{ bg: "rgba(255, 255, 255, 0.22)" }}
                    color="brand.wordmarkFg" rounded="full" py="4px" pl="4px" pr="12px"
                    maxWidth={{ base: "200px", md: "unset" }}
                >
                    <Avatar.Root size={{ base: "xs", md: "md" }} bg="surface.avatarCircle" color="surface.avatarCircleFg">
                        <Avatar.Image src={user.avatarUrl} />
                        <Avatar.Fallback name={user.name} />
                    </Avatar.Root>
                    <Text
                        display={{ base: "none", sm: "block" }}
                        fontSize={{ base: "sm", md: "md" }}
                        fontWeight="semibold"
                        truncate
                    >
                        {user.name}
                    </Text>
                    <ChevronDown size={16} strokeWidth={3} />
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content width="100%" rounded="sm" p="0">
                        <Stack gap="0" onClick={() => setOpen(false)}>
                            {/* asChild renders the Button *as* the Link's anchor - nesting a real
                                <button> inside an <a> is invalid HTML and double-stops keyboard tabbing. */}
                            <Button asChild size="sm" p="6" justifyContent="flex-start" variant="ghost" colorPalette="action">
                                <Link to="/profile/edit"><UserPen /> Edit Profile</Link>
                            </Button>
                            <Button
                                size="sm" p="6" justifyContent="flex-start" variant="ghost" colorPalette="action"
                                onClick={toggleColorMode}
                            >
                                {colorMode === "dark" ? <Sun /> : <Moon />} {colorMode === "dark" ? "Light Mode" : "Dark Mode"}
                            </Button>
                            <Button size="sm" p="6" justifyContent="flex-start" variant="ghost" colorPalette="action" onClick={() => { void handleLogout(); }}><LogOut /> Logout</Button>
                        </Stack>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>

    )
}