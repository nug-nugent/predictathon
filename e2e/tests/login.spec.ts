import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

test("logged-out visitor sees the login form and open registrations", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("button", { name: "Login" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Register" })).toBeVisible();
});

test("a seeded user can log in and reach their dashboard", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);

    // The user's account chip (avatar + username) in the header is the reliable "logged in"
    // signal - PageHeading elements like "Home" are hidden at desktop widths by design.
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
    await expect(page.getByLabel("Email / Username")).not.toBeVisible();
});

test("an invalid password shows an error instead of logging in", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, "not the real password");

    await expect(page.getByText(/incorrect|invalid|wrong/i)).toBeVisible();
    await expect(page.getByRole("button", { name: "Login" })).toBeVisible();
});
