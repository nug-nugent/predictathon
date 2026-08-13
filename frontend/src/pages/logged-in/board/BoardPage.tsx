import { useState } from "react";
import { Button, HStack, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { Plus } from "lucide-react";
import { getThreads } from "../../../services/messageboard-service";
import { ThreadListItem } from "../../../components/messageboard/ThreadListItem";
import { NewThreadDialog } from "../../../components/messageboard/NewThreadDialog";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";
import { TablePagination } from "../../../components/ui/table-pagination";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

const PAGE_SIZE = 15;

export function BoardPage() {
    const navigate = useNavigate();
    const [dialogOpen, setDialogOpen] = useState(false);
    const [page, setPage] = useState(1);
    const { data, error, reload } = useAsyncData(() => getThreads(page, PAGE_SIZE), [page]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (data === null) {
        return <LoadingSpinner />;
    }

    const threads = data.items;

    return (
        <VStack align="stretch" gap={2} maxW="container.md" mx="auto">
            <HStack justify="space-between" mb={2}>
                <PageHeading>Messageboard</PageHeading>
                <Button size="sm" colorPalette="action" onClick={() => setDialogOpen(true)} ml="auto">
                    <Plus size={16} /> New thread
                </Button>
            </HStack>
            {threads.length === 0 ? (
                <Text textAlign="center" color="fg.muted">No threads yet - start the conversation!</Text>
            ) : (
                <Panel accent>
                    {threads.map((thread, index) => (
                        <ThreadListItem key={thread.messageThreadID} thread={thread} striped={index % 2 === 1} />
                    ))}
                </Panel>
            )}
            <TablePagination count={data.totalCount} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            <NewThreadDialog
                open={dialogOpen}
                onClose={() => setDialogOpen(false)}
                onCreated={(threadId) => { void navigate(`/board/${threadId}`); }}
            />
        </VStack>
    );
}
