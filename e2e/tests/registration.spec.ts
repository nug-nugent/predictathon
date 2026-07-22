import { test, expect } from "@playwright/test";

// Sample Cup is seeded as free-to-enter (Scripts/Sample/02_Competition.sql) and always open, so
// registration completes in one step with no payment. Each run uses a unique username - accounts
// created here aren't cleaned up, so don't point this at anything but the disposable Docker DB
// (see e2e/README.md); wipe it with `docker compose down -v` / `.\make.ps1 clean` if it bothers you.
test("a new user can register for the free, open Sample Cup competition", async ({ page }) => {
    const unique = Date.now();
    const username = `e2e-test-${unique}`;

    await page.goto("/");

    await page.getByRole("link", { name: /Register for Sample Cup/ }).click();
    await expect(page.getByRole("heading", { name: "Sample Cup" })).toBeVisible();

    await page.getByLabel("First name").fill("E2E");
    await page.getByLabel("Surname").fill("Test");
    await page.getByLabel("Username").fill(username);
    await page.getByLabel("Email").fill(`${username}@example.com`);
    await page.getByRole("textbox", { name: "Password", exact: true }).fill("TestPass123!");
    await page.getByRole("textbox", { name: "Confirm password" }).fill("TestPass123!");

    await page.getByRole("button", { name: "Register" }).click();

    await expect(page.getByRole("heading", { name: "You're all set!" })).toBeVisible();
    await expect(page.getByText(`Thanks for registering for Sample Cup.`)).toBeVisible();

    // Registration also logs the new account in.
    await page.getByRole("link", { name: "Go to the home page" }).click();
    await expect(page.getByRole("button", { name: username })).toBeVisible();
});
