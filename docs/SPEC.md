# sl23-cs-reader (SL2/SL3 log analyser): High-Level Design and Data Format

Status: `approved`. Approved by Adam, 2026-11-23 23:23, in chat: "the design is approved." Clead reads this as approving the design with its proposals and the recommendations of section 10 as written; Adam can overrule any point. Where the text says Proposal, it is Clead's proposal that this approval confirms (`RULES.md` § 7 (3)). The name sl23-cs-reader is Adam's (2026-11-23 23:06), and the division is Sonatory (Adam, 2026-11-23 23:23).

Written 2026-11-23, carrying out Adam's decisions of 22:32 to 22:45 that day (`DECISIONS.md`): the design starts before the repository exists; it is a high-level design and a data format only, without implementation details, written from fomo-f's technical product specification (TPS) and its SL2/SL3 format document; it keeps fomo-f's decoder internals out and the SL2/SL3 byte format in. It lives in `projects/sl23-cs-reader/` of the product office (Adam, 2026-11-23 23:06) until Adam has created the repository with the bootstrapper, and then moves into that repository (`RULES.md` § 23 (2)). Template v2.11.0 was published by Adam on 2026-11-23 (22:15, `DECISIONS.md`), so the bootstrapper can create the repository now. Adam decided on 2026-10-29: SL2/SL3 first, in its own repository, bootstrapped with the C# pack.

## 1. Purpose

1. **Prove the template's C# pack with real code** (PO-60, template PBI-1.23). This is the main purpose; what counts as "the pack works" is defined by PBI-1.23 (done on the strength of Adam's end-to-end run; Miro's test stays with PO-60), not by this design. This design only has to give the pack real work: a binary format to read, tests, a command line, CI.
2. **Be of use**, as the second purpose (Adam, 2026-10-29): show us something we can use that is in the dark today, and that cannot be had by passing the file to Claude. Adam's 14 SL2/SL3 files sit on his disk and all but one (1 KB) are 88 MB to 2.1 GB, so Claude cannot read them; a tool can, and can hand Claude a small report.

## 2. Scope

**In (first cut onward):** read one or more `.sl2` or `.sl3` files from disk, read-only, in one pass each, and report what is in them.

**Out:**
- Wireshark capture files (the idea names them as a later step; separate decision).
- The live sonar stream and the GoFree WebSocket: not SL2/SL3 (fomo-f TPS section 2.1, with its correction of 2026-10-20). The analyser never touches the network.
- Writing or changing SL2/SL3 files.
- Fields whose meaning is not established (section 6.9): they are reported as raw values, never interpreted.
- Rendering pictures or video (fomo-f has a replay tool).
- How fomo-f's Python decoder works (section 9).

**Open (Adam's WHAT):** how the analyser relates to fomo-f's decoder: the decoder stays and the analyser only reports, or the analyser later replaces the decoder for files (point 3 of section 10).

## 3. What the analyser tells us

Four analyses, in the order Clead proposes building them. Each is an analysis over the same stream of frames; one command runs one analysis or several in one pass (section 4).

| Cut | Analysis | What it shows | What is in the dark today (fomo-f's format document, its section 11, and its notes) |
|---|---|---|---|
| **1** (Proposal: the first cut) | **Inventory and file health** | Per file: size, the file header values, the frame layouts found, frames per channel, first and last time values, whether the chain of frames ends exactly at the end of the file or breaks (where, how many bytes are left), and the result of each health check in section 6.3 | Whether real files can be read at all, and where they break: fomo-f's format document (checked against `e452e76`, 2026-11-05) records its decoder as run on one SL2 and one SL3 of Adam's 14 files, and not on the other 12 |
| 2 (Proposal) | **Depth and position track** | Per file: share of frames with a valid position, area covered, depth range, speed and course from the file | What the data collection passes (fomo-f PBI-3.3a) cover: how much of it is collected is not recorded |
| 3 (Proposal) | **Echo export** | The echo columns of one channel written out as plain bytes with a small description file (range limits per column), nothing converted | The input fomo-f PBI-3.1 and 3.2 wait for |
| 4 (Proposal, new, not discussed with Adam) | **Undecoded-bytes survey** | For header bytes not established (section 6.9), per channel: constant or varying, which values, how many | Where the frequency code sits, what the 16 extra bytes of the 184-byte SL3 header hold, how channels 7 and 8 and SL2 channels 10 and 11 are laid out |

What the first cut can settle, from the open items of fomo-f's format document:

- Which header layouts occur in each file, and whether a file mixes layouts (a mix is not flagged today).
- Whether the time field is signed: how many frames in how many files hold values above 2^31, and where in the file.
- Which channels occur: whether sidescan channels 3 and 4 occur anywhere, whether SL2 channels 10 and 11 occur on the other 12 files, and how many frames SL3 channels 7 and 8 hold.
- The distinct range-limit pairs per channel, which the format document records only for six files.
- Whether the invariants of section 6.3 hold on the files not yet examined.
- Whether any SL3 file is a "version 1" file in L's sense (L derives the echo length differently when flag 0x0008 is unset; never seen in any file), and which frame versions occur beyond 10 and 14.

**Proposal for the first cut:** inventory and file health only (Clead's recommendation of 2026-11-23 22:32 to 22:33, not yet confirmed by Adam). It needs no echo bytes, only header fields, the unit conversion and the two readings of the time field, so the tool is small, and it is the step that shows whether the real files can be read at all.

## 4. How the tool behaves

All Proposal until Adam approves the design.

- **Command line**, one command per analysis (`inventory` first), and a way to run several analyses in one pass. The names are placeholders. Input: one or more file paths; a folder is accepted and means the `.sl2` and `.sl3` files directly in it (not below it).
- **Output:** a readable text report on standard output; the same report as JSON on request; export files only where an analysis writes them, to a path the user names. Nothing is written next to or over an input file. The input is never modified.
- **A damaged file is a finding, not a crash.** The report says what was found, where the chain of frames broke and how many bytes were left, and the tool goes on to the next file. A file that cannot be opened is reported with the reason, and the other files are still read. How damaged files are treated beyond that (stop at the break, or search for the next frame) is point 4 of section 10.
- **One pass, bounded memory.** Files reach 2.1 GB, so frames are read one at a time, the echo bytes are skipped unless an analysis needs them, and several analyses can share one pass. Lists that grow with the file (distinct range-limit pairs, index gaps, first failing positions) are capped per file and channel, and the report says how many were left out. Progress goes to standard error for large files.
- **Local and offline.** No network.
- **Platforms:** Windows 11 and macOS, the systems of Adam's machines (fomo-f TPS section 2.3). The tool follows the C# pack's .NET SDK (the .NET 10 SDK, `DECISIONS.md`, 2026-11-23).
- **Private by default.** Adam's files hold positions: fishing places (`RULES.md` § 19 (2)); and their names carry the start time of the recording. The default report has structure and counts only: no position, no depth, no clock time, no file name or path (files appear as "file 1 of n" with their size). Time values in it are counters from the start of the recording, not clock time. So the default report can be pasted into a chat or committed. Positions, depths, file names and the full list of range-limit pairs appear only with an explicit option, and such a report is not committed to any repository.
- **Errors and exit codes:** a usage error (an unknown option, no input) stops the tool at once with a clear message and a non-zero exit code. A path that cannot be read is reported, the other files are still processed, and the exit code at the end is non-zero. Findings inside files (a broken chain, a failed check) do not change the exit code, so a script can tell "the tool failed" from "the file is damaged".

## 5. Structure

```
 file paths
     |
     v
 [ File reader ]       the SL2/SL3 byte format (section 6); hands over one frame at a time
     | frames, in file order
     v
 [ Analyses ]          inventory | track | echo export | survey; each keeps its own tallies
     | results
     v
 [ Report writers ]    text | JSON | export files

 [ Command-line shell ] arguments, wiring, exit codes (around all of the above)
```

- The **file reader** is the only part that knows bytes. It reads the file header, picks the frame layout from it, does checks 1 to 3 of section 6.3 (they decide where the chain breaks), and hands over frames as the record of section 7.1. It also reports where framing broke.
- An **analysis** sees records, never the file; the raw header bytes are in a record only when an analysis asks for them (the survey). Check 4 comes from the reader (where the chain ends), and checks 5 to 9 are done by the inventory from the records. Adding an analysis means adding one component; none touches the reader.
- **Report writers** turn results into text or JSON. The report content is section 7.2.

## 6. Data format: the SL2/SL3 file

Navico/Lowrance sonar logs recorded to SD card. Nothing here is a vendor specification; there is none. The layout below is what the sources and the examined recordings support, and each field says how well. Integers and floats are **little-endian**; `f32` is an IEEE 754 single; `u16`, `u32` are unsigned and `i32` signed. An "offset" is a byte offset from the start of the frame header, unless it says "file".

### 6.1 Evidence keys

| Key | Meaning |
|---|---|
| **L** | opensounder/python-sllib, `sllib/definitions.py` (MIT) |
| **S** | KennethTM/sonarlight, `sonarlight/sonar_class.py` (MIT) |
| **PUB** | The two public example recordings of sonarlight (one SL2, one SL3): framing checked from the first byte to the end of the file |
| **ADAM** | The recordings from Adam's own unit, as fomo-f examined them; only counts and structural findings are used here, no value from them |
| **A-SL3** | The one SL3 recording of Adam's unit (184-byte header) that fomo-f's decoder read completely, with the consistency checks of fomo-f's format document, sections 3 and 5.2 |
| **NAV** | fomo-f's navigation-field check of 2026-11-05: the time, GPS speed, course and heading fields of channel 0, read by a one-off script from 12 of Adam's recordings (8 SL2, 4 SL3) |
| **RANGE** | The range-limit survey in section 7.5 of fomo-f's format document, here only its four files that are not K's: the sonarlight pair and Adam's SL2 and SL3 |

A field marked "not established" is not interpreted by any analysis. The inventory (cut 1) reports some of them as raw values (the file-header words, the frame versions, the raw flags), and the survey (cut 4) reports the raw bytes of the rest.

The field names in the tables are this design's own; the sources name some fields differently, and the table says where.

### 6.2 File header (8 bytes, SL2 and SL3)

| Offset | Size | Type | Field | Meaning | Evidence |
|---:|---:|---|---|---|---|
| 0 | 2 | u16 | file format code | 1 = SLG, 2 = SL2, 3 = SL3. **The frame layout is taken from this field, not from the file extension.** An SLG file, or any other value, is reported as such and not read further (S calls this field `version`) | S, PUB |
| 2 | 2 | u16 | header word 2 | Meaning not established. Differs between the public SL3 sample and Adam's SL3 (S calls this field `device_id`) | S, ADAM |
| 4 | 2 | u16 | header word 4 | Not established; reported raw | S |
| 6 | 2 | u16 | header word 6 | Not established; reported raw | S |

Frames start at file byte 8. A file shorter than 8 bytes holds no frames.

### 6.3 Framing and health checks

A frame is a **frame header** followed by the **echo**: one unsigned byte per sample, one echo column of one channel. The header size is fixed for SL2 (144 bytes) and not fixed for SL3 (section 6.5). There is no separator and no magic value between frames. The **frame size** field holds header plus echo, and every frame begins with its own **offset in the file** (counted from the start of the file, file header included). The next frame starts at (this frame's position + its frame size). Positions and sizes are held as 64-bit integers: the offset field is a u32, so it cannot say more than 4 GiB, and files reach 2.1 GB.

**Where the chain breaks (Proposal).** Reading a frame needs at least 36 bytes (SL2: the echo size ends at offset 36) or 48 bytes (SL3: it ends at offset 48) left in the file. The chain breaks at a frame when fewer bytes are left (a truncated tail), when check 1 fails, or when check 2 fails. The report gives the position of that frame and the bytes left, and reading of that file stops (point 4 of section 10). A failure of check 3 is a finding only: the frame is counted, the header size found is recorded, and the chain goes on from the frame size.

Health checks, each a count per file and channel in the inventory. Checks 1 to 3 are applied while reading, check 4 is the outcome of the chain, and checks 5 to 9 are further invariants. All were **seen to hold on the files examined so far; none is guaranteed**, and testing them on the other files is part of what the inventory is for:

| # | Check | Seen to hold (evidence) |
|---|---|---|
| 1 | The offset field of a frame equals the frame's real position in the file | PUB; ADAM |
| 2 | The frame size is at least the smallest header (SL2: 144; SL3: 128), and the frame fits in the rest of the file | PUB; ADAM |
| 3 | SL2: the echo size equals frame size minus 144. SL3: frame size minus echo size is 128 on channels 7 and 8, and 168 or 184 on the channels seen (0, 2 and 5; section 6.5) | PUB; ADAM |
| 4 | The chain of frame sizes ends exactly at the end of the file | PUB; ADAM (one SL2 and one SL3) |
| 5 | The "previous frame size" field equals the frame size of the frame before it in the file (the first frame is not tested) | one SL3 file of ADAM, on every frame |
| 6 | The frame index of a frame is that of the previous frame of the same channel plus 1 (the first frame of a channel is not tested; its index is reported) | one SL3 file of ADAM |
| 7 | Each of the five "previous frame offset" values at bytes 128 to 147 of an SL3 header (section 6.5) equals the position of the latest earlier frame of the channel it names; pointer i names channel id i, in S's order (Proposal: the source lists the names only). The pointer to a frame's own channel is not tested, and a pointer to a channel not yet seen is counted apart, not as a failure (what the source did with these is not recorded). The sixth pointer, bytes 148 to 151, was not checked | PUB (SL3 sample) and one SL3 file of ADAM, every cross-channel pointer |
| 8 | **Proposal:** within a channel, in file order, the time field does not decrease, read as a signed i32. The source observed only that the raw value does not decrease within a recording; NAV shows that the first values of channel 0 read near zero as signed, while 2 of the 12 start just below 2^32 as unsigned (section 6.8). The signed reading and the per channel scope are this design's | PUB and A-SL3 (raw value, within a recording); NAV (first values) |
| 9 | Each frame's depth lies between its two range limits (vertical channels only, not sidescan) | RANGE |

### 6.4 SL2 frame header: 144 bytes

The echo starts at offset 144. The echo size (offset 34) must equal frame length minus 144.

| Offset | Size | Type | Field | Unit and meaning | Evidence |
|---:|---:|---|---|---|---|
| 0 | 4 | u32 | frame offset | bytes, in the file | L, S |
| 4 | 24 | bytes | not established | L reads six u32 (the last frame of each channel); S reads this region differently. Sources disagree | L vs S |
| 28 | 2 | u16 | frame size | bytes, header plus echo | L, S |
| 30 | 2 | u16 | previous frame size | bytes | L, S |
| 32 | 2 | u16 | channel | section 6.6 | L, S |
| 34 | 2 | u16 | echo size | bytes | L, S |
| 36 | 4 | u32 | frame index | | L, S |
| 40 | 4 | f32 | upper limit | feet; top of the echo column (section 6.8) | L, S |
| 44 | 4 | f32 | lower limit | feet; bottom of the echo column | L, S |
| 50 | 1 | u8 | frequency (L) | not established: S reads a u16 at 53; the value at neither place matched the channel on the real files examined | L vs S, ADAM |
| 60 | 4 | u32 | hardware time | S only: seconds; not verified | S |
| 64 | 4 | f32 | water depth | feet (section 6.8) | L, S |
| 68 | 4 | f32 | keel depth | not established | L |
| 100 | 4 | f32 | GPS speed | knots; checked on straight runs (section 6.8) | L, S, NAV |
| 104 | 4 | f32 | water temperature | unit not established | L, S |
| 108 | 4 | i32 | x | Mercator metres (section 6.8) | L, S, PUB |
| 112 | 4 | i32 | y | Mercator metres | L, S, PUB |
| 116 | 4 | f32 | water speed | 0.0 in every channel-0 frame of the two recordings checked | L, S, ADAM |
| 120 | 4 | f32 | course over ground | radians, true north; checked on straight runs (section 6.8) | L, S, NAV |
| 124 | 4 | f32 | altitude | unit not established; not checked on a real file | L, S |
| 128 | 4 | f32 | heading | radians; the value on Adam's unit is not reliable | L, S, ADAM |
| 132 | 2 | u16 | flags | section 6.7 | L |
| 134 | 6 | bytes | not established | | none |
| 140 | 4 | u32 | time | milliseconds since the recording started; sign not established (section 6.8) | L, S, ADAM |

Bytes 48 to 49, 51 to 59 and 72 to 99 are not established.

### 6.5 SL3 frame header: 168, 184 or 128 bytes

**The header size is not fixed.** It is frame size minus the echo size, the echo size being the u32 at offset 44. Sizes seen: **168** (the public SL3 sample), **184** (Adam's unit), and **128** on channels 7 and 8 in both. Any other value is a finding (section 6.3, check 3). The echo starts at the header size. The rows up to offset 127 hold for the 168 and the 184 header alike (seen as consistency in one SL3 file of Adam's, not documented); the rows from offset 128 on are the 168-byte layout, except that bytes 128 to 147 were also seen unshifted in the 184-byte header.

| Offset | Size | Type | Field | Unit and meaning | Evidence |
|---:|---:|---|---|---|---|
| 0 | 4 | u32 | frame offset | bytes, in the file | L, S |
| 4 | 4 | u32 | frame version | not established; 10 in the public sample, 14 in Adam's SL3 | S, ADAM |
| 8 | 2 | u16 | frame size | bytes, header plus echo | L, S |
| 10 | 2 | u16 | previous frame size | bytes | L, S |
| 12 | 2 | u16 | channel | section 6.6 (L reads a u32 here; the values agree for channels 0 to 8) | L, S |
| 14 | 2 | bytes | not established | S reads an i16 here | S |
| 16 | 4 | u32 | frame index | | L, S |
| 20 | 4 | f32 | upper limit | feet (section 6.8) | L, S |
| 24 | 4 | f32 | lower limit | feet | L, S |
| 28 | 12 | bytes | not established | | none |
| 40 | 4 | u32 | hardware time | not verified (S: seconds; L: "created at") | L, S |
| 44 | 4 | u32 | echo size | bytes | L, S |
| 48 | 4 | f32 | water depth | feet | L, S |
| 52 | 2 or 4 | u16 (S) / u32 (L) | frequency | not established: offset agrees, width differs, no check recorded | L, S |
| 56 | 28 | bytes | not established | | none |
| 84 | 4 | f32 | GPS speed | knots; checked on straight runs (section 6.8) | L, S, NAV |
| 88 | 4 | f32 | water temperature | unit not established | L, S |
| 92 | 4 | i32 | x | Mercator metres | L, S, PUB, ADAM |
| 96 | 4 | i32 | y | Mercator metres | L, S, PUB, ADAM |
| 100 | 4 | f32 | water speed | 0.0 in every channel-0 frame of the two recordings checked | L, S, ADAM |
| 104 | 4 | f32 | course over ground | radians, true north; checked on straight runs (section 6.8) | L, S, NAV |
| 108 | 4 | f32 | altitude | feet per S; not checked on a real file | L, S |
| 112 | 4 | f32 | heading | radians; not reliable on Adam's unit | L, S, ADAM |
| 116 | 2 | u16 | flags | section 6.7; the position flag is set on every frame of Adam's SL3 | L, ADAM |
| 118 | 6 | bytes | not established | | none |
| 124 | 4 | u32 | time | milliseconds since the recording started; sign not established | L, S, ADAM |
| 128 | 24 | 6 x u32 | previous frame offsets | the offset of the last frame of each channel (primary, secondary, downscan, left, right, composite sidescan) | S; PUB, ADAM (bytes 128 to 147 checked, 148 to 151 not) |
| 152 | 12 | 3 x u32 | not established | 168-byte layout only; zero in the public sample | S |
| 164 | 4 | u32 | previous 3D offset | 168-byte layout only; not established | S |

**The 184-byte header (Adam's unit).** No source documents it; the evidence is consistency in one SL3 file from Adam's unit. The fields that the decoder reads at offsets 0 to 127 (frame index, channel, the two limits, depth, x, y, flags, time) sit at the same offsets as in the 168-byte layout. Bytes 128 to 147 are unshifted, so the 16 extra bytes lie after byte 147. Bytes 152 to 183 are zero in every frame of channels 0, 2 and 5, so the position of the 16 extra bytes cannot be told from the data, and bytes 148 to 151 are not checked. **That the echo starts at 184 is an inference:** bytes 168 to 183 are zero on every frame and the byte at 184 is not zero on most frames, while in the public 168-byte sample the byte at 168 is not zero on every main-channel frame; a comparison of echo peak against reported depth did not separate 168 from 184. Cut 3 (echo export) depends on it (point 9 of section 10).

**Channels 7 and 8 (128-byte header):** the layout of those 128 bytes and the content of the channels are not examined.

### 6.6 Channels

The same ids are used in SL2 and SL3. The meanings are from S. The labels are the analyser's own; they match the names fomo-f's decoder uses, so that reports can be compared with fomo-f's.

| Id | Label | Meaning |
|---:|---|---|
| 0 | 2D | primary, traditional sonar |
| 1 | Secondary | secondary, traditional sonar |
| 2 | DownScan | DownScan imaging |
| 3 | SideScanLeft | left sidescan (seen in no examined file) |
| 4 | SideScanRight | right sidescan (seen in no examined file) |
| 5 | SideScan | composite sidescan |
| 7, 8 | Unknown(7), Unknown(8) | not established; SL3: frames with a 128-byte header |
| 10, 11 | Unknown(10), Unknown(11) | not established; seen in the public SL2 sample and in Adam's SL2 (channel 11: one pair of range limits in both; channel 10: 18 pairs in the public sample, 542 in Adam's) |
| any other | Unknown(n) | not established |

### 6.7 Flags (u16: SL2 offset 132, SL3 offset 116)

| Mask | Meaning |
|---:|---|
| 0x0200 | altitude valid |
| 0x0100 | heading valid |
| 0x0080 | track valid |
| 0x0040 | water speed valid |
| 0x0010 | **position valid**: x and y are meaningful only when it is set |
| 0x0008 | not established (L: "has packet" for SL2) |
| 0x0004 | water temperature valid |
| 0x0002 | GPS speed valid |

Evidence: the format document cites K and L together for the bit set and does not say which bit comes from which; only 0x0010 is checked on a real file (SL3: set on all frames; A-SL3). The bits are listed as L's pending a check against L itself (point 11 of section 10); no analysis in cut 1 interprets any of them, the inventory only counts the raw values.

### 6.8 Units and conversions

- **Depth and range limits:** f32 in **feet**; metres = feet x 0.3048. That depth is in feet is from L and S; it was not checked against a known real depth (plausibility only). Channel 10's limits change gradually and do not behave like a range setting. The echo samples are spread evenly from the upper to the lower limit, per S; **not checked against a real picture**.
- **Range window:** on vertical channels (2D, Secondary, DownScan, SL2 channels 10 and 11) the depth lies between the limits in every frame of the four files of RANGE. On sidescan the limits are symmetric about zero in every file examined (upper = minus lower), read as minus and plus the lateral range (an interpretation); the check does not apply there.
- **Position:** x and y are i32 in Mercator metres on a sphere of radius R = 6,356,752.3142 m, valid only when flag 0x0010 is set (L and S agree; plausible positions on the public and Adam's files):

  ```
  longitude = x / R * (180 / pi)                                  degrees
  latitude  = (2 * atan(exp(y / R)) - pi / 2) * (180 / pi)        degrees
  ```
- **GPS speed** is in knots and **course over ground** in radians from true north: logged and position-derived values agree within about 1% and about 1 degree on 205 straight 2-second steps above 5 knots, found in 4 of 12 of Adam's recordings (8 SL2, 4 SL3); the other 8 never exceed 2 knots, and the source does not split the result by format. Below 5 knots they were not checked. Heading is in radians and consistent, but not reliable on Adam's unit. Water speed was 0.0 in every frame checked.
- **Time** is the raw u32. Read as milliseconds since the recording started, speed worked out from positions matches the logged speed. Read as a signed i32, the first value of every one of 12 recordings lies between about minus 0.2 s and plus 0.8 s; in 2 of the 12 the first 2 or 3 frames of channel 0 hold values just below 2^32 before the counter continues from small values. So whether the field is signed is **not established**, and a plain reading as unsigned makes those files go backwards at the start. The inventory records both readings (section 7.2).
- The hardware time field (S only, seconds) is not used.

### 6.9 Not established, left out on purpose

- The unknown byte regions named "not established" in the tables, which the survey (cut 4) can report as raw values.
- Frequency: where it sits, its width and its codes.
- The 16 extra bytes of the 184-byte SL3 header; channels 7 and 8; SL2 channels 10 and 11.
- SL3 "version 1" files in L's sense (the echo length is derived differently when flag 0x0008 is unset): never seen in any file.
- SLG files (file-header format 1): not read; the file is reported as such.
- Whether files from other units or firmware follow the same layouts: none examined.

## 7. Data format: what the analyser holds and reports

### 7.1 The frame record

Every frame, SL2 or SL3, becomes the same record before an analysis sees it. Fields the file does not have are absent, never guessed. For a frame with a 128-byte header (SL3 channels 7 and 8) the layout beyond the framing fields is not examined (section 6.5): its record holds only the positions, frame size, echo size, header size, channel and label, and checks 1 to 4 apply to it; every other field is absent and checks 5 to 9 are not applied to its fields.

| Field | From | Note |
|---|---|---|
| file format | file header | SL2 or SL3 |
| position in file, offset field | framing | both kept (64-bit and u32), so a mismatch is visible |
| header size, frame size, echo size | framing | |
| channel id and label | channel | label as in section 6.6 (Unknown(n) for the others) |
| frame index | frame index | |
| previous frame size | | |
| range limits, metres | upper and lower limit | absent if not finite or upper is not smaller than lower; a finding |
| depth, metres | water depth | |
| flags | flags | raw u16 kept |
| longitude, latitude | x, y | only with flag 0x0010 |
| GPS speed (knots), course (radians) | | |
| time, raw | time | the u32 as stored; the signed reading is derived from it |
| previous frame offsets | SL3 offsets 128 to 151 | six u32, for check 7; only on SL3 frames with a 168 or 184 byte header |
| header bytes, raw | the header | only when an analysis asks for them (the survey) |
| frame version | SL3 offset 4 | SL3 only |
| echo | echo bytes | only when an analysis asks for it |

### 7.2 Inventory report (cut 1)

**Per file:** number ("file 1 of n") and size in bytes (the file name and path only with the option); file-header format, version and block-size values; layouts found (header size and count of frames with each); total frames; where framing ends (end of file exactly, or the offset of the break and the bytes left); the count of each health check of section 6.3 passed and failed, with the first failing position; first and last time value (counters from the start of the recording), both readings (unsigned, and signed i32), and the number of frames that read below zero as signed.

**Per channel in the file:** frame count; frame index first, last and any gap; header sizes seen; frame versions seen; number of distinct pairs of range limits (the pairs and their counts only with the option, since a pair that changes gradually tracks the depth); frames with a valid position (share); depth minimum and maximum (only with the option). These are the health view; the track of cut 2 adds the area covered, speed and course.

**Default report** holds no position, no depth, no clock time and no file name (section 4); counts and structure only.

**Expected figures for the first run** (from fomo-f's format document; the checks that frame counts and layout detection are right): the public SL2 example holds 10,304 frames; the public SL3 example holds 10,250 frames, of which 5,124 are channels 7 and 8 with a 128-byte header and the other 5,126 have a 168-byte header.

## 8. Provenance and third-party material

Policy: `policies/third-party-ip.md` of the product office, applied through the `third-party-code-policy` skill, 2026-11-23.

**Source line:** fomo-f `docs/SL2_SL3_FORMAT.md` at fomo-f `ff5f0714a78df7892526cc1ac0d0507e399ce55b`, drawing on L (`f54a27c`, MIT) and S (`f8fbb79`, MIT); K (kmpm/node-sl2format, `247d5f1`) left out; intended use a private proof of concept (Proposal, Clead's: Adam's words of 2026-10-29 make the C# pack the purpose and the utility secondary; they do not say that it stays private, point 10 of section 10), and anything that ships passes the policy's release gate first; mode **learn from (format facts)**, nothing copied.

| Item | Where it comes from | Licence as found | How this design uses it |
|---|---|---|---|
| The format document | A document of the fomo-f repository; its file does not name an author. Its sources are the rows below (fomo-f's `THIRD-PARTY-NOTICES.md`, checked 2026-11-04) | fomo-f has no LICENSE file; the repository is one of Adam's (`RULES.md` § 10 (1)), so the text is treated as ours. Whether a second collaborator holds rights in it was not checked | The layout and the evidence per field are taken from it, in new words and a new table; no sentence is copied |
| L, opensounder/python-sllib | https://github.com/opensounder/python-sllib | MIT, Copyright (c) 2019-2021 Peter Magnusson | Format facts (offsets, types, flags). Recorded as "learn from" in fomo-f's `THIRD-PARTY-NOTICES.md` (compliant) |
| S, KennethTM/sonarlight | https://github.com/KennethTM/sonarlight | MIT, Copyright (c) 2023 Kenneth Thorø Martinsen | Format facts and the channel names. Same record |
| K, kmpm/node-sl2format | https://github.com/kmpm/node-sl2format | No LICENSE file and no licence text; MIT named in `package.json` and the README only. Open in fomo-f (PBI-T.8) | **Nothing is taken from it.** The format document quotes a few of its words and takes a few K-only details (version values, block-size examples, some units); this design leaves them out, and every field it uses stands on L, S or observation. No clean room was run for K |
| O, OpenStreetMap wiki "SL2" | https://wiki.openstreetmap.org/wiki/SL2 | CC BY-SA 2.0 | Not read; not drawn on |
| Adam's recordings | his own data | his | Counts and structural findings only, as in fomo-f's document. No position, time or depth of his appears in this design (`RULES.md` § 19 (2)) |
| fomo-f TPS | fomo-f `docs/TECHNICAL_PRODUCT_SPECIFICATION.md` | as the format document | What the files are, the glossary, the machines (section 2.3). Its old packet-layout and unit tables are marked in the TPS itself as not applying to files, and their sources are unidentified in fomo-f's notices (open, PBI-T.8): **not used** |

**For the new repository:** its `THIRD-PARTY-NOTICES.md` lists L and S as "learn from (format facts)", with version and licence, and K as not used. If test data from the public sonarlight example files is copied in (a small excerpt, as fomo-f did), that is mode **copy** and carries sonarlight's licence text. Adam's own files are never committed (fomo-f's rule: capture files are never committed).

## 9. What this design leaves out on purpose

- **fomo-f's decoder internals:** its modules, its normalised ping record, how it skips or warns on a frame, its tests. The analyser has its own record (section 7.1) and its own policy for damaged files.
- **The TPS's live-network material**, the listener, the GoFree client and the superseded packet layout, which concern a different data source.
- **Implementation details of the analyser:** project layout, libraries, class design, the test suite. They are the next step, built on this design after Adam's approval (`RULES.md` § 29).

**Testing, as a design constraint only:** tests use frames built to section 6 (synthetic) and, if Adam agrees, an excerpt of the public sonarlight examples with its licence text carried (section 8). Real files are never committed, and no test needs one. The expected figures of section 7.2 give a check against fomo-f's results on the public files.

## 10. Open points for Adam

Each is his decision (the WHAT); Clead's recommendation follows. Adam approved the design as a whole on 2026-11-23 23:23, and Clead reads that as accepting each recommendation.

1. **The first cut.** Recommended: inventory and file health only (section 3). The alternative is to build cuts 1 and 2 together.
2. **Cut 4, the undecoded-bytes survey.** New in this design, not discussed. Recommended: keep it as a candidate after cut 1, decided with the first report in hand.
3. **Relation to fomo-f.** Recommended: the analyser only reports; fomo-f's decoder stays. What the analyser finds about the format goes to fomo-f's format document through fomo-f's own flow.
4. **Damaged files.** Recommended: stop at the first break and report it. Searching for the next valid frame after a break can be added later if the first report shows files that break.
5. **Folder input** (the files directly in a folder). Recommended: yes.
6. **Privacy default** (counts and structure only unless an option asks for more). Recommended: yes, as in section 4.
7. **Wireshark captures** as a later analysis in the same tool or a tool of their own. Recommended: decided after the first cut; nothing in this design depends on it.
8. **The name:** settled by Adam, 2026-11-23 23:06: sl23-cs-reader, for the project and the repository.
9. **Echo start of the 184-byte header** (section 6.5) is an inference, and how to decide it is open: the zero bytes at 168 to 183 favour 184 but do not prove it, the echo-peak comparison did not separate 168 from 184, and the survey of cut 4 reports header bytes, so it cannot decide it either. Recommended: cut 3 (echo export) waits until a check that can decide it is named; Clead has none yet.
10. **Does the tool stay private?** The design assumes a private proof of concept (section 8). If it is ever to be published or shipped, the policy's release gate applies first. Recommended: private until you say otherwise.
11. **The flag bits** (section 6.7): the format document cites K and L together. Recommended: a session checks them against L itself before any analysis interprets a bit beyond 0x0010.

Also for later: how Claude sees a result. Claude sessions cannot read Adam's disk unless he connects a folder, so the default report is made to be pasted into a chat.
