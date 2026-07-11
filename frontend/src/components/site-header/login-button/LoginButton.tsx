import { Button, Checkbox, Field, HStack, Input, Link, Popover, Portal, Stack } from "@chakra-ui/react";
import { useState } from "react";
import { PasswordInput } from "../../ui/password-input";
import { useUser } from "../../../providers/UserProvider";
import { useNavigate } from "react-router";
import { loginUser } from "../../../services/user-service";

export function LoginButton() {
    const [open, setOpen] = useState(false);
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [rememberMe, setRememberMe] = useState(true);
    const [isLoggingIn, setLoggingIn] = useState(false);
    const { setUser } = useUser();
    const navigate = useNavigate();

    const goToPasswordReset = () => {
        setOpen(false);
        navigate("/password-reset");
    }

    const login = async () => {
        setLoggingIn(true);
        const user = await loginUser(username);
        setUser(user, rememberMe);
        navigate("/");
    };

    return (
        <Popover.Root open={open} onOpenChange={(e) => setOpen(e.open)} positioning={{ placement: "bottom-end" }}>
            <Popover.Trigger asChild>
                <Button variant="subtle" colorPalette="white" size="sm">
                    Login
                </Button>
            </Popover.Trigger>
            <Portal>
                <Popover.Positioner>
                    <Popover.Content>
                        <Popover.Arrow />
                        <Popover.Body>
                            <Stack gap="4">
                                <Field.Root>
                                    <Field.Label>Email / Username</Field.Label>
                                    <Input size="sm" disabled={isLoggingIn} value={username} onChange={(e) => setUsername(e.target.value)} />
                                </Field.Root>

                                <Field.Root>
                                    <Field.Label>Password</Field.Label>
                                    <PasswordInput size="sm" disabled={isLoggingIn} value={password} onChange={(e) => setPassword(e.target.value)} />
                                </Field.Root>

                                <Checkbox.Root checked={rememberMe} disabled={isLoggingIn}
                                    onCheckedChange={(e) => setRememberMe(!!e.checked)}
                                    colorPalette="blue">
                                    <Checkbox.HiddenInput />
                                    <Checkbox.Control />
                                    <Checkbox.Label>Remember me</Checkbox.Label>
                                </Checkbox.Root>

                                <HStack justifyContent={"space-between"}>
                                    <Link variant="underline" onClick={goToPasswordReset}>Forgotten password?</Link>
                                    <Button loading={isLoggingIn} size="sm" colorPalette="blue" onClick={login}>Login</Button>
                                </HStack>
                            </Stack>
                        </Popover.Body>
                    </Popover.Content>
                </Popover.Positioner>
            </Portal>
        </Popover.Root>
    )
}