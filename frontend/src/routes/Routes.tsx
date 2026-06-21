import { Navigate, Route, Routes } from "react-router";
import { HomePage } from "../pages/public/home/Home";
import { UsersPage } from "../pages/logged-in/admin/users/Users";
import { ProtectedRoute } from "./ProtectedRoute";
import { UserRole } from "../providers/UserProvider";
import { ThreadPage } from "../pages/logged-in/board/thread/ThreadPage";
import { SiteLayout } from "../components/site-layout/SiteLayout";
import { LeaguePage } from "../pages/logged-in/league/LeaguePage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { ProfilePage } from "../pages/logged-in/profile/ProfilePage";

export function SiteRoutes() {
    return (
        <Routes>
            <Route path="/" element={<SiteLayout />}>
                // Public routes
                <Route index element={<HomePage />} />
                <Route path="register" element={<PlaceholderPage name="Register" />} />
                <Route path="password-reset" element={<PlaceholderPage name="Password Reset" />} />

                // Protected routes for logged in users
                <Route element={<ProtectedRoute allowedRoles={[UserRole.User, UserRole.Admin]} />}>
                    <Route path="predictions" element={<PlaceholderPage name="Predictions" />} />
                    <Route path="league" element={<LeaguePage />} />

                    <Route path="board" element={<PlaceholderPage name="Messageboard" />} />
                    <Route path="board/:id" element={<ThreadPage />} />
                    
                    <Route path="stats" element={<PlaceholderPage name="Statistics" />} />
                    <Route path="hof" element={<PlaceholderPage name="Hall of Fame" />} />
                    <Route path="rules" element={<PlaceholderPage name="Rules" />} />

                    <Route path="profile/edit" element={<PlaceholderPage name="Edit Profile" />} />
                    <Route path="profile/:id" element={<ProfilePage />} />
                </Route>

                // Protected routes for admin users
                <Route path="admin">
                    <Route element={<ProtectedRoute allowedRoles={[UserRole.Admin]} />}>
                        <Route path="tournaments" element={<PlaceholderPage name="Tournmanets Admin" />} />
                        <Route path="process" element={<PlaceholderPage name="Results Processing" />} />
                        <Route path="users" element={<UsersPage />} />
                    </Route>
                </Route>

                // Catch-all redirect back home for unknown paths
                <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
        </Routes>
    )
}