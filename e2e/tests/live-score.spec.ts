import { test, expect } from "@playwright/test";
import { DEMO_ADMIN, login } from "./helpers";

// Its own spec rather than a block inside live.spec.ts, whose beforeEach signs in as a player -
// signing in twice in one test would just leave the first session in place.
// Only DemoAdmin has the MatchAdministrator role (Scripts/Sample/06_UserRoles.sql) that the
// live-score section is gated behind.
test("a match admin can put a live score in, and it shows wherever the match appears", async ({ page }) => {
    await login(page, DEMO_ADMIN.username, DEMO_ADMIN.password);
    await expect(page.getByRole("button", { name: DEMO_ADMIN.username })).toBeVisible();

    await page.goto("/live");
    await expect(page.getByRole("heading", { name: "Update the live score" })).toBeVisible();

    await page.getByRole("textbox", { name: "Home goals" }).fill("4");
    await page.getByRole("textbox", { name: "Away goals" }).fill("2");
    await page.getByRole("button", { name: "Save" }).click();

    await expect(page.getByText("Live score saved.")).toBeVisible();
    await expect(page.getByText("4 - 2").first()).toBeVisible();

    // The same score reaches the Home page's Today's Matches card, which reads it from the match list
    // rather than fetching it separately - so this also covers the stored procedure's new columns.
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Today's Matches" })).toBeVisible();
    await expect(page.getByText("4 - 2").first()).toBeVisible();
});
