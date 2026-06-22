import { Navigate, Outlet } from "react-router";
import { useUser, type UserRole } from "../providers/UserProvider";

type ProtectedRouteProps = {
    allowedRoles: UserRole[];
};

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
    const { user } = useUser();

    if (!user || !allowedRoles.includes(user.role)) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
