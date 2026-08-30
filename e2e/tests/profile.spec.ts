import { test, expect } from "@playwright/test";
import { fileURLToPath } from "node:url";
import { DEMO_PREDICTOR, login } from "./helpers";

const AVATAR_FIXTURE = fileURLToPath(new URL("./fixtures/avatar.png", import.meta.url));

// Uploads a photo, opens it full size, then removes it again - the seeded accounts start with no
// avatar image (other specs rely on that), so this test puts the dataset back as it found it.
test("a player's photo can be uploaded, opened full size, and removed", async ({ page }) => {
    await login(page, DEMO_PREDICTOR.username, DEMO_PREDICTOR.password);
    await expect(page.getByRole("button", { name: DEMO_PREDICTOR.username })).toBeVisible();

    // The mini league table on the home page is the shortest route to one's own profile page.
    await page.getByRole("link", { name: DEMO_PREDICTOR.username }).first().click();
    await expect(page).toHaveURL(/\/profile\//);

    const openFullSize = page.getByRole("button", { name: `View ${DEMO_PREDICTOR.username}'s photo full size` });
    await expect(openFullSize).toBeHidden();

    await page.getByRole("button", { name: "Change photo" }).click();
    await page.locator('input[type="file"]').setInputFiles(AVATAR_FIXTURE);
    // The cropper reports its crop rectangle once the image has loaded, which is what enables Save.
    await page.getByRole("button", { name: "Save" }).click();

    // Clicking the picture opens it at full size - the large file, not the thumbnail beside the name.
    await expect(openFullSize).toBeVisible();
    await openFullSize.click();

    const dialog = page.getByRole("dialog").filter({ has: page.getByRole("button", { name: "Close" }) });
    const fullSizeImage = dialog.getByRole("img", { name: `${DEMO_PREDICTOR.username}'s photo` });
    await expect(fullSizeImage).toBeVisible();
    await expect(fullSizeImage).toHaveAttribute("src", /\/uploads\/avatars\/[0-9a-f-]+\.jpg\?v=/);

    await dialog.getByRole("button", { name: "Close" }).click();

    await page.getByRole("button", { name: "Change photo" }).click();
    await page.getByRole("button", { name: "Remove Photo" }).click();

    await expect(openFullSize).toBeHidden();
});
