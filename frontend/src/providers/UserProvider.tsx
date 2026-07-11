import { createContext, useContext, useMemo, useState } from "react";
import type { Role } from "../constants/roles";

export type User = {
    name: string;
    roles: Role[];
    avatarUrl?: string;
    currentCompetition?: string;
    // Bearer token for API calls, with its expiry. Optional only so Storybook mocks can omit it.
    token?: string;
    tokenExpiresAtUtc?: string;
};

type UserContextType = {
    user: User | null;
    setUser: (user: User | null, rememberMe?: boolean) => void;
};

const UserContext = createContext<UserContextType>({ user: null, setUser: () => { } });

// Guards against stale/incompatible data left over from a previous version of this app (e.g. the
// shape of User changes) rather than trusting whatever's in storage and crashing on render.
const isValidStoredUser = (value: unknown): value is User =>
    typeof value === "object" && value !== null &&
    typeof (value as User).name === "string" &&
    Array.isArray((value as User).roles);

const loadUserFromStorage = (): User | null => {
    const userString = localStorage.getItem('user') || sessionStorage.getItem('user');
    if (!userString) {
        return null;
    }

    let user: unknown;
    try {
        user = JSON.parse(userString);
    } catch {
        return null;
    }

    if (!isValidStoredUser(user)) {
        return null;
    }

    // Treat a stored user whose token has expired as logged out.
    if (user.tokenExpiresAtUtc && new Date(user.tokenExpiresAtUtc) <= new Date()) {
        return null;
    }

    return user;
}

export const UserProvider = ({ mockUser, children }: { mockUser?: User, children: React.ReactNode }) => {
    const [user, _setUser] = useState(() => mockUser || loadUserFromStorage());

    const setUser = (newUser: User | null, rememberMe: boolean = false) => {
        if (newUser) {
            if (rememberMe) {
                localStorage.setItem('user', JSON.stringify(newUser));
            } else {
                sessionStorage.setItem('user', JSON.stringify(newUser));
            }
        } else {
            localStorage.removeItem('user');
            sessionStorage.removeItem('user');
        }

        _setUser(newUser);
    };

    const contextValue = useMemo(
        () => ({
            user,
            setUser,
        }),
        [user]
    );

    return (
        <UserContext.Provider value={contextValue}>
            {children}
        </UserContext.Provider>
    )
};

export const useUser = () => useContext(UserContext);
