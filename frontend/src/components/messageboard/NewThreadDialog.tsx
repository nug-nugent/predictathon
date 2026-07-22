import { useState } from "react";
import { Button, Dialog, Field, Input, Portal, Textarea, Text } from "@chakra-ui/react";
import { createThread } from "../../services/messageboard-service";
import { ApiError } from "../../services/api";

export function NewThreadDialog({ open, onClose, onCreated }: {
    open: boolean;
    onClose: () => void;
    onCreated: (threadId: string) => void;
}) {
    const [subject, setSubject] = useState("");
    const [content, setContent] = useState("");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const reset = () => {
        setSubject("");
        setContent("");
        setError(null);
    };

    const handleClose = () => {
        reset();
        onClose();
    };

    const handleCreate = async () => {
        setSaving(true);
        setError(null);
        try {
            const thread = await createThread(subject, content);
            reset();
            onCreated(thread.messageThreadID);
        } catch (err) {
            setError(err instanceof ApiError ? err.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        <Dialog.Root open={open} onOpenChange={(e) => { if (!e.open) handleClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>New thread</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <Field.Root mb={3}>
                                <Field.Label>Subject</Field.Label>
                                <Input value={subject} onChange={(e) => setSubject(e.target.value)} maxLength={200} />
                            </Field.Root>
                            <Field.Root>
                                <Field.Label>Message</Field.Label>
                                <Textarea value={content} onChange={(e) => setContent(e.target.value)} rows={5} />
                            </Field.Root>
                            {error && <Text fontSize="sm" color="fg.error" mt={2}>{error}</Text>}
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={saving} onClick={handleClose}>Cancel</Button>
                            <Button
                                colorPalette="blue"
                                loading={saving}
                                disabled={!subject.trim() || !content.trim()}
                                onClick={() => { void handleCreate(); }}
                            >
                                Create thread
                            </Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}
