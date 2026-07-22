import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

test("league table lists registered competitors with their points columns", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    await page.goto("/league");

    await expect(page.getByRole("columnheader", { name: "POINTS" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "POS" })).toBeVisible();

    // Both seeded accounts are registered in Sample Cup (Scripts/Sample/07_UserCompetition.sql),
    // so DemoPredictor should always have a row here regardless of current standings.
    await expect(page.getByRole("link", { name: DEMO_PREDICTOR.username })).toBeVisible();
});
