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
- Decide the intended use of the analyser in the third-party policy's categories (commercial product, internal tooling, or personal or hobby). `THIRD-PARTY-NOTICES.md` assumes commercial product, the strictest, until you do. Clead recommends keeping commercial product: the project is Sonatory's, and the MIT and Apache-2.0 licences involved allow it, so nothing changes today. Detail: `THIRD-PARTY-NOTICES.md`, "Intended use".

### Later

None.

---

## Phase 1 — First cut: record inventory and file health

**Specification:** the approved design (status `approved`, Adam, 2026-11-23 23:23) is `docs/SPEC.md`, moved unchanged from the product office (`projects/sl23-cs-reader/design.md` at commit 0fda8139f5196e937ae63ca595c83311bf7f5f40). Its citations of `RULES.md`, `DECISIONS.md`, `policies/` and `projects/` name the product office's files as of 2026-11-23.

**Goal:** the `inventory` analysis of the approved design (its sections 3, 6 and 7) reads SL2 and SL3 files and reports what is in them and where they break. It is done when the report of design section 7.2 is produced for both formats and the expected figures for the public example files hold. (Clead's wording, not yet confirmed by Adam.)

- **[DONE] CS23-1** (Adam, 2026-11-23; done 2026-11-23 23:23; product office PO-60) — Design: Adam approved the high-level design and data format ("the design is approved"); Clead recorded the approval line and set its status to `approved`.
- **[DONE] CS23-2** (Adam, 2026-11-23; done 2026-11-24; product office PO-60) — Repository: Adam created the repository with the bootstrapper (template v2.11.0, C# pack; setup PR #1 merged 2026-11-23). The project's files moved in (#2, and the design as `docs/SPEC.md` in #3), and the product office's `PROJECTS.md` points here (product office #1164). The first cut runs inside PO-60's `NOW` slot and takes no sixth (Adam, 2026-11-23 22:45).
- **[DONE] CS23-3** (design section 8; done 2026-11-24; PR #20) — `THIRD-PARTY-NOTICES.md`: L (opensounder/python-sllib, `f54a27c`, MIT) and S (KennethTM/sonarlight, `f8fbb79`, MIT) as "learn from (format facts)", with version and licence; K (kmpm/node-sl2format) as not used; the two packages `Directory.Packages.props` pins as dependencies.
  - The licences were read on their exact commits: L by Adam (2026-11-24 08:57) and by the session through git; S through git at `f8fbb79` and `df834f0`, the same file at both. The notice names `f8fbb79` for S, the commit the design was written from.
- **[DONE] CS23-4** (done 2026-11-24; PR #5) — Skeleton: the starter projects renamed (`src/Sl23.Reader`, `src/Sl23.Reader.Cli`, `tests/Sl23.Reader.Tests`), the starter greeter removed; the shell reports no command and an unknown command as a usage error (exit code 2, message and usage on standard error). The `Sl23.Reader` library project is empty until CS23-5; the shell does not reference it yet.
- **[DONE] CS23-5** (done 2026-11-24; PR #8) — File header and frame record (design 6.2, 7.1): the 8-byte file header parsed (format code 1/2/3; SLG and other values reported as such and not read further; a file under 8 bytes holds no header), the channel labels of 6.6, the unit conversions of 6.8 (feet to metres, Mercator to longitude and latitude, the signed reading of the time field), and the frame record of 7.1 with absent fields `null`, never guessed.
- **[DONE] CS23-6** (done 2026-11-24; PR #10) — SL2 frame reader (design 6.3 checks 1 to 3, 6.4): `Sl2FrameReader` reads one frame at a time from a seekable stream, 64-bit positions, echo skipped unless asked for; the chain breaks on a truncated tail (fewer than 36 bytes left), a wrong offset field (check 1) or a frame size below 144 or past the end of the file (check 2), reported as a `ChainBreak` with position, bytes left and reason; a wrong echo size (check 3) is a finding only: the frame is returned with the header size found.
- **[DONE] CS23-7** (done 2026-11-24; PR #12) — SL3 frame reader (design 6.3, 6.5): `Sl3FrameReader` derives the header size as frame size minus echo size; a full record only for 168 and 184 (previous frame offsets 128 to 151 included); any other size, 128 of channels 7 and 8 included, gives the reduced record (positions, sizes, channel, label), and a size that is not 128, 168 or 184 is a finding only. The chain breaks on a truncated tail (fewer than 48 bytes left), a wrong offset field (check 1) or a frame size below 128 or past the end of the file (check 2). Header and echo size of `FrameRecord` are 64-bit, as the SL3 echo size is a u32.
  - Sonar format findings are kept in fomo-f, not here (Adam, 2026-11-24 00:36). The check of sonarlight `df834f0` (version 0.1.8) against the 184-byte SL3 header is in fomo-f's `docs/NEXT_SESSION.md`, section 9 (fomo-t/fomo-f #151).
- **[DONE] CS23-8** (done 2026-11-24; PR #14) — Inventory tallies (design 6.3 checks 1 to 9, 7.2): `InventoryBuilder` tallies the frames of one file in file order into a `FileInventory` (layouts, frame count, where the chain ends, first and last time value in both readings with the count below zero as signed, the nine checks with passed, failed, first failing positions (at most 10, the rest counted) and, for check 7, pointers counted apart) and a `ChannelInventory` per channel (frame count, first and last frame index, header sizes, frame versions, distinct range-limit pairs (up to 4096, the rest counted), frames with a valid position, depth range, the checks 1 to 3 and 5 to 9). Check 9 applies to channels 0, 1, 2 and, in SL2, 10 and 11 (design 6.8). The lists that grow with the file are capped and the rest counted: failing positions (10), range-limit pairs (4096), header sizes and frame versions (64). It holds counts, never frames.
- **[DONE] CS23-9** (done 2026-11-24; PR #15) — Inventory text report with the privacy default (design 4, 7.2): `InventoryTextReport.Write` writes, per file, the header, frames, layouts, where the chain ends, the time counter in both readings, the nine checks with their first failing positions and what was left out, and per channel the frames, index, header sizes, versions, range-limit pair count, valid-position share and checks. By default it holds counts and structure only (no file name, depth or pair list; a file that cannot be read appears with its reason and without its path); `ReportOptions.IncludeDetails` adds file names, depths and the pairs. Numbers use the invariant culture.
- **[DONE] CS23-10** (done 2026-11-24; PR #16) — Inventory JSON report (design 4, 7.2): `InventoryJsonReport.Write` writes one indented JSON document with the content of the text report (files, layouts, chain, time counter, the nine checks, channels, the counts of what the caps left out); camelCase names, reasons as words, unknown values `null`. The default holds no file name, depth or pair list; `ReportOptions.IncludeDetails` adds them, as for the text report.
- **[DONE] CS23-11** (done 2026-11-24; PR #17) — Command-line shell (design 4): `sl23-cs-reader inventory [--json] [--details] <path>...` takes files and folders (the `.sl2` and `.sl3` files directly in a folder, by name), reads each with `FileAnalyser`, writes the text or JSON report to standard output and progress ("File k of n: p% read", no name) to standard error for files of 64 MiB and more. An unknown option or no input stops at once (exit 2); an unreadable path or a folder without sonar files is reported and the rest is processed (exit 1); findings inside files, SLG, unknown and too-short files do not change the exit code (0).
- **[DONE] CS23-12** (done 2026-11-24; PR #21) — Expected figures on the public sonarlight example files (design 7.2, 9): 10,304 frames (SL2); 10,250 frames (SL3: 5,124 with a 128-byte header, 5,126 with 168). The analyser gives them on the two whole files, which Adam gave on 2026-11-24; no code under `src/` changed. Adam said yes to using a small excerpt of those files as test data (2026-11-24 00:23): the first 40 frames of each are in `tests/Sl23.Reader.Tests/TestData/sonarlight/` in copy mode, with sonarlight's licence text carried and their notice in `THIRD-PARTY-NOTICES.md`.
  - The whole files (21 MB each) are not in the repository. The two tests of the design's figures read them from the folder that the environment variable `SL23_SONARLIGHT_EXAMPLES` names and are skipped without it, so CI runs the excerpt tests only.
- **[NEXT] CS23-13** — Follow-ups from the non-blocking notes of Crog's reviews of #15 to #17. Not started unless noted; any may be dropped if Adam does not want it.
  - Done: a test that a frame without a valid position is not counted in the JSON report (a mutation survived).
  - JSON report: a file name with an unpaired surrogate makes `Utf8JsonWriter` throw under `--details`; no schema or version field; `frameVersions` is always present, unlike in the text report; the relaxed escaping must not be embedded in HTML.
  - Text report: "1 bytes" in the layouts and header-size lists; range-limit pairs print at two decimals, so pairs closer than 0.005 m look alike under `--details`; an empty layouts list leaves "Layouts: " with nothing after it; Windows line endings are not tested.
  - Shell: folder-listing failures come first in the report whatever their place among the inputs; `Unknown option: {arg}` echoes the argument; the `NotSupportedException` branch of the progress setup only runs on Windows; a folder that cannot be listed is not tested.
- **[LATER] CS23-14** (Clead, 2026-11-24, from CS23-3) — Third-party material that `THIRD-PARTY-NOTICES.md` does not list, with licences not read: the packages that `xunit.v3` and `coverlet.MTP` bring in, the .NET SDK and runtime, and the GitHub Actions of `.github/workflows/`. They come with the template's C# pack, so the answer may belong in the template. Decide whether they are listed, here or there.
- **[NEXT] CS23-15** (Clead, 2026-11-24, from CS23-12) — A finding on the public SL2 example goes to fomo-f: health check 6 (the frame index follows the previous one of the channel) fails on 1,288 of the 2,576 frames of each of channels 10 and 11, because each frame index occurs twice there; every other check passes on both whole examples. Sonar format findings are kept in fomo-f (Adam, 2026-11-24 00:36), so a session with push access to fomo-t/fomo-f records it there through fomo-f's flow. Whether check 6 then treats those two channels differently is a change to design 6.3 and Adam's to decide.
- **[LATER] CS23-16** (Clead, 2026-11-24) — Follow-ups from the non-blocking notes of Crog's reviews of #20 and #21. Not started; any may be dropped if Adam does not want it.
  - The two tests of the design's figures on the whole sonarlight examples never run in CI (CS23-12), so a later regression of those figures would not turn CI red. A CI step that fetches the two files at the pinned commit and checks their SHA-256 would close it, at the price of a 42 MB download and a network dependency in `build`.
  - `SonarlightExampleTests.ReadWholeExample`: a SHA-256 mismatch fails without naming the file; a value of `SL23_SONARLIGHT_EXAMPLES` that is only white space counts as set and fails with "folder does not hold".
  - The whole-file tests do not assert, as the excerpt test does, that the 128-byte headers are on channels 7 and 8 only.
  - When Adam decides the intended-use category ("Waiting for the product owner"), `THIRD-PARTY-NOTICES.md` and that line are updated together.
- **[NEXT] CS23-17** (Adam, 2026-11-24 10:05, "apyr", `memory/decisions.md`) — The repository's own licence: a `LICENSE` file at the root with the PolyForm Noncommercial 1.0.0 text (the template ships it as `licenses/PolyForm-Noncommercial-1.0.0.md`) and the line "Required Notice: Copyright Adam Smolowicz". It covers the project's own code only; the sonarlight excerpts and the other third-party files keep their licences (`THIRD-PARTY-NOTICES.md`). `licenses/NOTICE` stays, as the template's. The README says in one line which licence applies to what.

---

## Explicitly out of scope

- Wireshark capture files, the live sonar stream and the GoFree WebSocket, writing or changing SL2/SL3 files, interpreting fields whose meaning is not established, rendering pictures or video (design section 2).
