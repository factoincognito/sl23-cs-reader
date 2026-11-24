# Key Architectural Decisions

Key decisions, newest at the bottom of the table. Decisions that need
more than a table row get an ADR in `docs/decisions/`. The rules the
project works by are not kept here: they are the Rules in `rules/` and
the project's own rules in `RULES.md`.

| Date | Decision | Rationale |
|---|---|---|
| 2026-10-29 | SL2/SL3 first, in its own repository, bootstrapped with the C# pack. | Adam, in chat; the pack is shown to work with real code. |
| 2026-11-23 | The project and its repository are named sl23-cs-reader; its backlog prefix is `CS23`. | Adam, in chat, 23:06 and 23:08. |
| 2026-11-23 | The design is approved, with the recommendations of its section 10 read as accepted. The project's division is recorded in the product office's `DECISIONS.md`. | Adam, in chat, 23:23. |
| 2026-11-24 | The repository stays public and holds the design; the policy's release gate for the tool stays open (design point 10). | Adam, in chat, 00:06, "public", to Clead's question whether the public repository may hold the design. |
| 2026-11-24 | A small excerpt of the two public sonarlight example files (one SL2, one SL3, MIT) may be copied into the repository as test data, with sonarlight's licence text carried. Adam's own files are never committed. | Adam, in chat, 00:23, "yes", to Clead's question (design section 9). |
| 2026-11-24 | Sonar-related information is kept in fomo-f, not in this repository: this project is only a proof of concept of building C# projects. The sonarlight 0.1.8 result moved there; the CS23-3 licence line and design section 6.5 stay. | Adam, in chat, 00:36, and "apyr" to Clead's recommendation of 00:36. |
| 2026-11-24 | The repository's own licence is PolyForm Noncommercial 1.0.0, with the required-notice line "Copyright Adam Smolowicz", in a `LICENSE` file at the root (CS23-17). It covers the project's own code only; third-party files keep their licences. | Adam, in chat, 10:05, "apyr" to Clead's recommendation of 10:03. |
| 2026-11-24 | The analyser's intended use, in the third-party policy's categories, is internal tooling; a session asks Adam again before the tool ships or before non-permissive third-party material comes in. `THIRD-PARTY-NOTICES.md` is changed to say so (CS23-17). | Adam, in chat, 10:11, "yes internal tooling", to Clead's recommendation of 09:50. |

## Open questions

(none yet — number each open question, and strike it through with the
date and outcome when it is resolved)
