import { useState } from "react";
import { Link as RouterLink, useSearchParams } from "react-router";
import {
    Button, Center, Field, Heading, HStack, Input, Link, Text, VStack,
} from "@chakra-ui/react";
import { PasswordInput } from "../../../components/ui/password-input";
import { CompetitionSummaryCard } from "../../../components/registration/CompetitionSummaryCard";
import { PaymentStep } from "../../../components/registration/PaymentStep";
import { useUser } from "../../../hooks/useUser";
import { useCompetition } from "../../../hooks/useCompetition";
import { registerUser, type RegisterFields } from "../../../services/user-service";
import {
    getCompetitionForRegistration, registerFree, type CompetitionRegistrationDetails,
} from "../../../services/competition-registration-service";
import { ApiError } from "../../../services/api";
import { Panel } from "../../../components/ui/panel";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function RegisterPage() {
    const [searchParams] = useSearchParams();
    const competitionId = searchParams.get("competitionId");

    if (!competitionId) {
        return (
            <Center mt={8}>
                <Text maxW="sm" textAlign="center">
                    Registration requires a link to a specific competition. Please use the "Register" link next to the
                    competition you want to join.
                </Text>
            </Center>
        );
    }

    return <RegisterLoader key={competitionId} competitionId={competitionId} />;
}

function RegisterLoader({ competitionId }: { competitionId: string }) {
    const { data: competition, error } = useAsyncData(() => getCompetitionForRegistration(competitionId), [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (competition === null) {
        return <LoadingSpinner />;
    }

    return <RegisterForm competition={competition} />;
}

type Step = "details" | "payment" | "done";

function RegisterForm({ competition }: { competition: CompetitionRegistrationDetails }) {
    const { setUser } = useUser();
    const { setCurrentCompetitionId, refreshCompetitions } = useCompetition();

    const [step, setStep] = useState<Step>("details");
    const [fields, setFields] = useState<RegisterFields>({
        forenames: "", surname: "", userName: "", email: "", password: "",
    });
    const [confirmPassword, setConfirmPassword] = useState("");
    const [submitting, setSubmitting] = useState(false);
    // Set once the account itself exists, so a retry after a later failure (e.g. the free
    // competition-registration call) doesn't re-submit registration and hit "username taken".
    const [accountCreated, setAccountCreated] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const update = (patch: Partial<RegisterFields>) => setFields((f) => ({ ...f, ...patch }));

    const registrationComplete = async () => {
        // Refresh the shared competitions list before switching to the new one, so it actually
        // shows up in the header's CompetitionSelector instead of that list staying stuck on the
        // (empty) snapshot fetched the instant the account was created.
        await refreshCompetitions();
        setCurrentCompetitionId(competition.competitionID);
        setStep("done");
    };

    const submitDetails = async () => {
        if (fields.password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        setSubmitting(true);
        setError(null);

        try {
            if (!accountCreated) {
                const user = await registerUser(fields);
                setUser(user);
                setAccountCreated(true);
            }

            if (competition.entranceFee <= 0) {
                await registerFree(competition.competitionID);
                await registrationComplete();
            } else {
                setStep("payment");
            }
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSubmitting(false);
        }
    };

    if (step === "done") {
        return (
            <Center mt={8}>
                <VStack gap={3} maxW="sm" textAlign="center">
                    <Heading size="md">You're all set!</Heading>
                    <Text>Thanks for registering for {competition.competitionName}.</Text>
                    <Link asChild variant="underline"><RouterLink to="/">Go to the home page</RouterLink></Link>
                </VStack>
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={6} maxW="lg" mx="auto">
            <CompetitionSummaryCard competition={competition} />

            {step === "payment" ? (
                <PaymentStep
                    competitionId={competition.competitionID}
                    entranceFee={competition.entranceFee}
                    payPalPaymentAvailable={competition.payPalPaymentAvailable}
                    onRegistered={() => { void registrationComplete(); }}
                />
            ) : (
                <Panel>
                    {/* A real <form> so Enter submits and password managers recognise the signup -
                        paired with the autocomplete hints below. */}
                    <VStack as="form" align="stretch" gap={3} onSubmit={(e) => { e.preventDefault(); void submitDetails(); }}>
                        <Heading size="md">Create your account</Heading>

                        <HStack align="start">
                            <Field.Root>
                                <Field.Label>First name</Field.Label>
                                <Input size="sm" name="forenames" autoComplete="given-name" maxLength={50} disabled={submitting || accountCreated} value={fields.forenames} onChange={(e) => update({ forenames: e.target.value })} />
                            </Field.Root>
                            <Field.Root>
                                <Field.Label>Surname</Field.Label>
                                <Input size="sm" name="surname" autoComplete="family-name" maxLength={50} disabled={submitting || accountCreated} value={fields.surname} onChange={(e) => update({ surname: e.target.value })} />
                            </Field.Root>
                        </HStack>

                        <Field.Root>
                            <Field.Label>Username</Field.Label>
                            <Input size="sm" name="username" autoComplete="username" maxLength={256} disabled={submitting || accountCreated} value={fields.userName} onChange={(e) => update({ userName: e.target.value })} />
                        </Field.Root>

                        <Field.Root>
                            <Field.Label>Email</Field.Label>
                            <Input size="sm" name="email" type="email" autoComplete="email" maxLength={256} disabled={submitting || accountCreated} value={fields.email} onChange={(e) => update({ email: e.target.value })} />
                        </Field.Root>

                        <HStack align="start">
                            <Field.Root>
                                <Field.Label>Password</Field.Label>
                                <PasswordInput size="sm" name="new-password" autoComplete="new-password" disabled={submitting || accountCreated} value={fields.password} onChange={(e) => update({ password: e.target.value })} />
                            </Field.Root>
                            <Field.Root>
                                <Field.Label>Confirm password</Field.Label>
                                <PasswordInput size="sm" name="confirm-password" autoComplete="new-password" disabled={submitting || accountCreated} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
                            </Field.Root>
                        </HStack>

                        {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                        <Button
                            type="submit"
                            alignSelf="flex-end"
                            colorPalette="action"
                            loading={submitting}
                            disabled={!fields.forenames || !fields.surname || !fields.userName || !fields.email || !fields.password || !confirmPassword}
                        >
                            {competition.entranceFee > 0 ? "Continue to payment" : "Register"}
                        </Button>
                    </VStack>
                </Panel>
            )}
        </VStack>
    );
}
