import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
});

test("predictions page loads matches for the seeded Sample Cup competition", async ({ page }) => {
    await page.goto("/predictions");

    // The week picker only renders once matches for the default week have loaded.
    await expect(page.getByRole("combobox")).toBeVisible();
});

test("a prediction can be entered for an upcoming match and is saved", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    // Score inputs have no accessible label (MatchRow.tsx), and which specific match is still
    // open for predictions shifts over time - so find whichever rows aren't locked yet, rather
    // than hardcoding a match. Already-started/finished matches render their inputs readOnly.
    //
    // The LAST open match, deliberately not the first: the first is the next fixture to kick off,
    // which is exactly the one the Home card offers for quick predict, and quick-predict.spec.ts
    // drives it in parallel against the same database.
    const open = page.locator('input[data-role="score-input"]:not([readonly])');
    await expect(open.first()).toBeVisible();

    const count = await open.count();
    const homeInput = open.nth(count - 2);
    const awayInput = open.nth(count - 1);

    // MatchRow pre-fills these from any existing saved prediction for this match, so a re-run
    // against a database that already has one (predictions aren't reset by re-seeding, unlike
    // match results) could otherwise "fill" the same value already shown. React won't fire
    // onChange for a native value-set that doesn't actually change anything, so the save would
    // silently never happen - clear first to force a real transition either way.
    await homeInput.fill("");
    await homeInput.fill("2");
    await awayInput.fill("");
    await awayInput.fill("1");

    await expect(page.getByText("Prediction saved!")).toBeVisible();
    await expect(homeInput).toHaveValue("2");
    await expect(awayInput).toHaveValue("1");
});

test("matches are grouped under a kick-off time heading", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    // MatchList bands each run of matches sharing a kick-off under one time heading, so a match's
    // kick-off is only ever on screen as one of these - nothing on the row itself carries it. The
    // regex allows both a 24-hour and an AM/PM rendering, since the heading follows the browser
    // locale like every other time in the app.
    const kickoffHeadings = page.getByText(/^\d{1,2}:\d{2}(\s?[AP]M)?$/);
    await expect(kickoffHeadings.first()).toBeVisible();
});
