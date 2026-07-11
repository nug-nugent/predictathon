import { Navigate, Outlet } from "react-router";
import { useUser } from "../providers/UserProvider";
import type { Role } from "../constants/roles";

type ProtectedRouteProps = {
    // Omit to require only that the user is logged in. When provided, the user must hold at
    // least one of the listed roles.
    allowedRoles?: Role[];
};

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
    let { user } = useUser();

    if (!user) {
        return <Navigate to="/" replace />;
    }

    if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
