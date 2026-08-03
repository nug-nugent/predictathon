import { useEffect, useState } from "react";
import { useParams, Link as RouterLink } from "react-router";
import {
    Button, Center, Checkbox, Dialog, Field, Heading, HStack, IconButton, Input,
    Link, NativeSelect, Portal, Spinner, Table, Text, Textarea, VStack,
} from "@chakra-ui/react";
import { ArrowLeft, X } from "lucide-react";
import { useCompetition } from "../../../../hooks/useCompetition";
import {
    getCompetition, updateCompetition, importFixtures, type CompetitionAdmin, type FixtureImportSummary,
} from "../../../../services/competition-admin-service";
import {
    getAssignedTeamsForCompetition, getUnassignedTeamsForCompetition,
    addTeamToCompetition, removeTeamFromCompetition,
    type AssignedTeam, type Team,
} from "../../../../services/team-service";
import {
    getHallOfFameGenerationStatus, generateHallOfFame, type HallOfFameItem,
} from "../../../../services/hall-of-fame-service";
import { ApiError } from "../../../../services/api";
import { formatDateOnly } from "../../../../utils/formatDateOnly";
import { Panel } from "../../../../components/ui/panel";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

export function CompetitionEditPage() {
    const { id } = useParams<{ id: string }>();

    if (!id) {
        return null;
    }

    return <CompetitionEditLoader key={id} id={id} />;
}

function CompetitionEditLoader({ id }: { id: string }) {
    const { data: competition, error, reload } = useAsyncData(() => getCompetition(id), [id]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (competition === null) {
        return <LoadingSpinner />;
    }

    return <CompetitionEditForm key={competition.competitionID} competition={competition} onReload={reload} />;
}

function CompetitionEditForm({ competition, onReload }: { competition: CompetitionAdmin; onReload: () => void }) {
    const { currentCompetitionId, setCurrentCompetitionId } = useCompetition();

    const [form, setForm] = useState<CompetitionAdmin>(competition);
    const [saving, setSaving] = useState(false);
    const [saved, setSaved] = useState(false);
    const [error, setError] = useState<string | null>(null);
    // Bumped on a successful fixture import to force TeamsSection to remount and refetch - it
    // fetches its own team list once on mount and has no other way to learn teams were just added.
    const [teamsRefreshKey, setTeamsRefreshKey] = useState(0);

    const update = (patch: Partial<CompetitionAdmin>) => {
        setForm((f) => ({ ...f, ...patch }));
        setSaved(false);
    };

    const save = async () => {
        setSaving(true);
        setError(null);

        try {
            const updated = await updateCompetition(form.competitionID, form);
            setForm(updated);
            setSaved(true);
            onReload();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    const isCurrent = currentCompetitionId === form.competitionID;

    return (
        <VStack align="stretch" gap={6} maxW="container.md" mx="auto">
            <HStack justify="space-between">
                <Link asChild color="fg.muted">
                    <RouterLink to="/admin/tournaments">
                        <ArrowLeft size={14} /> Back to competitions
                    </RouterLink>
                </Link>
                <Button
                    size="sm" variant="outline" disabled={isCurrent}
                    onClick={() => setCurrentCompetitionId(form.competitionID)}
                >
                    {isCurrent ? "This is your current competition" : "Change to this competition"}
                </Button>
            </HStack>

            <Heading size="lg">{form.competitionName || "Edit competition"}</Heading>

            <Panel>
                <VStack align="stretch" gap={3}>
                    <HStack align="start">
                        <Field.Root>
                            <Field.Label>Competition name</Field.Label>
                            <Input size="sm" maxLength={50} value={form.competitionName} onChange={(e) => update({ competitionName: e.target.value })} />
                        </Field.Root>
                        <Field.Root maxW="fit-content" pt={7}>
                            <Checkbox.Root checked={form.prependNameWithThe} onCheckedChange={(e) => update({ prependNameWithThe: !!e.checked })}>
                                <Checkbox.HiddenInput />
                                <Checkbox.Control />
                                <Checkbox.Label>Prepend "The"</Checkbox.Label>
                            </Checkbox.Root>
                        </Field.Root>
                    </HStack>

                    <HStack align="start">
                        <Field.Root>
                            <Field.Label>Start date</Field.Label>
                            <Input size="sm" type="date" value={form.startDate} onChange={(e) => update({ startDate: e.target.value })} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label>End date</Field.Label>
                            <Input size="sm" type="date" value={form.endDate} onChange={(e) => update({ endDate: e.target.value })} />
                        </Field.Root>
                    </HStack>

                    <Field.Root maxW="200px">
                        <Field.Label>Entrance fee</Field.Label>
                        <Input
                            size="sm" type="number" min={0} max={99.99} step={0.01}
                            value={form.entranceFee}
                            onChange={(e) => update({ entranceFee: e.target.value === "" ? 0 : Number(e.target.value) })}
                        />
                    </Field.Root>

                    <Field.Root>
                        <Field.Label>Image filename</Field.Label>
                        <Input size="sm" maxLength={40} value={form.imageFilename ?? ""} onChange={(e) => update({ imageFilename: e.target.value || null })} />
                    </Field.Root>

                    <Field.Root>
                        <Field.Label>Information</Field.Label>
                        <Textarea rows={5} value={form.information ?? ""} onChange={(e) => update({ information: e.target.value || null })} />
                    </Field.Root>

                    <Field.Root maxW="200px">
                        <Field.Label>External API competition code</Field.Label>
                        <Input
                            size="sm" maxLength={10} placeholder="e.g. PL"
                            value={form.externalApiCompetitionCode ?? ""}
                            onChange={(e) => update({ externalApiCompetitionCode: e.target.value || null })}
                        />
                        <Field.HelperText>
                            football-data.org's code for this competition. Required to import fixtures below.
                        </Field.HelperText>
                    </Field.Root>

                    <VStack align="stretch" gap={2}>
                        <Checkbox.Root checked={form.duplicateFixturesAllowed} onCheckedChange={(e) => update({ duplicateFixturesAllowed: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Duplicate fixtures allowed</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.openForRegistration} onCheckedChange={(e) => update({ openForRegistration: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Open for registration</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.registrationAvailableOnLoginPage} onCheckedChange={(e) => update({ registrationAvailableOnLoginPage: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Registration available on login page</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.showInHallOfFame} onCheckedChange={(e) => update({ showInHallOfFame: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Show in Hall of Fame</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.payPalPaymentAvailable} onCheckedChange={(e) => update({ payPalPaymentAvailable: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>PayPal payment available</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.defaultToNeutralGround} onCheckedChange={(e) => update({ defaultToNeutralGround: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Default to neutral ground</Checkbox.Label>
                        </Checkbox.Root>
                        <Checkbox.Root checked={form.allowTwoPointers} onCheckedChange={(e) => update({ allowTwoPointers: !!e.checked })}>
                            <Checkbox.HiddenInput />
                            <Checkbox.Control />
                            <Checkbox.Label>Allow two-pointers</Checkbox.Label>
                        </Checkbox.Root>
                    </VStack>

                    {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                    <HStack justify="flex-end">
                        {saved && <Text fontSize="sm" color="fg.success">Your changes have been saved.</Text>}
                        <Button colorPalette="action" loading={saving} disabled={saving} onClick={() => { void save(); }}>Save</Button>
                    </HStack>
                </VStack>
            </Panel>

            <TeamsSection key={teamsRefreshKey} competitionId={form.competitionID} />

            <FixtureImportSection
                competitionId={form.competitionID} externalApiCompetitionCode={form.externalApiCompetitionCode}
                onImported={() => { onReload(); setTeamsRefreshKey((k) => k + 1); }}
            />

            <HallOfFameSection competitionId={form.competitionID} />
        </VStack>
    );
}

function TeamsSection({ competitionId }: { competitionId: string }) {
    const [assigned, setAssigned] = useState<AssignedTeam[] | null>(null);
    const [unassigned, setUnassigned] = useState<Team[] | null>(null);
    const [selectedTeamId, setSelectedTeamId] = useState("");
    const [removing, setRemoving] = useState<AssignedTeam | null>(null);
    const [error, setError] = useState<string | null>(null);

    const reload = () => {
        Promise.all([
            getAssignedTeamsForCompetition(competitionId),
            getUnassignedTeamsForCompetition(competitionId),
        ])
            .then(([assignedTeams, unassignedTeams]) => {
                setAssigned(assignedTeams);
                setUnassigned(unassignedTeams);
                setSelectedTeamId("");
            })
            .catch((err) => setError(err instanceof ApiError ? err.messages.join(" ") : "Something went wrong."));
    };

    useEffect(reload, [competitionId]);

    const addTeam = async () => {
        if (!selectedTeamId) return;

        setError(null);
        try {
            await addTeamToCompetition(competitionId, selectedTeamId);
            reload();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong.");
        }
    };

    const confirmRemove = async () => {
        if (!removing) return;

        setError(null);
        const team = removing;
        setRemoving(null);

        try {
            await removeTeamFromCompetition(team.teamCompetitionID);
            reload();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong.");
        }
    };

    return (
        <>
            <Panel>
                <VStack align="stretch" gap={3}>
                    <Heading size="md">Teams</Heading>

                    <HStack>
                        <NativeSelect.Root size="sm" maxW="300px" disabled={!unassigned || unassigned.length === 0}>
                            <NativeSelect.Field value={selectedTeamId} onChange={(e) => setSelectedTeamId(e.target.value)}>
                                <option value="">-- select a team --</option>
                                {unassigned?.map((t) => <option key={t.teamID} value={t.teamID}>{t.teamName}</option>)}
                            </NativeSelect.Field>
                            <NativeSelect.Indicator />
                        </NativeSelect.Root>
                        <Button size="sm" disabled={!selectedTeamId} onClick={() => { void addTeam(); }}>Add</Button>
                    </HStack>

                    {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                    {assigned === null ? (
                        <Center mt={2}><Spinner size="sm" /></Center>
                    ) : assigned.length === 0 ? (
                        <Text color="fg.muted">No teams assigned yet.</Text>
                    ) : (
                        <Table.Root size="sm" variant="line">
                            <Table.Body>
                                {assigned.map((t) => (
                                    <Table.Row key={t.teamCompetitionID}>
                                        <Table.Cell>{t.teamName}</Table.Cell>
                                        <Table.Cell textAlign="right" width="1">
                                            <IconButton size="xs" variant="ghost" colorPalette="red" aria-label="Remove team" onClick={() => setRemoving(t)}>
                                                <X size={14} />
                                            </IconButton>
                                        </Table.Cell>
                                    </Table.Row>
                                ))}
                            </Table.Body>
                        </Table.Root>
                    )}
                </VStack>
            </Panel>

            <Dialog.Root role="alertdialog" open={removing !== null} onOpenChange={(e) => { if (!e.open) setRemoving(null); }}>
                <Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Content>
                            <Dialog.Header>
                                <Dialog.Title>Remove team</Dialog.Title>
                            </Dialog.Header>
                            <Dialog.Body>
                                <Text>Are you sure you wish to remove {removing?.teamName} from the competition?</Text>
                            </Dialog.Body>
                            <Dialog.Footer>
                                <Button variant="ghost" onClick={() => setRemoving(null)}>Cancel</Button>
                                <Button colorPalette="red" onClick={() => { void confirmRemove(); }}>Remove</Button>
                            </Dialog.Footer>
                        </Dialog.Content>
                    </Dialog.Positioner>
                </Portal>
            </Dialog.Root>
        </>
    );
}

function FixtureImportSection({
    competitionId, externalApiCompetitionCode, onImported,
}: {
    competitionId: string;
    externalApiCompetitionCode: string | null;
    onImported: () => void;
}) {
    const [confirming, setConfirming] = useState(false);
    const [importing, setImporting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [summary, setSummary] = useState<FixtureImportSummary | null>(null);

    const runImport = async () => {
        setConfirming(false);
        setImporting(true);
        setError(null);

        try {
            const result = await importFixtures(competitionId);
            setSummary(result);
            onImported();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setImporting(false);
        }
    };

    const canImport = !!externalApiCompetitionCode;

    return (
        <>
            <Panel>
                <VStack align="stretch" gap={3}>
                    <Heading size="md">Fixture import</Heading>

                    <Text color="fg.muted">
                        {canImport
                            ? "Imports the full season fixture list (and any missing team assignments) from the external data source, and refines the start/end dates above from the actual fixtures. Safe to re-run - already-imported fixtures are skipped."
                            : "Set an external API competition code above first."}
                    </Text>

                    {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                    {summary && (
                        <Text fontSize="sm" color="fg.success">
                            Imported {summary.matchesImported} match{summary.matchesImported === 1 ? "" : "es"} and added{" "}
                            {summary.teamsAdded} team{summary.teamsAdded === 1 ? "" : "s"}. Competition now runs{" "}
                            {formatDateOnly(summary.startDate)} to {formatDateOnly(summary.endDate)}.
                        </Text>
                    )}

                    <HStack justify="flex-end">
                        <Button
                            colorPalette="action" loading={importing}
                            disabled={importing || !canImport}
                            onClick={() => setConfirming(true)}
                        >
                            Import season fixtures
                        </Button>
                    </HStack>
                </VStack>
            </Panel>

            <Dialog.Root role="alertdialog" open={confirming} onOpenChange={(e) => { if (!e.open) setConfirming(false); }}>
                <Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Content>
                            <Dialog.Header>
                                <Dialog.Title>Import season fixtures</Dialog.Title>
                            </Dialog.Header>
                            <Dialog.Body>
                                <Text>
                                    This will fetch the fixture list from the external data source and create any
                                    matches and team assignments that don&apos;t already exist. It won&apos;t
                                    duplicate or remove anything already imported.
                                </Text>
                            </Dialog.Body>
                            <Dialog.Footer>
                                <Button variant="ghost" onClick={() => setConfirming(false)}>Cancel</Button>
                                <Button colorPalette="action" onClick={() => { void runImport(); }}>Import</Button>
                            </Dialog.Footer>
                        </Dialog.Content>
                    </Dialog.Positioner>
                </Portal>
            </Dialog.Root>
        </>
    );
}

function HallOfFameSection({ competitionId }: { competitionId: string }) {
    const { data: status, error, reload } = useAsyncData(() => getHallOfFameGenerationStatus(competitionId), [competitionId]);
    const [confirming, setConfirming] = useState(false);
    const [generating, setGenerating] = useState(false);
    const [generateError, setGenerateError] = useState<string | null>(null);
    const [generated, setGenerated] = useState<HallOfFameItem | null>(null);

    const generate = async () => {
        setConfirming(false);
        setGenerating(true);
        setGenerateError(null);

        try {
            const result = await generateHallOfFame(competitionId);
            setGenerated(result);
            reload();
        } catch (e) {
            setGenerateError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setGenerating(false);
        }
    };

    if (error) {
        return (
            <Panel>
                <Heading size="md" mb={3}>Hall of Fame</Heading>
                <ErrorState error={error} onRetry={reload} />
            </Panel>
        );
    }

    if (status === null) {
        return (
            <Panel>
                <Heading size="md" mb={3}>Hall of Fame</Heading>
                <Center><Spinner size="sm" /></Center>
            </Panel>
        );
    }

    const alreadyGenerated = status.alreadyGenerated || generated !== null;

    return (
        <>
            <Panel>
                <VStack align="stretch" gap={3}>
                    <Heading size="md">Hall of Fame</Heading>

                    {alreadyGenerated ? (
                        <Text color="fg.muted">
                            This competition already has a Hall of Fame entry.{" "}
                            <Link asChild><RouterLink to="/hof">View Hall of Fame</RouterLink></Link>
                        </Text>
                    ) : status.allMatchesPlayed ? (
                        <Text color="fg.muted">
                            All matches have been played. Generating will add 1st, 2nd and 3rd place from the current league table.
                        </Text>
                    ) : (
                        <Text color="fg.muted">Available once every match in this competition has been played.</Text>
                    )}

                    {generateError && <Text fontSize="sm" color="fg.error">{generateError}</Text>}

                    {generated && (
                        <Text fontSize="sm" color="fg.success">
                            Added {generated.winner}, {generated.secondPlace} and {generated.thirdPlace} to the Hall of Fame.
                        </Text>
                    )}

                    <HStack justify="flex-end">
                        <Button
                            colorPalette="action" loading={generating}
                            disabled={generating || alreadyGenerated || !status.allMatchesPlayed}
                            onClick={() => setConfirming(true)}
                        >
                            Generate Hall of Fame entry
                        </Button>
                    </HStack>
                </VStack>
            </Panel>

            <Dialog.Root role="alertdialog" open={confirming} onOpenChange={(e) => { if (!e.open) setConfirming(false); }}>
                <Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Content>
                            <Dialog.Header>
                                <Dialog.Title>Generate Hall of Fame entry</Dialog.Title>
                            </Dialog.Header>
                            <Dialog.Body>
                                <Text>
                                    This will add the top 3 from the current league table to the Hall of Fame for this
                                    competition. This can&apos;t be undone here - it would need to be removed directly
                                    in the database.
                                </Text>
                            </Dialog.Body>
                            <Dialog.Footer>
                                <Button variant="ghost" onClick={() => setConfirming(false)}>Cancel</Button>
                                <Button colorPalette="action" onClick={() => { void generate(); }}>Generate</Button>
                            </Dialog.Footer>
                        </Dialog.Content>
                    </Dialog.Positioner>
                </Portal>
            </Dialog.Root>
        </>
    );
}
