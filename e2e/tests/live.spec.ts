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

test("a completed match on the home page opens that match's own page", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();

    // The Completed group's rows are the only /match/ hrefs on the page - live rows point at
    // /live/ and coming-up rows open the quick-predict popover instead of linking anywhere.
    const completedRow = page.locator('a[href^="/match/"]').first();
    await expect(completedRow).toBeVisible();

    const href = await completedRow.getAttribute("href");
    await completedRow.click();

    await expect(page).toHaveURL(new RegExp(`${href}$`));
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();
    await expect(page.getByRole("cell", { name: "Average score" })).toBeVisible();
});

test("the list of other live matches shows what each one is worth to you", async ({ page }) => {
    await page.goto("/live");
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();

    // The panel only appears when more than one match is in play, and process-results.spec can take
    // one of the seeded pair out from under this test by confirming its result - so skip rather
    // than fail when the run order has left a single live match.
    const allLive = page.getByRole("heading", { name: "All Live Matches" });
    test.skip(await allLive.isHidden(), "Only one match is in play - re-run `docker compose up` to reseed.");

    // The panel's rows are the only /live/ links on the page - the focused match at the top is the
    // one match line that isn't itself wrapped in a link (see the nested-links test below).
    const rows = page.locator('a[href^="/live/"]');

    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(1);

    // Every row says where you stand on that match, whether or not you predicted it.
    for (const row of await rows.all()) {
        await expect(row.getByText(/^(You: \d+ - \d+|No prediction)$/)).toBeVisible();
    }
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

test("the predictions list reads best-first", async ({ page }) => {
    await page.goto("/live");

    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();

    // The predictions table is the one with a Predictor column - the standings below it and the
    // summary above it both have neither.
    const predictions = page.locator("table").filter({ has: page.getByRole("columnheader", { name: "Predictor" }) });
    const projected = await predictions.locator("tbody tr td:nth-child(3)").allInnerTexts();

    expect(projected.length).toBeGreaterThan(1);

    // "-" is the dash shown for a prediction there's nothing to project - no live score yet, or
    // nobody predicted - and belongs below every real figure rather than sorted among them.
    const ranked = projected.map((text) => (/^\d+$/.test(text.trim()) ? Number(text.trim()) : -1));

    expect(ranked).toEqual([...ranked].sort((a, b) => b - a));
});

test("the predictions list folds away", async ({ page }) => {
    await page.goto("/live");

    // Open to start with - it's the greater part of why the page exists.
    const predictions = page.getByRole("button", { name: "All Predictions" });
    await expect(predictions).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Predictor" })).toBeVisible();

    // ...and out of the way for anyone here for the scoreline alone, without taking the standings
    // below it with it.
    await predictions.click();
    await expect(page.getByRole("columnheader", { name: "Predictor" })).toBeHidden();
    await expect(page.getByRole("columnheader", { name: "In play" })).toBeVisible();

    await predictions.click();
    await expect(page.getByRole("columnheader", { name: "Predictor" })).toBeVisible();
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

test("a team on the focused match links to its team page", async ({ page }) => {
    await page.goto("/live");
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();

    // The focused match at the top is the only thing on this page offering the teams as links of
    // their own - everything below it is a whole-row link to another match.
    const teamLink = page.locator('a[href^="/team/"]').first();
    await expect(teamLink).toBeVisible();

    const href = await teamLink.getAttribute("href");
    // The line renders each team's name at three lengths and shows one per screen width, so read
    // the screen-reader copy: it is the full name the team page's heading uses, at every width.
    const teamName = ((await teamLink.locator('[data-role="team-full-name"]').textContent()) ?? "").trim();

    await teamLink.click();

    await expect(page).toHaveURL(new RegExp(`${href}$`));
    await expect(page.getByRole("heading", { name: teamName })).toBeVisible();
});

test("the live page nests no links inside other links", async ({ page }) => {
    await page.goto("/live");
    await expect(page.getByRole("heading", { name: "All Predictions" })).toBeVisible();

    // The focused match's team links are only safe because that one line isn't itself wrapped in a
    // link, unlike every other match line on the page. An anchor inside an anchor would be invalid
    // markup and an ambiguous target, so guard the distinction rather than trusting it holds.
    expect(await page.locator("a a").count()).toBe(0);
});
