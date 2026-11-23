# sl23-cs-reader Product Backlog

**Status:** Living document
**Owner:** Adam Smolowicz with Clead

---

## How to read this backlog

This header is the format of this file (Process P-9 § 2).

- One entry per line: `- **[MARKER] ID**`, optionally followed by notes
  in parentheses, then ` — ` and the title. The detail is indented below
  the entry.
- A line inside a fenced code block is never an entry.
- The markers are these five, and no others:

| Marker | Meaning |
|---|---|
| [NOW] | Active — being worked on |
| [NEXT] | Queued — ready to start after current work |
| [LATER] | Confirmed future work, not yet scheduled |
| [BLOCKED] | Waiting on dependency or decision |
| [DONE] | Completed and merged |

Items are numbered `CS23-<n>` (`RULES.md` L § 4), from 1. Numbers are
never reused.

---

## Waiting for the product owner

The one list the product owner reads (Process P-9 § 3). Every open item
that needs their click, setting or decision is one line here, with
Clead's recommendation and the item that holds the detail. A line is
added when an item needs the product owner and removed when they have
answered.

### Now

None.

### When you have time

- Confirm the wording of the goal (README "Purpose and goal", `memory/project.md`, the Phase 1 goal below), which is Clead's and not yet confirmed. Clead recommends accepting it as written. Detail: Phase 1 goal below.

### Later

None.

---

## Phase 1 — First cut: record inventory and file health

**Specification:** the approved design (status `approved`, Adam, 2026-11-23 23:23) is `docs/SPEC.md`, moved unchanged from the product office (`projects/sl23-cs-reader/design.md` at commit 0fda8139f5196e937ae63ca595c83311bf7f5f40). Its citations of `RULES.md`, `DECISIONS.md`, `policies/` and `projects/` name the product office's files as of 2026-11-23.

**Goal:** the `inventory` analysis of the approved design (its sections 3, 6 and 7) reads SL2 and SL3 files and reports what is in them and where they break. It is done when the report of design section 7.2 is produced for both formats and the expected figures for the public example files hold. (Clead's wording, not yet confirmed by Adam.)

- **[DONE] CS23-1** (Adam, 2026-11-23; done 2026-11-23 23:23; product office PO-60) — Design: Adam approved the high-level design and data format ("the design is approved"); Clead recorded the approval line and set its status to `approved`.
- **[NOW] CS23-2** (Adam, 2026-11-23; product office PO-60) — Repository: Adam created the repository with the bootstrapper (template v2.11.0, C# pack; setup PR #1 merged 2026-11-23). Open: the pointer in the product office's `PROJECTS.md` changes and its `projects/sl23-cs-reader/` folder goes (the files are in: #2, and the design in #3). The first cut runs inside PO-60's `NOW` slot and takes no sixth (Adam, 2026-11-23 22:45).

---

## Explicitly out of scope

- Wireshark capture files, the live sonar stream and the GoFree WebSocket, writing or changing SL2/SL3 files, interpreting fields whose meaning is not established, rendering pictures or video (design section 2).
