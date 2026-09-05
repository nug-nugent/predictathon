import { test, expect, type Locator, type Page } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// Scripts/Sample/04_Match.sql seeds Quarter Final 1 half an hour out, so a freshly seeded stack has
// exactly one match in the Home card's "Coming up" group - the only state in which a row offers
// quick predict. That half-hour runs out, and it's the only thing on the seeded card that can, so
// every test here skips itself rather than failing once the match has kicked off.
test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();
});

/// The first row on the card still open for predictions, or a skip when the seeded fixture that
/// puts one there has kicked off.
async function comingUpRow(page: Page): Promise<Locator> {
    const rows = page.locator('[data-role="quick-predict"]');
    const count = await rows.count();

    test.skip(count === 0, "No match is still open for predictions - the seeded fixture has kicked off.");

    return rows.first();
}

/// Predicts a score that differs from the one already in the boxes, and returns it. Score entry
/// saves on change, so re-entering the prediction that's already there would save nothing and prove
/// nothing - and what's already there is whatever the last run of these tests left behind.
async function predictSomethingNew(popover: Locator): Promise<{ home: string; away: string }> {
    const scores = popover.getByRole("textbox");
    const home = (await scores.first().inputValue()) === "5" ? "6" : "5";
    const away = "0";

    await scores.first().fill(home);
    await scores.nth(1).fill(away);
    await expect(popover.getByText("Prediction saved!")).toBeVisible();

    return { home, away };
}

test("a match still open for predictions is predicted from the home page", async ({ page }) => {
    const row = await comingUpRow(page);
    await row.click();

    const popover = page.getByRole("dialog", { name: "Your Prediction" });
    await expect(popover).toBeVisible();

    // One score box each side, named after the team it belongs to rather than by position - which
    // is also what a screen reader is read out.
    await expect(popover.getByRole("textbox")).toHaveCount(2);

    const { home, away } = await predictSomethingNew(popover);

    // The row behind the popover catches up too, so closing it doesn't leave the card contradicting
    // what was just entered.
    await expect(row).toContainText(`You: ${home} - ${away}`);
});

test("reopening a predicted match shows the prediction to change", async ({ page }) => {
    const popover = page.getByRole("dialog", { name: "Your Prediction" });
    const scores = popover.getByRole("textbox");

    await (await comingUpRow(page)).click();
    const { home, away } = await predictSomethingNew(popover);

    // Reloading rather than just closing the popover: the boxes keep what was typed into them
    // either way, so only a fresh page proves they're filled from the saved prediction.
    await page.reload();
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();

    const row = await comingUpRow(page);
    await row.click();
    await expect(scores.first()).toHaveValue(home);
    await expect(scores.nth(1)).toHaveValue(away);

    // ...and changing it saves again, rather than the first prediction being the only one that counts.
    await scores.first().fill("1");
    await expect(popover.getByText("Prediction saved!")).toBeVisible();
    await expect(row).toContainText(`You: 1 - ${away}`);
});

test("the popover still offers the whole week on the predictions page", async ({ page }) => {
    const row = await comingUpRow(page);
    await row.click();

    const popover = page.getByRole("dialog", { name: "Your Prediction" });
    await popover.getByRole("link", { name: "All Matches This Week" }).click();

    await expect(page).toHaveURL(/\/predictions\?week=/);
    // The week picker - the Predictions page is up, on a week rather than mid-load.
    await expect(page.getByRole("combobox")).toBeVisible();
});
