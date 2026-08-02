import { test, expect } from "@playwright/test";
import { DEMO_ADMIN, login } from "./helpers";

// Only DemoAdmin has the UserAdministrator role (Scripts/Sample/06_UserRoles.sql) needed here.
test("an admin can view the error log page", async ({ page }) => {
    await login(page, DEMO_ADMIN.username, DEMO_ADMIN.password);
    await expect(page.getByRole("button", { name: DEMO_ADMIN.username })).toBeVisible();

    await page.goto("/admin/errors");

    // The seeded database starts with an empty dbo.ErrorLog, but a previous run of the stack may
    // have logged real warnings/errors into it - either the empty state or at least one table row
    // with a level badge proves the page loaded and queried the API successfully.
    const emptyState = page.getByText("No errors logged.");
    const levelHeader = page.getByRole("columnheader", { name: "Level" });

    await Promise.race([
        emptyState.waitFor(),
        levelHeader.waitFor(),
    ]);

    expect(await emptyState.isVisible() || await levelHeader.isVisible()).toBe(true);
});
