import { useEffect, useState } from "react";
import { Button, Center, HStack, Spinner, Text, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router";
import { Plus } from "lucide-react";
import { getThreads, type MessageThreadSummary } from "../../../services/messageboard-service";
import { ApiError } from "../../../services/api";
import { ThreadListItem } from "../../../components/messageboard/ThreadListItem";
import { NewThreadDialog } from "../../../components/messageboard/NewThreadDialog";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";

export function BoardPage() {
    const navigate = useNavigate();
    const [threads, setThreads] = useState<MessageThreadSummary[] | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);

    const reload = () => {
        getThreads()
            .then(setThreads)
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

    if (threads === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
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
                onCreated={(threadId) => navigate(`/board/${threadId}`)}
            />
        </VStack>
    );
}
