import { useState } from "react";
import { Button, Center, Field, Heading, Input, Text, VStack } from "@chakra-ui/react";
import { forgotPassword } from "../../../services/user-service";
import { Panel } from "../../../components/ui/panel";

export function ForgotPasswordPage() {
    const [userNameOrEmail, setUserNameOrEmail] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [submitted, setSubmitted] = useState(false);

    const submit = async () => {
        setSubmitting(true);

        try {
            await forgotPassword(userNameOrEmail);
        } finally {
            // Always show the same confirmation, whether or not it matched an account - the API
            // deliberately doesn't reveal that either.
            setSubmitting(false);
            setSubmitted(true);
        }
    };

    if (submitted) {
        return (
            <Center mt={8}>
                <Panel maxW="sm">
                    <VStack gap={3} textAlign="center">
                        <Heading size="md">Check your email</Heading>
                        <Text>If that username or email matches an account, we've sent a link to reset your password.</Text>
                    </VStack>
                </Panel>
            </Center>
        );
    }

    return (
        <Center mt={8}>
            <Panel maxW="sm" width="full">
                {/* A real <form> (rather than an Enter keydown handler) so submission works the
                    way browsers and assistive tech expect. */}
                <VStack as="form" gap={4} onSubmit={(e) => { e.preventDefault(); submit(); }}>
                    <Heading size="md">Reset your password</Heading>
                    <Text fontSize="sm" color="fg.muted">Enter your username or email and we'll send you a link to reset your password.</Text>

                    <Field.Root>
                        <Field.Label>Email / Username</Field.Label>
                        <Input
                            name="username"
                            autoComplete="username"
                            disabled={submitting}
                            value={userNameOrEmail}
                            onChange={(e) => setUserNameOrEmail(e.target.value)}
                        />
                    </Field.Root>

                    <Button
                        type="submit"
                        alignSelf="flex-end"
                        loading={submitting}
                        disabled={!userNameOrEmail}
                        colorPalette="blue"
                    >
                        Send reset link
                    </Button>
                </VStack>
            </Panel>
        </Center>
    );
}
