# E2E tests

Playwright end-to-end tests that drive the real app in a browser - login, predictions, admin
flows - as opposed to `UnitTests`/`IntegrationTests`, which cover the .NET backend directly.

## Running

These tests need the app actually running somewhere. The easiest option is the Docker dev stack
from the repo root (see the root `README.md`):

```
docker compose --env-file .env.docker up --build
```

Then, from this directory:

```
npm install
npx playwright install chromium   # first run only
npm test
```

By default tests run against the Docker stack's frontend (`http://localhost:5174`). To point at
the native host workflow (`npm run dev` in `frontend/`, on port 5173) instead:

```
PLAYWRIGHT_BASE_URL=http://localhost:5173 npm test
```

Other useful scripts:

- `npm run test:headed` - run with a visible browser window.
- `npm run test:ui` - Playwright's interactive UI mode, good for writing/debugging tests.
- `npm run report` - open the HTML report from the last run.

## Test accounts

Tests log in using the accounts seeded by `Scripts/Sample/00_RunAll.sql` (`DemoPredictor` /
`DemoAdmin` - see the root `README.md`). These only exist in the Docker/sample dataset, so don't
point `PLAYWRIGHT_BASE_URL` at a real deployment with these tests.

## Notes

- Tests should assert on things that are visible regardless of viewport width. `PageHeading`
  (e.g. "Predictions", "Home") is deliberately hidden at desktop widths (the side nav already
  shows the current page there) - it's not a reliable "did this page load" signal. Prefer content
  that's always rendered, like the user menu chip or page-specific controls.
