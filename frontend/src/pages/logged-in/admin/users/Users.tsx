import { useEffect, useState } from "react";
import {
    Badge, Button, ButtonGroup, Center, Checkbox, Dialog, Field, HStack, IconButton, Input,
    Pagination, Portal, Spinner, Table, Text, VStack,
} from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useUser } from "../../../../hooks/useUser";
import { Role } from "../../../../constants/roles";
import {
    getUsers, updateUserRoles, setUserLocked, adminResetPassword, type UserAdmin,
} from "../../../../services/users-admin-service";
import { ApiError } from "../../../../services/api";
import { Panel } from "../../../../components/ui/panel";

const PAGE_SIZE = 20;
const ALL_ROLES = [Role.UserAdministrator, Role.CompetitionAdministrator, Role.MatchAdministrator];

export function UsersPage() {
    const [users, setUsers] = useState<UserAdmin[] | null>(null);
    const [totalCount, setTotalCount] = useState(0);
    const [search, setSearch] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const [page, setPage] = useState(1);
    const [error, setError] = useState<ApiError | null>(null);
    const [editing, setEditing] = useState<UserAdmin | null>(null);

    // Debounce the free-text search so it doesn't refetch on every keystroke.
    useEffect(() => {
        const timeout = setTimeout(() => {
            setDebouncedSearch(search);
            setPage(1);
        }, 300);

        return () => clearTimeout(timeout);
    }, [search]);

    const reload = () => {
        getUsers(page, PAGE_SIZE, debouncedSearch)
            .then((result) => {
                setUsers(result.items);
                setTotalCount(result.totalCount);
            })
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [page, debouncedSearch]);

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={4}>
            <Input
                size="sm" maxWidth="320px" placeholder="Search by username, email, or name"
                value={search} onChange={(e) => setSearch(e.target.value)}
            />

            {users === null ? (
                <Center mt={4}>
                    <Spinner />
                </Center>
            ) : (
                <Panel overflowX="auto">
                    <Table.Root size="sm" variant="line" striped showColumnBorder>
                        <Table.Header>
                            <Table.Row>
                                <Table.ColumnHeader>Username</Table.ColumnHeader>
                                <Table.ColumnHeader>Name</Table.ColumnHeader>
                                <Table.ColumnHeader>Email</Table.ColumnHeader>
                                <Table.ColumnHeader>Roles</Table.ColumnHeader>
                                <Table.ColumnHeader>Status</Table.ColumnHeader>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {users.map((u) => (
                                <Table.Row key={u.id} onClick={() => setEditing(u)} cursor="pointer" _hover={{ bg: "bg.muted" }}>
                                    <Table.Cell>{u.userName}</Table.Cell>
                                    <Table.Cell>{[u.forenames, u.surname].filter(Boolean).join(" ")}</Table.Cell>
                                    <Table.Cell>{u.email}</Table.Cell>
                                    <Table.Cell>
                                        <HStack gap={1} wrap="wrap">
                                            {u.roles.map((r) => <Badge key={r} size="sm">{r}</Badge>)}
                                        </HStack>
                                    </Table.Cell>
                                    <Table.Cell>
                                        {u.isLockedOut
                                            ? <Badge colorPalette="red" size="sm">Locked</Badge>
                                            : <Badge colorPalette="green" size="sm">Active</Badge>}
                                    </Table.Cell>
                                </Table.Row>
                            ))}
                        </Table.Body>
                    </Table.Root>

                    {users.length === 0 && (
                        <Center mt={4}>
                            <Text color="fg.muted">No users found.</Text>
                        </Center>
                    )}

                    {totalCount > PAGE_SIZE && (
                        <Pagination.Root
                            count={totalCount}
                            pageSize={PAGE_SIZE}
                            page={page}
                            onPageChange={(e) => setPage(e.page)}
                        >
                            <ButtonGroup variant="ghost" size="sm" justifyContent="center" mt={2}>
                                <Pagination.PrevTrigger asChild>
                                    <IconButton aria-label="Previous page"><ChevronLeft /></IconButton>
                                </Pagination.PrevTrigger>
                                <Pagination.Items
                                    render={(p) => (
                                        <IconButton variant={{ base: "ghost", _selected: "outline" }} onClick={() => setPage(p.value)}>
                                            {p.value}
                                        </IconButton>
                                    )}
                                />
                                <Pagination.NextTrigger asChild>
                                    <IconButton aria-label="Next page"><ChevronRight /></IconButton>
                                </Pagination.NextTrigger>
                            </ButtonGroup>
                        </Pagination.Root>
                    )}
                </Panel>
            )}

            {editing && (
                <UserEditDialog
                    user={editing}
                    onClose={() => setEditing(null)}
                    onSaved={() => { setEditing(null); reload(); }}
                />
            )}
        </VStack>
    );
}

function UserEditDialog({ user, onClose, onSaved }: {
    user: UserAdmin;
    onClose: () => void;
    onSaved: () => void;
}) {
    const { user: currentUser } = useUser();
    const isSelf = currentUser?.id === user.id;

    const [roles, setRoles] = useState<string[]>(user.roles);
    const [locked, setLocked] = useState(user.isLockedOut);
    const [saving, setSaving] = useState(false);
    const [sendingReset, setSendingReset] = useState(false);
    const [resetSent, setResetSent] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const toggleRole = (role: string, checked: boolean) => {
        setRoles((prev) => (checked ? [...prev, role] : prev.filter((r) => r !== role)));
    };

    const save = async () => {
        setSaving(true);
        setError(null);

        try {
            await updateUserRoles(user.id, roles);
            if (locked !== user.isLockedOut) {
                await setUserLocked(user.id, locked);
            }

            onSaved();
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    const sendResetEmail = async () => {
        setSendingReset(true);
        setError(null);

        try {
            await adminResetPassword(user.id);
            setResetSent(true);
        } catch (e) {
            setError(e instanceof ApiError ? e.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSendingReset(false);
        }
    };

    return (
        <Dialog.Root open onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>{user.userName}</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={4}>
                                <VStack align="stretch" gap={1}>
                                    <Text fontWeight="medium" fontSize="sm">Roles</Text>
                                    {/* Deliberately not sharing one Field.Root across these checkboxes - Chakra's
                                        Field context hands every Checkbox inside it the same generated id, which
                                        breaks each one's own label/input association (clicking any checkbox but
                                        the first ends up toggling the first). Each Checkbox.Root here generates
                                        its own id instead. */}
                                    {ALL_ROLES.map((role) => {
                                        const disableUncheck = isSelf && role === Role.UserAdministrator && roles.includes(role);
                                        return (
                                            <Checkbox.Root
                                                key={role}
                                                checked={roles.includes(role)}
                                                disabled={disableUncheck}
                                                onCheckedChange={(e) => toggleRole(role, !!e.checked)}
                                            >
                                                <Checkbox.HiddenInput />
                                                <Checkbox.Control />
                                                <Checkbox.Label>{role}</Checkbox.Label>
                                            </Checkbox.Root>
                                        );
                                    })}
                                    {isSelf && (
                                        <Text fontSize="xs" color="fg.muted" mt={1}>
                                            You can't remove your own UserAdministrator role.
                                        </Text>
                                    )}
                                </VStack>

                                <Field.Root>
                                    <Field.Label>Account status</Field.Label>
                                    <Checkbox.Root
                                        checked={locked}
                                        disabled={isSelf}
                                        onCheckedChange={(e) => setLocked(!!e.checked)}
                                    >
                                        <Checkbox.HiddenInput />
                                        <Checkbox.Control />
                                        <Checkbox.Label>Account locked</Checkbox.Label>
                                    </Checkbox.Root>
                                    {isSelf && (
                                        <Text fontSize="xs" color="fg.muted" mt={1}>
                                            You can't lock your own account.
                                        </Text>
                                    )}
                                </Field.Root>

                                <Field.Root>
                                    <Field.Label>Password</Field.Label>
                                    <Button
                                        size="sm" variant="outline" alignSelf="flex-start"
                                        loading={sendingReset} disabled={sendingReset || resetSent}
                                        onClick={sendResetEmail}
                                    >
                                        {resetSent ? "Reset email sent" : "Send password reset email"}
                                    </Button>
                                </Field.Root>

                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" disabled={saving} onClick={onClose}>Cancel</Button>
                            <Button colorPalette="blue" loading={saving} disabled={saving} onClick={save}>Save</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}
