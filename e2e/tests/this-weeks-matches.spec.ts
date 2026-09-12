import { test, expect, type Locator, type Page } from "@playwright/test";
import { DEMO_WEEK_AHEAD, login } from "./helpers";

// DemoWeekAhead is defaulted into Week Ahead Cup (Scripts/Sample/13_WeekAheadCup.sql), whose whole
// point is having nothing on today: that is the only state in which the Home page shows this card
// instead of Today's Matches. The competition's fixtures are whole days either side of today, so
// there is always a week ahead to land on and always a row still open for prediction, whatever
// weekday the stack is brought up on.
//
// Which week the card lands on is not fixed, though. A competition week runs Friday to Thursday, so
// tomorrow's fixtures fall in the current week most days and in the next one when today is a
// Thursday - and the heading says which, so it is matched either way rather than pinned.
const CARD_HEADING = /^(This Week's|Upcoming) Matches$/;

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_WEEK_AHEAD.username, DEMO_WEEK_AHEAD.password);
    await expect(page.getByRole("button", { name: DEMO_WEEK_AHEAD.username })).toBeVisible();
    await expect(page.getByRole("heading", { name: CARD_HEADING })).toBeVisible();
});

/// The first row on the card that is still open for predictions. Unlike quick-predict.spec's
/// equivalent this never has to skip itself: the fixtures behind it are a whole day out rather than
/// pinned to the seeding clock, so they cannot age out during a working session.
function comingUpRow(page: Page): Locator {
    return page.locator('[data-role="quick-predict"]').first();
}

test("the home page leads with the week's matches on a day with nothing on", async ({ page }) => {
    // The two cards are alternatives, not neighbours - this one only earns the slot because the
    // competition isn't playing today.
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toHaveCount(0);

    // Matches are banded by the day they fall on rather than by stage, which is the difference
    // between a week's fixture list and a matchday's. Only the weekday is matched: what follows it
    // is the browser's own date formatting, which orders (and punctuates) the day and month
    // differently from one locale to the next.
    await expect(page.getByText(/^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)[,\s]/).first()).toBeVisible();

    await expect(comingUpRow(page)).toBeVisible();
});

test("a match later in the week is predicted from the home page", async ({ page }) => {
    const row = comingUpRow(page);
    await row.click();

    const popover = page.getByRole("dialog", { name: "Your Prediction" });
    await expect(popover).toBeVisible();

    // A score that differs from whatever is already there - entry saves on change, so re-entering
    // the prediction the last run left behind would save nothing and prove nothing.
    const scores = popover.getByRole("textbox");
    const home = (await scores.first().inputValue()) === "3" ? "4" : "3";

    await scores.first().fill(home);
    await scores.nth(1).fill("1");
    await expect(popover.getByText("Prediction saved!")).toBeVisible();

    // The row behind the popover catches up too, so closing it doesn't leave the card contradicting
    // what was just entered.
    await expect(row).toContainText(`You: ${home} - 1`);
});

test("the card links through to the week it is showing", async ({ page }) => {
    await page.getByRole("link", { name: "View All" }).click();

    // The week it was showing, not whichever one /predictions would otherwise resolve to on its own.
    await expect(page).toHaveURL(/\/predictions\?week=/);
    await expect(page.locator('input[data-role="score-input"]').first()).toBeVisible();
});
