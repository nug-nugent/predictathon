import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// The seeded Hall of Fame (Scripts/Sample/10_HallOfFame.sql) gives DemoPredictor three World Cups
// and one series-less one-off, which is exactly the pair of cases the trophy grouping has to get
// right: repeated wins in a series collapse into one counted badge, a one-off stays on its own.
test("a player's trophy cabinet groups repeated wins and keeps one-offs apart", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    await page.getByRole("link", { name: DEMO_PREDICTOR.username }).first().click();
    await expect(page).toHaveURL(/\/profile\//);

    await expect(page.getByText("Trophies")).toBeVisible();

    // Three World Cups on one badge, with the years that earned it.
    await expect(page.getByText("World Cup", { exact: true })).toBeVisible();
    await expect(page.getByText("×3")).toBeVisible();
    await expect(page.getByText("1998, 2006, 2014")).toBeVisible();

    // The one-off keeps its own competition name rather than being lumped in with anything else.
    await expect(page.getByText("Millennium Shield")).toBeVisible();
});

// The Home dashboard's own-profile card carries the compact stamp rather than the full cabinet.
// Unlike the profile page and the message board, that card has no payload of its own to fold the
// trophies into, so this also covers the /Trophy/User endpoint it fetches them from.
test("the home dashboard's profile card stamps the player's wins beside their name", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    // The stamp is icon-only, so its accessible name is the whole of what it conveys.
    await expect(page.getByRole("img", { name: "World Cup, won 3 times: 1998, 2006, 2014" })).toBeVisible();
    await expect(page.getByRole("img", { name: "Millennium Shield, won in 2000" })).toBeVisible();

    // Wins only, and only on the profile card - the mini league table beside it stays unstamped.
    await expect(page.getByRole("img", { name: /won/ })).toHaveCount(2);
});

// Most players have never won anything, and the cabinet is meant to draw nothing at all for them
// rather than an empty panel.
test("a player with no wins gets no trophy cabinet", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    // Bryn Callaghan is a seeded competitor who only ever placed, never won.
    await page.goto("/profile/da000000-0000-0000-0000-000000000002");

    // Exact, so this matches the profile card's own heading rather than the what-if league
    // table's "If Bryn Callaghan's predictions had all come true..." further down the page.
    await expect(page.getByRole("heading", { name: "Bryn Callaghan", exact: true })).toBeVisible();
    await expect(page.getByText("Trophies")).toBeHidden();
});
