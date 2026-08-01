import { Navigate, Route, Routes } from "react-router";
import { HomePage } from "../pages/public/home/Home";
import { UsersPage } from "../pages/logged-in/admin/users/Users";
import { ProtectedRoute } from "./ProtectedRoute";
import { Role } from "../constants/roles";
import { ThreadPage } from "../pages/logged-in/board/thread/ThreadPage";
import { BoardPage } from "../pages/logged-in/board/BoardPage";
import { SiteLayout } from "../components/site-layout/SiteLayout";
import { LeaguePage } from "../pages/logged-in/league/LeaguePage";
import { PredictionsPage } from "../pages/logged-in/predictions/PredictionsPage";
import { ForgotPasswordPage } from "../pages/public/password-reset/ForgotPasswordPage";
import { ResetPasswordPage } from "../pages/public/password-reset/ResetPasswordPage";
import { ProfilePage } from "../pages/logged-in/profile/ProfilePage";
import { ProfileEditPage } from "../pages/logged-in/profile/ProfileEditPage";
import { RulesPage } from "../pages/logged-in/rules/RulesPage";
import { MatchesAdminPage } from "../pages/logged-in/admin/matches/MatchesAdminPage";
import { ProcessResultsPage } from "../pages/logged-in/admin/process/ProcessResultsPage";
import { FixtureChangesAdminPage } from "../pages/logged-in/admin/fixture-changes/FixtureChangesAdminPage";
import { HallOfFamePage } from "../pages/logged-in/hall-of-fame/HallOfFamePage";
import { CompetitionsAdminPage } from "../pages/logged-in/admin/tournaments/CompetitionsAdminPage";
import { CompetitionEditPage } from "../pages/logged-in/admin/tournaments/CompetitionEditPage";
import { PaymentCreditsAdminPage } from "../pages/logged-in/admin/payment-credits/PaymentCreditsAdminPage";
import { StatisticsPage } from "../pages/logged-in/statistics/StatisticsPage";
import { RegisterPage } from "../pages/public/registration/RegisterPage";
import { CompetitionRegistrationPage } from "../pages/logged-in/registration/CompetitionRegistrationPage";
import { TeamDetailPage } from "../pages/logged-in/team/TeamDetailPage";

export function SiteRoutes() {
    return (
        <Routes>
            <Route path="/" element={<SiteLayout />}>
                {/* Public routes */}
                <Route index element={<HomePage />} />
                <Route path="register" element={<RegisterPage />} />
                <Route path="password-reset" element={<ForgotPasswordPage />} />
                <Route path="password-reset/confirm" element={<ResetPasswordPage />} />

                {/* Protected routes for logged in users */}
                <Route element={<ProtectedRoute />}>
                    <Route path="predictions" element={<PredictionsPage />} />
                    <Route path="league" element={<LeaguePage />} />

                    <Route path="board" element={<BoardPage />} />
                    <Route path="board/:id" element={<ThreadPage />} />

                    <Route path="stats" element={<StatisticsPage />} />
                    <Route path="team/:teamId" element={<TeamDetailPage />} />
                    <Route path="hof" element={<HallOfFamePage />} />
                    <Route path="rules" element={<RulesPage />} />

                    <Route path="profile/edit" element={<ProfileEditPage />} />
                    <Route path="profile/:id/edit" element={<ProfileEditPage />} />
                    <Route path="profile/:id" element={<ProfilePage />} />

                    <Route path="competition/:id/register" element={<CompetitionRegistrationPage />} />
                </Route>

                {/* Protected routes for admin users - each gated behind the specific role it needs */}
                <Route path="admin">
                    <Route element={<ProtectedRoute allowedRoles={[Role.CompetitionAdministrator]} />}>
                        <Route path="tournaments" element={<CompetitionsAdminPage />} />
                        <Route path="tournaments/:id" element={<CompetitionEditPage />} />
                    </Route>
                    <Route element={<ProtectedRoute allowedRoles={[Role.MatchAdministrator]} />}>
                        <Route path="matches" element={<MatchesAdminPage />} />
                        <Route path="process" element={<ProcessResultsPage />} />
                        <Route path="fixture-changes" element={<FixtureChangesAdminPage />} />
                    </Route>
                    <Route element={<ProtectedRoute allowedRoles={[Role.UserAdministrator]} />}>
                        <Route path="users" element={<UsersPage />} />
                        <Route path="payment-credits" element={<PaymentCreditsAdminPage />} />
                    </Route>
                </Route>

                {/* Catch-all redirect back home for unknown paths */}
                <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
        </Routes>
    )
}