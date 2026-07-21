import type { User } from "../providers/UserProvider";
import { Role } from "../constants/roles";
import { postJson, refreshAccessToken } from "./api";

// Mock users, kept for Storybook stories only - real logins come from the API below.
export const stu: User = {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Stu has a really long username",
    roles: [],
    avatarUrl: "https://www.predictathon.co.uk/Uploads/Images/13473b15-dc61-437c-833a-af2b987b67ef_sm.jpg",
};
export const nug: User = {
    id: "22222222-2222-2222-2222-222222222222",
    name: "Nugsson",
    roles: [Role.MatchAdministrator, Role.UserAdministrator, Role.CompetitionAdministrator],
    avatarUrl: "https://www.predictathon.co.uk/Uploads/Images/da93a123-baae-4ca4-9874-aad53feac685_sm.jpg",
};

type AuthResult = {
    token: string;
    expiresAtUtc: string;
    avatarUrl?: string;
};

// Claim type URIs as they appear in the API's JWT payload (System.Security.Claims.ClaimTypes).
const NAMEID_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

// Decodes a JWT's payload segment (base64url) into an object. This does NOT verify the
// signature - the API does that on every request; here we only read display data from it.
function decodeJwtPayload(token: string): Record<string, unknown> {
    const payload = token.split(".")[1];
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(atob(base64)) as Record<string, unknown>;
}

function extractRoles(claimValue: unknown): Role[] {
    // A single role arrives as a string, multiple roles as an array.
    const values = Array.isArray(claimValue) ? claimValue : claimValue ? [claimValue] : [];
    const knownRoles = Object.values(Role);
    return values.filter((value): value is Role => knownRoles.includes(value as Role));
}

function userFromAuthResult(auth: AuthResult): User {
    const payload = decodeJwtPayload(auth.token);

    return {
        id: typeof payload[NAMEID_CLAIM] === "string" ? payload[NAMEID_CLAIM] : "",
        name: typeof payload[NAME_CLAIM] === "string" ? payload[NAME_CLAIM] : "",
        roles: extractRoles(payload[ROLE_CLAIM]),
        avatarUrl: auth.avatarUrl,
        token: auth.token,
        tokenExpiresAtUtc: auth.expiresAtUtc,
    };
}

export async function loginUser(username: string, password: string, rememberMe: boolean): Promise<User> {
    const auth = await postJson<AuthResult>("/Auth/Login", { userName: username, password, rememberMe });
    const user = userFromAuthResult(auth);

    return user.name ? user : { ...user, name: username };
}

export type RegisterFields = {
    userName: string;
    email: string;
    password: string;
    forenames: string;
    surname: string;
};

/// Creates a new account and logs it straight in (matches Auth/Login's response shape).
export async function registerUser(fields: RegisterFields): Promise<User> {
    const auth = await postJson<AuthResult>("/Auth/Register", { ...fields, rememberMe: true });
    const user = userFromAuthResult(auth);

    return user.name ? user : { ...user, name: fields.userName };
}

/// Silently exchanges the HttpOnly refresh-token cookie (if any) for a fresh access token.
/// Throws (ApiError) if there's no valid session - callers should treat that as logged out.
export async function refreshSession(): Promise<User> {
    const auth = await refreshAccessToken();
    return userFromAuthResult(auth);
}

/// Revokes the current refresh token and clears its cookie server-side.
export async function logoutUser(): Promise<void> {
    await postJson<void>("/Auth/Logout", {});
}

/// Requests a password-reset email. Always resolves, regardless of whether the given
/// username/email matched an account - the API deliberately doesn't reveal that.
export async function forgotPassword(userNameOrEmail: string): Promise<void> {
    await postJson<void>("/Auth/ForgotPassword", { userNameOrEmail });
}

/// Completes a password reset using the userId/token from the link emailed by forgotPassword.
export async function resetPassword(userId: string, token: string, newPassword: string): Promise<void> {
    await postJson<void>("/Auth/ResetPassword", { userId, token, newPassword });
}
