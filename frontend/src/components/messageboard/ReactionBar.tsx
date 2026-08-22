import { useCallback, useEffect, useRef, useState } from "react";
import { Box, Button, HStack, Image, Popover, Portal, Text } from "@chakra-ui/react";
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
import { getCustomReactionCategory } from "../../constants/messageReactions";

type ReactionGroup = {
    reactionId: string;
    reactionName: string;
    imageFile: string;
    userIds: string[];
    usernames: string[];
};

// Grouped by identity, not display name: the same emoji can reach us under more than one name
// (emoji-mart renames between dataset versions, and until recently the UK flags were pickable both
// as custom entries and as standard emoji), and all of those must collapse into one pill.
function groupReactions(reactions: MessageReaction[]): ReactionGroup[] {
    const groups = new Map<string, ReactionGroup>();

    for (const reaction of reactions) {
        const existing = groups.get(reaction.reactionId);
        if (existing) {
            existing.userIds.push(reaction.userID);
            existing.usernames.push(reaction.username);
        } else {
            groups.set(reaction.reactionId, {
                reactionId: reaction.reactionId,
                reactionName: reaction.reactionName,
                imageFile: reaction.imageFile,
                userIds: [reaction.userID],
                usernames: [reaction.username],
            });
        }
    }

    return [...groups.values()];
}

// The shape of the object emoji-mart's onEmojiSelect passes back (see dist/module.js's internal
// emojiData builder - NOT documented in the README, and NOT what the custom-emoji config shape
// looks like: it's flattened to a top-level `src`, not `skins[0].src`). Standard (Unicode) emoji
// have `unified` (the codepoint sequence, e.g. "1f44d" or "2764-fe0f"); our custom entries don't,
// and carry the namespaced identity we gave them on `id` instead.
type EmojiMartSelection = { id: string; name: string; unified?: string };

// Maps a picker selection onto the identity the server stores. Note that no filename or URL is
// derived here any more: emoji-mart's `unified` and Twemoji's filenames disagree in several
// undocumented ways (the red heart is "2764-fe0f" but ships as "2764.svg", which is what made
// ~220 emoji render as dead images), so that mapping now lives server-side in ReactionCatalogue,
// resolved against the actual files on disk rather than guessed at here.
function toReactionId(emoji: EmojiMartSelection): string {
    return emoji.unified ? `u:${emoji.unified}` : emoji.id;
}

function EmojiPicker({ onSelect }: { onSelect: (reactionId: string, reactionName: string) => void }) {
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        // Captured here rather than read in the cleanup: the picker is appended asynchronously,
        // so the cleanup has to detach it from the same node the append used.
        const container = containerRef.current;
        let picker: HTMLElement | null = null;
        let cancelled = false;

        const handleEmojiSelect = (emoji: EmojiMartSelection) => {
            onSelect(toReactionId(emoji), emoji.name);
        };

        // The custom category comes from the server manifest, so the picker is built once that has
        // arrived. Standard emoji don't wait on anything - that dataset is bundled, not fetched.
        void getCustomReactionCategory().then((custom) => {
            if (cancelled) {
                return;
            }

            // set: "twitter" (rather than the default "native") renders the browsing grid from a
            // twemoji-style spritesheet. getImageURL/spritesheet:false do NOT work for this - the
            // Picker's own grid always renders via a spritesheet internally regardless of that prop
            // (confirmed by reading dist/module.js: the grid's internal Emoji renders always pass
            // spritesheet: true, hardcoded, ignoring the top-level prop entirely). getImageURL only
            // affects the standalone <em-emoji> component, not the Picker widget. So instead of a CDN
            // spritesheet, getSpritesheetURL points at our own self-hosted *exact byte-for-byte copy*
            // of the reference emoji-datasource-twitter@15.0.1 sheet - same file, same coordinates
            // that @emoji-mart/data's x/y metadata already assumes, just not fetched from jsdelivr.
            picker = new Picker({
                data: emojiData,
                set: "twitter",
                getSpritesheetURL: () => getReactionImageUrl("emoji-sheet-twitter-64.png"),
                custom,
                onEmojiSelect: handleEmojiSelect,
                previewPosition: "none",
                perLine: 8,
            }) as unknown as HTMLElement;

            container?.appendChild(picker);
        });

        return () => {
            cancelled = true;
            if (picker !== null && container?.contains(picker)) {
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

    const toggle = async (reactionId: string, reactionName: string) => {
        if (busy) return;
        setBusy(true);
        try {
            const hasReacted = user !== null
                && groups.some((g) => g.reactionId === reactionId && g.userIds.includes(user.id));
            const updated = hasReacted
                ? await removeReaction(messageId, reactionId)
                : await addReaction(messageId, reactionId, reactionName);
            onChanged(updated);
        } catch (error) {
            // Leave the pills as they were - the server state didn't change, so there's nothing
            // to roll back; the user can simply click again.
            console.error("Failed to toggle reaction", error);
        } finally {
            setBusy(false);
        }
    };

    // Picking from the full picker always adds (never removes) - unlike clicking an existing
    // pill, which toggles. Stable across renders (keyed only on messageId) so the emoji-mart
    // Picker instance isn't torn down and rebuilt every time this component re-renders.
    const handlePickerSelect = useCallback(async (reactionId: string, reactionName: string) => {
        try {
            const updated = await addReaction(messageId, reactionId, reactionName);
            onChanged(updated);
        } catch (error) {
            console.error("Failed to add reaction", error);
        } finally {
            setPickerOpen(false);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [messageId]);

    return (
        <HStack gap={1} wrap="wrap" mt={1}>
            {groups.map((group) => {
                const mine = user !== null && group.userIds.includes(user.id);
                return (
                    <Button
                        key={group.reactionId}
                        size="2xs"
                        variant={mine ? "subtle" : "outline"}
                        colorPalette={mine ? "action" : "gray"}
                        rounded="full"
                        px={2}
                        title={group.usernames.join(", ")}
                        onClick={() => { void toggle(group.reactionId, group.reactionName); }}
                    >
                        {/* imageFile is empty only when the server can't resolve the identity to a
                            file at all - fall back to the name rather than a broken image. */}
                        {group.imageFile
                            ? <Image src={getReactionImageUrl(group.imageFile)} boxSize="14px" alt={group.reactionName} />
                            : <Text as="span" fontSize="2xs">{group.reactionName}</Text>}
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
                                <EmojiPicker onSelect={(reactionId, reactionName) => { void handlePickerSelect(reactionId, reactionName); }} />
                            </Popover.Body>
                        </Popover.Content>
                    </Popover.Positioner>
                </Portal>
            </Popover.Root>
        </HStack>
    );
}
