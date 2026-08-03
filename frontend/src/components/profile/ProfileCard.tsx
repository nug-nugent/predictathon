import { useState } from "react";
import { Avatar, Box, Button, Heading, HStack, Text, VStack } from "@chakra-ui/react";
import { Camera } from "lucide-react";
import { Link as RouterLink } from "react-router";
import type { UserProfile } from "../../services/profile-service";
import { useUser } from "../../hooks/useUser";
import { Role } from "../../constants/roles";
import { AvatarUploadDialog } from "./AvatarUploadDialog";
import { Panel } from "../ui/panel";

export function ProfileCard({ profile, isOwnProfile }: { profile: UserProfile; isOwnProfile: boolean }) {
    const { user, setUser } = useUser();
    const [avatarUrl, setAvatarUrl] = useState(profile.avatarUrl);
    const [dialogOpen, setDialogOpen] = useState(false);
    const canEdit = isOwnProfile || (user?.roles.includes(Role.UserAdministrator) ?? false);

    const hasDetails = profile.caption || profile.profileText || profile.location || profile.favouriteTeam;

    const handleUploaded = (newAvatarUrl: string) => {
        // Cache-bust: the filename never changes, so the browser would otherwise keep showing the
        // previous image from cache.
        const busted = `${newAvatarUrl}?v=${Date.now()}`;
        setAvatarUrl(busted);
        if (user) setUser({ ...user, avatarUrl: busted });
    };

    const handleRemoved = () => {
        setAvatarUrl(null);
        if (user) setUser({ ...user, avatarUrl: undefined });
    };

    return (
        <Panel>
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
                    <Avatar.Root size="xl">
                        <Avatar.Image src={avatarUrl ?? undefined} />
                        <Avatar.Fallback name={profile.username} />
                    </Avatar.Root>
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
