import { useState } from "react";
import { Button, HStack, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { Plus } from "lucide-react";
import { getThreads } from "../../../services/messageboard-service";
import { ThreadListItem } from "../../../components/messageboard/ThreadListItem";
import { NewThreadDialog } from "../../../components/messageboard/NewThreadDialog";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function BoardPage() {
    const navigate = useNavigate();
    const [dialogOpen, setDialogOpen] = useState(false);
    const { data: threads, error, reload } = useAsyncData(getThreads, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (threads === null) {
        return <LoadingSpinner />;
    }

    return (
        <VStack align="stretch" gap={2} maxW="container.md" mx="auto">
            <HStack justify="space-between" mb={2}>
                <PageHeading>Messageboard</PageHeading>
                <Button size="sm" colorPalette="blue" onClick={() => setDialogOpen(true)} ml="auto">
                    <Plus size={16} /> New thread
                </Button>
            </HStack>
            {threads.length === 0 ? (
                <Text textAlign="center" color="fg.muted">No threads yet - start the conversation!</Text>
            ) : (
                <Panel>
                    {threads.map((thread) => <ThreadListItem key={thread.messageThreadID} thread={thread} />)}
                </Panel>
            )}
            <NewThreadDialog
                open={dialogOpen}
                onClose={() => setDialogOpen(false)}
                onCreated={(threadId) => { void navigate(`/board/${threadId}`); }}
            />
        </VStack>
    );
}
