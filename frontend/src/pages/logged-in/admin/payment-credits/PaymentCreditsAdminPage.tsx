import { useRef, useState } from "react";
import {
    Badge, Button, Center, Dialog, Field, HStack, IconButton, Input,
    NativeSelect, Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { Trash2 } from "lucide-react";
import { getAllCompetitions, type CompetitionAdmin } from "../../../../services/competition-admin-service";
import {
    getPaymentCredits, createPaymentCredit, deletePaymentCredit, type PaymentCreditAdmin,
} from "../../../../services/payment-credit-admin-service";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { TablePagination } from "../../../../components/ui/table-pagination";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

const PAGE_SIZE = 20;

export function PaymentCreditsAdminPage() {
    const [adding, setAdding] = useState(false);
    const [deleting, setDeleting] = useState<PaymentCreditAdmin | null>(null);
    const [page, setPage] = useState(1);

    const { data, error, reload } = useAsyncData(async () => {
        const [credits, competitions] = await Promise.all([getPaymentCredits(), getAllCompetitions()]);
        return { credits, competitions };
    }, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const { credits, competitions } = data;
    const pageCredits = credits.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Payment Credits</PageHeading>
            <HStack justify="flex-end">
                <Button size="sm" colorPalette="blue" onClick={() => setAdding(true)} disabled={competitions.length === 0}>
                    Add credit
                </Button>
            </HStack>

            <Panel overflowX="auto">
                <Table.Root size="sm" variant="line" striped showColumnBorder>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Competition</Table.ColumnHeader>
                            <Table.ColumnHeader>Expected username</Table.ColumnHeader>
                            <Table.ColumnHeader>Payment code</Table.ColumnHeader>
                            <Table.ColumnHeader>Issued</Table.ColumnHeader>
                            <Table.ColumnHeader>Status</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Delete</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageCredits.map((c) => (
                            <Table.Row key={c.paymentCreditID}>
                                <Table.Cell>{c.competitionName}</Table.Cell>
                                <Table.Cell>{c.expectedUsername}</Table.Cell>
                                <Table.Cell fontFamily="mono">{c.uniquePaymentCode}</Table.Cell>
                                <Table.Cell>{new Date(c.issueDate).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}</Table.Cell>
                                <Table.Cell>
                                    {c.creditUsed
                                        ? <Badge colorPalette="gray" size="sm">Used</Badge>
                                        : <Badge colorPalette="green" size="sm">Unused</Badge>}
                                </Table.Cell>
                                <Table.Cell textAlign="center">
                                    <IconButton
                                        aria-label="Delete credit" size="xs" variant="ghost" colorPalette="red"
                                        onClick={() => setDeleting(c)}
                                    >
                                        <Trash2 size={16} />
                                    </IconButton>
                                </Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>

                {credits.length === 0 && (
                    <Center mt={4}>
                        <Text color="fg.muted">No payment credits found.</Text>
                    </Center>
                )}

                <TablePagination count={credits.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </Panel>

            {adding && (
                <AddPaymentCreditDialog
                    competitions={competitions}
                    onClose={() => setAdding(false)}
                    onCreated={() => { setAdding(false); reload(); }}
                />
            )}

            {deleting && (
                <DeleteConfirmDialog
                    credit={deleting}
                    onClose={() => setDeleting(null)}
                    onDeleted={() => { setDeleting(null); reload(); }}
                />
            )}
        </VStack>
    );
}

function AddPaymentCreditDialog({ competitions, onClose, onCreated }: {
    competitions: CompetitionAdmin[];
    onClose: () => void;
    onCreated: () => void;
}) {
    const [competitionId, setCompetitionId] = useState(competitions[0]?.competitionID ?? "");
    const [expectedUsername, setExpectedUsername] = useState("");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const usernameInputRef = useRef<HTMLInputElement>(null);

    const save = async () => {
        if (!competitionId) {
            setError("Please select a competition.");
            return;
        }
        if (!expectedUsername.trim()) {
            setError("Please enter the expected username.");
            return;
        }

        setSaving(true);
        setError(null);

        try {
            await createPaymentCredit({ competitionID: competitionId, expectedUsername: expectedUsername.trim() });
            onCreated();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        // initialFocusEl (not the Input's own autoFocus) so the dialog's own focus trap decides
        // when to apply it, instead of racing a raw DOM autofocus against that trap.
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }} initialFocusEl={() => usernameInputRef.current}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>Add credit</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <Field.Root>
                                    <Field.Label>For competition</Field.Label>
                                    <NativeSelect.Root size="sm">
                                        <NativeSelect.Field
                                            value={competitionId}
                                            onChange={(e) => setCompetitionId(e.target.value)}
                                        >
                                            {competitions.map((c) => (
                                                <option key={c.competitionID} value={c.competitionID}>
                                                    {c.prependNameWithThe ? "The " : ""}{c.competitionName}
                                                </option>
                                            ))}
                                        </NativeSelect.Field>
                                        <NativeSelect.Indicator />
                                    </NativeSelect.Root>
                                </Field.Root>
                                <Field.Root>
                                    <Field.Label>For user (expected username)</Field.Label>
                                    <Input
                                        ref={usernameInputRef}
                                        size="sm" maxLength={50} value={expectedUsername}
                                        onChange={(e) => setExpectedUsername(e.target.value)}
                                    />
                                </Field.Root>
                                <Text fontSize="sm" color="fg.muted">
                                    A unique payment code will be generated for this user to redeem when registering.
                                </Text>
                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={saving} onClick={onClose}>Cancel</Button>
                            <Button colorPalette="blue" loading={saving} disabled={saving} onClick={() => { void save(); }}>Add</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

function DeleteConfirmDialog({ credit, onClose, onDeleted }: {
    credit: PaymentCreditAdmin;
    onClose: () => void;
    onDeleted: () => void;
}) {
    const [deleting, setDeleting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const confirmDelete = async () => {
        setDeleting(true);
        setError(null);

        try {
            await deletePaymentCredit(credit.paymentCreditID);
            onDeleted();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
            setDeleting(false);
        }
    };

    return (
        <Dialog.Root role="alertdialog" open onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>Delete credit</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={2}>
                                <Text>
                                    Are you sure you wish to delete the credit for{" "}
                                    <Text as="span" fontWeight="medium">{credit.expectedUsername}</Text> ({credit.competitionName})?
                                </Text>
                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={deleting} onClick={onClose}>Cancel</Button>
                            <Button colorPalette="red" loading={deleting} disabled={deleting} onClick={() => { void confirmDelete(); }}>Delete</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}
