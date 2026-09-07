import { defineConfig, devices } from "@playwright/test";

// Points at the Docker dev stack's frontend port by default (see README.md at the repo root).
// Override with PLAYWRIGHT_BASE_URL to run against the native host workflow (http://localhost:5173)
// or a deployed environment instead.
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:5174";

export default defineConfig({
    testDir: "./tests",
    // Files run in parallel; the tests inside one file do not. That matches how the suite is
    // isolated - a spec file is the unit that owns a player account, so two tests in the same file
    // genuinely do share prediction rows, while two files no longer share anything.
    //
    // This used to be fullyParallel with everything on one seeded competition and one player, which
    // failed a different two or three specs on each run depending on who got to the fixtures first
    // (CI's two retries were quietly papering over it). The fix was to remove the sharing rather
    // than to serialise around it: the admin specs enter results against a competition of their own
    // and the prediction specs have a player each. See tests/helpers.ts and Scripts/Sample.
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    reporter: "html",
    use: {
        baseURL,
        screenshot: "only-on-failure",
        trace: "on-first-retry",
    },
    projects: [
        { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    ],
});
