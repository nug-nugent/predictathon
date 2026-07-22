import { test, expect } from "@playwright/test";
import { DEMO_ADMIN, login } from "./helpers";

// Only DemoAdmin has the MatchAdministrator role (Scripts/Sample/06_UserRoles.sql) needed here.
test("an admin can enter a result for a match that's due", async ({ page }) => {
    await login(page, DEMO_ADMIN.username, DEMO_ADMIN.password);
    await expect(page.getByRole("button", { name: DEMO_ADMIN.username })).toBeVisible();

    await page.goto("/admin/process");

    const noneDue = page.getByText("No matches due for results right now.");
    const scoreInputs = page.locator("input:not([readonly])");

    // Whether a match is "due" depends on real time versus the seeded fixture dates - this stays
    // true for the foreseeable future (Sample Cup's Quarter Finals kick off from 2026-07-21), but
    // once this test has processed the one due match, re-running it against the same database
    // (without restarting db-seed, which resets MatchPlayed via its MERGE) will find nothing left.
    await Promise.race([
        noneDue.waitFor(),
        scoreInputs.first().waitFor(),
    ]);

    test.skip(await noneDue.isVisible(), "No match is currently due for a result - re-run `docker compose up` to reseed and reset.");

    await scoreInputs.nth(0).fill("2");
    await scoreInputs.nth(1).fill("1");

    await expect(page.getByText("Saved")).toBeVisible();
});
