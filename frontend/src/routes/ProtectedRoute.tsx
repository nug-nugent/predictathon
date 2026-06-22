import { Navigate, Outlet } from "react-router";
import { useUser, type UserRole } from "../providers/UserProvider";

type ProtectedRouteProps = {
    requiredRoles?: UserRole[];
    loggedOutOnly?: boolean;
};

export function ProtectedRoute({ requiredRoles, loggedOutOnly = false }: ProtectedRouteProps) {
    const { user } = useUser();

    if (loggedOutOnly) {
        return user ? <Navigate to="/home" replace /> : <Outlet />;
    }

    if (!user) {
        return <Navigate to="/" replace />;
    }

    if (requiredRoles && !requiredRoles.includes(user.role)) {
        return <Navigate to="/home" replace />;
    }

    return <Outlet />;
}
