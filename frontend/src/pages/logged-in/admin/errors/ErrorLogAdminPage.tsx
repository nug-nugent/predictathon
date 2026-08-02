import { useState } from "react";
import {
    Badge, Box, Center, Dialog, Portal, Table, Text, VStack,
} from "@chakra-ui/react";
import { getErrors, type ErrorLogEntry } from "../../../../services/error-log-service";
import { Panel } from "../../../../components/ui/panel";
import { PageHeading } from "../../../../components/ui/page-heading";
import { ClickableRow } from "../../../../components/ui/clickable-row";
import { TablePagination } from "../../../../components/ui/table-pagination";
import { useAsyncData } from "../../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../../components/ui/async-state";

const PAGE_SIZE = 20;

function levelColour(level: string | null): string {
    switch (level) {
        case "Fatal":
        case "Error":
            return "red";
        case "Warning":
            return "orange";
        default:
            return "gray";
    }
}

function formatTime(timeStampUtc: string): string {
    return new Date(timeStampUtc).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "medium" });
}

export function ErrorLogAdminPage() {
    const [page, setPage] = useState(1);
    const [viewing, setViewing] = useState<ErrorLogEntry | null>(null);

    const { data, error, reload } = useAsyncData(() => getErrors(page, PAGE_SIZE), [page]);
    const errors = data?.items ?? null;
    const totalCount = data?.totalCount ?? 0;

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Error Log</PageHeading>

            {errors === null ? (
                <LoadingSpinner />
            ) : (
                <Panel overflowX="auto">
                    <Table.Root size="sm" variant="line" striped showColumnBorder>
                        <Table.Header>
                            <Table.Row>
                                <Table.ColumnHeader>Time</Table.ColumnHeader>
                                <Table.ColumnHeader>Level</Table.ColumnHeader>
                                <Table.ColumnHeader>Message</Table.ColumnHeader>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {errors.map((e) => (
                                <ClickableRow key={e.id} onActivate={() => setViewing(e)}>
                                    <Table.Cell whiteSpace="nowrap">{formatTime(e.timeStampUtc)}</Table.Cell>
                                    <Table.Cell>
                                        <Badge colorPalette={levelColour(e.level)} size="sm">{e.level ?? "Unknown"}</Badge>
                                    </Table.Cell>
                                    <Table.Cell maxWidth="600px">
                                        <Text truncate>{e.message}</Text>
                                    </Table.Cell>
                                </ClickableRow>
                            ))}
                        </Table.Body>
                    </Table.Root>

                    {errors.length === 0 && (
                        <Center mt={4}>
                            <Text color="fg.muted">No errors logged. Long may it continue.</Text>
                        </Center>
                    )}

                    <TablePagination count={totalCount} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
                </Panel>
            )}

            {viewing && <ErrorDetailDialog entry={viewing} onClose={() => setViewing(null)} />}
        </VStack>
    );
}

function ErrorDetailDialog({ entry, onClose }: {
    entry: ErrorLogEntry;
    onClose: () => void;
}) {
    return (
        <Dialog.Root open size="xl" onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>
                                <Badge colorPalette={levelColour(entry.level)} mr={2}>{entry.level ?? "Unknown"}</Badge>
                                {formatTime(entry.timeStampUtc)}
                            </Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={4}>
                                <DetailSection label="Message" content={entry.message} />
                                <DetailSection label="Exception" content={entry.exception} />
                                <DetailSection label="Properties" content={entry.properties} />
                            </VStack>
                        </Dialog.Body>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

function DetailSection({ label, content }: { label: string; content: string | null }) {
    if (!content) {
        return null;
    }

    return (
        <Box>
            <Text fontWeight="medium" fontSize="sm" mb={1}>{label}</Text>
            <Box
                as="pre" fontSize="xs" fontFamily="mono" whiteSpace="pre-wrap" wordBreak="break-word"
                p={3} borderRadius="md" bg="bg.muted" maxHeight="320px" overflowY="auto"
            >
                {content}
            </Box>
        </Box>
    );
}
