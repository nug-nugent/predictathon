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
