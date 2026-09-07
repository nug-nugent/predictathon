import { defineConfig, devices } from "@playwright/test";

// Points at the Docker dev stack's frontend port by default (see README.md at the repo root).
// Override with PLAYWRIGHT_BASE_URL to run against the native host workflow (http://localhost:5173)
// or a deployed environment instead.
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:5174";

export default defineConfig({
    testDir: "./tests",
    // One worker, deliberately. Every spec drives the same seeded Sample Cup in the same database and
    // several of them change it - predictions get entered, reactions get added, and process-results
    // confirms a result, which consumes one of the in-play matches that live.spec asserts are in
    // play. Run four-wide those collide, and the suite fails a different two or three specs each time
    // depending on who got there first; CI's two retries were quietly papering over it.
    //
    // The alternative is giving each spec its own competition and users to play with, which is a far
    // bigger change than this suite is worth. Serial costs a few minutes and is honest.
    workers: 1,
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
