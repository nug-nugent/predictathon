import { createBrowserRouter, Navigate, type RouteObject } from "react-router";
import { LoggedInLayout } from "../layout/LoggedInLayout";
import { LoggedOutLayout } from "../layout/LoggedOutLayout";
import { UsersPage } from "../pages/logged-in/admin/users/Users";
import { ThreadPage } from "../pages/logged-in/board/thread/ThreadPage";
import { HomePage } from "../pages/logged-in/home/HomePage";
import { LeaguePage } from "../pages/logged-in/league/LeaguePage";
import { PredictPage } from "../pages/logged-in/predict/PredictPage";
import { ProfilePage } from "../pages/logged-in/profile/ProfilePage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { RootPage } from "../pages/public/root/RootPage";
import { UserRole } from "../providers/UserProvider";
import { ProtectedRoute } from "./ProtectedRoute";

const routes: RouteObject[] = [
    // Logged out routes
    {
        element: <LoggedOutLayout />,
        children: [
            {
                element: <ProtectedRoute loggedOutOnly />,
                children: [
                    { index: true, element: <RootPage /> },
                    { path: "/register", element: <PlaceholderPage name="Register" /> },
                    { path: "password-reset", element: <PlaceholderPage name="Password Reset" /> }
                ]
            }
        ]
    },

    // Protected routes for users
    {
        element: <ProtectedRoute requiredRoles={[UserRole.User, UserRole.Admin]} />,
        children: [
            {
                element: <LoggedInLayout />,
                children: [
                    { path: "/home", element: <HomePage /> },
                    { path: "predict", element: <PredictPage /> },
                    { path: "league", element: <LeaguePage /> },
                    {
                        path: "board",
                        children: [
                            { index: true, element: <PlaceholderPage name="Messageboard" /> },
                            { path: ":id", element: <ThreadPage /> }
                        ]
                    },
                    { path: "stats", element: <PlaceholderPage name="Statistics" /> },
                    { path: "hof", element: <PlaceholderPage name="Hall of Fame" /> },
                    { path: "rules", element: <PlaceholderPage name="Rules" /> },
                    { path: "profile/edit", element: <PlaceholderPage name="Edit Profile" /> },
                    { path: "profile/:id", element: <ProfilePage /> },

                    // Protected routes for admin users
                    {
                        path: "admin",
                        element: <ProtectedRoute requiredRoles={[UserRole.Admin]} />,
                        children: [
                            { index: true, element: <Navigate to="/" replace /> },
                            { path: "competitions", element: <PlaceholderPage name="Competitions admin" /> },
                            { path: "process", element: <PlaceholderPage name="Results Processing" /> },
                            { path: "users", element: <UsersPage /> }
                        ]
                    }
                ]
            }
        ]
    },

    // Catch-all redirect back home for unknown paths
    { path: "*", element: <Navigate to="/" replace /> }
];

export const router = createBrowserRouter(routes);
