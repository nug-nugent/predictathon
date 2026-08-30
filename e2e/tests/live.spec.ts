import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// Scripts/Sample/04_Match.sql pins three of Sample Cup's fixtures to today's clock at seed time:
// two quarter-finals that kicked off in the last couple of hours with no result yet, and a Round of
// 16 match played earlier today. So a freshly seeded stack always has something in the Live and
// Completed groups. "Coming up" holds Quarter Final 1, seeded 30 minutes out - it moves into Live
// once that half-hour is up, so this suite deliberately doesn't depend on it still being there.
test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
});

test("home page shows today's matches grouped into live and completed", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();

    await expect(page.getByText("LIVE", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Completed", { exact: true })).toBeVisible();
});

test("a live match on the home page opens the live page focused on it", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();

    // Rows are whole-row links; the live ones are the only /live/ hrefs on the page.
    const liveRow = page.locator('a[href^="/live/"]').first();
    await expect(liveRow).toBeVisible();

    const href = await liveRow.getAttribute("href");
    await liveRow.click();

    await expect(page).toHaveURL(new RegExp(`${href}$`));

    // Predictions are only served from two minutes before kick-off, which every match in the Live
    // group is past - so the full list renders rather than the API's "not yet" refusal.
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Predictor" })).toBeVisible();

    // Deliberately nothing about the "All Live Matches" panel: it only appears when more than one
    // match is in play, and process-results.spec can take one of the seeded pair out from under
    // this test by confirming its result. Asserting on it here would pass or fail on test order.
});

test("the card's corner link opens the live page", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();

    await page.getByRole("link", { name: "Live page" }).click();

    await expect(page).toHaveURL(/\/live$/);
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();
});

test("the live page picks a match in play when the url names none", async ({ page }) => {
    await page.goto("/live");

    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();
    await expect(page.getByText("LIVE", { exact: true }).first()).toBeVisible();
});

test("the live score section is hidden from players", async ({ page }) => {
    await page.goto("/live");

    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Update the Live Score" })).toHaveCount(0);
});

test("the live league table shows what each player is gaining, and folds away", async ({ page }) => {
    await page.goto("/live");

    const table = page.getByRole("button", { name: "Live League Table" });
    await expect(table).toBeVisible();

    // Open to start with - while a match is on, this is what people are here to watch.
    await expect(page.getByRole("columnheader", { name: "In play" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Points" })).toBeVisible();

    // Scoped to the standings: the predictions table above lists the same names, so an unscoped
    // match would find either and prove neither.
    const standings = page.locator("table").filter({ has: page.getByRole("columnheader", { name: "In play" }) });

    // Every registered player has a row whether or not they predicted anything in play, so the
    // seeded demo accounts are always in here.
    await expect(standings.getByRole("cell", { name: "DemoPredictor" })).toBeVisible();

    // ...and folds away again for anyone who only wants the match in front of them.
    await table.click();
    await expect(page.getByRole("columnheader", { name: "In play" })).toBeHidden();
});
