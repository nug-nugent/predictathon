import { useState } from "react";
import {
    Badge, Box, Button, Center, Checkbox, Dialog, HStack, NativeSelect, Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { useCompetition } from "../../../../hooks/useCompetition";
import {
    getBracketResolution, resolveBracketSlots, setBracketRounds, generateBracketPlaceholders,
    type BracketResolutionSlot, type BracketSlotAssignment, type BracketSetupSummary,
} from "../../../../services/bracket-admin-service";
import { numberBracketByKickOff } from "../../../../services/match-admin-service";
import { getKnockoutBracket } from "../../../../services/prediction-service";
import { getTeamsForCompetition, getGroupStandings, type GroupStandings } from "../../../../services/team-service";
import { ApiError } from "../../../../services/api";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";

export function BracketAdminPage() {
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

    return <BracketAdmin key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

/// A slot's own key, since a slot is a side of a match rather than a row of its own.
function slotKey(slot: BracketResolutionSlot): string {
    return `${slot.matchID}:${slot.isHome ? "home" : "away"}`;
}

function BracketAdmin({ competitionId }: { competitionId: string }) {
    // Which slots the admin has picked, and for a manual slot which team they chose. Ready slots
    // start ticked: the proposal is the answer in the ordinary case, and unticking the odd one is
    // less work than ticking twenty-six.
    const [picked, setPicked] = useState<Map<string, string> | null>(null);
    const [applying, setApplying] = useState(false);
    const [result, setResult] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [showGroups, setShowGroups] = useState(false);
    // Which setup action is in flight, by name, so only its own button shows a spinner.
    const [runningSetup, setRunningSetup] = useState<string | null>(null);
    const [confirmingNumbering, setConfirmingNumbering] = useState(false);

    const { data, error: loadError, reload } = useAsyncData(async () => {
        const [resolution, bracket, teams, groups] = await Promise.all([
            getBracketResolution(competitionId),
            getKnockoutBracket(competitionId),
            getTeamsForCompetition(competitionId),
            getGroupStandings(competitionId),
        ]);
        return { resolution, bracket, teams, groups };
    }, [competitionId]);

    if (loadError) {
        return <ErrorState error={loadError} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const { resolution, bracket, teams, groups } = data;

    // Derived on first render rather than in an effect, so the ticks are right on the first paint
    // and a reload after applying starts from the new proposals rather than the old ticks.
    const selections = picked ?? new Map(
        resolution.rounds
            .flatMap((round) => round.slots)
            .filter((slot) => slot.status === "Ready" && slot.proposedTeamID !== null)
            .map((slot) => [slotKey(slot), slot.proposedTeamID!] as const));

    const setSelection = (slot: BracketResolutionSlot, teamId: string | null) => {
        const next = new Map(selections);
        if (teamId === null) {
            next.delete(slotKey(slot));
        } else {
            next.set(slotKey(slot), teamId);
        }
        setPicked(next);
    };

    const apply = async () => {
        setError(null);
        setResult(null);
        setApplying(true);

        const assignments: BracketSlotAssignment[] = resolution.rounds
            .flatMap((round) => round.slots)
            .filter((slot) => selections.has(slotKey(slot)))
            .map((slot) => ({ matchID: slot.matchID, isHome: slot.isHome, teamID: selections.get(slotKey(slot))! }));

        try {
            const summary = await resolveBracketSlots(competitionId, assignments);
            setResult(`Filled in ${summary.slotsFilled} slot${summary.slotsFilled === 1 ? "" : "s"}.`);
            setPicked(null);
            reload();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong.");
        } finally {
            setApplying(false);
        }
    };

    const hasBracket = resolution.rounds.length > 0;
    const unrounded = resolution.knockoutMatchesWithoutRound;
    // The setup actions have to be reachable before the bracket exists, or they are hidden behind
    // the state they create - which is the trap the Matches page's own gate fell into.
    const hasAnythingKnockout = hasBracket || unrounded > 0;

    /// Runs one setup action, reporting what it changed in the same words for all three.
    const runSetup = async (name: string, action: () => Promise<BracketSetupSummary>, noun: string) => {
        setError(null);
        setResult(null);
        setRunningSetup(name);

        try {
            const summary = await action();
            const parts = [`Set ${summary.changed} ${summary.changed === 1 ? noun : `${noun}s`}`];
            if (summary.leftAlone > 0) {
                parts.push(`left ${summary.leftAlone} already set alone`);
            }
            if (summary.notDerivable > 0) {
                parts.push(`couldn't work out ${summary.notDerivable}`);
            }
            setResult(`${parts.join(", ")}.`);
            setPicked(null);
            reload();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong.");
        } finally {
            setRunningSetup(null);
        }
    };

    const numberBracket = async () => {
        setConfirmingNumbering(false);
        await runSetup("slots", async () => {
            const summary = await numberBracketByKickOff(competitionId);
            // Numbering replaces rather than fills in, so everything it touched counts as changed.
            return { changed: summary.matchesNumbered, leftAlone: 0, notDerivable: 0 };
        }, "draw position");
    };

    return (
        <VStack align="stretch" gap={4}>
            <Dialog.Root role="alertdialog" open={confirmingNumbering} onOpenChange={(e) => { if (!e.open) setConfirmingNumbering(false); }}>
                <Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Content>
                            <Dialog.Header><Dialog.Title>Number Bracket By Kick-off</Dialog.Title></Dialog.Header>
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
                                        Check the bracket afterwards, particularly that each tie is in the half of the
                                        draw it belongs to: slots 1 and 2 of a round feed slot 1 of the next.
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

            <HStack justify="space-between" align="center" wrap="wrap" gap={3}>
                <PageHeading mb={0}>Knockout Bracket</PageHeading>
                {selections.size > 0 && (
                    <Button size="sm" colorPalette="action" loading={applying} disabled={applying}
                        onClick={() => { void apply(); }}>
                        Fill In {selections.size} Slot{selections.size === 1 ? "" : "s"}
                    </Button>
                )}
            </HStack>

            {result && <Text fontSize="sm" color="fg.success">{result}</Text>}
            {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

            {hasAnythingKnockout && (
                <Panel>
                    <VStack align="stretch" gap={2}>
                        <Text fontWeight="bold">Set the bracket up</Text>
                        {/* In the order they want doing: a tie needs a round before it can have a
                            slot, and it needs both before anything can say what feeds it. */}
                        <Text fontSize="sm" color="fg.muted">
                            Rounds first, then the draw positions, then the placeholders that say what feeds each
                            tie. None of them overwrites anything already set.
                        </Text>
                        <HStack wrap="wrap" gap={2} pt={1}>
                            <Button size="sm" variant="outline" loading={runningSetup === "rounds"} disabled={runningSetup !== null}
                                onClick={() => { void runSetup("rounds", () => setBracketRounds(competitionId), "round"); }}>
                                Set Rounds From Descriptions
                            </Button>
                            <Button size="sm" variant="outline" loading={runningSetup === "slots"} disabled={runningSetup !== null}
                                onClick={() => setConfirmingNumbering(true)}>
                                Number Bracket By Kick-off
                            </Button>
                            <Button size="sm" variant="outline" loading={runningSetup === "placeholders"} disabled={runningSetup !== null}
                                onClick={() => { void runSetup("placeholders", () => generateBracketPlaceholders(competitionId), "placeholder"); }}>
                                Generate Placeholders
                            </Button>
                        </HStack>
                        {unrounded > 0 && (
                            <Text fontSize="sm" color="fg.muted" pt={1}>
                                {unrounded} knockout {unrounded === 1 ? "match has" : "matches have"} no round yet, so
                                {unrounded === 1 ? " it is" : " they are"} no part of the bracket below.
                            </Text>
                        )}
                    </VStack>
                </Panel>
            )}

            {!hasBracket ? (
                <Panel>
                    <Text fontSize="sm" color="fg.muted">
                        {unrounded > 0
                            ? "Nothing here has a bracket round yet - start with Set Rounds From Descriptions above."
                            : "No match in this competition is flagged as a knockout match. Flag one on the Matches page and the bracket appears here."}
                    </Text>
                </Panel>
            ) : (
                <>
                    {/* The same check the Predictions page gates its knockout view on, said out loud
                        where the bracket is actually managed. A bracket numbered wrongly still draws
                        as a tree, so the eye is no check on it. */}
                    <Panel>
                        <VStack align="stretch" gap={2}>
                            <Text fontWeight="bold">Bracket health</Text>
                            {bracket.problems.length === 0 ? (
                                <Text fontSize="sm" color="fg.muted">
                                    Complete - the knockout view is on offer to players.
                                </Text>
                            ) : (
                                <>
                                    <Text fontSize="sm" color="fg.muted">
                                        The knockout view stays hidden from players until these are fixed, on the
                                        Matches page.
                                    </Text>
                                    <VStack as="ul" align="stretch" gap={1} pl={4}>
                                        {bracket.problems.map((problem) => (
                                            <Text as="li" key={problem} fontSize="sm">{problem}</Text>
                                        ))}
                                    </VStack>
                                </>
                            )}
                        </VStack>
                    </Panel>

                    {resolution.rounds.map((round) => (
                        <Panel key={round.knockoutRound} overflowX="auto">
                            <VStack align="stretch" gap={2}>
                                <HStack justify="space-between">
                                    <Text fontWeight="bold">{round.roundName}</Text>
                                    <Text fontSize="sm" color="fg.muted">
                                        {round.slots.filter((s) => s.status === "Settled").length} of {round.slots.length} slots filled
                                    </Text>
                                </HStack>
                                <SlotTable
                                    slots={round.slots}
                                    teams={teams}
                                    selections={selections}
                                    onSelect={setSelection}
                                />
                            </VStack>
                        </Panel>
                    ))}

                    {groups.length > 0 && (
                        <Panel>
                            <VStack align="stretch" gap={3}>
                                {/* The evidence for every proposal above. Folded away because it is only
                                    wanted when an admin doubts one - but wanted badly then, since the
                                    orderings come from a tie-break rule they may disagree with. */}
                                <Button size="sm" variant="ghost" alignSelf="flex-start"
                                    onClick={() => setShowGroups(!showGroups)}>
                                    {showGroups ? "Hide" : "Show"} Group Tables ({groups.length})
                                </Button>
                                {showGroups && <GroupTables groups={groups} />}
                            </VStack>
                        </Panel>
                    )}
                </>
            )}
        </VStack>
    );
}

/// One round's slots: two rows per tie, the home side above the away side.
function SlotTable({ slots, teams, selections, onSelect }: {
    slots: BracketResolutionSlot[];
    teams: { teamID: string; teamName: string }[];
    selections: Map<string, string>;
    onSelect: (slot: BracketResolutionSlot, teamId: string | null) => void;
}) {
    return (
        <Table.Root size="sm" variant="line" showColumnBorder>
            <Table.Header>
                <Table.Row>
                    <Table.ColumnHeader>Tie</Table.ColumnHeader>
                    <Table.ColumnHeader>Comes from</Table.ColumnHeader>
                    <Table.ColumnHeader>Team</Table.ColumnHeader>
                    <Table.ColumnHeader>Status</Table.ColumnHeader>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {slots.map((slot, index) => {
                    const key = slotKey(slot);
                    // The tie's name goes on its first row only, so the two sides of one tie read as
                    // a pair rather than as two unrelated rows.
                    const startsTie = index === 0 || slots[index - 1].matchID !== slot.matchID;

                    return (
                        <Table.Row key={key}>
                            <Table.Cell>{startsTie ? slot.matchDescription : ""}</Table.Cell>
                            <Table.Cell color="fg.muted">{slot.placeholder ?? "-"}</Table.Cell>
                            <Table.Cell>
                                <SlotTeamCell slot={slot} teams={teams} selections={selections} onSelect={onSelect} />
                            </Table.Cell>
                            <Table.Cell>
                                <SlotStatusCell slot={slot} />
                            </Table.Cell>
                        </Table.Row>
                    );
                })}
            </Table.Body>
        </Table.Root>
    );
}

/// What is, or could be, in the slot - and the control for saying so where there's a choice.
function SlotTeamCell({ slot, teams, selections, onSelect }: {
    slot: BracketResolutionSlot;
    teams: { teamID: string; teamName: string }[];
    selections: Map<string, string>;
    onSelect: (slot: BracketResolutionSlot, teamId: string | null) => void;
}) {
    const key = slotKey(slot);

    if (slot.status === "Settled") {
        return <Text fontWeight="medium">{slot.currentTeamName}</Text>;
    }

    if (slot.status === "Ready") {
        return (
            <Checkbox.Root
                checked={selections.has(key)}
                onCheckedChange={(e) => onSelect(slot, e.checked ? slot.proposedTeamID : null)}
            >
                <Checkbox.HiddenInput />
                <Checkbox.Control />
                <Checkbox.Label fontWeight="medium">{slot.proposedTeamName}</Checkbox.Label>
            </Checkbox.Root>
        );
    }

    // Waiting slots get no control at all: there is nothing to choose between yet, and offering a
    // team list would invite an admin to guess at a group that hasn't finished.
    if (slot.status === "Waiting") {
        return <Text color="fg.muted">-</Text>;
    }

    // Manual. The Euros' four best third-placed teams land here, and so does any tie that finished
    // level, which is the ordinary way a knockout match ends.
    return (
        <NativeSelect.Root size="sm" maxW="200px">
            <NativeSelect.Field
                value={selections.get(key) ?? ""}
                onChange={(e) => onSelect(slot, e.target.value === "" ? null : e.target.value)}
            >
                <option value="">-- pick a team --</option>
                {teams.map((team) => <option key={team.teamID} value={team.teamID}>{team.teamName}</option>)}
            </NativeSelect.Field>
            <NativeSelect.Indicator />
        </NativeSelect.Root>
    );
}

function SlotStatusCell({ slot }: { slot: BracketResolutionSlot }) {
    if (slot.status === "Settled") {
        return <Badge colorPalette="green" variant="subtle">Filled</Badge>;
    }

    if (slot.status === "Ready") {
        return <Badge colorPalette="blue" variant="subtle">Ready</Badge>;
    }

    return <Text fontSize="sm" color="fg.muted">{slot.reason}</Text>;
}

/// The group tables the proposals are read from, exactly as ordered by the competition's tie-break
/// rule - so a proposal an admin doubts can be checked rather than taken on trust.
function GroupTables({ groups }: { groups: GroupStandings[] }) {
    return (
        <VStack align="stretch" gap={4}>
            {groups.map((group) => (
                <Box key={group.groupName}>
                    <HStack justify="space-between" mb={1}>
                        <Text fontWeight="bold" fontSize="sm">{group.groupName}</Text>
                        <Text fontSize="xs" color="fg.muted">
                            {group.isComplete
                                ? "Complete"
                                : `${group.matchesRemaining} match${group.matchesRemaining === 1 ? "" : "es"} left`}
                        </Text>
                    </HStack>
                    <Table.Root size="sm" variant="line">
                        <Table.Header>
                            <Table.Row>
                                <Table.ColumnHeader>Pos</Table.ColumnHeader>
                                <Table.ColumnHeader>Team</Table.ColumnHeader>
                                <Table.ColumnHeader textAlign="center">P</Table.ColumnHeader>
                                <Table.ColumnHeader textAlign="center">GD</Table.ColumnHeader>
                                <Table.ColumnHeader textAlign="center">Pts</Table.ColumnHeader>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {group.standings.map((standing) => (
                                <Table.Row key={standing.teamID}>
                                    <Table.Cell>{standing.position}</Table.Cell>
                                    <Table.Cell>{standing.teamName}</Table.Cell>
                                    <Table.Cell textAlign="center">{standing.played}</Table.Cell>
                                    <Table.Cell textAlign="center">{standing.goalDifference}</Table.Cell>
                                    <Table.Cell textAlign="center">{standing.points}</Table.Cell>
                                </Table.Row>
                            ))}
                        </Table.Body>
                    </Table.Root>
                </Box>
            ))}
        </VStack>
    );
}
