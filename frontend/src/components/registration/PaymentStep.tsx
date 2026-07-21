import { useState } from "react";
import { Button, Field, Heading, Input, Separator, Text, VStack } from "@chakra-ui/react";
import { PayPalButtons, PayPalScriptProvider } from "@paypal/react-paypal-js";
import {
    capturePayPalOrder, createPayPalOrder, registerFree, registerWithPaymentCredit,
} from "../../services/competition-registration-service";
import { ApiError } from "../../services/api";
import { Panel } from "../ui/panel";

export function PaymentStep({
    competitionId, entranceFee, payPalPaymentAvailable, onRegistered,
}: {
    competitionId: string;
    entranceFee: number;
    payPalPaymentAvailable: boolean;
    onRegistered: () => void;
}) {
    const [code, setCode] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const joinFree = async () => {
        setSubmitting(true);
        setError(null);
        try {
            await registerFree(competitionId);
            onRegistered();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSubmitting(false);
        }
    };

    const redeemCode = async () => {
        setSubmitting(true);
        setError(null);
        try {
            await registerWithPaymentCredit(competitionId, code);
            onRegistered();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSubmitting(false);
        }
    };

    if (entranceFee <= 0) {
        return (
            <VStack align="stretch" gap={3}>
                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                <Button colorPalette="blue" loading={submitting} onClick={() => { void joinFree(); }}>Join competition</Button>
            </VStack>
        );
    }

    return (
        <Panel>
            <VStack align="stretch" gap={4}>
                <Heading size="sm">Pay your entry fee</Heading>

                <VStack align="stretch" gap={2}>
                    <Field.Root>
                        <Field.Label>Payment credit code</Field.Label>
                        <Input size="sm" disabled={submitting} value={code} onChange={(e) => setCode(e.target.value)} />
                    </Field.Root>
                    <Button colorPalette="blue" loading={submitting} disabled={!code} onClick={() => { void redeemCode(); }} alignSelf="flex-start">
                        Redeem code
                    </Button>
                </VStack>

                {payPalPaymentAvailable && import.meta.env.VITE_PAYPAL_CLIENT_ID && (
                    <>
                        <Separator />
                        <PayPalScriptProvider options={{ clientId: import.meta.env.VITE_PAYPAL_CLIENT_ID, currency: "GBP" }}>
                            <PayPalButtons
                                style={{ layout: "vertical" }}
                                disabled={submitting}
                                createOrder={async () => {
                                    const order = await createPayPalOrder(competitionId);
                                    return order.orderId;
                                }}
                                onApprove={async (data) => {
                                    setSubmitting(true);
                                    setError(null);
                                    try {
                                        await capturePayPalOrder(competitionId, data.orderID);
                                        onRegistered();
                                    } catch (e) {
                                        setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
                                    } finally {
                                        setSubmitting(false);
                                    }
                                }}
                                onError={() => setError("PayPal payment failed. Please try again.")}
                            />
                        </PayPalScriptProvider>
                    </>
                )}

                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
            </VStack>
        </Panel>
    );
}
