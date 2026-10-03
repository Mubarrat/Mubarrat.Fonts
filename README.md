# Mubarrat.Fonts.OpenType

> A modern, reflection-free, AOT-compatible OpenType font library for .NET.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4.svg)](https://dotnet.microsoft.com)
[![AOT](https://img.shields.io/badge/AOT-compatible-brightgreen.svg)](#design-principles)
[![Trimming](https://img.shields.io/badge/trimming-safe-brightgreen.svg)](#design-principles)

`Mubarrat.Fonts.OpenType` is a complete OpenType specification implementation for .NET. It parses every table defined by the OpenType specification, exposes the parsed data as strongly-typed records, and ships with a rule-based diagnostic engine that reports font health the way a compiler reports code health — with stable rule IDs, configurable severities, and structured source locations.

---

## Table of Contents

- [Highlights](#highlights)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [Parsing a Font](#parsing-a-font)
  - [Resolving a Glyph](#resolving-a-glyph)
  - [Reading Glyph Outlines](#reading-glyph-outlines)
- [Diagnostics](#diagnostics)
  - [Running the Analyzer](#running-the-analyzer)
  - [Configuring Severities](#configuring-severities)
  - [Rule IDs](#rule-ids)
  - [Writing Custom Rules](#writing-custom-rules)
- [Sources](#sources)
- [Extending the Library](#extending-the-library)
- [Design Principles](#design-principles)
  - [No Reflection](#no-reflection)
  - [Zero-Allocation Parsing](#zero-allocation-parsing)
  - [AOT and Trimming](#aot-and-trimming)
  - [Immutable by Construction](#immutable-by-construction)
- [Supported Tables](#supported-tables)
- [Project Structure](#project-structure)
- [Performance](#performance)
- [Contributing](#contributing)
- [License](#license)

---

## Highlights

- **Complete specification coverage.** Every table defined by the OpenType 1.9 specification, including `CFF `, `CFF2`, `glyf`, `GSUB`, `GPOS`, `GDEF`, `COLR`, and the full variable-font stack.
- **Zero-reflection dispatch.** Table parsers use C# static abstract interface members, so every dispatch is resolved at JIT time. No `Activator`, no `MethodInfo.Invoke`, no assembly scanning.
- **AOT and trim-safe.** `IsTrimmable` and `IsAotCompatible` are enabled on the library. Publish your app with `PublishAot=true` and the font parser comes along for free.
- **Zero-allocation parsing core.** `ref struct Cursor`, `scoped Span<byte>`, and `stackalloc` fast paths keep the hot path off the heap.
- **Immutable value semantics.** Tables are `sealed record` types with `{ get; init; }` properties. Headers are `record struct` types. Everything is thread-safe for concurrent reads without synchronization.
- **Rule-based diagnostics.** A Roslyn-style diagnostic engine with stable rule IDs (`OT.head.magic-number`), configurable severity, and byte-level source locations.

---

## Installation

```
dotnet add package Mubarrat.Fonts.OpenType
```

For the diagnostics layer:

```
dotnet add package Mubarrat.Fonts.OpenType.Diagnostics
```

The core library has no dependencies beyond the .NET base class library. The diagnostics package adds the rule engine and the per-table analyzers.

---

## Quick Start

### Parsing a Font

```csharp
using Mubarrat.Fonts.OpenType;
using Mubarrat.Fonts.OpenType.Binary;

using var source = FileSource.Open("Roboto-Regular.ttf");
FontFace face = source.ReadRecordAt<FontFace>(0);

Console.WriteLine($"Tables: {face.Directory.Count}");
foreach (Tag tag in face.Directory.Tags)
    Console.WriteLine($"  {tag}");
```

`FontFace` is the root of the parse tree. It holds the font's `Source` and its table directory, and it lazily parses and caches tables on first request.

### Resolving a Glyph

```csharp
using Mubarrat.Fonts.OpenType.Tables;

CmapTable cmap = face.GetTable<CmapTable>();
int glyphId = cmap.GetGlyphId('A');
```

`GetTable<T>()` returns `T` directly. The tag comes from the type's `static abstract Tag` member, so there is no way to pass the wrong tag or look up a table with a type that doesn't match.

### Reading Glyph Outlines

```csharp
using Mubarrat.Fonts.OpenType.Tables.Outlines;

GlyfTable glyf = face.GetTable<GlyfTable>();
Glyph glyph = glyf[glyphId];

switch (glyph)
{
    case SimpleGlyph simple:
        foreach (Contour contour in simple.Contours)
        {
            for (int i = contour.StartPoint; i <= contour.EndPoint; i++)
            {
                GlyphPoint p = simple.Points[i];
                Console.WriteLine($"({p.X}, {p.Y}) on-curve={p.OnCurve}");
            }
        }
        break;

    case CompositeGlyph composite:
        foreach (Component component in composite.Components)
            Console.WriteLine($"Component: glyph {component.GlyphIndex} " +
                              $"at ({component.Argument1}, {component.Argument2})");
        break;

    case EmptyGlyph:
        Console.WriteLine("Empty glyph (no outline).");
        break;
}
```

The `Glyph` hierarchy is a closed set of three types — `SimpleGlyph`, `CompositeGlyph`, and `EmptyGlyph` — so pattern matching is exhaustive and the compiler will warn if a new variant is added without handling it.

---

## Diagnostics

The diagnostics layer treats a font the way a compiler treats source code. Every rule has a stable identifier, a category, a default severity, and a message with structured arguments. Running the analyzer produces a list of `Diagnostic` values that you can filter, sort, and display.

### Running the Analyzer

```csharp
using Mubarrat.Fonts.OpenType.Diagnostics;

var analyzer = new FontAnalyzer();
IReadOnlyList<Diagnostic> diagnostics = analyzer.Analyze(face);

foreach (Diagnostic d in diagnostics)
    Console.WriteLine(d);
```

Output:

```
Warning OT.os2.use-typo-metrics-not-set: 'OS/2'.fsSelection: does not have USE_TYPO_METRICS set; ...
Error   OT.head.magic-number: 'head'.magicNumber: is 0xDEADBEEF, expected 0x5F0F3CF5.
```

### Configuring Severities

Every rule has a default severity, but you can override it per run. Use `null` to suppress a rule entirely.

```csharp
var options = new AnalysisOptions();
options.SetSeverity("OT.os2.use-typo-metrics-not-set", DiagnosticSeverity.Information);
options.SetSeverity("OT.name.no-mac-roman", null);   // suppress

var analyzer = new FontAnalyzer(options);
```

This is the same model VS Code uses for its `.eslintrc`-style rule configuration.

### Rule IDs

Every rule has a stable, self-describing ID in the form `OT.<table>.<slug>`:

| ID | Meaning |
|---|---|
| `OT.head.magic-number` | `head.magicNumber` is not `0x5F0F3CF5` |
| `OT.head.units-per-em` | `head.unitsPerEm` is outside `[16, 16384]` |
| `OT.cmap.records-unsorted` | Encoding records are not sorted |
| `OT.maxp.num-glyphs-zero` | `maxp.numGlyphs` is 0 |
| `OT.hmtx.count-vs-maxp` | `hmtx` record count disagrees with `maxp.numGlyphs` |

IDs are **stable across versions**. The message text may change; the ID will not. This makes suppressions and documentation links durable.

### Writing Custom Rules

Implement `IFontAnalyzer` to add your own rules. The analyzer receives the whole `FontFace`, so a single analyzer can inspect multiple tables.

```csharp
public class MyRules : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MissingOS2 = new(
        "OT.custom.missing-os2", "Font has no OS/2 table",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Compatibility,
        "The font has no OS/2 table; metrics will be missing on Windows.");

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        if (!face.Directory.Contains(Os2Table.Tag))
            bag.Add(MissingOS2.Create([], table: null));
    }
}

var analyzer = new FontAnalyzer();
analyzer.Add(new MyRules());
```

Custom analyzers run after the built-in rule set.

---

## Sources

A `Source` is a random-access byte provider with `long`-offset addressing. The abstract base class is small; concrete implementations cover every backing store.

| Type | Backing | Notes |
|---|---|---|
| `MemorySource` | `byte[]` / `ReadOnlyMemory<byte>` | Zero-copy span fast path |
| `FileSource` | `SafeFileHandle` | Uses `RandomAccess` — lock-free concurrent reads |
| `StreamMemorySource` | `Stream` | Buffers on demand, or reads to completion in `Create` |
| `SliceSource` | Another `Source` | A sliced view that collapses nested slices at construction |

Parsers are written against `Source`, so parsing a font from disk, memory, or a network stream is the same code. Opening a font from a `Stream` and closing the stream before parsing is a one-liner:

```csharp
var source = await StreamMemorySource.CreateAsync(httpStream);
var cursor = source.CreateCursor();
FontFace face = FontFace.Parse(ref cursor);
// The stream is buffered; the source is independent of it.
```

---

## Extending the Library

Adding a new table type requires no changes to the library. Implement `IOpenTypeTable<T>` on your type, provide a `Tag` and a `Parse`, and call it through `FontFace.GetTable<T>()`.

```csharp
public sealed record MyTable : IOpenTypeTable<MyTable>
{
    public static Tag Tag => "MYTB";

    public ushort Version { get; init; }
    public IReadOnlyList<byte> Payload { get; init; } = [];

    static MyTable IRecord<MyTable>.Parse(ref Cursor cursor, object? context)
    {
        ushort version = cursor.ReadUInt16();
        byte[] payload = cursor.ReadBytes((int)cursor.Remaining);
        return new MyTable { Version = version, Payload = payload };
    }
}

var table = face.GetTable<MyTable>();
```

There is no registration step, no dictionary of factories, no reflection scan. The generic constraint `T : IOpenTypeTable<T>` is what lets the compiler and JIT resolve `T.Tag` and `T.Parse` without any runtime type lookup.

---

## Design Principles

The library's performance and compatibility properties are not accidental — they fall out of four decisions made at the beginning.

### No Reflection

`System.Reflection` does not appear anywhere in the library. Dispatch is expressed through C# 11's **static abstract interface members**:

```csharp
public interface IOpenTypeTable<T> : IRecord<T> where T : IOpenTypeTable<T>
{
    static abstract Tag Tag { get; }
    static abstract T Parse(ref Cursor cursor, object? context);
}
```

When `FontFace.GetTable<T>()` runs, the JIT has `T` and can resolve both members to direct calls. There is no `GetMethod("Parse")`, no `MethodInfo.Invoke`, no `Activator.CreateInstance`.

The result:
- Native AOT works without annotations.
- The trimmer never removes anything that is actually called.
- Cold-start is faster — no assembly scanning, no lazy method caching.

### Zero-Allocation Parsing

`Cursor` is a `ref struct`. A `ref struct` cannot be stored in a field, captured in a lambda, or survive an `await`. That constraint is what makes it zero-allocation: it lives on the stack for the duration of a single `Parse`, and no heap object is created to represent parsing state.

Every read is bounds-checked against `Source.Length` before touching bytes, and the span fast path avoids a buffer copy entirely when the source can hand out a `ReadOnlySpan<byte>`.

### AOT and Trimming

Both source projects declare:

```xml
<IsTrimmable>true</IsTrimmable>
<IsAotCompatible>true</IsAotCompatible>
```

Publishing a consuming app with `PublishAot=true` produces zero trim warnings. The library is used by the demo project, which is built AOT in CI.

### Immutable by Construction

Tables are `sealed record` types. Properties are `{ get; init; }`. Collections are `IReadOnlyList<T>`. Header structs are `record struct` with public fields and a `static abstract ReverseEndianness` that returns a new value.

Consequences:
- Every parsed table is thread-safe for concurrent reads without locks.
- `with` expressions produce shallow copies with one field changed, useful for building modified fonts.
- Value equality is compiler-generated; two structurally identical tables compare equal.

---

## Supported Tables

<details>
<summary><b>Outline</b></summary>

- `glyf`, `loca`, `CFF `, `CFF2`
</details>

<details>
<summary><b>Metrics and metadata</b></summary>

- `head`, `hhea`, `hmtx`, `maxp`, `name`, `OS/2`, `post`, `VORG`, `vhea`, `vmtx`
- `DSIG`, `hdmx`, `kern`, `LTSH`, `meta`, `PCLT`, `VDMX`
</details>

<details>
<summary><b>Character mapping</b></summary>

- `cmap` — formats 0, 2, 4, 6, 8, 10, 12, 13, 14
</details>

<details>
<summary><b>Layout</b></summary>

- `BASE`, `GDEF`, `GPOS`, `GSUB`, `JSTF`, `MATH`
</details>

<details>
<summary><b>Color</b></summary>

- `COLR` (v0 and v1 paint graphs), `CPAL`, `sbix`, `SVG `
</details>

<details>
<summary><b>Bitmap</b></summary>

- `CBDT`, `CBLC`, `EBDT`, `EBLC`, `EBSC`
</details>

<details>
<summary><b>Hinting</b></summary>

- `cvt `, `fpgm`, `prep`, `gasp`
</details>

<details>
<summary><b>Variations</b></summary>

- `avar`, `cvar`, `fvar`, `gvar`, `HVAR`, `MVAR`, `STAT`, `VVAR`
</details>

---

## Project Structure

```
Mubarrat.Fonts.slnx
Directory.Build.props                    // shared identity, AOT/trimming policy
Mubarrat.Fonts.OpenType/
    Binary/                              // Source, Cursor, IRecord<T>, IBigEndianStruct<T>
    Primitives/                          // Tag, Fixed, UInt24, Int24, F2Dot14
    Tables/                              // one file per table
Mubarrat.Fonts.OpenType.Diagnostics/
    DiagnosticSeverity.cs
    DiagnosticDescriptor.cs
    DiagnosticBag.cs
    FontAnalyzer.cs
    Analyzers/                           // one analyzer per table
Mubarrat.Fonts.OpenType.Tests/
    Fonts/                               // real fonts used as fixtures
    SmokeTests.cs
```

The core library has no dependency on the diagnostics library. A consumer that only parses and shapes gets the parser and nothing else.

---

## Performance

The library is designed around three assumptions:

1. **Fonts are parsed once per `FontFace`.** Table data is materialized during `Parse`, so subsequent glyph and metric lookups are dictionary or array hits.
2. **Reads are concurrent.** `Source` implementations are safe for parallel reads at different offsets. `FileSource` uses `RandomAccess`, which does not share a file cursor between threads.
3. **The hot path is glyph lookup.** `CmapTable.GetGlyphId` and `GlyfTable[int]` are the two methods you call most; both are optimized for that use.

To measure against your own workload, add a `BenchmarkDotNet` project and reference `Mubarrat.Fonts.OpenType`. The design decisions that matter most for benchmarking are:

- **Cold load time** — dominated by eager `glyf` or `CFF ` materialization. A full 65,000-glyph CJK font decodes every outline at load time.
- **Hot glyph access** — `face.GetTable<CmapTable>().GetGlyphId(c)` is O(1) after the first call. `face.GetTable<GlyfTable>()[gid]` is an array index.
- **Memory footprint** — the decoded object graph is larger than the raw font bytes. Whether the trade is worth it depends on how many times you query each glyph.

---

## Contributing

Issues and pull requests are welcome. Before opening a PR:

- Run `dotnet test` and confirm all tests pass.
- Match the existing code style: `sealed record` for reference tables, `record struct` for headers, `{ get; init; }` for properties, braces on every `if` and `for`.
- Add a test that reproduces the bug or covers the new behavior.

The codebase has one convention worth highlighting: **table parsers never allocate in the hot path**. If you add a new table, ensure `Parse` does not retain a `Source` reference unless the table genuinely needs lazy reads.

---

## License

MIT. See [LICENSE.md](LICENSE.md).
