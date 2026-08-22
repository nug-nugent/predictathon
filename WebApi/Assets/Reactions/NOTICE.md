The standard-emoji SVGs in this folder (filenames matching Unicode codepoints, e.g. `1f44d.svg`)
are the [Twemoji](https://github.com/jdecked/twemoji) graphics, vendored from the `jdecked/twemoji`
fork (the actively-maintained continuation of Twitter's original project). Licensed under
[CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/) — copyright Twitter, Inc and other
contributors, graphics licensed under CC-BY 4.0.

All other files here (e.g. `ludo.png`, `brewdog.png`, ...) are Predictathon's own custom reactions,
not covered by that license.

Vendored on 2026-07-18 from the `assets/svg` folder of `jdecked/twemoji` at the version current at
that date. Re-run the same process (shallow clone, `git sparse-checkout set assets/svg`, copy) to
pick up newly-added emoji in a future Unicode release.

## custom-reactions.json

`custom-reactions.json` is the manifest for Predictathon's own reactions - the single source of
truth for what a `c:{id}` reaction identity resolves to. `ReactionCatalogue` reads it, and the
client's emoji picker builds its custom category from `GET /Messageboard/Reactions/Catalogue`
rather than a hardcoded list, so adding a reaction is a file drop plus a manifest line with no
frontend change.

An entry whose `imageFile` isn't present here is skipped rather than served, so a typo shows up as
a missing picker entry, not a broken image on the board. `id` is permanent once used: it's stored
against every reaction in the database.

Standard Unicode emoji aren't listed - they're addressed by codepoint (`u:{unified}`) and resolved
against the Twemoji filenames directly. Note those two spellings disagree: emoji-mart pads
codepoints and keeps the FE0F variation selector (`2764-fe0f`), Twemoji usually does neither
(`2764.svg`). `ReactionCatalogue` reconciles them, and `ReactionCatalogueTests` asserts that every
emoji in the shipped dataset resolves - re-run it after re-vendoring, and regenerate
`UnitTests/TestData/emoji-mart-unified-15.txt` if `@emoji-mart/data` is upgraded.
