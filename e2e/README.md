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

Tests log in using the accounts seeded by `Scripts/Sample/00_RunAll.sql` (see the root
`README.md`). These only exist in the Docker/sample dataset, so don't point `PLAYWRIGHT_BASE_URL`
at a real deployment with these tests.

| Account | Used by | Competition |
| --- | --- | --- |
| `DemoPredictor` | `predictions.spec.ts`, and every read-only player spec | Sample Cup |
| `DemoQuickPredict` | `quick-predict.spec.ts` | Sample Cup |
| `DemoBracket` | `knockout-bracket.spec.ts` | Sample Cup |
| `DemoWeekAhead` | `this-weeks-matches.spec.ts` | **Week Ahead Cup** |
| `DemoAdmin` | `process-results.spec.ts`, `live-score.spec.ts`, `error-log.spec.ts` | **Admin Cup** |

All five share the passwords in the root `README.md`; the three extra players use `DemoPredictor`'s.

They're named for the spec that owns them rather than numbered, because Playwright matches an
accessible name by substring: `DemoPredictor2` answered to a locator asking for `DemoPredictor`,
which made `league.spec.ts`'s search for its own row in the table ambiguous.

The split is what lets the suite run its files in parallel. Two things get contended for, and each
is isolated on the axis it's keyed by:

- **Predictions** are keyed on `(UserID, MatchID)`, so the specs that enter them have a player each.
  Sharing one account meant each spec's saved score surfaced in another's assertions.
- **Match state** is keyed on `CompetitionID`, so the admin specs - which confirm results and set
  live scores - work in `Admin Cup`, seeded by `Scripts/Sample/11_AdminCup.sql`. Confirming a result
  takes a match out of play permanently, and `live.spec.ts` asserts matches *are* in play.
- **What day it is** is a property of a competition's fixture list, so `Week Ahead Cup`
  (`Scripts/Sample/13_WeekAheadCup.sql`) has none today at all. Sample Cup and Admin Cup both pin
  fixtures into today deliberately, which leaves the Home page's This Week's Matches card - the one
  that only appears when nothing is on - with nowhere to be seen.

No spec selects a competition; `CompetitionProvider` resolves it from the signed-in account's own
registrations, so being defaulted into `Admin Cup` is all it takes. `DemoAdmin` is still registered
in Sample Cup, so you can switch to it by hand in the browser.

## Re-seeding

`docker compose --env-file .env.docker up db-seed` (about 20 seconds) puts the sample data back.
It genuinely restores rather than tops up: predictions against fixtures that haven't kicked off are
cleared, `Admin Cup`'s matches are rewritten, and the accounts `registration.spec.ts` and
`no-competitions.spec.ts` create are deleted. Before that it only ever inserted what was missing,
so the suite steadily ate the unpredicted fixtures it needed and two tests in `predictions.spec.ts`
had skipped themselves permanently.

## Notes

- Tests should assert on things that are visible regardless of viewport width. `PageHeading`
  (e.g. "Predictions", "Home") is deliberately hidden at desktop widths (the side nav already
  shows the current page there) - it's not a reliable "did this page load" signal. Prefer content
  that's always rendered, like the user menu chip or page-specific controls.
