import { Navigate, Route, Routes } from "react-router";
import { HomePage } from "../pages/public/home/Home";
import { UsersPage } from "../pages/logged-in/admin/users/Users";
import { ProtectedRoute } from "./ProtectedRoute";
import { Role } from "../constants/roles";
import { ThreadPage } from "../pages/logged-in/board/thread/ThreadPage";
import { SiteLayout } from "../components/site-layout/SiteLayout";
import { LeaguePage } from "../pages/logged-in/league/LeaguePage";
import { PredictionsPage } from "../pages/logged-in/predictions/PredictionsPage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { ProfilePage } from "../pages/logged-in/profile/ProfilePage";
import { RulesPage } from "../pages/logged-in/rules/RulesPage";
import { MatchesAdminPage } from "../pages/logged-in/admin/matches/MatchesAdminPage";

export function SiteRoutes() {
    return (
        <Routes>
            <Route path="/" element={<SiteLayout />}>
                // Public routes
                <Route index element={<HomePage />} />
                <Route path="register" element={<PlaceholderPage name="Register" />} />
                <Route path="password-reset" element={<PlaceholderPage name="Password Reset" />} />

                // Protected routes for logged in users
                <Route element={<ProtectedRoute />}>
                    <Route path="predictions" element={<PredictionsPage />} />
                    <Route path="league" element={<LeaguePage />} />

                    <Route path="board" element={<PlaceholderPage name="Messageboard" />} />
                    <Route path="board/:id" element={<ThreadPage />} />

                    <Route path="stats" element={<PlaceholderPage name="Statistics" />} />
                    <Route path="hof" element={<PlaceholderPage name="Hall of Fame" />} />
                    <Route path="rules" element={<RulesPage />} />

                    <Route path="profile/edit" element={<PlaceholderPage name="Edit Profile" />} />
                    <Route path="profile/:id" element={<ProfilePage />} />
                </Route>

                // Protected routes for admin users - each gated behind the specific role it needs
                <Route path="admin">
                    <Route element={<ProtectedRoute allowedRoles={[Role.CompetitionAdministrator]} />}>
                        <Route path="tournaments" element={<PlaceholderPage name="Tournmanets Admin" />} />
                    </Route>
                    <Route element={<ProtectedRoute allowedRoles={[Role.MatchAdministrator]} />}>
                        <Route path="matches" element={<MatchesAdminPage />} />
                        <Route path="process" element={<PlaceholderPage name="Results Processing" />} />
                    </Route>
                    <Route element={<ProtectedRoute allowedRoles={[Role.UserAdministrator]} />}>
                        <Route path="users" element={<UsersPage />} />
                    </Route>
                </Route>

                // Catch-all redirect back home for unknown paths
                <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
        </Routes>
    )
}