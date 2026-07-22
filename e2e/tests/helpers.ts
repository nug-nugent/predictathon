import type { Page } from "@playwright/test";

export const DEMO_PREDICTOR = { username: "DemoPredictor", password: "DemoPass123!" };

export async function login(page: Page, username: string, password: string): Promise<void> {
    await page.goto("/");
    await page.getByLabel("Email / Username").fill(username);
    // The visibility-toggle button is also labelled "Password", so getByLabel matches both -
    // scope to the actual textbox.
    await page.getByRole("textbox", { name: "Password" }).fill(password);
    await page.getByRole("button", { name: "Login" }).click();
}
