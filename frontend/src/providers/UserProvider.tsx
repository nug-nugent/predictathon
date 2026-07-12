import { useEffect, useMemo, useState } from "react";
import type { Role } from "../constants/roles";
import { refreshSession } from "../services/user-service";
import { setAccessToken, setSessionExpiredHandler } from "../services/api";
import { UserContext } from "../hooks/useUser";

export type User = {
    name: string;
    roles: Role[];
    avatarUrl?: string;
    // Access token for API calls, held in memory only - never persisted to Web Storage. A page
    // reload always starts from null and relies on refreshSession() (backed by the HttpOnly
    // refresh-token cookie) to silently restore the session.
    token?: string;
    tokenExpiresAtUtc?: string;
};

export const UserProvider = ({ mockUser, children }: { mockUser?: User | null, children: React.ReactNode }) => {
    const [user, setUser] = useState<User | null>(mockUser ?? null);
    // mockUser is only ever undefined when this isn't Storybook/a test (see .storybook/preview.tsx,
    // which always passes a concrete value, even explicit null for "logged out").
    const [isLoading, setIsLoading] = useState(mockUser === undefined);

    useEffect(() => {
        if (mockUser !== undefined) {
            return;
        }

        // One-time cleanup: earlier versions of this app persisted the user/token to Web Storage.
        // That's no longer how sessions work - clear any leftovers so they don't linger unused.
        localStorage.removeItem('user');
        sessionStorage.removeItem('user');

        refreshSession()
            .then(setUser)
            .catch(() => setUser(null))
            .finally(() => setIsLoading(false));
        // Deliberately runs once on mount only.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Keeps api.ts's module-level token store in sync so plain (non-React) service functions can
    // attach the current access token to authenticated requests.
    useEffect(() => {
        setAccessToken(user?.token ?? null);
    }, [user]);

    useEffect(() => {
        // If a background token refresh fails (e.g. the refresh cookie expired or was revoked),
        // log the user out so the UI reflects reality instead of holding a dead session.
        setSessionExpiredHandler(() => setUser(null));
        return () => setSessionExpiredHandler(null);
    }, []);

    const contextValue = useMemo(
        () => ({
            user,
            isLoading,
            setUser,
        }),
        [user, isLoading]
    );

    return (
        <UserContext.Provider value={contextValue}>
            {children}
        </UserContext.Provider>
    )
};
