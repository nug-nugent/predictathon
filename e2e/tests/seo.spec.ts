import { test, expect } from "@playwright/test";

// "/" (the logged-out landing/login page) is the only page on the site a search engine or a link
// scraper can reach - everything else is behind auth. These tags are therefore the whole of the
// site's SEO and link-preview surface, and they live as static markup in frontend/index.html, so
// nothing in the app would fail if one were dropped. Hence asserting on them here.

test("the landing page carries its canonical and social tags", async ({ page }) => {
    await page.goto("/");

    // Hardcoded to production in index.html on purpose (see the comment there), so it reads the
    // same whichever environment the tests point at.
    await expect(page.locator("link[rel=canonical]")).toHaveAttribute("href", "https://predictathon.co.uk/");

    await expect(page.locator('meta[property="og:title"]')).toHaveAttribute("content", /Predictathon/);
    await expect(page.locator('meta[property="og:image"]')).toHaveAttribute("content", /og-image\.png$/);
    await expect(page.locator('meta[name="twitter:card"]')).toHaveAttribute("content", "summary_large_image");

    // Scrapers truncate well before this; an empty or runaway description is the failure worth catching.
    const description = await page.locator('meta[name=description]').getAttribute("content");
    expect(description?.length).toBeGreaterThan(50);
    expect(description?.length).toBeLessThan(200);
});

test("the landing page has exactly one h1, and it is the proposition", async ({ page }) => {
    await page.goto("/");

    // The "Predictathon" wordmark deliberately isn't the h1 - the sales pitch beneath it is, since
    // that's the text describing the site to a search engine (see LoggedOutLanding).
    const h1s = page.locator("h1");
    await expect(h1s).toHaveCount(1);
    await expect(h1s.first()).toContainText("Predict the scores");
});

test("robots.txt and sitemap.xml are served", async ({ request }) => {
    const robots = await request.get("/robots.txt");
    expect(robots.status()).toBe(200);
    expect(await robots.text()).toContain("Sitemap: https://predictathon.co.uk/sitemap.xml");

    const sitemap = await request.get("/sitemap.xml");
    expect(sitemap.status()).toBe(200);
    expect(await sitemap.text()).toContain("<loc>https://predictathon.co.uk/</loc>");
});
