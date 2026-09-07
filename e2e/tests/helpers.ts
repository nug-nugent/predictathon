import type { Page } from "@playwright/test";

// Credentials for the accounts seeded by Scripts/Sample/00_RunAll.sql - see e2e/README.md.
//
// Three players rather than one, because the unit of isolation in this suite is the spec file. A
// A prediction is keyed on (UserID, MatchID), so two specs entering predictions as the same player
// write the same rows and each one's saved score turns up in the other's assertions. A player each
// removes the contention, named for the spec that owns it. Assigned by hand here rather than picked
// off testInfo.parallelIndex, so which spec owns which account doesn't shift with the worker count.
//
// Not DemoPredictor2/3: Playwright matches an accessible name by substring unless a locator passes
// exact, so those answered to a lookup for "DemoPredictor" and made league.spec's search for its own
// row in the table ambiguous.
//
// DemoAdmin is isolated the other way round. The admin specs contend over match rows rather than
// prediction rows, and a match belongs to a competition - so DemoAdmin is defaulted into a
// competition of their own (Scripts/Sample/11_AdminCup.sql) and nothing here has to know about it.
export const DEMO_PREDICTOR = { username: "DemoPredictor", password: "DemoPass123!" };
export const DEMO_QUICK_PREDICT = { username: "DemoQuickPredict", password: "DemoPass123!" };
export const DEMO_BRACKET = { username: "DemoBracket", password: "DemoPass123!" };
export const DEMO_ADMIN = { username: "DemoAdmin", password: "DemoAdmin!2026" };

export async function login(page: Page, username: string, password: string): Promise<void> {
    await page.goto("/");
    await page.getByLabel("Email or username").fill(username);
    // The visibility-toggle button is also labelled "Password", so getByLabel matches both -
    // scope to the actual textbox.
    await page.getByRole("textbox", { name: "Password" }).fill(password);
    await page.getByRole("button", { name: "Login" }).click();
}
