import { useEffect, useState } from "react";
import {
    Button, ButtonGroup, Center, Dialog, Field, HStack, IconButton, Input,
    Pagination, Portal, Spinner, Table, Text, VStack,
} from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useNavigate } from "react-router";
import { getAllCompetitions, createCompetition, type CompetitionAdmin } from "../../../../services/competition-admin-service";
import { formatDateOnly } from "../../../../utils/formatDateOnly";
import { toDateOnly } from "../../../../utils/toDateOnly";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";

const currencyFormatter = new Intl.NumberFormat(undefined, { style: "currency", currency: "GBP" });

const PAGE_SIZE = 20;

export function CompetitionsAdminPage() {
    const navigate = useNavigate();
    const [competitions, setCompetitions] = useState<CompetitionAdmin[] | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [adding, setAdding] = useState(false);
    const [page, setPage] = useState(1);

    const reload = () => {
        getAllCompetitions()
            .then(setCompetitions)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, []);

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    if (competitions === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    const pageCompetitions = competitions.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Tournaments</PageHeading>
            <HStack justify="flex-end">
                <Button size="sm" colorPalette="blue" onClick={() => setAdding(true)}>Add competition</Button>
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
                            <Table.Row
                                key={c.competitionID}
                                onClick={() => navigate(`/admin/tournaments/${c.competitionID}`)}
                                cursor="pointer"
                                _hover={{ bg: "bg.muted" }}
                            >
                                <Table.Cell>{c.prependNameWithThe ? "The " : ""}{c.competitionName}</Table.Cell>
                                <Table.Cell>{formatDateOnly(c.startDate)}</Table.Cell>
                                <Table.Cell>{formatDateOnly(c.endDate)}</Table.Cell>
                                <Table.Cell textAlign="right">{currencyFormatter.format(c.entranceFee)}</Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>

                {competitions.length === 0 && (
                    <Center mt={4}>
                        <Text color="fg.muted">No competitions found.</Text>
                    </Center>
                )}

                {competitions.length > PAGE_SIZE && (
                    <Pagination.Root
                        count={competitions.length}
                        pageSize={PAGE_SIZE}
                        page={page}
                        onPageChange={(e) => setPage(e.page)}
                    >
                        <ButtonGroup variant="ghost" size="sm" justifyContent="center" mt={2}>
                            <Pagination.PrevTrigger asChild>
                                <IconButton aria-label="Previous page"><ChevronLeft /></IconButton>
                            </Pagination.PrevTrigger>
                            <Pagination.Items
                                render={(p) => (
                                    <IconButton variant={{ base: "ghost", _selected: "outline" }} onClick={() => setPage(p.value)}>
                                        {p.value}
                                    </IconButton>
                                )}
                            />
                            <Pagination.NextTrigger asChild>
                                <IconButton aria-label="Next page"><ChevronRight /></IconButton>
                            </Pagination.NextTrigger>
                        </ButtonGroup>
                    </Pagination.Root>
                )}
            </Panel>

            {adding && (
                <AddCompetitionDialog
                    onClose={() => setAdding(false)}
                    onCreated={(id) => navigate(`/admin/tournaments/${id}`)}
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
                allowTwoPointers: false,
            });

            onCreated(created.competitionID);
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }}>
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
                                    <Input size="sm" maxLength={50} value={name} onChange={(e) => setName(e.target.value)} autoFocus />
                                </Field.Root>
                                <Text fontSize="sm" color="fg.muted">
                                    You can set the dates, entrance fee, and other details after creating it.
                                </Text>
                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={saving} onClick={onClose}>Cancel</Button>
                            <Button colorPalette="blue" loading={saving} disabled={saving} onClick={save}>Add</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}
