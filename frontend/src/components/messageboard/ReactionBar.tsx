import { useState } from "react";
import { Box, Button, HStack, Image, Popover, Portal, SimpleGrid } from "@chakra-ui/react";
import { SmilePlus } from "lucide-react";
import { useUser } from "../../hooks/useUser";
import { addReaction, removeReaction, type MessageReaction } from "../../services/messageboard-service";
import { MESSAGE_REACTIONS } from "../../constants/messageReactions";

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

    return (
        <HStack gap={1} wrap="wrap" mt={1}>
            {groups.map((group) => {
                const mine = group.usernames.includes(user?.name ?? "");
                return (
                    <Button
                        key={group.reactionName}
                        size="2xs"
                        variant={mine ? "solid" : "outline"}
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
                        <Popover.Content width="220px">
                            <Popover.Body>
                                <SimpleGrid columns={4} gap={2}>
                                    {MESSAGE_REACTIONS.map((option) => (
                                        <Box
                                            key={option.name}
                                            as="button"
                                            title={option.name}
                                            onClick={() => { toggle(option.name, option.imageUrl); setPickerOpen(false); }}
                                            _hover={{ bg: "bg.muted" }}
                                            rounded="md"
                                            p={1}
                                        >
                                            <Image src={option.imageUrl} boxSize="24px" mx="auto" alt={option.name} />
                                        </Box>
                                    ))}
                                </SimpleGrid>
                            </Popover.Body>
                        </Popover.Content>
                    </Popover.Positioner>
                </Portal>
            </Popover.Root>
        </HStack>
    );
}
