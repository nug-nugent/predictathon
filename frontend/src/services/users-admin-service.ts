import { getJsonAuthenticated, postJsonAuthenticated, putJsonAuthenticated } from "./api";

// Matches Application/Models/UserAdminListItem.cs.
export type UserAdmin = {
    id: string;
    userName: string;
    email: string;
    forenames: string | null;
    surname: string | null;
    roles: string[];
    isLockedOut: boolean;
    lastLoginDateTime: string | null;
};

// Matches Application/Models/PagedResult.cs.
export type PagedResult<T> = {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
};

export async function getUsers(page: number, pageSize: number, search: string): Promise<PagedResult<UserAdmin>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search.trim()) {
        params.set("search", search.trim());
    }

    return getJsonAuthenticated<PagedResult<UserAdmin>>(`/User?${params.toString()}`);
}

export async function updateUserRoles(userId: string, roles: string[]): Promise<void> {
    return putJsonAuthenticated<void>(`/User/${userId}/Roles`, { roles });
}

export async function setUserLocked(userId: string, locked: boolean): Promise<void> {
    return putJsonAuthenticated<void>(`/User/${userId}/Lockout`, { locked });
}

export async function adminResetPassword(userId: string): Promise<void> {
    return postJsonAuthenticated<void>(`/Auth/AdminResetPassword/${userId}`, {});
}
