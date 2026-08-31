import { Button, Checkbox, Field, HStack, Heading, Input, Link, Stack, Text } from "@chakra-ui/react";
import { useState } from "react";
import { PasswordInput } from "../../ui/password-input";
import { Panel } from "../../ui/panel";
import { useUser } from "../../../hooks/useUser";
import { useLocation, useNavigate } from "react-router";
import { loginUser } from "../../../services/user-service";
import { ApiError, PASSWORD_RESET_REQUIRED_ERROR_TYPE } from "../../../services/api";

export function LoginForm() {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [rememberMe, setRememberMe] = useState(true);
    const [isLoggingIn, setLoggingIn] = useState(false);
    const [error, setError] = useState<string | null>(null);
    // Set instead of `error` when the account exists but has no usable password yet (e.g. one
    // migrated from the legacy dbo.User table) - shown as a distinct prompt rather than implying
    // the entered credentials were simply wrong.
    const [needsPasswordReset, setNeedsPasswordReset] = useState(false);
    const { setUser } = useUser();
    const navigate = useNavigate();
    const location = useLocation();

    // Set by ProtectedRoute when a logged-out visit to a protected URL bounced here - return the
    // user there after login rather than to Home. Same-app paths only (it always starts with "/").
    const returnTo = (location.state as { from?: string } | null)?.from;

    const goToPasswordReset = () => {
        void navigate("/password-reset");
    }

    const login = async () => {
        setLoggingIn(true);
        setError(null);
        setNeedsPasswordReset(false);

        try {
            const user = await loginUser(username, password, rememberMe);
            setUser(user);
            void navigate(returnTo?.startsWith("/") ? returnTo : "/");
        } catch (e) {
            if (e instanceof ApiError && e.type === PASSWORD_RESET_REQUIRED_ERROR_TYPE) {
                setNeedsPasswordReset(true);
            } else {
                setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
            }
        } finally {
            setLoggingIn(false);
        }
    };

    if (needsPasswordReset) {
        return (
            <Panel borderRadius="16px" boxShadow="cardRaised" py={{ base: "24px", md: "40px" }} px={{ base: "20px", md: "36px" }}>
                <Stack gap="3">
                    <Text fontWeight="semibold">We've upgraded our security</Text>
                    <Text fontSize="15px" color="fg.muted">
                        You'll need to reset your password before you can log in again. It only takes a minute.
                    </Text>
                    <Button h="44px" fontSize="15px" colorPalette="action" alignSelf="flex-start" onClick={goToPasswordReset}>
                        Reset Your Password
                    </Button>
                </Stack>
            </Panel>
        );
    }

    return (
        <Panel borderRadius="16px" boxShadow="cardRaised" py={{ base: "24px", md: "40px" }} px={{ base: "20px", md: "36px" }}>
            {/* A real <form> (not a click handler) so Enter submits and password managers can
                recognise and fill the login - paired with the autocomplete hints below. */}
            <Stack as="form" gap="4" onSubmit={(e) => { e.preventDefault(); void login(); }}>
                <Stack gap="1">
                    <Heading fontSize="24px" lineHeight="1.2">Already registered?</Heading>
                    <Text fontSize="15px" color="fg.muted">Log in to make your predictions.</Text>
                </Stack>

                <Field.Root>
                    <Field.Label fontSize="14.5px">Email or username</Field.Label>
                    <Input h="44px" fontSize="15px" name="username" autoComplete="username" disabled={isLoggingIn}
                        value={username} onChange={(e) => setUsername(e.target.value)} />
                </Field.Root>

                <Field.Root>
                    <Field.Label fontSize="14.5px">Password</Field.Label>
                    <PasswordInput h="44px" fontSize="15px" name="password" autoComplete="current-password" disabled={isLoggingIn}
                        value={password} onChange={(e) => setPassword(e.target.value)} />
                </Field.Root>

                <Checkbox.Root checked={rememberMe} disabled={isLoggingIn}
                    onCheckedChange={(e) => setRememberMe(!!e.checked)}
                    colorPalette="action">
                    <Checkbox.HiddenInput />
                    <Checkbox.Control />
                    <Checkbox.Label>Remember me</Checkbox.Label>
                </Checkbox.Root>

                {error && (
                    <Text fontSize="sm" color="fg.error">{error}</Text>
                )}

                <HStack justifyContent={"space-between"}>
                    <Link variant="underline" colorPalette="action" onClick={goToPasswordReset}>Forgotten password?</Link>
                    <Button type="submit" loading={isLoggingIn} h="44px" fontSize="15px" colorPalette="action">Login</Button>
                </HStack>
            </Stack>
        </Panel>
    )
}
