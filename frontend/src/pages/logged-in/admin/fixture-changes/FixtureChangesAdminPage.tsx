import { useState } from "react";
import { Button, Center, HStack, Table, Text, VStack } from "@chakra-ui/react";
import {
    getPendingFixtureChanges, confirmFixtureChange, dismissFixtureChange, syncFixtureChangesNow,
    type FixtureChangeProposal,
} from "../../../../services/fixture-change-service";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

function formatKickoff(dateTime: string): string {
    return new Date(dateTime).toLocaleString(undefined, {
        weekday: "short", day: "numeric", month: "short", hour: "2-digit", minute: "2-digit",
    });
}

export function FixtureChangesAdminPage() {
    const { data: proposals, error, reload } = useAsyncData(getPendingFixtureChanges, []);
    const [processingId, setProcessingId] = useState<number | null>(null);
    const [syncing, setSyncing] = useState(false);
    const [confirmingAll, setConfirmingAll] = useState(false);
    const [actionError, setActionError] = useState<string | null>(null);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (proposals === null) {
        return <LoadingSpinner />;
    }

    const runAction = async (action: (proposalId: number) => Promise<void>, proposalId: number) => {
        setProcessingId(proposalId);
        setActionError(null);

        try {
            await action(proposalId);
            reload();
        } catch (e) {
            setActionError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setProcessingId(null);
        }
    };

    const runConfirmAll = async () => {
        setConfirmingAll(true);
        setActionError(null);

        let confirmed = 0;

        try {
            for (const p of proposals) {
                await confirmFixtureChange(p.fixtureChangeProposalID);
                confirmed++;
            }
        } catch (e) {
            const message = e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.";

            setActionError(confirmed === 0
                ? message
                : `Confirmed ${confirmed} of ${proposals.length} changes before stopping. ${message}`);
        } finally {
            setConfirmingAll(false);
            reload();
        }
    };

    const runSyncNow = async () => {
        setSyncing(true);
        setActionError(null);

        try {
            await syncFixtureChangesNow();
            reload();
        } catch (e) {
            setActionError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSyncing(false);
        }
    };

    return (
        <VStack align="stretch" gap={4}>
            <HStack justify="space-between">
                <PageHeading>Fixture Changes</PageHeading>
                <HStack>
                    <Button
                        size="sm" variant="outline" loading={syncing} disabled={syncing || confirmingAll}
                        onClick={() => { void runSyncNow(); }}
                    >
                        Check for Changes
                    </Button>
                    {proposals.length > 0 && (
                        <Button
                            size="sm" colorPalette="action" loading={confirmingAll}
                            disabled={confirmingAll || processingId !== null}
                            onClick={() => { void runConfirmAll(); }}
                        >
                            Confirm All ({proposals.length})
                        </Button>
                    )}
                </HStack>
            </HStack>

            <Panel overflowX="auto">
                <VStack align="stretch" gap={3}>
                    <Text color="fg.muted">
                        Reschedules detected against the external fixture data source, awaiting review. Confirming
                        updates the match's kickoff time; dismissing leaves it as-is. Nothing runs in the
                        background - click "Check for changes" to check now.
                    </Text>

                    {actionError && <Text fontSize="sm" color="fg.error">{actionError}</Text>}

                    {proposals.length === 0 ? (
                        <Center mt={2}><Text color="fg.muted">No pending fixture changes.</Text></Center>
                    ) : (
                        <Table.Root size="sm" variant="line" striped showColumnBorder>
                            <Table.Header>
                                <Table.Row>
                                    <Table.ColumnHeader>Match</Table.ColumnHeader>
                                    <Table.ColumnHeader>Previous kickoff</Table.ColumnHeader>
                                    <Table.ColumnHeader>Proposed kickoff</Table.ColumnHeader>
                                    <Table.ColumnHeader textAlign="right">Actions</Table.ColumnHeader>
                                </Table.Row>
                            </Table.Header>
                            <Table.Body>
                                {proposals.map((p: FixtureChangeProposal) => {
                                    const processing = processingId === p.fixtureChangeProposalID;
                                    const busy = processing || confirmingAll;

                                    return (
                                        <Table.Row key={p.fixtureChangeProposalID}>
                                            <Table.Cell>{p.homeTeamName} v {p.awayTeamName}</Table.Cell>
                                            <Table.Cell>{formatKickoff(p.previousMatchDateTime)}</Table.Cell>
                                            <Table.Cell fontWeight="bold">
                                                {formatKickoff(p.proposedMatchDateTime)}
                                            </Table.Cell>
                                            <Table.Cell textAlign="right">
                                                <HStack justify="flex-end">
                                                    <Button
                                                        size="xs" variant="outline" disabled={busy}
                                                        onClick={() => { void runAction(dismissFixtureChange, p.fixtureChangeProposalID); }}
                                                    >
                                                        Dismiss
                                                    </Button>
                                                    <Button
                                                        size="xs" colorPalette="action" loading={processing} disabled={busy}
                                                        onClick={() => { void runAction(confirmFixtureChange, p.fixtureChangeProposalID); }}
                                                    >
                                                        Confirm
                                                    </Button>
                                                </HStack>
                                            </Table.Cell>
                                        </Table.Row>
                                    );
                                })}
                            </Table.Body>
                        </Table.Root>
                    )}
                </VStack>
            </Panel>
        </VStack>
    );
}
