import { useState } from "react";
import { Link as RouterLink } from "react-router";
import {
    Button, Center, Checkbox, Dialog, Field, HStack, Input, Link, NativeSelect,
    Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { useCompetition } from "../../../../hooks/useCompetition";
import {
    getMatchesForAdmin, createMatch, updateMatch, deleteMatch, numberBracketByKickOff,
    type MatchAdmin, type CreateMatchAdmin,
} from "../../../../services/match-admin-service";
import { getTeamsForCompetition, type Team } from "../../../../services/team-service";
import { getKnockoutBracket } from "../../../../services/prediction-service";
import { ApiError } from "../../../../services/api";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { ClickableRow } from "../../../../components/ui/clickable-row";
import { TablePagination } from "../../../../components/ui/table-pagination";
import { compactCellsOnSmallScreens } from "../../../../components/ui/table-density";

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
    knockoutRound: null,
    bracketSlot: null,
    matchPlayed: false,
});

// The rounds a match can be placed in, valued as dbo.Match.KnockoutRound expects - the number of
// teams contesting the round, and the sentinel 3 for the third-place play-off, which isn't part of
// the bracket tree. Mirrors Application/Common/KnockoutRounds.cs.
const BRACKET_ROUNDS = [
    { value: 32, label: "Round of 32" },
    { value: 16, label: "Round of 16" },
    { value: 8, label: "Quarter-final" },
    { value: 4, label: "Semi-final" },
    { value: 3, label: "3rd place playoff" },
    { value: 2, label: "Final" },
];

const ROUND_LABELS = new Map(BRACKET_ROUNDS.map((r) => [r.value, r.label]));

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

// The round/stage description is the one column here a phone can do without - the two teams and
// the date already identify the match, and the description is on the edit dialog a tap away.
const DESCRIPTION_DISPLAY = { base: "none", md: "table-cell" };

// Round and slot go the same way, and for the same reason: they matter when an admin is checking a
// draw at a desk, and a phone has room for the teams and the date or for these, not both.
const BRACKET_DISPLAY = { base: "none", md: "table-cell" };

const PAGE_SIZE = 20;

function MatchesAdminTable({ competitionId }: { competitionId: string }) {
    const [includePlayed, setIncludePlayed] = useState(false);
    const [numberingBracket, setNumberingBracket] = useState(false);
    const [confirmingNumbering, setConfirmingNumbering] = useState(false);
    const [numberingResult, setNumberingResult] = useState<string | null>(null);
    const [numberingError, setNumberingError] = useState<string | null>(null);
    const [editing, setEditing] = useState<MatchAdmin | "new" | null>(null);
    const [page, setPage] = useState(1);
    const [homeTeamFilter, setHomeTeamFilter] = useState("");
    const [awayTeamFilter, setAwayTeamFilter] = useState("");
    const [eitherTeamFilter, setEitherTeamFilter] = useState("");

    const { data, error, reload } = useAsyncData(async () => {
        // The bracket comes from the same endpoint the Predictions page draws from, rather than
        // being worked out again from the matches below, so an admin is told exactly what the view
        // itself objects to. It carries this admin's own predictions too, which are ignored here -
        // a wasted column or two against having one answer to "is this bracket sound?".
        const [matches, teams, bracket] = await Promise.all([
            getMatchesForAdmin(competitionId, includePlayed),
            getTeamsForCompetition(competitionId),
            getKnockoutBracket(competitionId).catch(() => null),
        ]);
        return { matches, teams, bracket };
    }, [competitionId, includePlayed]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const { teams } = data;

    const teamName = (teamId: string | null, tbc: string | null) =>
        teams.find((t) => t.teamID === teamId)?.teamName ?? tbc ?? "TBC";

    const matches = data.matches.filter((m) =>
        (!homeTeamFilter || m.homeTeamID === homeTeamFilter) &&
        (!awayTeamFilter || m.awayTeamID === awayTeamFilter) &&
        (!eitherTeamFilter || m.homeTeamID === eitherTeamFilter || m.awayTeamID === eitherTeamFilter)
    );

    const pageMatches = matches.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    // Only worth offering where there is a bracket to number - a league season has none.
    const hasBracketMatches = data.matches.some((m) => m.knockoutRound !== null);

    const numberBracket = async () => {
        setConfirmingNumbering(false);
        setNumberingError(null);
        setNumberingResult(null);
        setNumberingBracket(true);

        try {
            const summary = await numberBracketByKickOff(competitionId);
            setNumberingResult(
                `Numbered ${summary.matchesNumbered} match${summary.matchesNumbered === 1 ? "" : "es"} across `
                + `${summary.roundsNumbered} round${summary.roundsNumbered === 1 ? "" : "s"}. Check the halves of the draw.`);
            reload();
        } catch (e) {
            setNumberingError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong.");
        } finally {
            setNumberingBracket(false);
        }
    };

    const numberingDialog = (
        <Dialog.Root role="alertdialog" open={confirmingNumbering} onOpenChange={(e) => { if (!e.open) setConfirmingNumbering(false); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>Number Bracket By Kick-off</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <Text>
                                    This gives every match that already has a bracket round a slot, numbering each
                                    round's matches in kick-off order and replacing any slots already set.
                                </Text>
                                {/* Said plainly because the result looks plausible either way: a bracket numbered
                                    from the wrong order still draws as a tree, just with the wrong ties in the
                                    wrong halves, and nothing downstream can tell. */}
                                <Text fontWeight="bold">
                                    A competition's schedule is not its draw, so expect to correct this by hand.
                                </Text>
                                <Text>
                                    Check the knockout view afterwards, particularly that each tie is in the half of
                                    the draw it belongs to: slots 1 and 2 of a round feed slot 1 of the next.
                                </Text>
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" onClick={() => setConfirmingNumbering(false)}>Cancel</Button>
                            <Button colorPalette="action" onClick={() => { void numberBracket(); }}>Number Bracket</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );

    return (
        <VStack align="stretch" gap={4}>
            {numberingDialog}
            <PageHeading>Matches</PageHeading>
            <HStack justify="space-between" wrap="wrap" gap={3}>
                <Checkbox.Root checked={includePlayed} onCheckedChange={(e) => { setIncludePlayed(!!e.checked); setPage(1); }}>
                    <Checkbox.HiddenInput />
                    <Checkbox.Control />
                    <Checkbox.Label>Include played matches</Checkbox.Label>
                </Checkbox.Root>
                <HStack gap={2}>
                    {hasBracketMatches && (
                        <Button size="sm" variant="outline" loading={numberingBracket} disabled={numberingBracket}
                            onClick={() => setConfirmingNumbering(true)}>
                            Number Bracket By Kick-off
                        </Button>
                    )}
                    <Button size="sm" colorPalette="action" onClick={() => setEditing("new")}>Add Match</Button>
                </HStack>
            </HStack>

            {numberingError && <Text fontSize="sm" color="fg.error">{numberingError}</Text>}
            {numberingResult && <Text fontSize="sm" color="fg.success">{numberingResult}</Text>}

            <HStack wrap="wrap" gap={3} align="end">
                <Field.Root maxW="200px">
                    <Field.Label>Home team</Field.Label>
                    <NativeSelect.Root size="sm">
                        <NativeSelect.Field
                            value={homeTeamFilter}
                            onChange={(e) => { setHomeTeamFilter(e.target.value); setPage(1); }}
                        >
                            <option value="">Any</option>
                            {teams.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                        </NativeSelect.Field>
                        <NativeSelect.Indicator />
                    </NativeSelect.Root>
                </Field.Root>
                <Field.Root maxW="200px">
                    <Field.Label>Away team</Field.Label>
                    <NativeSelect.Root size="sm">
                        <NativeSelect.Field
                            value={awayTeamFilter}
                            onChange={(e) => { setAwayTeamFilter(e.target.value); setPage(1); }}
                        >
                            <option value="">Any</option>
                            {teams.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                        </NativeSelect.Field>
                        <NativeSelect.Indicator />
                    </NativeSelect.Root>
                </Field.Root>
                <Field.Root maxW="200px">
                    <Field.Label>Home or away team</Field.Label>
                    <NativeSelect.Root size="sm">
                        <NativeSelect.Field
                            value={eitherTeamFilter}
                            onChange={(e) => { setEitherTeamFilter(e.target.value); setPage(1); }}
                        >
                            <option value="">Any</option>
                            {teams.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                        </NativeSelect.Field>
                        <NativeSelect.Indicator />
                    </NativeSelect.Root>
                </Field.Root>
            </HStack>

            {hasBracketMatches && data.bracket !== null && (
                <Panel>
                    <VStack align="stretch" gap={2}>
                        <Text fontWeight="bold">Knockout bracket</Text>
                        {data.bracket.problems.length === 0 ? (
                            <Text fontSize="sm" color="fg.muted">
                                Complete - the knockout view is on offer to players.
                            </Text>
                        ) : (
                            <>
                                {/* Said here rather than left to be inferred from the knockout view not
                                    appearing, which is all an admin used to get. A bracket numbered
                                    wrongly still draws as a tree, so the eye is no check on it. The
                                    fixing is done in this page's own rows, which is why the list stays
                                    here as well as on the bracket page. */}
                                <Text fontSize="sm" color="fg.muted">
                                    The knockout view stays hidden from players until these are fixed.
                                </Text>
                                <VStack as="ul" align="stretch" gap={1} pl={4}>
                                    {data.bracket.problems.map((problem) => (
                                        <Text as="li" key={problem} fontSize="sm">{problem}</Text>
                                    ))}
                                </VStack>
                            </>
                        )}
                        <Link asChild color="fg.link" fontSize="sm" alignSelf="flex-start">
                            <RouterLink to="/admin/bracket">Fill in the draw from the group tables</RouterLink>
                        </Link>
                    </VStack>
                </Panel>
            )}

            <Panel overflowX="auto">
                <Table.Root size="sm" variant="line" striped showColumnBorder css={compactCellsOnSmallScreens}>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Home</Table.ColumnHeader>
                            <Table.ColumnHeader>Away</Table.ColumnHeader>
                            <Table.ColumnHeader>Date</Table.ColumnHeader>
                            <Table.ColumnHeader display={DESCRIPTION_DISPLAY}>Description</Table.ColumnHeader>
                            {hasBracketMatches && <Table.ColumnHeader display={BRACKET_DISPLAY}>Round</Table.ColumnHeader>}
                            {hasBracketMatches && <Table.ColumnHeader display={BRACKET_DISPLAY} textAlign="center">Slot</Table.ColumnHeader>}
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
                                <Table.Cell display={DESCRIPTION_DISPLAY}>{m.description}</Table.Cell>
                                {/* The draw, readable down the column. Checking the numbering used to
                                    mean opening every knockout match in turn and holding the tree in
                                    your head; a bracket is wrong in ways only the whole shape shows. */}
                                {hasBracketMatches && (
                                    <Table.Cell display={BRACKET_DISPLAY} color={m.knockoutRound === null ? "fg.muted" : undefined}>
                                        {m.knockoutRound === null ? "-" : ROUND_LABELS.get(m.knockoutRound) ?? m.knockoutRound}
                                    </Table.Cell>
                                )}
                                {hasBracketMatches && (
                                    <Table.Cell display={BRACKET_DISPLAY} textAlign="center" color={m.bracketSlot === null ? "fg.muted" : undefined}>
                                        {m.bracketSlot ?? "-"}
                                    </Table.Cell>
                                )}
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

                                {/* Only a knockout match can sit in the bracket, so the two fields
                                    that place it there stay out of the way until it is one. */}
                                {form.knockout && (
                                    <HStack alignItems="flex-start">
                                        <Field.Root>
                                            <Field.Label>Bracket round</Field.Label>
                                            <NativeSelect.Root size="sm">
                                                <NativeSelect.Field
                                                    value={form.knockoutRound === null ? "" : String(form.knockoutRound)}
                                                    onChange={(e) => setForm({ ...form, knockoutRound: e.target.value === "" ? null : Number(e.target.value) })}
                                                >
                                                    <option value="">-- not in the bracket --</option>
                                                    {BRACKET_ROUNDS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
                                                </NativeSelect.Field>
                                                <NativeSelect.Indicator />
                                            </NativeSelect.Root>
                                            <Field.HelperText>Leave unset to keep this match out of the knockout view.</Field.HelperText>
                                        </Field.Root>
                                        <Field.Root>
                                            <Field.Label>Bracket slot</Field.Label>
                                            <Input
                                                size="sm" type="number" min={1}
                                                value={form.bracketSlot === null ? "" : String(form.bracketSlot)}
                                                onChange={(e) => setForm({ ...form, bracketSlot: e.target.value === "" ? null : Number(e.target.value) })}
                                            />
                                            <Field.HelperText>Position in the draw, top to bottom: 1 and 2 feed slot 1 of the next round.</Field.HelperText>
                                        </Field.Root>
                                    </HStack>
                                )}

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
