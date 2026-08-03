import { useRef, useState } from "react";
import {
    Button, Center, Dialog, Field, HStack, Input,
    Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { getAllCompetitions, createCompetition } from "../../../../services/competition-admin-service";
import { formatDateOnly } from "../../../../utils/formatDateOnly";
import { toDateOnly } from "../../../../utils/toDateOnly";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { ClickableRow } from "../../../../components/ui/clickable-row";
import { TablePagination } from "../../../../components/ui/table-pagination";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

const currencyFormatter = new Intl.NumberFormat(undefined, { style: "currency", currency: "GBP" });

const PAGE_SIZE = 20;

export function CompetitionsAdminPage() {
    const navigate = useNavigate();
    const [adding, setAdding] = useState(false);
    const [page, setPage] = useState(1);

    const { data: competitions, error, reload } = useAsyncData(getAllCompetitions, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (competitions === null) {
        return <LoadingSpinner />;
    }

    const pageCompetitions = competitions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Tournaments</PageHeading>
            <HStack justify="flex-end">
                <Button size="sm" colorPalette="action" onClick={() => setAdding(true)}>Add competition</Button>
            </HStack>

            <Panel overflowX="auto">
                <Table.Root size="sm" variant="line" striped showColumnBorder>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Name</Table.ColumnHeader>
                            <Table.ColumnHeader>Start date</Table.ColumnHeader>
                            <Table.ColumnHeader>End date</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="right">Entrance fee</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageCompetitions.map((c) => (
                            <ClickableRow
                                key={c.competitionID}
                                onActivate={() => { void navigate(`/admin/tournaments/${c.competitionID}`); }}
                            >
                                <Table.Cell>{c.prependNameWithThe ? "The " : ""}{c.competitionName}</Table.Cell>
                                <Table.Cell>{formatDateOnly(c.startDate)}</Table.Cell>
                                <Table.Cell>{formatDateOnly(c.endDate)}</Table.Cell>
                                <Table.Cell textAlign="right">{currencyFormatter.format(c.entranceFee)}</Table.Cell>
                            </ClickableRow>
                        ))}
                    </Table.Body>
                </Table.Root>

                {competitions.length === 0 && (
                    <Center mt={4}>
                        <Text color="fg.muted">No competitions found.</Text>
                    </Center>
                )}

                <TablePagination count={competitions.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </Panel>

            {adding && (
                <AddCompetitionDialog
                    onClose={() => setAdding(false)}
                    onCreated={(id) => { void navigate(`/admin/tournaments/${id}`); }}
                />
            )}
        </VStack>
    );
}

// A newly-created competition needs sensible placeholder dates to satisfy StartDate < EndDate
// validation - the admin fills in the real details on the edit page immediately afterwards.
function defaultDates(): { startDate: string; endDate: string } {
    const start = new Date();
    const end = new Date();
    end.setFullYear(end.getFullYear() + 1);

    return { startDate: toDateOnly(start), endDate: toDateOnly(end) };
}

function AddCompetitionDialog({ onClose, onCreated }: { onClose: () => void; onCreated: (competitionId: string) => void }) {
    const [name, setName] = useState("");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const nameInputRef = useRef<HTMLInputElement>(null);

    const save = async () => {
        if (!name.trim()) {
            setError("Please enter a competition name.");
            return;
        }

        setSaving(true);
        setError(null);

        try {
            const { startDate, endDate } = defaultDates();
            const created = await createCompetition({
                competitionName: name.trim(),
                prependNameWithThe: false,
                startDate,
                endDate,
                duplicateFixturesAllowed: false,
                openForRegistration: false,
                registrationAvailableOnLoginPage: false,
                showInHallOfFame: false,
                entranceFee: 0,
                payPalPaymentAvailable: true,
                information: null,
                imageFilename: null,
                defaultToNeutralGround: false,
                allowTwoPointers: true,
                externalApiCompetitionCode: null,
            });

            onCreated(created.competitionID);
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        // initialFocusEl (not the Input's own autoFocus) so the dialog's own focus trap decides
        // when to apply it, instead of racing a raw DOM autofocus against that trap.
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }} initialFocusEl={() => nameInputRef.current}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>Add competition</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <Field.Root>
                                    <Field.Label>Competition name</Field.Label>
                                    <Input ref={nameInputRef} size="sm" maxLength={50} value={name} onChange={(e) => setName(e.target.value)} />
                                </Field.Root>
                                <Text fontSize="sm" color="fg.muted">
                                    You can set the dates, entrance fee, and other details after creating it.
                                </Text>
                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={saving} onClick={onClose}>Cancel</Button>
                            <Button colorPalette="action" loading={saving} disabled={saving} onClick={() => { void save(); }}>Add</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}
