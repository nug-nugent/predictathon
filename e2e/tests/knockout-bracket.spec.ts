import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
});

test("the knockout view is offered on the predictions page and draws the whole bracket", async ({ page }) => {
    await page.goto("/predictions");

    // The week picker only renders once the default week's matches have loaded.
    await expect(page.getByRole("combobox")).toBeVisible();

    await page.getByRole("button", { name: "Show Knockout View" }).click();

    // Sample Cup's bracket is a round of 16 through to the final (Scripts/Sample/04_Match.sql), so
    // every tie in the tree is on screen at once rather than a week at a time.
    await expect(page.getByRole("button", { name: "Show Match List" })).toBeVisible();
    await expect(page.locator('button[data-role="quick-predict"]')).toHaveCount(16);

    // The play-off is a knockout match but no part of the tree, so it sits on its own under the
    // final with its own label.
    await expect(page.getByText("3rd place playoff")).toBeVisible();

    // Every round of the tree names itself across the top of the bracket.
    await expect(page.getByText("Round of 16", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Quarter-final", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Final", { exact: true }).first()).toBeVisible();

    // Which view you are on rides in the URL, so a refresh comes back to the same place.
    await expect(page).toHaveURL(/view=knockout/);
    await page.reload();
    await expect(page.getByRole("button", { name: "Show Match List" })).toBeVisible();
});

test("the knockout view goes back to the match list", async ({ page }) => {
    await page.goto("/predictions?view=knockout");

    await page.getByRole("button", { name: "Show Match List" }).click();

    await expect(page.getByRole("combobox")).toBeVisible();
    await expect(page.getByRole("button", { name: "Show Knockout View" })).toBeVisible();
    await expect(page).not.toHaveURL(/view=knockout/);
});

test("an undecided tie names the groups that feed it rather than reading TBC", async ({ page }) => {
    await page.goto("/predictions?view=knockout");

    // With the group stage unfinished, the round of 16 is all placeholders - and the placeholder is
    // the only thing identifying the tie, so it has to be readable at every width.
    const firstTie = page.locator('button[data-role="quick-predict"]').first();
    await expect(firstTie).toContainText("Winner Group A");
    await expect(firstTie).toContainText("Runner-up Group B");
});

test("a tie can be predicted from the bracket itself", async ({ page }) => {
    await page.goto("/predictions?view=knockout");

    // Whichever tie is still unpredicted - the seeded predictions cover most but not all of them,
    // and another spec may have taken one, so don't pin to a particular match.
    const openTie = page.locator('button[data-role="quick-predict"]', { hasText: "Predict" }).first();
    await expect(openTie).toBeVisible();
    await openTie.click();

    // The bracket reuses the same quick-predict popover as the Home page rather than carrying a
    // second score-entry form of its own.
    const popover = page.locator('[data-scope="popover"][data-part="content"][data-state="open"]');
    await expect(popover).toBeVisible();

    const boxes = popover.locator("input");
    await boxes.first().click();
    await boxes.first().pressSequentially("3");
    await boxes.nth(1).pressSequentially("1");

    // The card behind the popover catches up once the save lands.
    await expect(page.locator('button[data-role="quick-predict"]', { hasText: "You: 3 - 1" }).first()).toBeVisible();
});
