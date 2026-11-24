<!--
The format of this file (Process P-9 § 6). It has the title "# Status",
a "Last update: YYYY-MM-DD" line, and the headings Phase, Current focus,
Next step and Blockers, in that order and written exactly so: the
product owner's overview of all projects reads them, so the headings
and the format are not changed. A session that changes what a field
below says updates this file in the same PR as that change, and sets
"Last update" to that day. Forward-looking only: where the project is,
what comes next and what stops it. History lives in Git, the backlog
and the changelog, not here. A blocker that waits on the product owner
starts with "Product Owner:", written exactly so; a session that finds
a blocker resolved removes it.
-->

# Status

Last update: 2026-11-24

## Phase
Build: the first cut's library and command are merged ([#10](https://github.com/factoincognito/sl23-cs-reader/pull/10) to [#17](https://github.com/factoincognito/sl23-cs-reader/pull/17)): the SL2 and SL3 readers, the inventory with the nine health checks, the text and JSON reports and `sl23-cs-reader inventory`. On the two whole public sonarlight example files it gives the design's expected figures ([#21](https://github.com/factoincognito/sl23-cs-reader/pull/21)), so both conditions of the Phase 1 goal in the [backlog](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) are met; the goal's wording is Clead's and not yet confirmed by Adam. The design is approved (Adam, 2026-11-23 23:23) and is the specification, [`docs/SPEC.md`](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/SPEC.md).

## Current focus
Waiting for Adam: his test of the VS Code entry ([CS23-18](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md)) and the two questions in the backlog's "Waiting for the product owner". No work is under way.

## Next step
Clead's proposal, not decided: Adam runs `sl23-cs-reader inventory` on his own recordings and gives the default report to a session, since the design leaves the next cut to be decided with the first report in hand ([`docs/SPEC.md`](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/SPEC.md), section 10, point 2). The follow-ups left over from the reviews are [CS23-19](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) and need no decision except the schema field.

## Blockers
- Product Owner: [CS23-18](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) is done only when Adam has run the entry in VS Code on Windows; the session had no VS Code and no Windows.
