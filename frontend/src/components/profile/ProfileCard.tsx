import { useState } from "react";
import { Avatar, Box, Button, Heading, HStack, Text, VStack } from "@chakra-ui/react";
import { Camera } from "lucide-react";
import { Link as RouterLink } from "react-router";
import type { UploadedAvatar, UserProfile } from "../../services/profile-service";
import { useUser } from "../../hooks/useUser";
import { Role } from "../../constants/roles";
import { AvatarUploadDialog } from "./AvatarUploadDialog";
import { AvatarViewerDialog } from "./AvatarViewerDialog";
import { Panel } from "../ui/panel";
import { TrophyCabinet } from "../trophies/TrophyCabinet";

export function ProfileCard({ profile, isOwnProfile }: { profile: UserProfile; isOwnProfile: boolean }) {
    const { user, setUser } = useUser();
    const [avatarUrl, setAvatarUrl] = useState(profile.avatarUrl);
    const [avatarLargeUrl, setAvatarLargeUrl] = useState(profile.avatarLargeUrl);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [viewerOpen, setViewerOpen] = useState(false);
    const canEdit = isOwnProfile || (user?.roles.includes(Role.UserAdministrator) ?? false);

    const hasDetails = profile.caption || profile.profileText || profile.location || profile.favouriteTeam;

    const handleUploaded = (avatar: UploadedAvatar) => {
        // Both URLs carry the server's version stamp, so they point at the new picture rather than
        // whatever the browser has cached under the (unchanged) filename.
        setAvatarUrl(avatar.avatarUrl);
        setAvatarLargeUrl(avatar.avatarLargeUrl);
        if (user) setUser({ ...user, avatarUrl: avatar.avatarUrl });
    };

    const handleRemoved = () => {
        setAvatarUrl(null);
        setAvatarLargeUrl(null);
        setViewerOpen(false);
        if (user) setUser({ ...user, avatarUrl: undefined });
    };

    const avatar = (
        <Avatar.Root size="xl">
            <Avatar.Image src={avatarUrl ?? undefined} />
            <Avatar.Fallback name={profile.username} />
        </Avatar.Root>
    );

    return (
        <Panel accent hoverLift>
            <HStack justify="space-between" mb={3}>
                <Heading size="md">{profile.username}</Heading>
                {canEdit && (
                    <Button asChild size="xs" variant="ghost">
                        <RouterLink to={isOwnProfile ? "/profile/edit" : `/profile/${profile.userID}/edit`}>Edit User</RouterLink>
                    </Button>
                )}
            </HStack>
            <HStack align="start" gap={4}>
                <Box position="relative">
                    {/* Clicking the picture opens it full size - only worth offering when there
                        is one, since the initials fallback has nothing bigger to show. It's a
                        sibling of the change-photo button below, not a wrapper around it, so the
                        two buttons don't nest. */}
                    {avatarLargeUrl ? (
                        <Button
                            aria-label={`View ${profile.username}'s photo full size`}
                            variant="plain"
                            p={0}
                            h="auto"
                            minW="auto"
                            rounded="full"
                            cursor="zoom-in"
                            onClick={() => setViewerOpen(true)}
                        >
                            {avatar}
                        </Button>
                    ) : avatar}
                    {isOwnProfile && (
                        <Button
                            aria-label="Change photo"
                            size="2xs"
                            variant="solid"
                            colorPalette="action"
                            rounded="full"
                            position="absolute"
                            bottom="-2px"
                            right="-2px"
                            p={1}
                            minW="auto"
                            h="auto"
                            onClick={() => setDialogOpen(true)}
                        >
                            <Camera size={12} />
                        </Button>
                    )}
                </Box>
                <VStack align="start" gap={1} flex="1">
                    {profile.caption && (
                        <Text fontStyle="italic">&#8220;{profile.caption}&#8221;</Text>
                    )}
                    {profile.profileText && (
                        <Text fontSize="sm" fontStyle="italic" color="fg.muted">{profile.profileText}</Text>
                    )}
                    {profile.location && (
                        <Text fontSize="sm"><Text as="span" fontWeight="bold">Location: </Text>{profile.location}</Text>
                    )}
                    {profile.favouriteTeam && (
                        <Text fontSize="sm"><Text as="span" fontWeight="bold">Favourite team: </Text>{profile.favouriteTeam}</Text>
                    )}
                    {!hasDetails && (
                        <Text fontSize="sm" fontStyle="italic" color="fg.muted">No profile information available.</Text>
                    )}
                </VStack>
            </HStack>

            <TrophyCabinet trophies={profile.trophies} />

            {avatarLargeUrl && (
                <AvatarViewerDialog
                    open={viewerOpen}
                    onClose={() => setViewerOpen(false)}
                    username={profile.username}
                    imageUrl={avatarLargeUrl}
                />
            )}

            {isOwnProfile && (
                <AvatarUploadDialog
                    open={dialogOpen}
                    onClose={() => setDialogOpen(false)}
                    hasAvatar={avatarUrl !== null}
                    onUploaded={handleUploaded}
                    onRemoved={handleRemoved}
                />
            )}
        </Panel>
    );
}
