import { Avatar, Button, Popover, Portal, Text, Stack } from "@chakra-ui/react";
import { useUser } from "../../../providers/UserProvider";
import { ChevronDown, LogOut, UserPen } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { useState } from "react";

export function UserMenu() {
    const [open, setOpen] = useState(false);
    const { user, setUser } = useUser();
    const navigate = useNavigate();

    const handleLogout = () => {
        setUser(null);
        navigate("/");
    };

    return (
        user && (
            <Popover.Root
                open={open}
                onOpenChange={(e) => setOpen(e.open)}
                positioning={{
                    placement: "bottom-end",
                    offset: { crossAxis: -10, mainAxis: 2 }
                }}>
                <Popover.Trigger asChild>
                    <Button
                        size={{ base: "xs", md: "md" }}
                        variant="surface"
                        rounded="full"
                        pr="8px"
                        pl="0"
                        maxWidth={{ base: "200px", md: "unset" }}>
                        <Avatar.Root size={{ base: "xs", md: "md" }}>
                            <Avatar.Image src={user.avatarUrl} />
                            <Avatar.Fallback name={user.name} />
                        </Avatar.Root>
                        <Text fontSize={{ base: "sm", md: "md" }} truncate>
                            {user.name}
                        </Text>
                        <ChevronDown />
                    </Button>
                </Popover.Trigger>
                <Portal>
                    <Popover.Positioner>
                        <Popover.Content width="100%" rounded="sm" p="0">
                            <Stack gap="0" onClick={() => setOpen(false)}>
                                <Link to="/profile/edit">
                                    <Button
                                        size="sm"
                                        p="6"
                                        justifyContent="flex-start"
                                        variant="ghost"
                                        colorPalette="blue">
                                        <UserPen /> Edit Profile
                                    </Button>
                                </Link>
                                <Button
                                    size="sm"
                                    p="6"
                                    justifyContent="flex-start"
                                    variant="ghost"
                                    colorPalette="blue"
                                    onClick={handleLogout}>
                                    <LogOut /> Logout
                                </Button>
                            </Stack>
                        </Popover.Content>
                    </Popover.Positioner>
                </Portal>
            </Popover.Root>
        )
    );
}
