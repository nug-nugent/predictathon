import { test, expect, type Locator, type Page } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

/// The score boxes of the LAST match still open for predictions, deliberately not the first. Score
/// inputs have no accessible label (MatchRow.tsx) and which match is still open shifts over time, so
/// they're found by not being readOnly rather than by hardcoding a fixture - and taken from the end
/// of the list because the first open match is the next one to kick off, which is exactly the one
/// the Home card offers for quick predict. quick-predict.spec.ts drives that one in parallel against
/// the same database, so sharing it would let each spec's saved score surface in the other's
/// assertions. Once it kicks off there is only one open match left and this falls back to it, which
/// is safe: quick-predict.spec.ts skips itself in exactly that state.
async function lastOpenScoreInputs(page: Page): Promise<{ home: Locator; away: Locator }> {
    // The week picker renders as soon as the week summaries land, a beat before the matches
    // themselves - so wait for the list to actually be on screen before counting, or an empty
    // count reads as "nothing open" and skips the test for no reason.
    await expect(page.locator('input[data-role="score-input"]').first()).toBeVisible();

    const open = page.locator('input[data-role="score-input"]:not([readonly])');
    const count = await open.count();

    test.skip(count < 2, "No match is still open for predictions - re-seed the sample data.");

    return { home: open.nth(count - 2), away: open.nth(count - 1) };
}

/// The score boxes of an open match that still has an *unpredicted* open match after it. That is
/// the arrangement the focus-advance rule depends on - MatchList only ever moves on to a match
/// nobody has predicted yet, so with everything after it already predicted there is nowhere for
/// focus to go and a regression here would pass unnoticed. Taken from the end of the list for the
/// same reason lastOpenScoreInputs is: quick-predict.spec.ts drives the first open match.
///
/// The very last open match is skipped over when looking for that unpredicted follower, because
/// that is the one every other test in this file writes to - it would otherwise be predicted out
/// from under this test by a sibling running alongside it.
async function openMatchBeforeAnUnpredictedOne(page: Page): Promise<{ home: Locator; away: Locator }> {
    // The week picker renders as soon as the week summaries land, a beat before the matches
    // themselves - so wait for the list to actually be on screen before counting, or an empty
    // count reads as "nothing open" and skips the test for no reason.
    await expect(page.locator('input[data-role="score-input"]').first()).toBeVisible();

    const open = page.locator('input[data-role="score-input"]:not([readonly])');
    const values = await open.evaluateAll((inputs) => inputs.map((input) => (input as HTMLInputElement).value));

    // The inputs are home/away pairs in document order, one pair per open match.
    let lastUnpredicted = -1;
    for (let pair = 0; pair * 2 + 1 < values.length - 2; pair++) {
        if (values[pair * 2] === "" && values[pair * 2 + 1] === "") {
            lastUnpredicted = pair;
        }
    }

    test.skip(lastUnpredicted < 1, "No open match has an unpredicted one after it - re-seed the sample data.");

    return { home: open.nth((lastUnpredicted - 1) * 2), away: open.nth((lastUnpredicted - 1) * 2 + 1) };
}

test.beforeEach(async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();
});

test("predictions page loads matches for the seeded Sample Cup competition", async ({ page }) => {
    await page.goto("/predictions");

    // The week picker only renders once matches for the default week have loaded.
    await expect(page.getByRole("combobox")).toBeVisible();
});

test("a prediction can be entered for an upcoming match and is saved", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    const { home: homeInput, away: awayInput } = await lastOpenScoreInputs(page);
    await expect(homeInput).toBeVisible();

    // MatchRow pre-fills these from any existing saved prediction for this match, so a re-run
    // against a database that already has one (predictions aren't reset by re-seeding, unlike
    // match results) could otherwise "fill" the same value already shown. React won't fire
    // onChange for a native value-set that doesn't actually change anything, so the save would
    // silently never happen - clear first to force a real transition either way.
    await homeInput.fill("");
    await homeInput.fill("2");
    await awayInput.fill("");
    await awayInput.fill("1");

    await expect(page.getByText("Prediction saved!")).toBeVisible();
    await expect(homeInput).toHaveValue("2");
    await expect(awayInput).toHaveValue("1");
});

test("matches are grouped under a kick-off time heading", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    // MatchList bands each run of matches sharing a kick-off under one time heading, so a match's
    // kick-off is only ever on screen as one of these - nothing on the row itself carries it. The
    // regex allows both a 24-hour and an AM/PM rendering, since the heading follows the browser
    // locale like every other time in the app.
    const kickoffHeadings = page.getByText(/^\d{1,2}:\d{2}(\s?[AP]M)?$/);
    await expect(kickoffHeadings.first()).toBeVisible();
});

test("half a scoreline is called out rather than looking like an untouched row", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    const { home, away } = await lastOpenScoreInputs(page);

    // Get to a known complete pair first, so clearing one box is unambiguously a half-entered edit
    // rather than a row that was already empty.
    await home.fill("");
    await home.fill("2");
    await away.fill("");
    await away.fill("1");
    await expect(page.getByText("Prediction saved!")).toBeVisible();

    // Half a scoreline is never sent, so without this the row would sit on its countdown looking
    // exactly like one nobody had touched - and the deadline would pass on a prediction the user
    // believed they'd made.
    await away.fill("");
    await expect(page.getByText("Enter both scores")).toBeVisible();

    // Completing the pair clears the warning and saves.
    await away.fill("3");
    await expect(page.getByText("Prediction saved!")).toBeVisible();
});

test("editing both digits of an existing prediction sends one save, not two", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    const { home, away } = await lastOpenScoreInputs(page);

    await home.fill("");
    await home.fill("2");
    await away.fill("");
    await away.fill("1");
    await expect(page.getByText("Prediction saved!")).toBeVisible();

    let saveCount = 0;
    page.on("request", (request) => {
        if (request.method() === "POST" && request.url().endsWith("/Prediction")) {
            saveCount++;
        }
    });

    // Un-debounced, this wrote "4 - 1" before "4 - 3" - a scoreline the user never chose, kept in
    // PredictionHistory where the results reconciliation can revert to it, and storable on its own
    // if the second digit lands the other side of the cutoff.
    await home.fill("4");
    await away.fill("3");

    await expect(page.getByText("Prediction saved!")).toBeVisible();
    await expect(home).toHaveValue("4");
    await expect(away).toHaveValue("3");
    expect(saveCount).toBe(1);
});

test("a failed save can be retried without retyping the score", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    await page.route("**/Prediction", async (route) => {
        if (route.request().method() === "POST") {
            await route.fulfill({
                status: 500,
                contentType: "application/problem+json",
                body: JSON.stringify({ title: "Simulated failure." }),
            });
            return;
        }

        await route.continue();
    });

    const { home, away } = await lastOpenScoreInputs(page);

    await home.fill("");
    await home.fill("3");
    await away.fill("");
    await away.fill("2");

    await expect(page.getByText("Failed to save prediction!")).toBeVisible();

    // The score is on screen but not on the server, and before this the only way back was to retype
    // a digit to trigger a fresh save.
    const retry = page.getByRole("button", { name: "Retry" });
    await expect(retry).toBeVisible();

    await page.unroute("**/Prediction");
    await retry.click();

    await expect(page.getByText("Prediction saved!")).toBeVisible();
    await expect(retry).toBeHidden();
});

test("changing the home score of an existing prediction moves to the away box, not the next match", async ({ page }) => {
    await page.goto("/predictions");
    await expect(page.getByRole("combobox")).toBeVisible();

    const { home, away } = await openMatchBeforeAnUnpredictedOne(page);

    // Establish a complete prediction first, so changing the home digit below is an edit rather
    // than a first entry - and let its own save settle, since finishing a scoreline is exactly what
    // legitimately moves focus on to the next match.
    await home.fill("");
    await home.fill("2");
    await away.fill("");
    await away.fill("1");
    await expect(page.getByText("Prediction saved!").first()).toBeVisible();

    await home.fill("4");

    // Straight to the away box, with its digit selected so the replacement types over the top.
    await expect(away).toBeFocused();
    const selected = await away.evaluate((input: HTMLInputElement) => input.selectionStart === 0 && input.selectionEnd === input.value.length);
    expect(selected).toBe(true);

    // ...and still there once the debounced save has landed. Every successful save used to advance
    // to the next match, so editing the home digit of an existing prediction put focus on the away
    // box and then snatched it away again a beat later. A fixed wait rather than a state to wait
    // for: the assertion is that nothing steals focus during the window, so the window is the test.
    await page.waitForTimeout(1500);
    await expect(away).toBeFocused();
});
