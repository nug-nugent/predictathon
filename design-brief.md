# Predictathon — Visual Redesign Brief (cosmetic only)

I'm the sole developer/maintainer of Predictathon, a football score-prediction competition site for a private group of ~50 friends/family. Users predict match scores each week and earn points for accuracy (3/2/1/0 depending on how close). It's a hobby project I recently rebuilt on a modern stack (React 19 + Chakra UI v3), and functionally it's done and correct — but visually it's still exactly what you get out of the box from Chakra's default theme. No branding, no color identity, no personality. It looks like an internal admin tool, not a matchday product for friends.

**Attached:** screenshots of the current site.

**Scope — read carefully, this is a hard constraint:**
This is a **purely cosmetic redesign**. Do not propose new features, changed layouts, different information architecture, new pages, or altered user flows. Every screen, button, table, and form field that exists today needs to keep doing exactly what it does today — I just want it to look like a designed product instead of a wireframe. Treat the attached screenshots and this description of the page inventory as the fixed functional spec.

**What's in scope:**
- A full visual identity: logo/wordmark, color palette, typography system, iconography style
- Applying that identity across the full page inventory below
- Light theme as the priority (mention if you think dark mode is worth a variant, but don't design full dark-mode without me confirming)

**Page inventory to cover** (routes, not full mockups of every one — but the design system should visibly work across all of them):
Login/home, registration, password reset, predictions (score entry grid), league/leaderboard table, message board + thread view, statistics page, team detail, hall of fame, rules, user profile + edit, competition registration, and a set of admin screens (manage competitions, manage matches, process results, manage users, payment credits).

**Technical constraints:**
- Built on Chakra UI v3 (`createSystem`/theme tokens + recipes). Palettes and type scales should be realistic to express as Chakra theme tokens — not a bespoke component-by-component rebuild.
- The `points` color scale (currently red/orange/olive/green for 0/1/2/3-point predictions) is functional, not decorative — it's how users read prediction accuracy at a glance. Any new palette needs an equivalent 4-step "wrong → close → good → perfect" progression that stays legible, even if the exact hues change.
- Needs to hold up on mobile — people predict from their phones between matches.
- Keep contrast at WCAG AA at minimum.

**Tone:** This is a fun, casual competition between friends, not a corporate SaaS product — but I don't want it juvenile either. Open to a range from "clean modern app" to "playful matchday energy."

**What I want back:** Please give me **3 distinct visual directions** to choose between, not one final answer. For each direction, include:
1. A one-sentence description of the mood/positioning
2. A color palette with hex values (primary, secondary/accent, neutrals, and the 4-step points scale)
3. A typography pairing (heading + body, with fallback stack)
4. A logo/wordmark concept
5. A mockup of 2-3 key screens (e.g. predictions grid + leaderboard) applying the direction, using the attached screenshots as the functional reference
6. A short rationale for why this direction fits
