import { createContext, useContext } from "react";
import type { User } from "../providers/UserProvider";

type UserContextType = {
    user: User | null;
    // True only while the initial silent-refresh check (on app load) is in flight, so consumers
    // can avoid flashing "logged out" content before that resolves.
    isLoading: boolean;
    setUser: (user: User | null) => void;
};

export const UserContext = createContext<UserContextType>({ user: null, isLoading: false, setUser: () => { } });

export const useUser = () => useContext(UserContext);
