import { test, expect } from "@playwright/test";
import { DEMO_PREDICTOR, login } from "./helpers";

// The sample dataset seeds no message threads at all, so this makes its own. What it creates isn't
// cleaned up - the same caveat as registration.spec.ts, so don't point this at anything but the
// disposable Docker DB (see e2e/README.md).
test("a reaction pill lists who reacted, and takes your own reaction back", async ({ page }) => {
    const subject = `E2E reactions ${Date.now()}`;

    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    await page.goto("/board");
    await page.getByRole("button", { name: "New Thread" }).click();
    await page.getByLabel("Subject").fill(subject);
    await page.getByLabel("Message").fill("Something worth reacting to.");
    await page.getByRole("button", { name: "Create Thread" }).click();

    await expect(page.getByRole("heading", { name: subject })).toBeVisible();

    // emoji-mart renders its grid inside an open shadow root, and one picker is mounted per message
    // whether or not it's open - hence :visible. Each emoji carries its name as a title attribute,
    // and the same one appears twice (once under "Frequently used", once in its own category), so
    // either match will do.
    await page.getByRole("button", { name: "React" }).click();
    await page.locator("em-emoji-picker:visible").getByTitle("Thumbs Up", { exact: true }).first().click();

    // The pill's accessible name is the whole of what it conveys: which reaction, how many, and
    // that there's a list behind it.
    const pill = page.getByRole("button", { name: "Thumbs Up, 1 reaction. Show who reacted" });
    await expect(pill).toBeVisible();

    await pill.click();
    await expect(page.getByRole("link", { name: `${DEMO_PREDICTOR.username} (you)` })).toBeVisible();

    // Taking it back from the popover leaves nothing behind: no reactors, so no pill.
    await page.getByRole("button", { name: "Remove me" }).click();
    await expect(pill).toBeHidden();
});
