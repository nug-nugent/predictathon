import { useState } from "react";
import {
    Button, Center, Checkbox, Dialog, Field, HStack, Input, NativeSelect,
    Portal, Table, Text, Textarea, VStack,
} from "@chakra-ui/react";
import {
    getAllAnnouncements, createAnnouncement, updateAnnouncement, deleteAnnouncement,
    type AnnouncementAdmin, type CreateAnnouncementAdmin,
} from "../../../../services/announcement-admin-service";
import type { AnnouncementSeverity } from "../../../../services/announcement-service";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { ClickableRow } from "../../../../components/ui/clickable-row";
import { TablePagination } from "../../../../components/ui/table-pagination";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

const PAGE_SIZE = 20;

const emptyAnnouncement: CreateAnnouncementAdmin = {
    content: "",
    showOnLoginPage: false,
    showOnHomepage: true,
    severity: "Info",
    expiryDateTimeUtc: null,
};

// Expiry is a real UTC instant (compared against the server's DateTime.UtcNow) rather than the naive
// local wall-clock convention used for match kickoffs, so - unlike MatchesAdminPage's helper - this
// genuinely converts between the datetime-local input's local time and a UTC ISO string.
function toDateTimeLocal(isoUtc: string | null): string {
    if (!isoUtc) return "";
    const d = new Date(isoUtc);
    const pad = (n: number) => n.toString().padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDateTimeLocal(local: string): string | null {
    return local ? new Date(local).toISOString() : null;
}

export function AnnouncementsAdminPage() {
    const [editing, setEditing] = useState<AnnouncementAdmin | "new" | null>(null);
    const [page, setPage] = useState(1);

    const { data: announcements, error, reload } = useAsyncData(getAllAnnouncements, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (announcements === null) {
        return <LoadingSpinner />;
    }

    const pageAnnouncements = announcements.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Announcements</PageHeading>
            <HStack justify="flex-end">
                <Button size="sm" colorPalette="action" onClick={() => setEditing("new")}>Add announcement</Button>
            </HStack>

            <Panel overflowX="auto">
                <Table.Root size="sm" variant="line" striped showColumnBorder>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Content</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Shown on login page</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Shown on homepage</Table.ColumnHeader>
                            <Table.ColumnHeader>Severity</Table.ColumnHeader>
                            <Table.ColumnHeader>Expires</Table.ColumnHeader>
                            <Table.ColumnHeader>Created</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageAnnouncements.map((a) => (
                            <ClickableRow key={a.announcementID} onActivate={() => setEditing(a)}>
                                <Table.Cell maxW="400px" truncate>{a.content}</Table.Cell>
                                <Table.Cell textAlign="center">{a.showOnLoginPage ? "Yes" : ""}</Table.Cell>
                                <Table.Cell textAlign="center">{a.showOnHomepage ? "Yes" : ""}</Table.Cell>
                                <Table.Cell color={a.severity === "Warning" ? "status.urgent" : undefined} fontWeight={a.severity === "Warning" ? "medium" : undefined}>
                                    {a.severity === "Warning" ? "Warning" : ""}
                                </Table.Cell>
                                <Table.Cell>
                                    {a.expiryDateTimeUtc
                                        ? new Date(a.expiryDateTimeUtc).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })
                                        : "Never"}
                                </Table.Cell>
                                <Table.Cell>{new Date(a.createdAtUtc).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}</Table.Cell>
                            </ClickableRow>
                        ))}
                    </Table.Body>
                </Table.Root>

                {announcements.length === 0 && (
                    <Center mt={4}>
                        <Text color="fg.muted">No announcements found.</Text>
                    </Center>
                )}

                <TablePagination count={announcements.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </Panel>

            {editing !== null && (
                <AnnouncementEditDialog
                    announcement={editing === "new" ? null : editing}
                    onClose={() => setEditing(null)}
                    onSaved={() => { setEditing(null); reload(); }}
                />
            )}
        </VStack>
    );
}

function AnnouncementEditDialog({ announcement, onClose, onSaved }: {
    announcement: AnnouncementAdmin | null;
    onClose: () => void;
    onSaved: () => void;
}) {
    const [form, setForm] = useState<CreateAnnouncementAdmin>(announcement ?? emptyAnnouncement);
    const [saving, setSaving] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [confirmingDelete, setConfirmingDelete] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const save = async () => {
        if (!form.content.trim()) {
            setError("Please enter the announcement content.");
            return;
        }

        if (!form.showOnLoginPage && !form.showOnHomepage) {
            setError("Select at least one of \"Show on login page\" or \"Show on homepage\".");
            return;
        }

        setSaving(true);
        setError(null);

        try {
            if (announcement) {
                await updateAnnouncement(announcement.announcementID, { ...announcement, ...form });
            } else {
                await createAnnouncement(form);
            }

            onSaved();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    const onDelete = async () => {
        if (!announcement) return;

        setConfirmingDelete(false);
        setDeleting(true);
        setError(null);

        try {
            await deleteAnnouncement(announcement.announcementID);
            onSaved();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
            setDeleting(false);
        }
    };

    const busy = saving || deleting;

    return (
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>{announcement ? "Edit announcement" : "Add announcement"}</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <Field.Root>
                                    <Field.Label>Content</Field.Label>
                                    <Textarea
                                        rows={4} maxLength={2000}
                                        value={form.content}
                                        onChange={(e) => setForm({ ...form, content: e.target.value })}
                                    />
                                </Field.Root>

                                <Field.Root maxW="200px">
                                    <Field.Label>Severity</Field.Label>
                                    <NativeSelect.Root size="sm">
                                        <NativeSelect.Field
                                            value={form.severity}
                                            onChange={(e) => setForm({ ...form, severity: e.target.value as AnnouncementSeverity })}
                                        >
                                            <option value="Info">Info</option>
                                            <option value="Warning">Warning</option>
                                        </NativeSelect.Field>
                                        <NativeSelect.Indicator />
                                    </NativeSelect.Root>
                                </Field.Root>

                                <Field.Root>
                                    <Field.Label>Expires</Field.Label>
                                    <Input
                                        size="sm" type="datetime-local"
                                        value={toDateTimeLocal(form.expiryDateTimeUtc)}
                                        onChange={(e) => setForm({ ...form, expiryDateTimeUtc: fromDateTimeLocal(e.target.value) })}
                                    />
                                </Field.Root>

                                <Checkbox.Root
                                    checked={form.showOnHomepage}
                                    onCheckedChange={(e) => setForm({ ...form, showOnHomepage: !!e.checked })}
                                >
                                    <Checkbox.HiddenInput />
                                    <Checkbox.Control />
                                    <Checkbox.Label>Show on homepage</Checkbox.Label>
                                </Checkbox.Root>

                                <Checkbox.Root
                                    checked={form.showOnLoginPage}
                                    onCheckedChange={(e) => setForm({ ...form, showOnLoginPage: !!e.checked })}
                                >
                                    <Checkbox.HiddenInput />
                                    <Checkbox.Control />
                                    <Checkbox.Label>Show on login page</Checkbox.Label>
                                </Checkbox.Root>

                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer justifyContent={announcement ? "space-between" : "flex-end"}>
                            {announcement && (
                                <Button variant="ghost" colorPalette="red" loading={deleting} disabled={busy} onClick={() => setConfirmingDelete(true)}>
                                    Delete
                                </Button>
                            )}
                            <HStack>
                                <Button variant="ghost" disabled={busy} onClick={onClose}>Cancel</Button>
                                <Button colorPalette="action" loading={saving} disabled={busy} onClick={() => { void save(); }}>Save</Button>
                            </HStack>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>

            <Dialog.Root role="alertdialog" open={confirmingDelete} onOpenChange={(e) => { if (!e.open) setConfirmingDelete(false); }}>
                <Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Content>
                            <Dialog.Header>
                                <Dialog.Title>Delete announcement</Dialog.Title>
                            </Dialog.Header>
                            <Dialog.Body>
                                <Text>Are you sure you wish to delete this announcement?</Text>
                            </Dialog.Body>
                            <Dialog.Footer>
                                <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>Cancel</Button>
                                <Button colorPalette="red" onClick={() => { void onDelete(); }}>Delete</Button>
                            </Dialog.Footer>
                        </Dialog.Content>
                    </Dialog.Positioner>
                </Portal>
            </Dialog.Root>
        </Dialog.Root>
    );
}
