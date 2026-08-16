import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router";
import { Center, Spinner } from "@chakra-ui/react";
import { ProtectedRoute } from "./ProtectedRoute";
import { Role } from "../constants/roles";
import { SiteLayout } from "../components/site-layout/SiteLayout";

// Every page is code-split via lazy() so the initial bundle only ships the app shell; each
// route's chunk is fetched on first navigation to it. Named exports need the .then() wrapper
// since React.lazy only accepts a module with a `default` export.
const HomePage = lazy(() => import("../pages/public/home/Home").then((m) => ({ default: m.HomePage })));
const RegisterPage = lazy(() => import("../pages/public/registration/RegisterPage").then((m) => ({ default: m.RegisterPage })));
const ForgotPasswordPage = lazy(() => import("../pages/public/password-reset/ForgotPasswordPage").then((m) => ({ default: m.ForgotPasswordPage })));
const ResetPasswordPage = lazy(() => import("../pages/public/password-reset/ResetPasswordPage").then((m) => ({ default: m.ResetPasswordPage })));

const PredictionsPage = lazy(() => import("../pages/logged-in/predictions/PredictionsPage").then((m) => ({ default: m.PredictionsPage })));
const LeaguePage = lazy(() => import("../pages/logged-in/league/LeaguePage").then((m) => ({ default: m.LeaguePage })));
const BoardPage = lazy(() => import("../pages/logged-in/board/BoardPage").then((m) => ({ default: m.BoardPage })));
const ThreadPage = lazy(() => import("../pages/logged-in/board/thread/ThreadPage").then((m) => ({ default: m.ThreadPage })));
const StatisticsPage = lazy(() => import("../pages/logged-in/statistics/StatisticsPage").then((m) => ({ default: m.StatisticsPage })));
const TeamDetailPage = lazy(() => import("../pages/logged-in/team/TeamDetailPage").then((m) => ({ default: m.TeamDetailPage })));
const HallOfFamePage = lazy(() => import("../pages/logged-in/hall-of-fame/HallOfFamePage").then((m) => ({ default: m.HallOfFamePage })));
const RulesPage = lazy(() => import("../pages/logged-in/rules/RulesPage").then((m) => ({ default: m.RulesPage })));
const ProfileEditPage = lazy(() => import("../pages/logged-in/profile/ProfileEditPage").then((m) => ({ default: m.ProfileEditPage })));
const ProfilePage = lazy(() => import("../pages/logged-in/profile/ProfilePage").then((m) => ({ default: m.ProfilePage })));
const CompetitionRegistrationPage = lazy(() => import("../pages/logged-in/registration/CompetitionRegistrationPage").then((m) => ({ default: m.CompetitionRegistrationPage })));

const CompetitionsAdminPage = lazy(() => import("../pages/logged-in/admin/tournaments/CompetitionsAdminPage").then((m) => ({ default: m.CompetitionsAdminPage })));
const CompetitionEditPage = lazy(() => import("../pages/logged-in/admin/tournaments/CompetitionEditPage").then((m) => ({ default: m.CompetitionEditPage })));
const MatchesAdminPage = lazy(() => import("../pages/logged-in/admin/matches/MatchesAdminPage").then((m) => ({ default: m.MatchesAdminPage })));
const ProcessResultsPage = lazy(() => import("../pages/logged-in/admin/process/ProcessResultsPage").then((m) => ({ default: m.ProcessResultsPage })));
const FixtureChangesAdminPage = lazy(() => import("../pages/logged-in/admin/fixture-changes/FixtureChangesAdminPage").then((m) => ({ default: m.FixtureChangesAdminPage })));
const UsersPage = lazy(() => import("../pages/logged-in/admin/users/Users").then((m) => ({ default: m.UsersPage })));
const PaymentCreditsAdminPage = lazy(() => import("../pages/logged-in/admin/payment-credits/PaymentCreditsAdminPage").then((m) => ({ default: m.PaymentCreditsAdminPage })));
const ErrorLogAdminPage = lazy(() => import("../pages/logged-in/admin/errors/ErrorLogAdminPage").then((m) => ({ default: m.ErrorLogAdminPage })));
const AnnouncementsAdminPage = lazy(() => import("../pages/logged-in/admin/announcements/AnnouncementsAdminPage").then((m) => ({ default: m.AnnouncementsAdminPage })));

function RouteFallback() {
    return (
        <Center minH="50vh">
            <Spinner size="xl" />
        </Center>
    );
}

export function SiteRoutes() {
    return (
        <Suspense fallback={<RouteFallback />}>
            <Routes>
                <Route path="/" element={<SiteLayout />}>
                    {/* Public routes */}
                    <Route index element={<HomePage />} />
                    <Route path="register" element={<RegisterPage />} />
                    <Route path="password-reset" element={<ForgotPasswordPage />} />
                    <Route path="password-reset/confirm" element={<ResetPasswordPage />} />

                    {/* Protected routes for logged in users - requires being registered for a
                        competition, since these all assume one exists (see ProtectedRoute). */}
                    <Route element={<ProtectedRoute requireCompetition />}>
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
                    </Route>

                    {/* Registering for a specific competition is how a zero-competition user
                        escapes the sign-up prompt, so it only requires being logged in. */}
                    <Route element={<ProtectedRoute />}>
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
                            <Route path="errors" element={<ErrorLogAdminPage />} />
                            <Route path="announcements" element={<AnnouncementsAdminPage />} />
                        </Route>
                    </Route>

                    {/* Catch-all redirect back home for unknown paths */}
                    <Route path="*" element={<Navigate to="/" replace />} />
                </Route>
            </Routes>
        </Suspense>
    )
}
