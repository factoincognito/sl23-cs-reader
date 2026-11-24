# Excerpts of the sonarlight example files

Third-party test data, copied unchanged from
[KennethTM/sonarlight](https://github.com/KennethTM/sonarlight) at commit
`f8fbb79ed59c3e508bf7595598b2c919293d2d15` under its MIT licence, whose text
is `LICENSE` in this folder (Copyright (c) 2023 Kenneth Thorø Martinsen).
The notice is in `THIRD-PARTY-NOTICES.md` at the root of the repository.

Each excerpt is the start of an example file up to the end of its 40th
frame: the 8-byte file header and the first 40 frames, byte for byte.

| Excerpt | Bytes | SHA-256 | Taken from | Bytes | SHA-256 | Git blob |
|---|---:|---|---|---:|---|---|
| `example_sl2_file.first-40-frames.sl2` | 82,608 | `44ea9efdfc996e5232b0568b2cf120fb22b718914ccfa62efc6d0efc6292ab53` | `example_files/example_sl2_file.sl2` | 21,277,768 | `126553ff01a460384d8dab1a4d5ef13e27f9040618ff9bd841ce9f1149385d08` | `c1d664e97962c1cff85135f8c68092b8a2c09335` |
| `example_sl3_file.first-40-frames.sl3` | 82,768 | `59786cefee9b8ab52743ebcc3c12989d4ec129774ef3305a8406a5fa38d05686` | `example_files/example_sl3_file.sl3` | 21,207,656 | `8208d90733bd0487f1a9417398cb85d433f184a4539f703778ba5d8fd2efe285` | `db81244b66114643b4229339fa5bee5f3ccb2cf2` |

To make an excerpt again: `head -c 82608 example_sl2_file.sl2` and
`head -c 82768 example_sl3_file.sl3`.

The whole example files are not in this repository. The tests that read
them (`SonarlightExampleTests`) run when the environment variable
`SL23_SONARLIGHT_EXAMPLES` names a folder that holds both files, and check
their SHA-256 first; without the variable they are skipped.

The example files hold positions and depths of the recording their author
published. No recording of Adam's is in this folder or anywhere in the
repository.
