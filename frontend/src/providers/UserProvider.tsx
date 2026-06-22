import { createContext, useContext, useMemo, useState } from "react";

export const UserRole = {
    User: "User",
    Admin: "Admin"
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export type User = {
    name: string;
    role: UserRole;
    avatarUrl: string;
    currentCompetition: string;
};

type UserContextType = {
    user: User | null;
    setUser: (user: User | null, rememberMe?: boolean) => void;
};

const UserContext = createContext<UserContextType>({
    user: null,
    setUser: () => {}
});

const loadUserFromStorage = (): User | null => {
    const userString = localStorage.getItem("user") || sessionStorage.getItem("user");
    return userString ? JSON.parse(userString) : null;
};

export const UserProvider = ({ mockUser, children }: { mockUser?: User; children: React.ReactNode }) => {
    const [user, _setUser] = useState(() => mockUser || loadUserFromStorage());

    const setUser = (newUser: User | null, rememberMe: boolean = false) => {
        if (newUser) {
            if (rememberMe) {
                localStorage.setItem("user", JSON.stringify(newUser));
            } else {
                sessionStorage.setItem("user", JSON.stringify(newUser));
            }
        } else {
            localStorage.removeItem("user");
            sessionStorage.removeItem("user");
        }

        _setUser(newUser);
    };

    const contextValue = useMemo(
        () => ({
            user,
            setUser
        }),
        [user]
    );

    return <UserContext.Provider value={contextValue}>{children}</UserContext.Provider>;
};

export const useUser = () => useContext(UserContext);
