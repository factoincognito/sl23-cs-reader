# Changelog

All notable changes to sl23-cs-reader are documented here.

## [Unreleased]

- 2026-11-23: the project's files (README, STATUS, backlog, local rules, memory) fitted to the setup (#2).
- 2026-11-24: the approved design added as the specification, `docs/SPEC.md` (#3).
- 2026-11-24: the first cut's backlog items written (#4); Adam's decision on the sonarlight excerpt as test data recorded (#6); the result of the sonarlight 0.1.8 check recorded (#7).
- 2026-11-24: the starter projects replaced by the analyser's library, command-line shell and tests (#5).
- 2026-11-24: the file header and frame record (#8), the SL2 frame reader (#10), the SL3 frame reader (#12) and the inventory tallies with the nine health checks (#14), the inventory text report (#15), the inventory JSON report (#16) and the inventory command (#17) added to the library.
- 2026-11-24: the changelog lines for #4 to #7 added (#9); Adam's decision that sonar information is kept in fomo-f recorded, and the CS23-7 note on the sonarlight check turned into a pointer to fomo-f (#11).
- 2026-11-24: the follow-ups from the reviews of #15 to #17 recorded as CS23-13 (#18); the status and handover after the first cut written (#19).
- 2026-11-24: the third-party notices added, `THIRD-PARTY-NOTICES.md` (#20).
- 2026-11-24: tests of the analyser on the public sonarlight example files added, with the first 40 frames of each as test data (#21).
- 2026-11-24: the follow-ups from the reviews of #20 and #21 recorded as CS23-16 (#22); this line added (#23).
- 2026-11-24: the decision on the repository's own licence recorded and CS23-17 added (#24); this line added (#25).
- 2026-11-24: the decision on the analyser's intended use (internal tooling) recorded (#26); this line added (#27).
- 2026-11-24: the request to run the analyser from VS Code recorded as CS23-18 (#28); this line added (#29).
- README: one sentence says the project was developed with the assistance of AI (#30).
- 2026-11-24: the repository's own licence added and the intended use set to internal tooling (#31); a VS Code build task and a launch entry for the `inventory` command added (#32), not yet run in VS Code on Windows.
- 2026-11-24: follow-ups of the review notes of #15 to #17 (CS23-13): the JSON report's handling of an unpaired surrogate in a file name pinned by a test and the escaping noted (#33); `1 byte` and `Layouts: none` in the text report (#34); the shell keeps the input order, escapes control characters in what it echoes and tests a folder that cannot be listed (#35). The check 6 finding on SL2 channels 10 and 11 recorded in fomo-f (CS23-15).
- 2026-11-24: the JSON report gets a schema version field, `"schemaVersion": 1` as its first property (CS23-20, #38); this line added (#39).
- 2026-11-24: the decision on the shell's choices and on check 9 for SL2 channels 10 and 11 recorded, and the goal wording marked as confirmed (#40).
- 2026-11-24: Adam's first run of the `inventory` command on one of his own SL2 files recorded in `STATUS.md`, and the missed "not yet confirmed" mark of the Phase 1 definition in `memory/project.md` removed (#41).

## Project created

- Bootstrapped from the Clead + Crog project template
  v2.11.0, built from
  factoincognito/ai-project-template-v2@721f49fc18145f174f671724d4208febbd5d5c7b.
- Language pack: csharp.
- Licence: none.
- Set up by bootstrap-project.sh.
