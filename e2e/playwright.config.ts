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
    // Both raised from their defaults (30s and 5s) because four Chromium instances driving a
    // Dockerised API and dev server on one laptop is genuinely slow sometimes, and the assertions
    // were impatient rather than wrong: a run would fail one spec, a different one each time, always
    // on a timeout and never on a wrong value. At two workers the same suite is clean, which is what
    // load looks like rather than a test fault. Serialising would cost more than the wait does, and
    // hard-coding a worker count guesses at a machine we do not own.
    //
    // Generous, deliberately: nothing here is measuring how quick anything is, so a slow pass is a
    // pass. What these must not do is hide a genuine hang, which is what the ceiling is still for.
    timeout: 45_000,
    expect: { timeout: 10_000 },
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
