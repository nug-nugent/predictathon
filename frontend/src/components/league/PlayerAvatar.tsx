import { Avatar } from "@chakra-ui/react";

// A player's picture for use beside their name in a ranked list (the full league table, the Home
// page's mini table). Avatar.Fallback renders their initial when they haven't uploaded one.
// The avatar is decorative - the username it sits beside already names the row - so it's hidden
// from assistive tech rather than read out as a stray letter.
export function PlayerAvatar({ username, avatarUrl }: { username: string; avatarUrl: string | null }) {
    return (
        <Avatar.Root size="2xs" flexShrink={0} aria-hidden="true">
            <Avatar.Image src={avatarUrl ?? undefined} />
            <Avatar.Fallback name={username} />
        </Avatar.Root>
    );
}
