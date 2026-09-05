import { useEffect, useRef, useState } from "react";
import { Box, Button, HStack, Input, Text, Textarea, VStack } from "@chakra-ui/react";
import { CornerUpLeft, Image as ImageIcon, Link2, Video, X } from "lucide-react";
import { postMessage, postMessageWithImage, type Message } from "../../services/messageboard-service";
import { ApiError } from "../../services/api";

type AttachmentType = "none" | "file" | "url" | "youtube";

/// Shows the first line of what is being replied to, so the chip says which message without
/// needing the server's snippet (which only exists once the reply has been posted).
function replyPreview(message: Message): string {
    if (message.messageContent) {
        return message.messageContent.replace(/\s+/g, " ").trim();
    }

    return message.imageUrl ? "Photo" : message.youTubeVideoID ? "YouTube video" : "Message";
}

export function MessageComposer({ threadId, replyTo, onClearReply, onPosted }: {
    threadId: string;
    /// The message being replied to, or null for an ordinary post.
    replyTo: Message | null;
    onClearReply: () => void;
    onPosted: () => void;
}) {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const [content, setContent] = useState("");
    const [attachmentType, setAttachmentType] = useState<AttachmentType>("none");
    const [imageFile, setImageFile] = useState<File | null>(null);
    const [imageUrl, setImageUrl] = useState("");
    const [youTubeUrl, setYouTubeUrl] = useState("");
    const [posting, setPosting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Picking a message to reply to brings the composer to the caller: on a phone the message
    // tapped is usually well above the composer, so without this the Reply button appears to do
    // nothing at all.
    useEffect(() => {
        if (!replyTo) return;
        textareaRef.current?.scrollIntoView({ block: "center", behavior: "smooth" });
        textareaRef.current?.focus();
    }, [replyTo]);

    const reset = () => {
        setContent("");
        setAttachmentType("none");
        setImageFile(null);
        setImageUrl("");
        setYouTubeUrl("");
        if (fileInputRef.current) fileInputRef.current.value = "";
        onClearReply();
    };

    const chooseAttachment = (type: AttachmentType) => {
        setAttachmentType(type === attachmentType ? "none" : type);
        setImageFile(null);
        setImageUrl("");
        setYouTubeUrl("");
        if (type === "file" && type !== attachmentType) {
            fileInputRef.current?.click();
        }
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const selected = e.target.files?.[0];
        if (selected) setImageFile(selected);
    };

    const canSubmit =
        content.trim().length > 0 ||
        (attachmentType === "file" && imageFile) ||
        (attachmentType === "url" && imageUrl.trim()) ||
        (attachmentType === "youtube" && youTubeUrl.trim());

    const handleSubmit = async () => {
        setPosting(true);
        setError(null);
        try {
            if (attachmentType === "file" && imageFile) {
                await postMessageWithImage(threadId, imageFile, content.trim() || null, replyTo?.messageID);
            } else {
                await postMessage(
                    threadId,
                    content.trim() || null,
                    attachmentType === "youtube" ? youTubeUrl.trim() : undefined,
                    attachmentType === "url" ? imageUrl.trim() : undefined,
                    replyTo?.messageID,
                );
            }
            reset();
            onPosted();
        } catch (err) {
            setError(err instanceof ApiError ? err.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setPosting(false);
        }
    };

    return (
        <VStack align="stretch" gap={2} borderTopWidth="1px" pt={3} mt={2}>
            {replyTo && (
                <HStack
                    gap={2}
                    px={2}
                    py={1}
                    rounded="sm"
                    bg="surface.quote"
                    borderLeftWidth="2px"
                    borderColor="border.divider"
                    minW={0}
                >
                    <Box color="fg.muted" flexShrink={0} aria-hidden="true">
                        <CornerUpLeft size={12} />
                    </Box>
                    <Text fontSize="xs" color="fg.muted" truncate minW={0}>
                        Replying to <Text as="span" fontWeight="bold">{replyTo.postedByUsername}</Text> &mdash; {replyPreview(replyTo)}
                    </Text>
                    <Button
                        size="2xs"
                        variant="ghost"
                        ml="auto"
                        flexShrink={0}
                        onClick={onClearReply}
                        aria-label="Stop replying to this message"
                    >
                        <X size={12} />
                    </Button>
                </HStack>
            )}

            <Textarea
                ref={textareaRef}
                placeholder={replyTo ? "Write a reply..." : "Write a message..."}
                value={content}
                onChange={(e) => setContent(e.target.value)}
                rows={3}
            />

            {attachmentType === "file" && imageFile && (
                <HStack fontSize="sm" color="fg.muted">
                    <Text>Attached: {imageFile.name}</Text>
                    <Button size="2xs" variant="ghost" onClick={() => chooseAttachment("file")}><X size={12} /></Button>
                </HStack>
            )}
            {attachmentType === "url" && (
                <Input placeholder="Image URL" value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} size="sm" />
            )}
            {attachmentType === "youtube" && (
                <Input placeholder="YouTube link" value={youTubeUrl} onChange={(e) => setYouTubeUrl(e.target.value)} size="sm" />
            )}

            <input ref={fileInputRef} type="file" accept="image/*" hidden onChange={handleFileChange} />

            {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

            <HStack gap={2} wrap="wrap">
                <HStack gap={1} wrap="wrap">
                    <Button size="xs" variant={attachmentType === "file" ? "solid" : "ghost"} onClick={() => chooseAttachment("file")}>
                        <ImageIcon size={14} /> Photo
                    </Button>
                    <Button size="xs" variant={attachmentType === "url" ? "solid" : "ghost"} onClick={() => chooseAttachment("url")}>
                        <Link2 size={14} /> Image URL
                    </Button>
                    <Button size="xs" variant={attachmentType === "youtube" ? "solid" : "ghost"} onClick={() => chooseAttachment("youtube")}>
                        <Video size={14} /> YouTube
                    </Button>
                </HStack>
                <Button
                    size="sm"
                    colorPalette="action"
                    ml="auto"
                    flexShrink={0}
                    loading={posting}
                    disabled={!canSubmit}
                    onClick={() => { void handleSubmit(); }}
                >
                    Post
                </Button>
            </HStack>
        </VStack>
    );
}
