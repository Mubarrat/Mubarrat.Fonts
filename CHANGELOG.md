# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Mubarrat.Fonts.OpenType` — a complete OpenType specification parser for .NET.
  - Full table coverage: outlines (`glyf`, `loca`, `CFF `, `CFF2`), metrics and
    metadata (`head`, `hhea`, `hmtx`, `maxp`, `name`, `OS/2`, `post`, `VORG`,
    `vhea`, `vmtx`, `DSIG`, `hdmx`, `kern`, `LTSH`, `meta`, `PCLT`, `VDMX`),
    character mapping (`cmap` formats 0, 2, 4, 6, 8, 10, 12, 13, 14), layout
    (`BASE`, `GDEF`, `GPOS`, `GSUB`, `JSTF`, `MATH`), color (`COLR` v0 and v1,
    `CPAL`, `sbix`, `SVG `), bitmap (`CBDT`, `CBLC`, `EBDT`, `EBLC`, `EBSC`),
    hinting (`cvt `, `fpgm`, `prep`, `gasp`), and variations (`avar`, `cvar`,
    `fvar`, `gvar`, `HVAR`, `MVAR`, `STAT`, `VVAR`).
  - Zero-reflection dispatch via static abstract interface members.
  - `ref struct Cursor` parsing core with `Span<byte>` and `stackalloc` fast
    paths.
  - `Source` abstraction with `MemorySource`, `FileSource`, `StreamMemorySource`,
    and `SliceSource`.
  - AOT-compatible and trim-safe.
- `Mubarrat.Fonts.OpenType.Diagnostics` — a rule-based font analyzer.
  - `DiagnosticDescriptor`, `DiagnosticBag`, `FontAnalyzer`, and per-table
    analyzers.
  - Stable rule IDs in the form `OT.<table>.<slug>`.
  - Configurable per-rule severity through `AnalysisOptions`.

### Security

- Parser bounds-checks every read against the source length and enforces
  explicit safety limits on counts, offsets, and recursion depth to prevent
  resource exhaustion from malformed fonts.

[Unreleased]: https://github.com/Mubarrat/Mubarrat.Fonts/commits/main
