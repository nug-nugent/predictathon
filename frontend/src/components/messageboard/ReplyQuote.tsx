import { Box, chakra, Image, Text } from "@chakra-ui/react";
import { CornerUpLeft } from "lucide-react";
import type { MessageReplyReference } from "../../services/messageboard-service";

/// The quoted stub shown above a reply, identifying the message it answers. Clicking it jumps to
/// that message.
///
/// Rendered as plain text rather than markdown on purpose: the parent's content is arbitrary, and
/// a quoted heading, list or table would wreck the single line this is meant to occupy. The server
/// has already truncated the snippet - the CSS truncation below is the second line of defence, for
/// when the viewport is narrower than the snippet limit assumed.
export function ReplyQuote({ replyTo, onJump }: {
    replyTo: MessageReplyReference;
    onJump: () => void;
}) {
    // What the stub says when the parent carried no text of its own. Sentence case, since this is
    // descriptive text rather than a label.
    const fallbackLabel = replyTo.imageUrl ? "Photo" : replyTo.hasYouTubeVideo ? "YouTube video" : "Message";

    return (
        // chakra.button rather than an HStack with as="button": Chakra v3's `as` doesn't widen the
        // prop types to the element's own attributes, so `type` wouldn't typecheck on the latter.
        <chakra.button
            type="button"
            onClick={onJump}
            // Names the action rather than the quote, per the repo's aria-label convention - and
            // avoids a trailing "..." when the snippet already ends in a full stop.
            aria-label={`Jump to ${replyTo.postedByUsername}'s message: ${replyTo.snippet ?? fallbackLabel}`}
            display="flex"
            alignItems="center"
            w="100%"
            maxW="100%"
            minW={0}
            gap={2}
            px={2}
            py={1}
            mb={1}
            textAlign="left"
            borderLeftWidth="2px"
            borderColor="border.divider"
            rounded="sm"
            bg="surface.quote"
            cursor="pointer"
            _hover={{ bg: "surface.highlightRow" }}
            _focusVisible={{ outline: "2px solid", outlineColor: "input.borderFocus", outlineOffset: "-2px" }}
        >
            <Box color="fg.muted" flexShrink={0} aria-hidden="true">
                <CornerUpLeft size={12} />
            </Box>

            {replyTo.imageUrl && (
                // Decorative here: the stub's accessible name already says what is being replied
                // to, and the thumbnail is a duplicate of an image that is itself in the thread.
                <Image
                    src={replyTo.imageUrl}
                    alt=""
                    boxSize="24px"
                    objectFit="cover"
                    rounded="xs"
                    flexShrink={0}
                />
            )}

            <Text fontSize="xs" fontWeight="bold" color="fg.muted" flexShrink={0} truncate maxW="35%">
                {replyTo.postedByUsername}
            </Text>
            <Text fontSize="xs" color="fg.muted" truncate minW={0} fontStyle={replyTo.snippet ? undefined : "italic"}>
                {replyTo.snippet ?? fallbackLabel}
            </Text>
        </chakra.button>
    );
}
