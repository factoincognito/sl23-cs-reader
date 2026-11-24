# Third-party notices

Third-party material this project uses or draws on, as
`rules/policies/third-party-ip.md` requires (rule 5 and the merge gate).
No third-party file is copied into this repository.

The template's files (`licenses/NOTICE`) are Adam's own material and not
third-party (the policy, "Scope and status").

## Intended use

Category assumed for everything in this file: **commercial product**, the
strictest of the policy's three (Appendix A.1, step 3). Adam has not decided
the category. The design calls the analyser a proof of concept
(`docs/SPEC.md`, section 8) and leaves open whether it stays private
(section 10, point 10), and the project belongs to the Sonatory division; so
the stricter category is used, as Appendix A.1 says for a case that is not
clear. Nothing is shipped today, and the policy's release gate comes before
anything ships.

## SL2/SL3 format facts

The analyser reads Lowrance SL2 and SL3 sonar logs. Mode: **learn from
(format facts)**. The layout in `docs/SPEC.md`, section 6, takes offsets,
types, flag bits and channel names from the two sources below. They reached
the design through the SL2/SL3 format document of fomo-f, one of Adam's
repositories (`docs/SL2_SL3_FORMAT.md` at fomo-f
`ff5f0714a78df7892526cc1ac0d0507e399ce55b`), and stand there in the design's
own words and tables. No file of either source is in this repository. Both
licences are permissive, so no clean room was needed (the policy,
Appendix A.2).

| Key | Source | Version | Licence as found | What is drawn on |
|---|---|---|---|---|
| L | [opensounder/python-sllib](https://github.com/opensounder/python-sllib), `sllib/definitions.py` | commit `f54a27c76667c4eb83340a60b378e78b5e6dce71` (2021-10-08; six commits after the tag `v0.2.3`) | MIT, "Copyright (c) 2019-2021 Peter Magnusson <me@kmpm.se>" | offsets, types, flag bits |
| S | [KennethTM/sonarlight](https://github.com/KennethTM/sonarlight), `sonarlight/sonar_class.py` | commit `f8fbb79ed59c3e508bf7595598b2c919293d2d15` (2025-03-19; version 0.1.7) | MIT, "Copyright (c) 2023 Kenneth Thorø Martinsen" | offsets, types, channel names |

How the licences were checked:

- L: the `LICENSE` file at that commit, read by Adam from the repository
  (2026-11-24 08:57) and by a Clead session through git on the same day.
- S: the `LICENSE` file, read through git on 2026-11-24 at `f8fbb79` and at
  `df834f0d0343a9b0ce18aaf6e8bc8594af368b74` (2026-11-18; version 0.1.8). It
  is the same file at both (git blob
  `bdd4b2f30b0c80579d331618e8aaa037194487d8`). The design was written from S
  at `f8fbb79`, so that is the commit named above; the findings on version
  0.1.8 are kept in fomo-f, not here.

Not checked for this file: a comparison of the code under `src/` with the
code of L and S. The backlog records that code as written to
`docs/SPEC.md`.

## Not used

| Key | Source | Licence | Use |
|---|---|---|---|
| K | [kmpm/node-sl2format](https://github.com/kmpm/node-sl2format), commit `247d5f1` | No `LICENSE` file and no licence text; MIT is named in `package.json` and the README only (as `docs/SPEC.md`, section 8, records it; not read again for this file) | Nothing is taken from it. Every field the design uses stands on L, S or observation (`docs/SPEC.md`, section 8). |

## Build and test dependencies

They build and test the analyser (category: as under "Intended use"; both
licences allow that use). Mode: **depend**,
unmodified, restored from NuGet. Only the test project references them, so
they are in no output of `src/`.

| Package | Version | Licence (package metadata) | Source named by the package |
|---|---|---|---|
| `xunit.v3` | 4.0.2 | Apache-2.0, "Copyright (C) .NET Foundation" | https://github.com/xunit/xunit, commit `0c7aa4991fa340689155d52043ce88f52f2f7421` |
| `coverlet.MTP` | 10.1.0 | MIT, "(c) 2018 Toni Solarin-Sodara" | https://github.com/coverlet-coverage/coverlet, commit `10a8051216eeaebb661624eef348ee768693fed9` |

These are the two packages `Directory.Packages.props` pins; their licences
were read from the metadata of the restored packages on 2026-11-24. Not
listed, and not read for this file: the packages those two bring in, the
.NET SDK and runtime, and the GitHub Actions that the workflows in
`.github/workflows/` use (CS23-14).
