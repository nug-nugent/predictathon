import { Navigate, Route, Routes } from "react-router";
import { LoggedInLayout } from "../layout/LoggedInLayout";
import { LoggedOutLayout } from "../layout/LoggedOutLayout";
import { UsersPage } from "../pages/logged-in/admin/users/Users";
import { ThreadPage } from "../pages/logged-in/board/thread/ThreadPage";
import { HomePage } from "../pages/logged-in/home/HomePage";
import { LeaguePage } from "../pages/logged-in/league/LeaguePage";
import { ProfilePage } from "../pages/logged-in/profile/ProfilePage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { RootPage } from "../pages/public/root/RootPage";
import { UserRole } from "../providers/UserProvider";
import { ProtectedRoute } from "./ProtectedRoute";

// prettier-ignore
export function SiteRoutes() {
    return (
        <Routes>
            // Logged out routes
            <Route element={<LoggedOutLayout />}>
                <Route element={<ProtectedRoute loggedOutOnly />}>
                    <Route index element={<RootPage />} />
                    <Route path="/register" element={<PlaceholderPage name="Register" />} />
                    <Route path="password-reset" element={<PlaceholderPage name="Password Reset" />} />
                </Route>
            </Route>

            // Protected routes for users
            <Route element={<ProtectedRoute requiredRoles={[UserRole.User, UserRole.Admin]} />}>
                <Route element={<LoggedInLayout />}>
                    <Route path="/home" element={<HomePage />} />
                    <Route path="predictions" element={<PlaceholderPage name="Predictions" />} />
                    <Route path="league" element={<LeaguePage />} />

                    <Route path="board" element={<PlaceholderPage name="Messageboard" />} />
                    <Route path="board/:id" element={<ThreadPage />} />

                    <Route path="stats" element={<PlaceholderPage name="Statistics" />} />
                    <Route path="hof" element={<PlaceholderPage name="Hall of Fame" />} />
                    <Route path="rules" element={<PlaceholderPage name="Rules" />} />

                    <Route path="profile/edit" element={<PlaceholderPage name="Edit Profile" />} />
                    <Route path="profile/:id" element={<ProfilePage />} />

                    // Protected routes for admin users
                    <Route path="admin" element={<ProtectedRoute requiredRoles={[UserRole.Admin]} />}>
                        <Route path="competitions" element={<PlaceholderPage name="Competitions admin" />} />
                        <Route path="process" element={<PlaceholderPage name="Results Processing" />} />
                        <Route path="users" element={<UsersPage />} />
                    </Route>
                </Route>
            </Route>

            // Catch-all redirect back home for unknown paths
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}
