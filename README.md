# Mubarrat.Fonts.OpenType (Outdated in this commit, Needs update)

> A modern, reflection-free, AOT-compatible OpenType font library for .NET.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4.svg)](https://dotnet.microsoft.com)
[![AOT](https://img.shields.io/badge/AOT-compatible-brightgreen.svg)](#aot-and-trimming)
[![Trimming](https://img.shields.io/badge/trimming-safe-brightgreen.svg)](#aot-and-trimming)

`Mubarrat.Fonts.OpenType` is a complete OpenType specification implementation for .NET. It parses every table defined by the OpenType specification, exposes the parsed data as strongly typed records, and ships with a rule-based diagnostic engine that reports font health the way a compiler reports code health — with stable rule IDs, configurable severities, and structured source locations.

---

## Table of Contents

* [Highlights](#highlights)
* [Installation](#installation)
* [Quick Start](#quick-start)
  * [Parsing a Font](#parsing-a-font)
  * [Resolving a Glyph](#resolving-a-glyph)
  * [Reading Glyph Outlines](#reading-glyph-outlines)
* [Diagnostics](#diagnostics)
  * [Running the Analyzer](#running-the-analyzer)
  * [Configuring Severities](#configuring-severities)
  * [Rule IDs](#rule-ids)
  * [Writing Custom Rules](#writing-custom-rules)
* [Sources](#sources)
* [Extending the Library](#extending-the-library)
* [Design Principles](#design-principles)
  * [No Reflection](#no-reflection)
  * [Allocation-Conscious Parsing](#allocation-conscious-parsing)
  * [AOT and Trimming](#aot-and-trimming)
  * [Immutable Semantic Data](#immutable-semantic-data)
* [Supported Tables](#supported-tables)
* [Project Structure](#project-structure)
* [Performance](#performance)
* [Contributing](#contributing)
* [License](#license)

---

## Highlights

* **Complete specification coverage.** Every table defined by the OpenType specification is implemented, including `CFF `, `CFF2`, `glyf`, `GSUB`, `GPOS`, `GDEF`, `COLR`, and the complete variable-font stack.
* **Zero-reflection dispatch.** Table parsers use C# static abstract interface members, so generic record dispatch is resolved statically by the runtime. There is no `Activator`, `MethodInfo.Invoke`, or assembly scanning.
* **AOT and trim-safe.** The library is designed for Native AOT and trimming, with `IsTrimmable` and `IsAotCompatible` enabled in the project configuration.
* **Allocation-conscious parsing.** `ref struct Cursor`, span-based reads, and direct binary readers keep parser state off the heap and avoid allocations that do not contribute to the resulting object graph.
* **Immutable semantic data.** Parsed tables use `sealed record` types with `{ get; init; }` properties, while wire-format headers use mutable `record struct` transfer types where appropriate.
* **Rule-based diagnostics.** The diagnostics layer provides stable rule IDs such as `OT.head.magic-number`, configurable severity, structured arguments, and source locations.

---

## Installation

```text
dotnet add package Mubarrat.Fonts.OpenType
```

For the diagnostics layer:

```text
dotnet add package Mubarrat.Fonts.OpenType.Diagnostics
```

The core library has no dependencies beyond the .NET base class library. The diagnostics package adds the rule engine and table-specific analyzers.

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

`FontFace` is the root parsed record. It contains the font's source and table directory. Individual tables are resolved and cached when requested through `GetTable<T>()`.

The public entry point is `Source.ReadRecordAt<T>(offset)`. The underlying `IRecord<T>.Parse` member is part of the library's static parsing infrastructure and is not a consumer-facing parsing API.

### Resolving a Glyph

```csharp
using Mubarrat.Fonts.OpenType.Tables;

CmapTable cmap = face.GetTable<CmapTable>();
int glyphId = cmap.GetGlyphId('A');
```

`GetTable<T>()` returns `T` directly. The table tag is supplied by the type's public static `Tag` member, so consumers do not provide a separate tag when requesting a typed table.

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

The outline model currently represents glyphs using `SimpleGlyph`, `CompositeGlyph`, and `EmptyGlyph`.

---

## Diagnostics

The diagnostics layer treats font validation similarly to compiler and linter diagnostics. Each rule has a stable identifier, a category, a default severity, and a message with structured arguments. Running the analyzer produces `Diagnostic` values that can be filtered, sorted, and displayed.

### Running the Analyzer

```csharp
using Mubarrat.Fonts.OpenType.Diagnostics;

var analyzer = new FontAnalyzer();
IReadOnlyList<Diagnostic> diagnostics = analyzer.Analyze(face);

foreach (Diagnostic d in diagnostics)
    Console.WriteLine(d);
```

Example output:

```text
Warning OT.os2.use-typo-metrics-not-set: 'OS/2'.fsSelection: does not have USE_TYPO_METRICS set; ...
Error   OT.head.magic-number: 'head'.magicNumber: is 0xDEADBEEF, expected 0x5F0F3CF5.
```

### Configuring Severities

Every rule has a default severity, but it can be overridden per analysis run. Use `null` to suppress a rule entirely.

```csharp
var options = new AnalysisOptions();
options.SetSeverity("OT.os2.use-typo-metrics-not-set", DiagnosticSeverity.Information);
options.SetSeverity("OT.name.no-mac-roman", null);

var analyzer = new FontAnalyzer(options);
```

This provides a configurable rule model similar to those used by modern compiler and linter ecosystems.

### Rule IDs

Every rule has a stable, self-describing ID in the form `OT.<table>.<slug>`:

| ID                         | Meaning                                             |
| -------------------------- | --------------------------------------------------- |
| `OT.head.magic-number`     | `head.magicNumber` is not `0x5F0F3CF5`              |
| `OT.head.units-per-em`     | `head.unitsPerEm` is outside `[16, 16384]`          |
| `OT.cmap.records-unsorted` | Encoding records are not sorted                     |
| `OT.maxp.num-glyphs-zero`  | `maxp.numGlyphs` is 0                               |
| `OT.hmtx.count-vs-maxp`    | `hmtx` record count disagrees with `maxp.numGlyphs` |

Rule IDs are intended to remain stable across versions. Message text may change while the identifier remains suitable for suppressions, configuration, and documentation references.

### Writing Custom Rules

Implement `IFontAnalyzer` to add custom rules. The analyzer receives the complete `FontFace`, allowing a single analyzer to inspect multiple tables.

```csharp
public class MyRules : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MissingOS2 = new(
        "OT.custom.missing-os2",
        "Font has no OS/2 table",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Compatibility,
        "The font has no OS/2 table; metrics will be missing on Windows.");

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(Os2Table.Tag))
            bag.Add(MissingOS2.Create([], table: null));
    }
}

var analyzer = new FontAnalyzer();
analyzer.Add(new MyRules());
```

Custom analyzers run after the built-in rule set.

---

## Sources

A `Source` is a random-access byte provider with `long`-offset addressing. The abstract source abstraction allows the parser to operate independently of the underlying storage mechanism.

| Type                 | Backing                           | Notes                                                          |
| -------------------- | --------------------------------- | -------------------------------------------------------------- |
| `MemorySource`       | `byte[]` / `ReadOnlyMemory<byte>` | Direct span-based reads                                        |
| `FileSource`         | `SafeFileHandle`                  | Uses `RandomAccess` for concurrent reads                       |
| `StreamMemorySource` | `Stream`                          | Buffers stream data for independent random access              |
| `SliceSource`        | Another `Source`                  | Provides a sliced coordinate space and collapses nested slices |

Parsers operate against `Source`, so the same record implementation can read from memory, files, buffered streams, or other source implementations.

For example, a stream can be buffered into a `StreamMemorySource` before parsing:

```csharp
var source = await StreamMemorySource.CreateAsync(httpStream);
FontFace face = source.ReadRecordAt<FontFace>(0);

// The source owns the buffered representation and no longer depends
// on the original stream for subsequent reads.
```

The public parsing model is source-oriented:

```text
Source
  │
  └── ReadRecordAt<T>(offset)
          │
          ▼
      IRecord<T>
          │
          └── Parse(...)
```

`Cursor` represents the local sequential state used internally while decoding a record. Consumers normally do not invoke `Parse` directly.

---

## Extending the Library

Adding a new table type does not require a registration table, factory dictionary, or reflection scan. Implement `IOpenTypeTable<T>`, provide its public static `Tag`, and explicitly implement the `IRecord<T>.Parse` infrastructure member.

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
        return new MyTable
        {
            Version = version,
            Payload = payload
        };
    }
}

var table = face.GetTable<MyTable>();
```

The public surface is intentionally separated from the parsing infrastructure:

* `Tag` is **public static and implicitly implemented**.
* `Parse` is **explicitly implemented** through `IRecord<T>`.
* `GetTable<T>()` provides the consumer-facing typed lookup.
* No registration step is required.
* No runtime type lookup or reflection is required.

The generic constraint `T : IOpenTypeTable<T>` allows the static interface contract to provide the table-specific metadata and parser implementation without a runtime factory registry.

---

## Design Principles

The library's performance and compatibility properties follow from a small set of architectural decisions.

### No Reflection

`System.Reflection` is not used as the table-dispatch mechanism. Dispatch is expressed through C# static abstract interface members.

The table interface only declares the table-specific static metadata; the common parsing contract comes from `IRecord<T>`:

```csharp
public interface IOpenTypeTable<T> : IRecord<T>
    where T : IOpenTypeTable<T>
{
    static abstract Tag Tag { get; }
}
```

Concrete tables explicitly implement the parsing contract:

```csharp
static MyTable IRecord<MyTable>.Parse(ref Cursor cursor, object? context)
{
    // ...
}
```

When generic table code requests `T.Tag` or invokes the `IRecord<T>` parsing contract, the static interface dispatch is resolved without a reflection-based factory.

There is no:

* `GetMethod("Parse")`
* `MethodInfo.Invoke`
* `Activator.CreateInstance`
* assembly scanning
* runtime registration dictionary for table parsers

This keeps the parser compatible with Native AOT and trimming without requiring reflection metadata for table discovery.

### Allocation-Conscious Parsing

`Cursor` is a `ref struct`. Its parsing state therefore does not require a heap allocation.

A `ref struct` cannot be:

* stored in a heap object,
* captured by a lambda,
* boxed,
* or allowed to survive an `await`.

The parser is therefore **allocation-conscious rather than universally allocation-free**. Parsing a table can legitimately allocate arrays, records, strings, and other objects that form the returned representation. The goal is to avoid allocations that do not contribute to that representation.

Where possible, binary data is read directly into the required representation:

```csharp
Record[] records = cursor.ReadBigEndianStructArray<Record>(count);
```

rather than first constructing a temporary collection and subsequently converting it.

The source layer can also expose span-based reads where the backing store supports them, avoiding unnecessary intermediate buffers.

### AOT and Trimming

The relevant source projects declare:

```xml
<IsTrimmable>true</IsTrimmable>
<IsAotCompatible>true</IsAotCompatible>
```

The architecture avoids reflection-based type discovery, which is particularly important for Native AOT and trimming.

A consuming application can therefore publish with:

```xml
<PublishAot>true</PublishAot>
```

without requiring the font parser to maintain a reflection-driven registration system.

AOT and trimming compatibility should still be validated against the complete consuming application, since application-specific code and dependencies can introduce their own requirements.

### Immutable Semantic Data

Parsed semantic tables are represented as `sealed record` types with `{ get; init; }` properties.

Wire-format headers are different: they are mutable `record struct` transfer objects because their purpose is to represent binary layouts during parsing and byte-order conversion.

For example, header types expose a public static `ReverseEndianness` operation:

```csharp
public static Header ReverseEndianness(Header value)
{
    // ...
}
```

The method is **public static and implicitly implemented** when it participates in the relevant static interface contract.

The resulting distinction is intentional:

* **Semantic records:** `sealed record`
* **Semantic properties:** `{ get; init; }`
* **Wire headers:** mutable `record struct`
* **`Tag`:** public static, implicitly implemented
* **`ReverseEndianness`:** public static, implicitly implemented
* **`Parse`:** explicit interface implementation
* **`FromHeader`:** explicit interface implementation where required

Record `with` expressions produce shallow copies, which is useful when constructing modified semantic values.

---

## Supported Tables

The following tables and formats constitute the complete OpenType specification implementation.

<details>
<summary><b>Outline</b></summary>

* `glyf`
* `loca`
* `CFF `
* `CFF2`

</details>

<details>
<summary><b>Metrics and Metadata</b></summary>

* `head`
* `hhea`
* `hmtx`
* `maxp`
* `name`
* `OS/2`
* `post`
* `VORG`
* `vhea`
* `vmtx`
* `DSIG`
* `hdmx`
* `kern`
* `LTSH`
* `meta`
* `PCLT`
* `VDMX`

</details>

<details>
<summary><b>Character Mapping</b></summary>

* `cmap`

  * format 0
  * format 2
  * format 4
  * format 6
  * format 8
  * format 10
  * format 12
  * format 13
  * format 14

</details>

<details>
<summary><b>Layout</b></summary>

* `BASE`
* `GDEF`
* `GPOS`
* `GSUB`
* `JSTF`
* `MATH`

</details>

<details>
<summary><b>Color</b></summary>

* `COLR` — version 0 and version 1 paint graphs
* `CPAL`
* `sbix`
* `SVG `

</details>

<details>
<summary><b>Bitmap</b></summary>

* `CBDT`
* `CBLC`
* `EBDT`
* `EBLC`
* `EBSC`

</details>

<details>
<summary><b>Hinting</b></summary>

* `cvt `
* `fpgm`
* `prep`
* `gasp`

</details>

<details>
<summary><b>Variations</b></summary>

* `avar`
* `cvar`
* `fvar`
* `gvar`
* `HVAR`
* `MVAR`
* `STAT`
* `VVAR`

</details>

---

## Project Structure

```text
Mubarrat.Fonts.slnx
Directory.Build.props                    // shared identity, AOT/trimming policy
Directory.Build.targets                  // shared build configuration

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

Mubarrat.Fonts.Tests/
    Fonts/                               // real fonts used as fixtures
    SmokeTests.cs
```

The core library does not depend on the diagnostics library. Consumers that only need font parsing can reference `Mubarrat.Fonts.OpenType` without taking a dependency on the diagnostic layer.

---

## Performance

The library is designed around three architectural assumptions:

1. **A `FontFace` is a reusable parsed representation.** The font directory is parsed when the `FontFace` is created, while individual tables are resolved and materialized when first requested through `GetTable<T>()`.
2. **Sources support concurrent random access.** `Source` implementations are designed for reads at independent offsets. `FileSource` uses `RandomAccess`, avoiding a shared mutable file cursor.
3. **Common glyph operations should be inexpensive after table materialization.** `CmapTable.GetGlyphId` and `GlyfTable[int]` are intended to make repeated glyph access straightforward after the corresponding tables have been loaded.

To measure performance against a particular workload, add a benchmark project and reference `Mubarrat.Fonts.OpenType`.

Important measurements include:

* **Font initialization** — parsing the root directory and establishing the table structure.
* **Table materialization** — the cost of decoding a requested table into its semantic representation.
* **Hot glyph lookup** — repeated `CmapTable.GetGlyphId` and `GlyfTable[int]` operations after materialization.
* **Memory footprint** — the decoded object graph compared with the original font data.
* **Source throughput** — the effect of the backing `Source`, especially for large fonts and concurrent reads.

The appropriate trade-off depends on the workload. A representation that is efficient for repeated random glyph access can require more memory than keeping the original font entirely in its encoded form.

---

## Contributing

Issues and pull requests are welcome.

Before opening a PR:

* Run `dotnet test` and confirm all tests pass.
* Match the existing code style.
* Use `sealed record` for semantic reference types.
* Use `record struct` for wire-format headers where appropriate.
* Use `{ get; init; }` for semantic properties.
* Use explicit interface implementation for `Parse` and `FromHeader`.
* Keep `Tag` and `ReverseEndianness` public static and implicitly implemented where required.
* Add a test that reproduces the bug or covers the new behavior.

### Control-Flow Braces

Use braces for multi-statement control-flow bodies. A single-statement body does not require braces.

```csharp
if (condition)
    return value;

if (condition)
{
    DoSomething();
    return value;
}
```

### Parsing Conventions

Table parsers should avoid unnecessary allocations. Do not describe this as a requirement that `Parse` allocate nothing: returned records, arrays, strings, and other semantic data may necessarily require allocations.

If a table genuinely requires lazy access to source data, document that ownership and lifetime requirement explicitly rather than retaining a `Source` reference merely to avoid decoding data that should have been materialized.

### Static Interface Conventions

When implementing the library's static parsing infrastructure:

```csharp
static MyTable IRecord<MyTable>.Parse(ref Cursor cursor, object? context)
{
    // ...
}
```

For header conversion infrastructure:

```csharp
static MyRecord IHeaderRecord<MyRecord, MyHeader>.FromHeader(MyHeader header)
{
    // ...
}
```

Public static members such as `Tag` and `ReverseEndianness` should remain implicitly implemented:

```csharp
public static Tag Tag => "MYTB";
```

This distinction keeps the consumer-facing API clean while preserving static interface dispatch internally.

---

## License

MIT. See [LICENSE.md](LICENSE.md).
