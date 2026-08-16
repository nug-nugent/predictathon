import { Navigate, Outlet, useLocation } from "react-router";
import { useUser } from "../hooks/useUser";
import { useCompetition } from "../hooks/useCompetition";
import { LoadingSpinner } from "../components/ui/async-state";
import type { Role } from "../constants/roles";

type ProtectedRouteProps = {
    // Omit to require only that the user is logged in. When provided, the user must hold at
    // least one of the listed roles.
    allowedRoles?: Role[];
    // When true, a logged-in user who isn't registered for any competition is redirected to
    // Home (which shows the competition sign-up prompt instead of the dashboard) rather than
    // reaching this route. Admins are exempt, since they may need admin pages to set up the
    // first competition before registering for one themselves.
    requireCompetition?: boolean;
};

export function ProtectedRoute({ allowedRoles, requireCompetition }: ProtectedRouteProps) {
    const { user } = useUser();
    const { competitions, isLoading: competitionsLoading } = useCompetition();
    const location = useLocation();

    if (!user) {
        // Carry the URL the user was trying to reach, so the login form can return them there
        // instead of dumping them on Home (see LoginForm).
        return <Navigate to="/" replace state={{ from: location.pathname + location.search }} />;
    }

    if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
        return <Navigate to="/" replace />;
    }

    if (requireCompetition && user.roles.length === 0) {
        if (competitionsLoading) {
            return <LoadingSpinner />;
        }
        if (competitions.length === 0) {
            return <Navigate to="/" replace />;
        }
    }

    return <Outlet />;
}
