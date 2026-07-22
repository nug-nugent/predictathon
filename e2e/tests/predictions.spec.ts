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
