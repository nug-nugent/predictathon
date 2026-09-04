import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// Teams come from the database project's reference data (Database/Post-Deployment/ReferenceData/01_Teams.sql),
// whose ids are fixed - so linking straight to Brazil's page is stable against any migrated database.
// Not the id Scripts/Sample/01_SampleCupSetup.sql lists for Brazil: that's the legacy Sample Cup id,
// which the sample seed only uses to map its hand-authored fixtures onto these rows by team name.
const BRAZIL_TEAM_ID = "6E71EB7F-53D5-493B-A268-A80B39676AAB";

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
});

test("team detail page lists the team's upcoming fixtures alongside its results", async ({ page }) => {
    await page.goto(`/team/${BRAZIL_TEAM_ID}`);

    await expect(page.getByRole("heading", { name: "Brazil" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Fixtures" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Results" })).toBeVisible();
});

test("team detail page hides the league table for a competition with knockout matches", async ({ page }) => {
    await page.goto(`/team/${BRAZIL_TEAM_ID}`);

    await expect(page.getByRole("heading", { name: "Fixtures" })).toBeVisible();

    // Sample Cup has a knockout stage (Scripts/Sample/04_Match.sql), so a single table would be
    // meaningless - the API returns no league table and the page leaves the panel out entirely.
    await expect(page.getByRole("heading", { name: "League Table" })).toHaveCount(0);
    await expect(page.getByRole("columnheader", { name: "PTS" })).toHaveCount(0);
});

test("recent results for a team open from its name on the predictions page", async ({ page }) => {
    await page.goto("/predictions");

    // The week picker only renders once matches for the default week have loaded.
    await expect(page.getByRole("combobox")).toBeVisible();

    // Which teams are on show depends on the week, so drive whichever one is listed first rather
    // than hardcoding a team. The user-menu chip and each row's "All Predictions" toggle are
    // popovers too, hence the filters.
    const teamTrigger = page.locator('[data-scope="popover"][data-part="trigger"]')
        .filter({ hasNotText: "All Predictions" })
        .filter({ hasNotText: DEMO_PREDICTOR.username })
        .first();
    // The trigger renders the team's name at three lengths (acronym, short, full) and shows one
    // of them per screen width, so read the screen-reader copy: it is the full name the dialog
    // heading uses, at every width.
    const teamName = ((await teamTrigger.locator('[data-role="team-full-name"]').textContent()) ?? "").trim();

    await teamTrigger.click();

    const popover = page.locator('[data-scope="popover"][data-part="content"][data-state="open"]');
    await popover.getByRole("button", { name: "Recent Results" }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Recent Results" });
    await expect(dialog.getByRole("heading", { name: `${teamName} - Recent Results` })).toBeVisible();
    await expect(dialog.getByRole("link", { name: "View Team Detail" })).toBeVisible();

    await dialog.getByRole("button", { name: "Close" }).click();
    await expect(dialog).toHaveCount(0);
});
