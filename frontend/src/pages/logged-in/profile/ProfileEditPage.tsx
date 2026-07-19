import { useEffect, useState } from "react";
import { useParams, Link as RouterLink } from "react-router";
import {
    Button, Center, Checkbox, Field, Heading, HStack, Input, Link, Spinner, Text, Textarea, VStack,
} from "@chakra-ui/react";
import { ArrowLeft } from "lucide-react";
import { useUser } from "../../../hooks/useUser";
import { Role } from "../../../constants/roles";
import { getUserProfileForEdit, updateProfile, type UserProfileEdit } from "../../../services/profile-service";
import { ApiError } from "../../../services/api";
import { Panel } from "../../../components/ui/panel";

export function ProfileEditPage() {
    const { id: routeId } = useParams<{ id: string }>();
    const { user } = useUser();
    const targetId = routeId ?? user?.id;

    const [profile, setProfile] = useState<UserProfileEdit | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    const reload = () => {
        if (!targetId) return;

        setError(null);
        getUserProfileForEdit(targetId)
            .then(setProfile)
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [targetId]);

    if (!targetId) {
        return null;
    }

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>
                        {error.status === 403
                            ? "You don't have permission to edit this profile."
                            : error.messages.join(" ")}
                    </Text>
                    {error.status !== 403 && (
                        <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                    )}
                </VStack>
            </Center>
        );
    }

    if (profile === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return <ProfileEditForm key={profile.userId} profile={profile} />;
}

function ProfileEditForm({ profile }: { profile: UserProfileEdit }) {
    const { user } = useUser();
    const canEditAdminFields = user?.roles.includes(Role.UserAdministrator) ?? false;

    const [form, setForm] = useState<UserProfileEdit>(profile);
    const [saving, setSaving] = useState(false);
    const [saved, setSaved] = useState(false);
    const [error, setError] = useState<string | null>(null);

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
                        <Button colorPalette="blue" loading={saving} disabled={saving} onClick={save}>Save</Button>
                    </HStack>
                </VStack>
            </Panel>
        </VStack>
    );
}
