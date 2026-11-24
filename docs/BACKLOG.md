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
- **[DONE] CS23-2** (Adam, 2026-11-23; done 2026-11-24; product office PO-60) — Repository: Adam created the repository with the bootstrapper (template v2.11.0, C# pack; setup PR #1 merged 2026-11-23). The project's files moved in (#2, and the design as `docs/SPEC.md` in #3), and the product office's `PROJECTS.md` points here (product office #1164). The first cut runs inside PO-60's `NOW` slot and takes no sixth (Adam, 2026-11-23 22:45).
- **[NOW] CS23-3** (design section 8) — `THIRD-PARTY-NOTICES.md`: L (opensounder/python-sllib, `f54a27c`, MIT) and S (KennethTM/sonarlight, `f8fbb79`, MIT) as "learn from (format facts)", with version and licence; K (kmpm/node-sl2format) as not used. Done when the file is in the repository and the template's notices rule is met. Docs only, no test.
  - Open: the licence of L and S on their exact commits has to be checked before the file is written (third-party policy, Step 1). The first attempt (2026-11-24) could not read the two repositories from the workspace, and the web fetch of their licence pages was not approved. Until it is done, the file would repeat what design section 8 records from fomo-f's notices.
  - S checked (2026-11-24, session 59c4d690-badd-5031-b795-842edd894dde): the licence file of KennethTM/sonarlight reads MIT, Copyright (c) 2023 Kenneth Thorø Martinsen, on `f8fbb79` (version 0.1.7) and on `df834f0` (version 0.1.8, 2026-11-18); L not checked. The commit the notice names is open: `f8fbb79` predates the fix `7b5dd55` that fomo-f records (`docs/NEXT_SESSION.md`, section 9).
- **[DONE] CS23-4** (done 2026-11-24; PR #5) — Skeleton: the starter projects renamed (`src/Sl23.Reader`, `src/Sl23.Reader.Cli`, `tests/Sl23.Reader.Tests`), the starter greeter removed; the shell reports no command and an unknown command as a usage error (exit code 2, message and usage on standard error). The `Sl23.Reader` library project is empty until CS23-5; the shell does not reference it yet.
- **[DONE] CS23-5** (done 2026-11-24; PR #8) — File header and frame record (design 6.2, 7.1): the 8-byte file header parsed (format code 1/2/3; SLG and other values reported as such and not read further; a file under 8 bytes holds no header), the channel labels of 6.6, the unit conversions of 6.8 (feet to metres, Mercator to longitude and latitude, the signed reading of the time field), and the frame record of 7.1 with absent fields `null`, never guessed.
- **[DONE] CS23-6** (done 2026-11-24; PR #10) — SL2 frame reader (design 6.3 checks 1 to 3, 6.4): `Sl2FrameReader` reads one frame at a time from a seekable stream, 64-bit positions, echo skipped unless asked for; the chain breaks on a truncated tail (fewer than 36 bytes left), a wrong offset field (check 1) or a frame size below 144 or past the end of the file (check 2), reported as a `ChainBreak` with position, bytes left and reason; a wrong echo size (check 3) is a finding only: the frame is returned with the header size found.
- **[DONE] CS23-7** (done 2026-11-24; PR #12) — SL3 frame reader (design 6.3, 6.5): `Sl3FrameReader` derives the header size as frame size minus echo size; a full record only for 168 and 184 (previous frame offsets 128 to 151 included); any other size, 128 of channels 7 and 8 included, gives the reduced record (positions, sizes, channel, label), and a size that is not 128, 168 or 184 is a finding only. The chain breaks on a truncated tail (fewer than 48 bytes left), a wrong offset field (check 1) or a frame size below 128 or past the end of the file (check 2). Header and echo size of `FrameRecord` are 64-bit, as the SL3 echo size is a u32.
  - Sonar format findings are kept in fomo-f, not here (Adam, 2026-11-24 00:36). The check of sonarlight `df834f0` (version 0.1.8) against the 184-byte SL3 header is in fomo-f's `docs/NEXT_SESSION.md`, section 9 (fomo-t/fomo-f #151).
- **[DONE] CS23-8** (done 2026-11-24; PR #14) — Inventory tallies (design 6.3 checks 1 to 9, 7.2): `InventoryBuilder` tallies the frames of one file in file order into a `FileInventory` (layouts, frame count, where the chain ends, first and last time value in both readings with the count below zero as signed, the nine checks with passed, failed, first failing positions (at most 10, the rest counted) and, for check 7, pointers counted apart) and a `ChannelInventory` per channel (frame count, first and last frame index, header sizes, frame versions, distinct range-limit pairs (up to 4096, the rest counted), frames with a valid position, depth range, the checks 1 to 3 and 5 to 9). Check 9 applies to channels 0, 1, 2 and, in SL2, 10 and 11 (design 6.8). The lists that grow with the file are capped and the rest counted: failing positions (10), range-limit pairs (4096), header sizes and frame versions (64). It holds counts, never frames.
- **[DONE] CS23-9** (done 2026-11-24; PR #15) — Inventory text report with the privacy default (design 4, 7.2): `InventoryTextReport.Write` writes, per file, the header, frames, layouts, where the chain ends, the time counter in both readings, the nine checks with their first failing positions and what was left out, and per channel the frames, index, header sizes, versions, range-limit pair count, valid-position share and checks. By default it holds counts and structure only (no file name, depth or pair list; a file that cannot be read appears with its reason and without its path); `ReportOptions.IncludeDetails` adds file names, depths and the pairs. Numbers use the invariant culture.
- **[NEXT] CS23-10** — Inventory JSON report (design 4, 7.2): the same content as the text report.
- **[NEXT] CS23-11** — Command-line shell (design 4): `inventory`, one or more files or a folder (the `.sl2` and `.sl3` files directly in it), progress on standard error for large files, usage errors stop at once, an unreadable path is reported while the rest is processed, exit codes as design 4.
- **[NEXT] CS23-12** — Expected figures on the public sonarlight example files (design 7.2, 9): 10,304 frames (SL2); 10,250 frames (SL3: 5,124 with a 128-byte header, 5,126 with 168). Adam said yes to using a small excerpt of those files as test data (2026-11-24 00:23; copy mode, with sonarlight's licence text carried; its notice goes into `THIRD-PARTY-NOTICES.md`, CS23-3).

---

## Explicitly out of scope

- Wireshark capture files, the live sonar stream and the GoFree WebSocket, writing or changing SL2/SL3 files, interpreting fields whose meaning is not established, rendering pictures or video (design section 2).
