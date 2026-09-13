import { test, expect, type Page } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// A phone-sized viewport, roughly an iPhone 14 / Pixel 7. The suite's one project is Desktop
// Chrome, so this is set per-file rather than as another project.
test.use({ viewport: { width: 390, height: 844 } });

// Every page that carries a table, plus the ones whose cards hold a wide header row. Several of
// these tables are wider than a phone at their full column set, and each fits by standing some
// columns down below `md` - so this is the check that says the standing-down is still enough.
// A match Scripts/Sample/04_Match.sql pins to earlier today, so it is past the two-minute reveal
// cutoff and the page actually renders the predictions table this test is here to measure. A future
// fixture would leave everyone else's predictions hidden and the "All Predictions" card absent.
const MATCH_ID = "fa000000-0000-0000-0000-000000000003";
const PAGES: { path: string; readyHeading: string }[] = [
    { path: "/", readyHeading: "League Table" },
    { path: "/league", readyHeading: "League Table" },
    { path: "/live", readyHeading: "Live League Table" },
    { path: "/results", readyHeading: "Results" },
    { path: `/match/${MATCH_ID}`, readyHeading: "All Predictions" },
];

/**
 * What sticks out past the right-hand edge on this page, if anything: the document itself scrolling
 * sideways, or a card whose content is wider than the card. Runs in the browser, so it sees the
 * real laid-out widths rather than anything the test assumes about them.
 */
async function findOverflow(page: Page): Promise<string[]> {
    return page.evaluate(() => {
        const problems: string[] = [];
        const viewportWidth = document.documentElement.clientWidth;

        if (document.documentElement.scrollWidth > viewportWidth) {
            problems.push(`page scrolls sideways: ${document.documentElement.scrollWidth}px of content in ${viewportWidth}px`);
        }

        document.querySelectorAll("*").forEach((element) => {
            const overflowX = getComputedStyle(element).overflowX;
            if (overflowX !== "auto" && overflowX !== "scroll") {
                return;
            }

            // A pixel or two is rounding, not a layout problem.
            if (element.scrollWidth > element.clientWidth + 2) {
                const label = (element.textContent ?? "").trim().replace(/\s+/g, " ").slice(0, 40);
                problems.push(`card scrolls sideways (${element.scrollWidth}px in ${element.clientWidth}px): "${label}"`);
            }
        });

        return problems;
    });
}

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);

    // The user-menu chip is the usual "we're logged in now" signal, but at this width it shows the
    // avatar alone - the username beside it is hidden - so there is no name to wait for. The login
    // form going away says the same thing and says it at any width.
    await expect(page.getByRole("button", { name: "Login" })).toHaveCount(0);
});

for (const { path, readyHeading } of PAGES) {
    test(`${path} fits a phone screen without scrolling sideways`, async ({ page }) => {
        await page.goto(path);

        // Waiting on a heading the page only renders once its data has arrived - measuring a
        // half-loaded page would pass on an empty table. `.first()`: the Results page's own title
        // and the card inside it share a name at this width, where page titles are on show.
        await expect(page.getByRole("heading", { name: readyHeading }).first()).toBeVisible();

        expect(await findOverflow(page)).toEqual([]);
    });
}

// A real pair of Premier League names, supplied by the route rather than waited for: every seeded
// competition is internationals, whose names are short enough that the match header is never tight.
const LONG_HOME = { homeTeam: "Wolverhampton Wanderers", homeTeamShortName: "Wolves", homeTeamAcronym: "WOL" };
const LONG_AWAY = { awayTeam: "Brighton & Hove Albion", awayTeamShortName: "Brighton", awayTeamAcronym: "BHA" };

/**
 * Serves the match detail page with a chosen pair of team names, leaving the rest of the response -
 * the real result, prediction and averages - as the API sent it.
 *
 * @param page The page to intercept requests from.
 * @param names The team-name fields to override on the response.
 */
async function withTeamNames(page: Page, names: Record<string, string | null>): Promise<void> {
    await page.route(`**/Match/*/${MATCH_ID}/Detail`, async (route) => {
        const response = await route.fetch();
        const detail = await response.json();

        // Fulfilled from the real response so its CORS headers survive - the API is a different
        // origin from the frontend in both the Docker and the host workflow.
        await route.fulfill({ response, json: { ...detail, ...names } });
    });
}

/**
 * How many lines the match header's scoreline occupies. Counts distinct tops rather than rects: the
 * scoreline is three text nodes, and each contributes a rect of its own on every line it sits on.
 *
 * @param page The page showing a match.
 */
async function scorelineLineCount(page: Page): Promise<number> {
    return page.getByRole("heading", { name: /^\d+ - \d+$/ }).evaluate((element) => {
        const range = document.createRange();
        range.selectNodeContents(element);

        return new Set([...range.getClientRects()].map((rect) => Math.round(rect.top))).size;
    });
}

test("the match header shows the acronym on a phone and the full name to a screen reader", async ({ page }) => {
    await withTeamNames(page, { ...LONG_HOME, ...LONG_AWAY });
    await page.goto(`/match/${MATCH_ID}`);

    // Found by its accessible name, which is the full name at every width - so this locator passing
    // is half the assertion: the abbreviation is for the eye, not for assistive technology.
    const homeHeading = page.getByRole("heading", { name: LONG_HOME.homeTeam });
    await expect(homeHeading).toBeVisible();

    const onScreen = await homeHeading.evaluate((element) => [...element.querySelectorAll("[aria-hidden='true']")]
        .filter((span) => getComputedStyle(span).display !== "none")
        .map((span) => span.textContent));

    expect(onScreen).toEqual([LONG_HOME.homeTeamAcronym]);
    expect(await scorelineLineCount(page)).toBe(1);
});

test("a team with no acronym or short name still doesn't split the scoreline", async ({ page }) => {
    // The fallback TeamLabel documents - a team added in a hurry with neither shorter name filled
    // in - which puts two full club names either side of the score at a phone width. This is the
    // case that used to leave "0 -" above a lonely "5".
    await withTeamNames(page, {
        ...LONG_HOME, homeTeamShortName: null, homeTeamAcronym: null,
        ...LONG_AWAY, awayTeamShortName: null, awayTeamAcronym: null,
    });
    await page.goto(`/match/${MATCH_ID}`);

    await expect(page.getByRole("heading", { name: LONG_AWAY.awayTeam })).toBeVisible();

    expect(await scorelineLineCount(page)).toBe(1);
    expect(await findOverflow(page)).toEqual([]);
});

test("every statistics tab fits a phone screen", async ({ page }) => {
    await page.goto("/stats");

    for (const tabName of ["All time", "Current competition", "All-time league table"]) {
        await page.getByRole("tab", { name: tabName }).click();

        // Each tab loads its own data; the tables only appear once that has arrived.
        await expect(page.getByRole("table").first()).toBeVisible();

        expect(await findOverflow(page), `on the "${tabName}" tab`).toEqual([]);
    }
});
