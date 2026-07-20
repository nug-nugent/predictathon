import { Navigate, Outlet, useLocation } from "react-router";
import { useUser } from "../hooks/useUser";
import type { Role } from "../constants/roles";

type ProtectedRouteProps = {
    // Omit to require only that the user is logged in. When provided, the user must hold at
    // least one of the listed roles.
    allowedRoles?: Role[];
};

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
    const { user } = useUser();
    const location = useLocation();

    if (!user) {
        // Carry the URL the user was trying to reach, so the login form can return them there
        // instead of dumping them on Home (see LoginForm).
        return <Navigate to="/" replace state={{ from: location.pathname + location.search }} />;
    }

    if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
