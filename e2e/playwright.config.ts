import { defineConfig, devices } from "@playwright/test";

// Points at the Docker dev stack's frontend port by default (see README.md at the repo root).
// Override with PLAYWRIGHT_BASE_URL to run against the native host workflow (http://localhost:5173)
// or a deployed environment instead.
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:5174";

export default defineConfig({
    testDir: "./tests",
    fullyParallel: true,
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
