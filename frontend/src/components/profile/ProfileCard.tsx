import { Avatar, Box, Button, Heading, HStack, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { UserProfile } from "../../services/profile-service";

export function ProfileCard({ profile, isOwnProfile }: { profile: UserProfile; isOwnProfile: boolean }) {
    const hasDetails = profile.caption || profile.profileText || profile.location || profile.favouriteTeam;

    return (
        <Box borderWidth="1px" rounded="md" p={4}>
            <HStack justify="space-between" mb={3}>
                <Heading size="md">{profile.username}</Heading>
                {isOwnProfile && (
                    <Button asChild size="xs" variant="ghost">
                        <RouterLink to="/profile/edit">Edit User</RouterLink>
                    </Button>
                )}
            </HStack>
            <HStack align="start" gap={4}>
                <Avatar.Root size="xl">
                    <Avatar.Fallback name={profile.username} />
                </Avatar.Root>
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
        </Box>
    );
}
