import { useEffect, useState } from "react";
import { Link as RouterLink, useSearchParams } from "react-router";
import {
    Button, Center, Field, Heading, HStack, Input, Link, Spinner, Text, VStack,
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

export function RegisterPage() {
    const [searchParams] = useSearchParams();
    const competitionId = searchParams.get("competitionId");

    const [competition, setCompetition] = useState<CompetitionRegistrationDetails | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        if (!competitionId) return;

        getCompetitionForRegistration(competitionId)
            .then(setCompetition)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    }, [competitionId]);

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

    if (error) {
        return (
            <Center mt={8}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (competition === null) {
        return (
            <Center mt={8}>
                <Spinner />
            </Center>
        );
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
            const user = await registerUser(fields);
            setUser(user);

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
        <VStack align="stretch" gap={6} maxW="container.sm" mx="auto">
            <CompetitionSummaryCard competition={competition} />

            {step === "payment" ? (
                <PaymentStep
                    competitionId={competition.competitionID}
                    entranceFee={competition.entranceFee}
                    payPalPaymentAvailable={competition.payPalPaymentAvailable}
                    onRegistered={registrationComplete}
                />
            ) : (
                <Panel>
                    <VStack align="stretch" gap={3}>
                        <Heading size="md">Create your account</Heading>

                        <HStack align="start">
                            <Field.Root>
                                <Field.Label>First name</Field.Label>
                                <Input size="sm" maxLength={50} disabled={submitting} value={fields.forenames} onChange={(e) => update({ forenames: e.target.value })} />
                            </Field.Root>
                            <Field.Root>
                                <Field.Label>Surname</Field.Label>
                                <Input size="sm" maxLength={50} disabled={submitting} value={fields.surname} onChange={(e) => update({ surname: e.target.value })} />
                            </Field.Root>
                        </HStack>

                        <Field.Root>
                            <Field.Label>Username</Field.Label>
                            <Input size="sm" maxLength={256} disabled={submitting} value={fields.userName} onChange={(e) => update({ userName: e.target.value })} />
                        </Field.Root>

                        <Field.Root>
                            <Field.Label>Email</Field.Label>
                            <Input size="sm" type="email" maxLength={256} disabled={submitting} value={fields.email} onChange={(e) => update({ email: e.target.value })} />
                        </Field.Root>

                        <HStack align="start">
                            <Field.Root>
                                <Field.Label>Password</Field.Label>
                                <PasswordInput size="sm" disabled={submitting} value={fields.password} onChange={(e) => update({ password: e.target.value })} />
                            </Field.Root>
                            <Field.Root>
                                <Field.Label>Confirm password</Field.Label>
                                <PasswordInput size="sm" disabled={submitting} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
                            </Field.Root>
                        </HStack>

                        {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                        <Button
                            alignSelf="flex-end"
                            colorPalette="blue"
                            loading={submitting}
                            disabled={!fields.forenames || !fields.surname || !fields.userName || !fields.email || !fields.password || !confirmPassword}
                            onClick={submitDetails}
                        >
                            {competition.entranceFee > 0 ? "Continue to payment" : "Register"}
                        </Button>
                    </VStack>
                </Panel>
            )}
        </VStack>
    );
}
