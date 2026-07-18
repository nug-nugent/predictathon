import { useCallback, useEffect, useRef, useState } from "react";
import { Box, Button, HStack, Image, Popover, Portal } from "@chakra-ui/react";
import { SmilePlus } from "lucide-react";
import { Picker } from "emoji-mart";
// @emoji-mart/data's default export (sets/15/native.json) omits the x/y sheet-position fields
// entirely - they only exist in the per-image-set data files. Since the picker below renders
// with set: "twitter" (a spritesheet, not native OS glyphs), it needs the matching twitter.json,
// not the default - undocumented in the README, found by diffing the two JSON files directly.
import emojiData from "@emoji-mart/data/sets/15/twitter.json";
import { useUser } from "../../hooks/useUser";
import { addReaction, removeReaction, type MessageReaction } from "../../services/messageboard-service";
import { getReactionImageUrl } from "../../services/api";
import { CUSTOM_REACTION_CATEGORY } from "../../constants/messageReactions";

type ReactionGroup = { reactionName: string; imageUrl: string; usernames: string[] };

function groupReactions(reactions: MessageReaction[]): ReactionGroup[] {
    const groups = new Map<string, ReactionGroup>();

    for (const reaction of reactions) {
        const existing = groups.get(reaction.reactionName);
        if (existing) {
            existing.usernames.push(reaction.username);
        } else {
            groups.set(reaction.reactionName, {
                reactionName: reaction.reactionName,
                imageUrl: reaction.imageUrl,
                usernames: [reaction.username],
            });
        }
    }

    return [...groups.values()];
}

// The shape of the object emoji-mart's onEmojiSelect passes back (see dist/module.js's internal
// emojiData builder - NOT documented in the README, and NOT what the custom-emoji config shape
// looks like: it's flattened to a top-level `src`, not `skins[0].src`). Standard (Unicode) emoji
// have `unified` (the codepoint filename, e.g. "1f44d" or "1f3f4-e0067-..."); our custom category
// entries don't, and carry their image on `src` directly instead.
type EmojiMartSelection = { name: string; unified?: string; src?: string };

function EmojiPicker({ onSelect }: { onSelect: (reactionName: string, imageUrl: string) => void }) {
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handleEmojiSelect = (emoji: EmojiMartSelection) => {
            const imageUrl = emoji.unified ? getReactionImageUrl(`${emoji.unified}.svg`) : emoji.src;
            if (imageUrl) {
                onSelect(emoji.name, imageUrl);
            }
        };

        // set: "twitter" (rather than the default "native") renders the browsing grid from a
        // twemoji-style spritesheet. getImageURL/spritesheet:false do NOT work for this - the
        // Picker's own grid always renders via a spritesheet internally regardless of that prop
        // (confirmed by reading dist/module.js: the grid's internal Emoji renders always pass
        // spritesheet: true, hardcoded, ignoring the top-level prop entirely). getImageURL only
        // affects the standalone <em-emoji> component, not the Picker widget. So instead of a CDN
        // spritesheet, getSpritesheetURL points at our own self-hosted *exact byte-for-byte copy*
        // of the reference emoji-datasource-twitter@15.0.1 sheet - same file, same coordinates
        // that @emoji-mart/data's x/y metadata already assumes, just not fetched from jsdelivr.
        const picker = new Picker({
            data: emojiData,
            set: "twitter",
            getSpritesheetURL: () => getReactionImageUrl("emoji-sheet-twitter-64.png"),
            custom: CUSTOM_REACTION_CATEGORY,
            onEmojiSelect: handleEmojiSelect,
            previewPosition: "none",
            perLine: 8,
        }) as unknown as HTMLElement;

        const container = containerRef.current;
        container?.appendChild(picker);

        return () => {
            if (container?.contains(picker)) {
                container.removeChild(picker);
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <Box ref={containerRef} />;
}

export function ReactionBar({ messageId, reactions, onChanged }: {
    messageId: string;
    reactions: MessageReaction[];
    onChanged: (reactions: MessageReaction[]) => void;
}) {
    const { user } = useUser();
    const [pickerOpen, setPickerOpen] = useState(false);
    const [busy, setBusy] = useState(false);

    const groups = groupReactions(reactions);

    const toggle = async (reactionName: string, imageUrl: string) => {
        if (busy) return;
        setBusy(true);
        try {
            const hasReacted = groups.some((g) => g.reactionName === reactionName && g.usernames.includes(user?.name ?? ""));
            const updated = hasReacted
                ? await removeReaction(messageId, reactionName)
                : await addReaction(messageId, reactionName, imageUrl);
            onChanged(updated);
        } finally {
            setBusy(false);
        }
    };

    // Picking from the full picker always adds (never removes) - unlike clicking an existing
    // pill, which toggles. Stable across renders (keyed only on messageId) so the emoji-mart
    // Picker instance isn't torn down and rebuilt every time this component re-renders.
    const handlePickerSelect = useCallback(async (reactionName: string, imageUrl: string) => {
        const updated = await addReaction(messageId, reactionName, imageUrl);
        onChanged(updated);
        setPickerOpen(false);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [messageId]);

    return (
        <HStack gap={1} wrap="wrap" mt={1}>
            {groups.map((group) => {
                const mine = group.usernames.includes(user?.name ?? "");
                return (
                    <Button
                        key={group.reactionName}
                        size="2xs"
                        variant={mine ? "subtle" : "outline"}
                        colorPalette={mine ? "blue" : "gray"}
                        rounded="full"
                        px={2}
                        title={group.usernames.join(", ")}
                        onClick={() => toggle(group.reactionName, group.imageUrl)}
                    >
                        <Image src={group.imageUrl} boxSize="14px" alt={group.reactionName} />
                        {group.usernames.length}
                    </Button>
                );
            })}

            <Popover.Root open={pickerOpen} onOpenChange={(e) => setPickerOpen(e.open)}>
                <Popover.Trigger asChild>
                    <Button size="2xs" variant="ghost" rounded="full" px={2} aria-label="Add reaction">
                        <SmilePlus size={14} />
                    </Button>
                </Popover.Trigger>
                <Portal>
                    <Popover.Positioner>
                        <Popover.Content width="auto" p={0} borderWidth={0}>
                            <Popover.Body p={0}>
                                <EmojiPicker onSelect={handlePickerSelect} />
                            </Popover.Body>
                        </Popover.Content>
                    </Popover.Positioner>
                </Portal>
            </Popover.Root>
        </HStack>
    );
}
