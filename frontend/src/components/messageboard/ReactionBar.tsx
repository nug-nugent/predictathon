import { useCallback, useEffect, useRef, useState } from "react";
import { Box, Button, HStack, Image, Link as ChakraLink, Popover, Portal, Stack, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
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
    // Who reacted, in the order the server returned them (oldest reaction first).
    users: { userId: string; username: string }[];
};

// Grouped by identity, not display name: the same emoji can reach us under more than one name
// (emoji-mart renames between dataset versions, and until recently the UK flags were pickable both
// as custom entries and as standard emoji), and all of those must collapse into one pill.
function groupReactions(reactions: MessageReaction[]): ReactionGroup[] {
    const groups = new Map<string, ReactionGroup>();

    for (const reaction of reactions) {
        const existing = groups.get(reaction.reactionId);
        if (existing) {
            existing.users.push({ userId: reaction.userID, username: reaction.username });
        } else {
            groups.set(reaction.reactionId, {
                reactionId: reaction.reactionId,
                reactionName: reaction.reactionName,
                imageFile: reaction.imageFile,
                users: [{ userId: reaction.userID, username: reaction.username }],
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
    // At most one pill's "who reacted" popover is open at a time, tracked by reaction identity
    // rather than a flag per pill so opening one closes any other.
    const [openGroupId, setOpenGroupId] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const groups = groupReactions(reactions);

    // Adds or removes the caller's own reaction from an existing pill. The popover deliberately
    // stays open afterwards: the list it is showing is the thing that just changed - and if the
    // pill was only there because of this user, it and its popover disappear anyway.
    const toggle = async (group: ReactionGroup, mine: boolean) => {
        if (busy) {
            return;
        }

        setBusy(true);
        try {
            const updated = mine
                ? await removeReaction(messageId, group.reactionId)
                : await addReaction(messageId, group.reactionId, group.reactionName);
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
                const mine = user !== null && group.users.some((u) => u.userId === user.id);
                // imageFile is empty only when the server can't resolve the identity to a file at
                // all - fall back to the name rather than a broken image.
                const emoji = (size: string) => (group.imageFile
                    ? <Image src={getReactionImageUrl(group.imageFile)} boxSize={size} alt={group.reactionName} />
                    : <Text as="span" fontSize="2xs">{group.reactionName}</Text>);

                return (
                    // lazyMount/unmountOnExit for the same reason as the picker below: one of these
                    // per reaction group per message adds up to dozens of portalled subtrees on a
                    // full page, none of which anyone has asked to see.
                    <Popover.Root
                        key={group.reactionId}
                        open={openGroupId === group.reactionId}
                        onOpenChange={(e) => setOpenGroupId(e.open ? group.reactionId : null)}
                        positioning={{ placement: "bottom-start" }}
                        lazyMount
                        unmountOnExit
                    >
                        <Popover.Trigger asChild>
                            <Button
                                size="xs"
                                // Both states are outlines: a filled pill for "mine" read as a heavy
                                // blue block next to the emoji it was meant to be a backdrop for. The
                                // accent border and count carry the meaning instead.
                                variant="outline"
                                colorPalette={mine ? "action" : "gray"}
                                rounded="full"
                                px={2}
                                aria-label={`${group.reactionName}, ${group.users.length} ${group.users.length === 1 ? "reaction" : "reactions"}. Show who reacted`}
                            >
                                {emoji("18px")}
                                {group.users.length}
                            </Button>
                        </Popover.Trigger>
                        <Portal>
                            <Popover.Positioner>
                                <Popover.Content width="auto" minW="180px" maxW="260px">
                                    <Popover.Arrow />
                                    <Popover.Body p={3}>
                                        <Stack gap={2} align="stretch">
                                            <HStack gap={2} minW={0}>
                                                {emoji("22px")}
                                                <Text fontSize="sm" fontWeight="semibold" truncate>{group.reactionName}</Text>
                                            </HStack>
                                            {/* Capped rather than unbounded: a reaction everyone piles
                                                onto would otherwise run the popover off the screen. */}
                                            <Stack gap={1} align="stretch" maxH="180px" overflowY="auto">
                                                {group.users.map((u) => (
                                                    <ChakraLink key={u.userId} asChild fontSize="sm" variant="underline">
                                                        <RouterLink to={`/profile/${u.userId}`}>
                                                            {u.userId === user?.id ? `${u.username} (you)` : u.username}
                                                        </RouterLink>
                                                    </ChakraLink>
                                                ))}
                                            </Stack>
                                            <Button
                                                size="xs"
                                                variant="outline"
                                                colorPalette={mine ? "gray" : "action"}
                                                loading={busy}
                                                onClick={() => { void toggle(group, mine); }}
                                            >
                                                {mine ? "Remove me" : "Add me"}
                                            </Button>
                                        </Stack>
                                    </Popover.Body>
                                </Popover.Content>
                            </Popover.Positioner>
                        </Portal>
                    </Popover.Root>
                );
            })}

            {/* lazyMount/unmountOnExit are load-bearing, not tidiness: without them Ark mounts every
                message's picker content up front, so a 30-message page builds 30 emoji-mart pickers
                (each parsing the dataset and laying out its grid) for a page where the user usually
                opens none. EmojiPicker's effect appends/detaches the Picker per mount, so it copes
                with the real mount/unmount cycles these introduce. */}
            <Popover.Root open={pickerOpen} onOpenChange={(e) => setPickerOpen(e.open)} lazyMount unmountOnExit>
                <Popover.Trigger asChild>
                    {/* Labelled rather than icon-only: a bare smiley was easy to miss entirely,
                        and on a phone it was a small target to hit. The visible text is the
                        accessible name, so no aria-label. */}
                    <Button size="xs" variant="outline" rounded="full" px={2.5} gap={1}>
                        <SmilePlus size={14} />
                        React
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
