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
Build: the first cut's library and command are merged ([#10](https://github.com/factoincognito/sl23-cs-reader/pull/10) to [#17](https://github.com/factoincognito/sl23-cs-reader/pull/17)): the SL2 and SL3 readers, the inventory with the nine health checks, the text and JSON reports and `sl23-cs-reader inventory`. The design is approved (Adam, 2026-11-23 23:23) and is the specification, [`docs/SPEC.md`](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/SPEC.md).

## Current focus
[CS23-3](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md), the third-party notices, and [CS23-12](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md), the expected figures on the public sonarlight examples; both wait on the blockers below. [CS23-13](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) follows up the review notes.

## Next step
A fresh session writes `THIRD-PARTY-NOTICES.md` ([CS23-3](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md)) and then [CS23-12](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) test first, as soon as the blockers are answered; [CS23-13](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) can be taken in the meantime, split by area. The report to the product owner for the first build PR was made on 2026-11-24 00:30.

## Blockers
- Adam: the licence of [opensounder/python-sllib](https://github.com/opensounder/python-sllib) at `f54a27c` is unchecked, and the session's access to that repository was refused ([CS23-3](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md)). Clead's recommendation and the other open questions are in the product office's "Waiting for Adam".
- Adam: [CS23-12](https://github.com/factoincognito/sl23-cs-reader/blob/main/docs/BACKLOG.md) needs the bytes of a small excerpt of the sonarlight example files, which the session could not read (Adam approved the excerpt, 2026-11-24 00:23).
