import { useState } from "react";
import {
    Button, Center, Checkbox, Dialog, Field, HStack, Input, NativeSelect,
    Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { useCompetition } from "../../../../hooks/useCompetition";
import {
    getMatchesForAdmin, createMatch, updateMatch, deleteMatch,
    type MatchAdmin, type CreateMatchAdmin,
} from "../../../../services/match-admin-service";
import { getTeamsForCompetition, type Team } from "../../../../services/team-service";
import { ApiError } from "../../../../services/api";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { ClickableRow } from "../../../../components/ui/clickable-row";
import { TablePagination } from "../../../../components/ui/table-pagination";

const emptyMatch = (competitionId: string): CreateMatchAdmin => ({
    competitionID: competitionId,
    matchDateTime: "",
    homeTeamID: null,
    awayTeamID: null,
    homeTeamTBC: "",
    awayTeamTBC: "",
    description: "",
    homeTeamGoals: null,
    awayTeamGoals: null,
    neutralGround: false,
    knockout: false,
    matchPlayed: false,
});

// datetime-local inputs need "YYYY-MM-DDTHH:mm" with no timezone/seconds.
function toDateTimeLocal(isoString: string): string {
    if (!isoString) return "";
    return isoString.slice(0, 16);
}

export function MatchesAdminPage() {
    const { currentCompetitionId, isLoading } = useCompetition();

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (!currentCompetitionId) {
        return (
            <Center mt={4}>
                <Text>You're not registered for any competitions yet.</Text>
            </Center>
        );
    }

    return <MatchesAdminTable key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

const PAGE_SIZE = 20;

function MatchesAdminTable({ competitionId }: { competitionId: string }) {
    const [includePlayed, setIncludePlayed] = useState(false);
    const [editing, setEditing] = useState<MatchAdmin | "new" | null>(null);
    const [page, setPage] = useState(1);

    const { data, error, reload } = useAsyncData(async () => {
        const [matches, teams] = await Promise.all([
            getMatchesForAdmin(competitionId, includePlayed),
            getTeamsForCompetition(competitionId),
        ]);
        return { matches, teams };
    }, [competitionId, includePlayed]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const { matches, teams } = data;

    const teamName = (teamId: string | null, tbc: string | null) =>
        teams.find((t) => t.teamID === teamId)?.teamName ?? tbc ?? "TBC";

    const pageMatches = matches.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Matches</PageHeading>
            <HStack justify="space-between">
                <Checkbox.Root checked={includePlayed} onCheckedChange={(e) => { setIncludePlayed(!!e.checked); setPage(1); }}>
                    <Checkbox.HiddenInput />
                    <Checkbox.Control />
                    <Checkbox.Label>Include played matches</Checkbox.Label>
                </Checkbox.Root>
                <Button size="sm" colorPalette="blue" onClick={() => setEditing("new")}>Add match</Button>
            </HStack>

            <Panel overflowX="auto">
                <Table.Root size="sm" variant="line" striped showColumnBorder>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Home</Table.ColumnHeader>
                            <Table.ColumnHeader>Away</Table.ColumnHeader>
                            <Table.ColumnHeader>Date</Table.ColumnHeader>
                            <Table.ColumnHeader>Description</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Score</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageMatches.map((m) => (
                            <ClickableRow key={m.matchID} onActivate={() => setEditing(m)}>
                                <Table.Cell>{teamName(m.homeTeamID, m.homeTeamTBC)}</Table.Cell>
                                <Table.Cell>{teamName(m.awayTeamID, m.awayTeamTBC)}</Table.Cell>
                                {/* "medium" spells the month, so it reads unambiguously regardless of
                                    whether the user's locale orders day/month as DD/MM or MM/DD. */}
                                <Table.Cell>{new Date(m.matchDateTime).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}</Table.Cell>
                                <Table.Cell>{m.description}</Table.Cell>
                                <Table.Cell textAlign="center">
                                    {m.matchPlayed ? `${m.homeTeamGoals ?? "?"} - ${m.awayTeamGoals ?? "?"}` : ""}
                                </Table.Cell>
                            </ClickableRow>
                        ))}
                    </Table.Body>
                </Table.Root>

                {matches.length === 0 && (
                    <Center mt={4}>
                        <Text color="fg.muted">No matches found.</Text>
                    </Center>
                )}

                <TablePagination count={matches.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </Panel>

            {editing !== null && (
                <MatchEditDialog
                    competitionId={competitionId}
                    match={editing === "new" ? null : editing}
                    teams={teams}
                    onClose={() => setEditing(null)}
                    onSaved={() => { setEditing(null); reload(); }}
                />
            )}
        </VStack>
    );
}

function MatchEditDialog({ competitionId, match, teams, onClose, onSaved }: {
    competitionId: string;
    match: MatchAdmin | null;
    teams: Team[];
    onClose: () => void;
    onSaved: () => void;
}) {
    const [form, setForm] = useState<CreateMatchAdmin>(match ?? emptyMatch(competitionId));
    const [saving, setSaving] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [confirmingDelete, setConfirmingDelete] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const save = async () => {
        setSaving(true);
        setError(null);

        try {
            // Sent as a naive local wall-clock string (no timezone conversion) - MatchDateTime is
            // stored and compared as naive local time throughout the app (see the prediction
            // cutoff logic), so round-tripping through Date/toISOString here would shift it by
            // the browser's UTC offset.
            const localDateTime = toDateTimeLocal(form.matchDateTime);
            const payload = { ...form, matchDateTime: localDateTime ? `${localDateTime}:00` : "" };

            if (match) {
                await updateMatch(match.matchID, { ...payload, matchID: match.matchID });
            } else {
                await createMatch(payload);
            }

            onSaved();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    const onDelete = async () => {
        if (!match) return;

        setConfirmingDelete(false);
        setDeleting(true);
        setError(null);

        try {
            await deleteMatch(match.matchID);
            onSaved();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
            setDeleting(false);
        }
    };

    const goalsDisabled = !form.matchPlayed;
    const busy = saving || deleting;

    return (
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>{match ? "Edit match" : "Add match"}</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <HStack align="start">
                                    <Field.Root>
                                        <Field.Label>Home team</Field.Label>
                                        <NativeSelect.Root size="sm">
                                            <NativeSelect.Field
                                                value={form.homeTeamID ?? ""}
                                                onChange={(e) => setForm({ ...form, homeTeamID: e.target.value || null })}
                                            >
                                                <option value="">-- select --</option>
                                                {teams.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                                            </NativeSelect.Field>
                                            <NativeSelect.Indicator />
                                        </NativeSelect.Root>
                                        <Input
                                            mt={1} size="sm" placeholder="or a placeholder name, e.g. Winner of Group A"
                                            value={form.homeTeamTBC ?? ""}
                                            onChange={(e) => setForm({ ...form, homeTeamTBC: e.target.value })}
                                        />
                                    </Field.Root>
                                    <Field.Root>
                                        <Field.Label>Away team</Field.Label>
                                        <NativeSelect.Root size="sm">
                                            <NativeSelect.Field
                                                value={form.awayTeamID ?? ""}
                                                onChange={(e) => setForm({ ...form, awayTeamID: e.target.value || null })}
                                            >
                                                <option value="">-- select --</option>
                                                {teams.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                                            </NativeSelect.Field>
                                            <NativeSelect.Indicator />
                                        </NativeSelect.Root>
                                        <Input
                                            mt={1} size="sm" placeholder="or a placeholder name"
                                            value={form.awayTeamTBC ?? ""}
                                            onChange={(e) => setForm({ ...form, awayTeamTBC: e.target.value })}
                                        />
                                    </Field.Root>
                                </HStack>

                                <Field.Root>
                                    <Field.Label>Date and time</Field.Label>
                                    <Input
                                        size="sm" type="datetime-local"
                                        value={toDateTimeLocal(form.matchDateTime)}
                                        onChange={(e) => setForm({ ...form, matchDateTime: e.target.value })}
                                    />
                                </Field.Root>

                                <Field.Root>
                                    <Field.Label>Description</Field.Label>
                                    <Input
                                        size="sm" maxLength={50}
                                        value={form.description ?? ""}
                                        onChange={(e) => setForm({ ...form, description: e.target.value })}
                                    />
                                </Field.Root>

                                <HStack>
                                    <Field.Root>
                                        <Field.Label>Home goals</Field.Label>
                                        <Input
                                            size="sm" type="number" min={0} max={99} width="80px" disabled={goalsDisabled}
                                            value={form.homeTeamGoals ?? ""}
                                            onChange={(e) => setForm({ ...form, homeTeamGoals: e.target.value === "" ? null : Number(e.target.value) })}
                                        />
                                    </Field.Root>
                                    <Field.Root>
                                        <Field.Label>Away goals</Field.Label>
                                        <Input
                                            size="sm" type="number" min={0} max={99} width="80px" disabled={goalsDisabled}
                                            value={form.awayTeamGoals ?? ""}
                                            onChange={(e) => setForm({ ...form, awayTeamGoals: e.target.value === "" ? null : Number(e.target.value) })}
                                        />
                                    </Field.Root>
                                </HStack>

                                <HStack>
                                    <Checkbox.Root checked={form.neutralGround} onCheckedChange={(e) => setForm({ ...form, neutralGround: !!e.checked })}>
                                        <Checkbox.HiddenInput />
                                        <Checkbox.Control />
                                        <Checkbox.Label>Neutral ground</Checkbox.Label>
                                    </Checkbox.Root>
                                    <Checkbox.Root checked={form.knockout} onCheckedChange={(e) => setForm({ ...form, knockout: !!e.checked })}>
                                        <Checkbox.HiddenInput />
                                        <Checkbox.Control />
                                        <Checkbox.Label>Knockout</Checkbox.Label>
                                    </Checkbox.Root>
                                </HStack>

                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer justifyContent={match ? "space-between" : "flex-end"}>
                            {match && (
                                <Button variant="ghost" colorPalette="red" loading={deleting} disabled={busy} onClick={() => setConfirmingDelete(true)}>
                                    Delete
                                </Button>
                            )}
                            <HStack>
                                <Button variant="ghost" disabled={busy} onClick={onClose}>Cancel</Button>
                                <Button colorPalette="blue" loading={saving} disabled={busy} onClick={() => { void save(); }}>Save</Button>
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
                                <Dialog.Title>Delete match</Dialog.Title>
                            </Dialog.Header>
                            <Dialog.Body>
                                <Text>Are you sure you wish to delete this match?</Text>
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
