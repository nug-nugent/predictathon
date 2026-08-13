import type { Page } from "@playwright/test";

// Credentials for the accounts seeded by Scripts/Sample/00_RunAll.sql - see e2e/README.md.
export const DEMO_PREDICTOR = { username: "DemoPredictor", password: "DemoPass123!" };
export const DEMO_ADMIN = { username: "DemoAdmin", password: "DemoAdmin!2026" };

export async function login(page: Page, username: string, password: string): Promise<void> {
    await page.goto("/");
    await page.getByLabel("Email or username").fill(username);
    // The visibility-toggle button is also labelled "Password", so getByLabel matches both -
    // scope to the actual textbox.
    await page.getByRole("textbox", { name: "Password" }).fill(password);
    await page.getByRole("button", { name: "Login" }).click();
}
