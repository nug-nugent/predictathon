import { useRef, useState } from "react";
import { Box, Button, HStack, Input, Text, Textarea, VStack } from "@chakra-ui/react";
import { Image as ImageIcon, Link2, Video, X } from "lucide-react";
import { postMessage, postMessageWithImage } from "../../services/messageboard-service";
import { ApiError } from "../../services/api";

type AttachmentType = "none" | "file" | "url" | "youtube";

export function MessageComposer({ threadId, onPosted }: { threadId: string; onPosted: () => void }) {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [content, setContent] = useState("");
    const [attachmentType, setAttachmentType] = useState<AttachmentType>("none");
    const [imageFile, setImageFile] = useState<File | null>(null);
    const [imageUrl, setImageUrl] = useState("");
    const [youTubeUrl, setYouTubeUrl] = useState("");
    const [posting, setPosting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const reset = () => {
        setContent("");
        setAttachmentType("none");
        setImageFile(null);
        setImageUrl("");
        setYouTubeUrl("");
        if (fileInputRef.current) fileInputRef.current.value = "";
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
                await postMessageWithImage(threadId, imageFile, content.trim() || null);
            } else {
                await postMessage(
                    threadId,
                    content.trim() || null,
                    attachmentType === "youtube" ? youTubeUrl.trim() : undefined,
                    attachmentType === "url" ? imageUrl.trim() : undefined,
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
            <Textarea
                placeholder="Write a message..."
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

            <HStack justify="space-between">
                <HStack gap={1}>
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
                <Box>
                    <Button size="sm" colorPalette="blue" loading={posting} disabled={!canSubmit} onClick={() => { void handleSubmit(); }}>
                        Post
                    </Button>
                </Box>
            </HStack>
        </VStack>
    );
}
