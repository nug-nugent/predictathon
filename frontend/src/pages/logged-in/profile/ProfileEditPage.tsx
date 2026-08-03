import { useState } from "react";
import { useParams, Link as RouterLink } from "react-router";
import {
    Avatar, Box, Button, Center, Checkbox, Field, Heading, HStack, Input, Link, Text, Textarea, VStack,
} from "@chakra-ui/react";
import { ArrowLeft, Camera } from "lucide-react";
import { useUser } from "../../../hooks/useUser";
import { Role } from "../../../constants/roles";
import { getUserProfileForEdit, updateProfile, type UserProfileEdit } from "../../../services/profile-service";
import { ApiError } from "../../../services/api";
import { Panel } from "../../../components/ui/panel";
import { AvatarUploadDialog } from "../../../components/profile/AvatarUploadDialog";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

export function ProfileEditPage() {
    const { id: routeId } = useParams<{ id: string }>();
    const { user } = useUser();
    const targetId = routeId ?? user?.id;

    if (!targetId) {
        return null;
    }

    return <ProfileEditLoader key={targetId} targetId={targetId} />;
}

function ProfileEditLoader({ targetId }: { targetId: string }) {
    const { data: profile, error, reload } = useAsyncData(() => getUserProfileForEdit(targetId), [targetId]);

    if (error) {
        // A 403 means this user simply may not edit that profile - retrying won't change that.
        return error.status === 403
            ? (
                <Center mt={4}>
                    <Text>You don't have permission to edit this profile.</Text>
                </Center>
            )
            : <ErrorState error={error} onRetry={reload} />;
    }

    if (profile === null) {
        return <LoadingSpinner />;
    }

    return <ProfileEditForm key={profile.userId} profile={profile} />;
}

function ProfileEditForm({ profile }: { profile: UserProfileEdit }) {
    const { user, setUser } = useUser();
    const canEditAdminFields = user?.roles.includes(Role.UserAdministrator) ?? false;
    const isOwnProfile = user?.id === profile.userId;

    const [form, setForm] = useState<UserProfileEdit>(profile);
    const [saving, setSaving] = useState(false);
    const [saved, setSaved] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [avatarDialogOpen, setAvatarDialogOpen] = useState(false);

    const handleAvatarUploaded = (newAvatarUrl: string) => {
        // Cache-bust: the filename never changes, so the browser would otherwise keep showing the
        // previous image from cache.
        const busted = `${newAvatarUrl}?v=${Date.now()}`;
        if (user) setUser({ ...user, avatarUrl: busted });
    };

    const handleAvatarRemoved = () => {
        if (user) setUser({ ...user, avatarUrl: undefined });
    };

    const update = (patch: Partial<UserProfileEdit>) => {
        setForm((f) => ({ ...f, ...patch }));
        setSaved(false);
    };

    const save = async () => {
        setSaving(true);
        setError(null);

        try {
            const updated = await updateProfile(form.userId, form);
            setForm(updated);
            setSaved(true);
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        <VStack align="stretch" gap={6} maxW="container.md" mx="auto">
            <Link asChild color="fg.muted">
                <RouterLink to={`/profile/${form.userId}`}>
                    <ArrowLeft size={14} /> Back to profile
                </RouterLink>
            </Link>

            <Heading size="lg">Edit profile</Heading>

            <Panel>
                <VStack align="stretch" gap={3}>
                    {isOwnProfile && (
                        <HStack gap={3} pb={1}>
                            <Avatar.Root size="lg">
                                <Avatar.Image src={user?.avatarUrl ?? undefined} />
                                <Avatar.Fallback name={form.userName} />
                            </Avatar.Root>
                            <Button size="sm" variant="outline" onClick={() => setAvatarDialogOpen(true)}>
                                <Box as={Camera} boxSize={4} mr={1} />
                                Change photo
                            </Button>
                        </HStack>
                    )}

                    <HStack align="start">
                        <Field.Root>
                            <Field.Label>Username</Field.Label>
                            <Input size="sm" maxLength={256} value={form.userName} onChange={(e) => update({ userName: e.target.value })} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label>Email</Field.Label>
                            <Input size="sm" type="email" maxLength={256} value={form.email} onChange={(e) => update({ email: e.target.value })} />
                        </Field.Root>
                    </HStack>

                    <HStack align="start">
                        <Field.Root>
                            <Field.Label>First name</Field.Label>
                            <Input size="sm" maxLength={50} value={form.forenames ?? ""} onChange={(e) => update({ forenames: e.target.value || null })} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label>Surname</Field.Label>
                            <Input size="sm" maxLength={50} value={form.surname ?? ""} onChange={(e) => update({ surname: e.target.value || null })} />
                        </Field.Root>
                    </HStack>

                    <HStack align="start">
                        <Field.Root>
                            <Field.Label>Favourite team</Field.Label>
                            <Input size="sm" maxLength={50} value={form.favouriteTeam ?? ""} onChange={(e) => update({ favouriteTeam: e.target.value || null })} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label>Location</Field.Label>
                            <Input size="sm" maxLength={50} value={form.location ?? ""} onChange={(e) => update({ location: e.target.value || null })} />
                        </Field.Root>
                    </HStack>

                    <Field.Root>
                        <Field.Label>Caption</Field.Label>
                        <Input size="sm" maxLength={30} value={form.caption ?? ""} onChange={(e) => update({ caption: e.target.value || null })} />
                    </Field.Root>

                    <Field.Root>
                        <Field.Label>Profile text</Field.Label>
                        <Textarea rows={5} value={form.profileText ?? ""} onChange={(e) => update({ profileText: e.target.value || null })} />
                    </Field.Root>

                    <Field.Root maxW="260px">
                        <Field.Label>Email prediction reminder (days before, blank for none)</Field.Label>
                        <Input
                            size="sm" type="number" min={1}
                            value={form.emailPredictionReminderDays ?? ""}
                            onChange={(e) => update({ emailPredictionReminderDays: e.target.value === "" ? null : Number(e.target.value) })}
                        />
                    </Field.Root>

                    {canEditAdminFields && (
                        <VStack align="stretch" gap={2}>
                            <Checkbox.Root checked={form.canViewMessageboard} onCheckedChange={(e) => update({ canViewMessageboard: !!e.checked })}>
                                <Checkbox.HiddenInput />
                                <Checkbox.Control />
                                <Checkbox.Label>Can view messageboard</Checkbox.Label>
                            </Checkbox.Root>
                            <Checkbox.Root checked={form.canViewHiddenMessageThreads} onCheckedChange={(e) => update({ canViewHiddenMessageThreads: !!e.checked })}>
                                <Checkbox.HiddenInput />
                                <Checkbox.Control />
                                <Checkbox.Label>Can view hidden message threads</Checkbox.Label>
                            </Checkbox.Root>
                        </VStack>
                    )}

                    {error && <Text fontSize="sm" color="fg.error">{error}</Text>}

                    <HStack justify="flex-end">
                        {saved && <Text fontSize="sm" color="fg.success">Your changes have been saved.</Text>}
                        <Button colorPalette="action" loading={saving} disabled={saving} onClick={() => { void save(); }}>Save</Button>
                    </HStack>
                </VStack>
            </Panel>

            {isOwnProfile && (
                <AvatarUploadDialog
                    open={avatarDialogOpen}
                    onClose={() => setAvatarDialogOpen(false)}
                    hasAvatar={!!user?.avatarUrl}
                    onUploaded={handleAvatarUploaded}
                    onRemoved={handleAvatarRemoved}
                />
            )}
        </VStack>
    );
}
