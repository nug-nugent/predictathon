import { test, expect } from "@playwright/test";
import { login } from "./helpers";

// Founders Trophy is seeded with an entrance fee and PayPal disabled (Scripts/Sample/02_Competition.sql),
// so starting registration for it creates the account and stops at the "pay your entry fee" step
// without needing any real payment - the easiest way to reach "account exists, but isn't
// registered for any competition" without touching the database directly. Each run uses a unique
// username; accounts created here aren't cleaned up (same disposable-Docker-DB caveat as
// registration.spec.ts).
test("a user not registered for any competition is confined to the sign-up prompt until they join one", async ({ page }) => {
    const unique = Date.now();
    const username = `e2e-nocomp-${unique}`;
    const password = "TestPass123!";

    await page.goto("/");
    await page.getByRole("link", { name: /Register for Founders Trophy/ }).click();
    await expect(page.getByRole("heading", { name: "Founders Trophy" })).toBeVisible();

    await page.getByLabel("First name").fill("E2E");
    await page.getByLabel("Surname").fill("NoComp");
    await page.getByLabel("Username").fill(username);
    await page.getByLabel("Email").fill(`${username}@example.com`);
    await page.getByRole("textbox", { name: "Password", exact: true }).fill(password);
    await page.getByRole("textbox", { name: "Confirm password" }).fill(password);
    await page.getByRole("button", { name: "Continue to Payment" }).click();

    // The account now exists (and is logged in) but no UserCompetition row was ever created -
    // abandon here instead of redeeming a payment credit code.
    await expect(page.getByRole("heading", { name: "Pay your entry fee" })).toBeVisible();

    // Log out and back in as the same account, reaching the gate via a normal login rather than
    // relying on the session left over from registration.
    await page.getByRole("button", { name: username }).click();
    await page.getByRole("button", { name: "Logout" }).click();
    await login(page, username, password);

    // Home shows the sign-up prompt instead of a dashboard, listing every competition open for
    // registration (both the free and the paid one).
    await expect(page.getByText("You're not registered for any competitions yet. Sign up to one below to get started.")).toBeVisible();
    await expect(page.getByRole("link", { name: /Register for Sample Cup/ })).toBeVisible();
    await expect(page.getByRole("link", { name: /Register for Founders Trophy/ })).toBeVisible();

    // The side nav only offers Home - no dead-end links to pages that would just bounce back.
    await expect(page.getByRole("link", { name: "Predictions", exact: true })).toHaveCount(0);

    // Any other page redirects straight back to Home instead of loading.
    await page.goto("/board");
    await expect(page).toHaveURL("/");
    await expect(page.getByText("You're not registered for any competitions yet.", { exact: false })).toBeVisible();

    // Completing a (free) registration from the prompt lifts the gate.
    await page.getByRole("link", { name: /Register for Sample Cup/ }).click();
    await expect(page.getByRole("heading", { name: "Join a competition" })).toBeVisible();
    await page.getByRole("button", { name: "Join Competition" }).click();

    await expect(page).toHaveURL("/");
    await expect(page.getByText("You're not registered for any competitions yet.")).not.toBeVisible();
    await expect(page.getByRole("link", { name: "Predictions", exact: true })).toBeVisible();

    await page.goto("/board");
    await expect(page).toHaveURL("/board");
});
