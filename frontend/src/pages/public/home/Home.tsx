import { lazy, Suspense } from "react";
import { useUser } from "../../../hooks/useUser";
import { LoggedOutLanding } from "./LoggedOutLanding";
import { LoadingSpinner } from "../../../components/ui/async-state";

// The signed-in dashboard is lazy()'d, but LoggedOutLanding above is not. "/" is the entry point
// for every logged-out visitor and is itself no longer code-split (see Routes.tsx), so whatever
// this file imports eagerly ends up in the main bundle - which is exactly where the landing page
// wants to be, and exactly where the dashboard's seven cards do not, since nobody sees them until
// they've logged in.
const Dashboard = lazy(() => import("./Dashboard").then((m) => ({ default: m.Dashboard })));

export function HomePage() {
    const { user, isLoading: userLoading } = useUser();

    if (userLoading) {
        return <LoadingSpinner />;
    }

    if (!user) {
        return <LoggedOutLanding />;
    }

    // Suspense boundary of its own rather than leaning on the one in Routes.tsx, so HomePage also
    // works when rendered directly outside the router (Home.stories.tsx does exactly that).
    return (
        <Suspense fallback={<LoadingSpinner />}>
            <Dashboard />
        </Suspense>
    );
}
