# Mubarrat.Fonts

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../LICENSE.md)
[![.NET 11](https://img.shields.io/badge/.NET-11.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language: C#](https://img.shields.io/badge/language-C%23-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)

**Mubarrat.Fonts** is a low-level font-engineering framework for .NET. It provides binary-reading infrastructure, typed font records, SFNT directory access, and implementations of font tables and structures.

The long-term objective is **complete, specification-driven coverage of the relevant font formats**—not merely the subset implemented today. The framework is being developed incrementally, with an emphasis on explicit binary layouts, predictable parsing, a strongly typed API, and an architecture suitable for trimming and Native AOT.

> **Development status:** Active development. Specification coverage and API stability vary by component. Check the implementation and tests for the exact functionality you need.

This README documents the `Mubarrat.Fonts/` project only. Repository-wide contribution guidance and licensing information are maintained at the repository root.

## Contents

- [Goals](#goals)
- [Scope](#scope)
- [Architecture](#architecture)
- [Implementation and support status](#implementation-and-support-status)
- [Quick start](#quick-start)
- [Working with the lower-level API](#working-with-the-lower-level-api)
- [Design principles](#design-principles)
- [Roadmap](#roadmap)
- [Building and testing](#building-and-testing)
- [Contributing](#contributing)
- [License](#license)

## Goals

Mubarrat.Fonts aims to provide a reusable foundation for font-related software, including font inspectors, analyzers, validators, converters, editors, and other font-engineering tools.

The project's goals are to:

- **Cover the specifications comprehensively.** Implement the relevant formats, tables, records, versions, and data structures, including less commonly used and legacy features.
- **Make binary data accessible through typed APIs.** Represent font structures with explicit types instead of requiring every consumer to interpret raw byte sequences independently.
- **Keep parsing infrastructure reusable.** Separate data sources, cursors, record parsing, and format-specific structures.
- **Avoid unnecessary runtime machinery.** Prefer compile-time type relationships and static dispatch over reflection-driven discovery and registries.
- **Support efficient data access.** Make random-access sources and bounded parsing contexts available to format implementations.
- **Keep the dependency surface small.** Restrict dependencies to `System.*` and `Microsoft.*` packages and framework components.
- **Build toward trimming and Native AOT.** Design the architecture with static analysis, predictable reachability, and ahead-of-time compilation in mind.
- **Support systematic validation.** Build toward a dedicated diagnostics and analysis layer rather than treating successful parsing as proof of specification conformance.

## Scope

Mubarrat.Fonts is the **font-data and parsing layer**, not a complete text-rendering engine.

Its responsibility is to read font data, expose its structures, and provide a foundation for higher-level font tools. Text shaping, glyph rasterization, graphics integration, and user-interface functionality are separate concerns unless explicitly introduced by a future component.

The intended format scope includes the following families:

| Format family | Long-term objective | Current status |
|---|---|---|
| OpenType | Comprehensive table, layout, and variation coverage | Implemented incrementally; completeness varies by table |
| TrueType | Comprehensive outline, metric, and related data coverage | Implemented incrementally; completeness varies by structure |
| Apple Advanced Typography (AAT) | Coverage of relevant tables and state-machine structures | Implemented incrementally; completeness varies |
| WOFF | Container parsing and decompression | Coverage to be established |
| WOFF2 | Container parsing and decompression | Coverage to be established |
| Type 1 | Relevant format structures and parsing | Coverage to be established |
| Other font formats | Additional formats where included in the project's scope | Evaluated as development progresses |

The table describes the intended scope, not a claim that every format is currently supported. In particular, the presence of a table implementation does not establish support for every version, subtable format, edge case, or conformance requirement.

## Architecture

The project separates generic binary-reading infrastructure from font-format-specific records and structures.

### Binary data sources

A `Source` abstracts access to binary data. Source implementations provide different ways to access the underlying bytes, allowing parsing code to operate without being tied to a single storage mechanism.

The project includes source implementations such as:

- `FileSource` for file-backed data.
- `MemorySource` for in-memory data.
- `SliceSource` for a bounded view of existing data.
- `StreamMemorySource` for stream-backed data held in memory.

This abstraction is intended to keep storage decisions separate from the logic that interprets font structures.

### Cursors and bounded parsing

`Cursor` is a `ref struct` used to read binary data while maintaining a position within a parsing context. It provides the foundation for reading fields and sequences without scattering offset arithmetic throughout format-specific code.

Bounded cursors and source slices also help make the extent of a table or record explicit. Format implementations can interpret their data within the relevant region rather than treating the entire font as an undifferentiated byte array.

### Typed records and static dispatch

Record types describe the binary structures being read. The record infrastructure provides a common way to associate a type with its parsing logic.

Font-table types implement `IFontTable<T>`, which extends `IRecord<T>` and exposes a static table `Tag`. This allows table selection to be expressed through types and compile-time relationships rather than requiring a reflection-based table registry.

The intended benefits are:

- Explicit associations between table types and their tags.
- Reusable parsing infrastructure for different records.
- Less dependence on runtime type discovery.
- A foundation for trimming-friendly and Native-AOT-oriented applications.

These are architectural goals; individual implementations still need to be tested and verified against the relevant publishing configurations.

### SFNT font faces and directories

`SfntFontFace` represents the SFNT font container and its table directory. It provides typed table access through APIs such as `GetTable<T>()` and `TryGetTable<T>()`.

The directory describes the tables available in the font. Individual table types are responsible for interpreting their corresponding binary structures.

This separates three concerns:

1. Accessing the underlying font data.
2. Discovering and locating tables in the container.
3. Parsing a particular table into its typed representation.

### Primitive types

The `Primitives/` area contains types for common font-format values that do not map directly to ordinary C# primitive types or that benefit from explicit representations.

Examples include:

- `Tag` — a four-byte table identifier.
- `F2Dot14` — a fixed-point representation used by font formats.
- `Fixed` — a fixed-point numeric representation.
- `Int24` and `UInt24` — signed and unsigned 24-bit integer representations.

These types provide a shared foundation for higher-level records and tables.

## Implementation and support status

The framework is developed toward complete specification coverage. Its current state should be assessed at the level of individual formats, tables, versions, and data structures.

| Component or capability | Current position |
|---|---|
| Binary source abstractions | Implemented |
| Cursor-based binary reading | Implemented |
| Shared record and table abstractions | Implemented |
| SFNT directory parsing and typed table access | Implemented |
| Primitive font-format value types | Implemented |
| OpenType and TrueType table structures | Broad implementation in progress; verify individual structures |
| OpenType layout structures, including GSUB, GPOS, and GDEF | Implemented incrementally; verify individual lookups, formats, and edge cases |
| CFF/CFF2, variation, bitmap, color, and mathematical font structures | Implemented incrementally; verify individual tables and formats |
| AAT structures | Implemented incrementally; verify individual tables and state-machine formats |
| Dedicated conformance diagnostics and analysis | A first-class development objective; coverage depends on the available implementation |
| Complete WOFF, WOFF2, Type 1, and other-format support | Not claimed by this README; verify implementation before relying on it |
| Native AOT and trimming | Design goal; publishing compatibility must be verified |

**Important distinctions:**

- **Implemented** means the corresponding infrastructure or implementation exists; it does not imply exhaustive specification coverage.
- **Partial** means some of the relevant structures or cases are implemented, while others remain incomplete.
- **Planned** means the capability is part of the intended scope but is not yet available as a complete implementation.
- **Unsupported or unverified** means consumers should not assume the capability works without inspecting the code and tests.

For a particular table or feature, the source code and tests are more authoritative than a broad format-level label. As implementation and test coverage expand, the support matrix should become more granular and identify the applicable versions, formats, and known limitations.

## Quick start

The project is currently intended to be consumed through a project reference. No published NuGet package is assumed by this README.

### Add a project reference

From a consuming project, add a reference to the project file:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Mubarrat.Fonts/Mubarrat.Fonts.csproj" />
</ItemGroup>
```

Adjust the path to match your repository layout.

### Open an SFNT font and access a table

The following illustrates the intended usage pattern with the existing source, SFNT, and table APIs. It assumes that the selected font contains the relevant table and that the referenced APIs are available in your checked-out version.

```csharp
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Sfnt;
using Mubarrat.Fonts.Tables;

using var source = FileSource.Open("font.ttf");

SfntFontFace face =
    source.ParseRecordAt<SfntFontFace>(0);

CmapTable cmap =
    face.GetTable<CmapTable>();

int glyphId = cmap.GetGlyphId('A');

Console.WriteLine($"Glyph ID: {glyphId}");
```

This example demonstrates the general flow:

1. Open the font through a binary source.
2. Parse the SFNT font face at offset zero.
3. Retrieve the character-to-glyph mapping table.
4. Request the glyph ID associated with a character.

A glyph ID of zero conventionally represents the `.notdef` glyph. A mapping result of zero is therefore not, by itself, proof that a character has an explicit mapping in the font.

The example is not a complete font validator and does not demonstrate support for every possible font container. Keep the source alive while the font face and its table objects depend on it.

## Working with the lower-level API

The framework exposes both reusable parsing infrastructure and typed font structures. These layers are useful for different tasks.

### Reading records

Use the shared record infrastructure when implementing or consuming structures whose binary layout is described by a font specification.

A record implementation should make its parsing requirements explicit, including its input context, fields, bounds, and any format-dependent interpretation. The common infrastructure should handle generic reading mechanics wherever practical.

### Accessing tables

Prefer typed table access when a corresponding table implementation exists:

```csharp
CmapTable cmap = face.GetTable<CmapTable>();
```

Use `TryGetTable<T>()` when table absence is an expected condition that should be handled without treating it as an exceptional path.

A table's existence does not guarantee that every internal structure is valid. Consumers that process untrusted fonts should distinguish missing tables, malformed data, unsupported formats, and successful parsing.

### Character-to-glyph mapping

Character-to-glyph mapping deserves particular care. A font may contain multiple `cmap` subtables, and the appropriate mapping depends on their supported encodings and formats.

`CmapTable.GetGlyphId(int)` uses the highest-ranked subtable. `TryGetGlyphIdFromAnySubtable(int, out int)` provides a way to search across subtables.

These operations have different semantics. Searching all subtables should not be treated as interchangeable with selecting the preferred subtable, and a returned glyph ID of zero should not automatically be interpreted as proof that no subtable contains a mapping.

### Adding a new table or record

When extending the framework:

1. Identify the authoritative specification and the exact structure being implemented.
2. Reuse existing primitive types, source abstractions, cursors, and record conventions where appropriate.
3. Implement the required fields and format-specific behavior explicitly.
4. Handle offsets, lengths, counts, and nested structures with careful bounds checking.
5. Add tests for valid input, malformed input, boundary conditions, and supported format variants.
6. Document any unsupported variants rather than silently implying complete coverage.
7. Consider trimming, Native AOT, and the project's dependency policy when designing the implementation.

A new type should fit the existing architecture rather than introduce a parallel parsing mechanism without a clear reason.

## Design principles

### Specification-first implementation

The goal is not simply to parse a few common fonts. The framework aims to cover relevant specifications systematically, including uncommon structures and variants that are easy to overlook.

Implementation progress should therefore be measured against the applicable specifications, not only against whether a handful of popular fonts can be opened.

### Explicit types and predictable dispatch

The framework favors typed records, explicit tags, and static dispatch where these choices make the code easier to inspect and more compatible with static analysis.

This does not eliminate the need for runtime validation. Font files contain untrusted data, and even a strongly typed parser must validate lengths, offsets, counts, and relationships encoded in the file.

### Reusable infrastructure

Storage, binary reading, record parsing, table discovery, and format-specific interpretation should remain distinct responsibilities wherever practical.

This reduces duplication and allows new formats and records to build on shared infrastructure.

### Dependency discipline

The project follows a strict dependency policy: dependencies should be limited to `System.*` and `Microsoft.*` packages and framework components.

New dependencies should not be introduced casually. Where an external package appears convenient, first consider whether the requirement can be met using the platform or the existing project architecture.

### Trimming and Native AOT awareness

The architecture aims to remain compatible with trimming and Native AOT. Static dispatch and explicit type relationships support that direction, but they are not sufficient on their own to establish compatibility.

Actual support must be demonstrated through builds, publishing tests, and runtime tests for the relevant configurations.

### Parsing versus conformance

A parser and a conformance analyzer answer different questions.

A parser determines whether data can be interpreted according to its implementation. A conformance analyzer evaluates whether that data satisfies the applicable specification requirements and constraints.

Mubarrat.Fonts aims to support both responsibilities, with systematic diagnostics and analysis as an important part of its long-term development. Successfully parsing a font must not be presented as proof that the font is fully conformant.

## Roadmap

The roadmap describes the direction of development, not a promise that each milestone is already implemented or scheduled for a particular release.

### 1. Strengthen the parsing foundation

- Consolidate shared binary-reading and record-parsing conventions.
- Improve bounds checking and handling of malformed input.
- Keep offsets, lengths, and parsing contexts explicit.
- Expand tests for primitive types, source implementations, cursors, and generic records.

### 2. Expand specification coverage

- Continue implementing OpenType and TrueType structures.
- Extend coverage of table versions, subtable formats, and less common structures.
- Continue developing AAT table and state-machine support.
- Track implementation coverage at the level of individual specification requirements.
- Record unsupported variants and known limitations precisely.

### 3. Complete format-family coverage

- Develop complete coverage of the relevant OpenType, TrueType, and AAT specifications.
- Add the required container handling for WOFF and WOFF2.
- Evaluate and implement the relevant Type 1 structures.
- Extend the framework to other font formats included in the project's scope.

Completeness should be demonstrated through traceable specification coverage and tests, not inferred from the presence of a format name or a small set of working examples.

### 4. Build the diagnostics and conformance layer

- Establish consistent diagnostic identifiers and reporting conventions.
- Detect malformed structures and invalid cross-references.
- Add checks for specification requirements that cannot be established by parsing alone.
- Distinguish errors, warnings, unsupported features, and informational findings.
- Build tests from valid examples, deliberately malformed inputs, and edge cases.

The objective is to make validation systematic and reusable rather than scattering unrelated checks throughout individual table implementations.

### 5. Verify performance and deployment characteristics

- Benchmark representative parsing workloads.
- Evaluate allocations and repeated access to binary data.
- Test trimming and Native AOT publishing configurations.
- Preserve correctness and maintainability when optimizing hot paths.
- Verify behavior on the supported .NET runtimes and deployment targets.

Performance optimizations should follow measurement and retain correct behavior for boundary cases and malformed inputs.

### 6. Improve documentation and usability

- Document individual table implementations and their supported formats.
- Publish precise support matrices with implementation and test status.
- Add focused examples for directory access, table parsing, record implementation, and diagnostics.
- Clarify API stability and compatibility as the framework matures.

## Building and testing

The project targets .NET 11 and is under active development.

From the repository root, build the project with:

```bash
dotnet build Mubarrat.Fonts/Mubarrat.Fonts.csproj
```

Run the relevant test project or test suite from the repository according to its project layout. For example, if a test project is available:

```bash
dotnet test path/to/test-project.csproj
```

The second command is a template; replace the path with the actual test project.

A successful build confirms that the selected code compiles. It does not establish complete specification coverage, conformance with every font format, or Native AOT compatibility. Those claims require the appropriate tests and verification.

## Contributing

Contributions toward specification coverage, parsing correctness, diagnostics, tests, documentation, and performance are welcome.

Before contributing, read the repository-level [Contributing Guide](./CONTRIBUTING.md).

When proposing a parser or table change, include the relevant specification reference, explain the supported formats or cases, and provide tests that demonstrate the intended behavior. If a change implements only part of a specification, make that boundary explicit.

## License

Mubarrat.Fonts is licensed under the **MIT License**. The license file is maintained at the repository root:

- [LICENSE.md](../LICENSE.md)

Refer to that file for the complete license terms.
