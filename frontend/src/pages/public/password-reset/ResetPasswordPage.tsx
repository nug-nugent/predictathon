import { useState } from "react";
import { useNavigate, useSearchParams, Link as RouterLink } from "react-router";
import { Button, Center, Field, Heading, Link, Text, VStack } from "@chakra-ui/react";
import { PasswordInput } from "../../../components/ui/password-input";
import { resetPassword } from "../../../services/user-service";
import { ApiError } from "../../../services/api";

export function ResetPasswordPage() {
    const [searchParams] = useSearchParams();
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");
    const navigate = useNavigate();

    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [succeeded, setSucceeded] = useState(false);
    const [error, setError] = useState<string | null>(null);

    if (!userId || !token) {
        return (
            <Center mt={8}>
                <VStack gap={3} maxW="sm" textAlign="center">
                    <Heading size="md">Invalid reset link</Heading>
                    <Text>This password reset link is missing some information. Please request a new one.</Text>
                    <Link asChild variant="underline"><RouterLink to="/password-reset">Request a new link</RouterLink></Link>
                </VStack>
            </Center>
        );
    }

    if (succeeded) {
        return (
            <Center mt={8}>
                <VStack gap={3} maxW="sm" textAlign="center">
                    <Heading size="md">Password reset</Heading>
                    <Text>Your password has been reset. You can now log in with your new password.</Text>
                    <Button colorPalette="blue" onClick={() => navigate("/")}>Go to Predictathon</Button>
                </VStack>
            </Center>
        );
    }

    const submit = async () => {
        setError(null);

        if (newPassword !== confirmPassword) {
            setError("Passwords don't match.");
            return;
        }

        setSubmitting(true);

        try {
            await resetPassword(userId, token, newPassword);
            setSucceeded(true);
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <Center mt={8}>
            <VStack gap={4} maxW="sm" width="full">
                <Heading size="md">Choose a new password</Heading>

                <Field.Root>
                    <Field.Label>New password</Field.Label>
                    <PasswordInput disabled={submitting} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
                </Field.Root>

                <Field.Root>
                    <Field.Label>Confirm new password</Field.Label>
                    <PasswordInput disabled={submitting} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
                </Field.Root>

                {error && (
                    <Text fontSize="sm" color="fg.error">{error}</Text>
                )}

                <Button
                    alignSelf="flex-end"
                    loading={submitting}
                    disabled={!newPassword || !confirmPassword}
                    colorPalette="blue"
                    onClick={submit}
                >
                    Reset password
                </Button>
            </VStack>
        </Center>
    );
}
