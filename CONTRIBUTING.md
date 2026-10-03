# Contributing to Mubarrat.Fonts.OpenType

Thank you for considering a contribution. This document explains the conventions, workflows, and expectations for changes to this library. It is written for someone who has never opened the source and wants to add a table, fix a parser, or write a rule — not as a legal contract, but as a description of how the codebase is organized and why.

Read the [Design Principles](../README.md#design-principles) section of the README first if you haven't. Everything below builds on those decisions.

---

## Table of Contents

* [Prerequisites](#prerequisites)
* [Getting Started](#getting-started)
* [Development Workflow](#development-workflow)
* [Code Conventions](#code-conventions)

  * [File Layout](#file-layout)
  * [Namespaces](#namespaces)
  * [Type Kinds](#type-kinds)
  * [Braces and Formatting](#braces-and-formatting)
  * [XML Documentation](#xml-documentation)
  * [Visibility](#visibility)
  * [Interface Implementation](#interface-implementation)
* [The Record Pattern](#the-record-pattern)

  * [Leaf Records](#leaf-records)
  * [Base and Derived Records](#base-and-derived-records)
  * [Header Records](#header-records)
* [Big-Endian Handling](#big-endian-handling)
* [Adding a New Table](#adding-a-new-table)
* [Adding a New Analyzer](#adding-a-new-analyzer)
* [Rule IDs](#rule-ids)
* [Testing](#testing)
* [Performance](#performance)
* [Commit Messages](#commit-messages)
* [Pull Requests](#pull-requests)
* [Using AI Assistants](#using-ai-assistants)
* [Questions](#questions)

---

## Prerequisites

* **.NET 11 SDK** (preview or RC, as required by the repository). The project targets `net11.0`; use the language version configured by the repository rather than assuming a particular compiler version.
* **An editor that understands modern C#**: Visual Studio, JetBrains Rider, or VS Code with the C# Dev Kit.
* **For test fonts**: real `.ttf` and `.otf` files. Noto Sans, Roboto, and Noto Color Emoji from Google Fonts are the corpus used by the test suite. See [`Mubarrat.Fonts.Tests/Fonts/`](Mubarrat.Fonts.Tests/Fonts) for the current set.

Nothing else is required. There is no build script, no code generator to run, and no package to install beyond what `dotnet restore` handles.

---

## Getting Started

```bash
git clone https://github.com/Mubarrat/Mubarrat.Fonts.git
cd Mubarrat.Fonts
dotnet build
dotnet test
```

The first `dotnet restore` will restore the test framework and any other declared tooling. The core library has no NuGet dependencies.

If you are adding a table that requires a font fixture you do not have, open an issue first. The test corpus is deliberately small and each font is present for a reason.

---

## Development Workflow

1. **Open an issue first for non-trivial changes.** A new table, a new analyzer, or a change to the parser's public API should be discussed before code is written. Small bug fixes, documentation improvements, and additional test coverage can go straight to a PR.
2. **Branch from `main`.** Use a descriptive name: `add-cff2-analyzer`, `fix-loca-offset-check`, `docs-readme-quickstart`.
3. **Write the code, XML docs, and tests together.** A PR that adds a table but no test will be asked to add one.
4. **Run `dotnet test` locally before pushing.** The full suite should remain reasonably fast; avoid introducing unnecessary work into the normal test path.
5. **Open a pull request against `main`.** Fill in the PR template.

Maintainers review contributions as time permits. If a PR has no response for an extended period, a follow-up comment is welcome.

---

## Code Conventions

### File Layout

One public type per file is preferred, with the exception of small, tightly coupled helpers where keeping the types together clearly improves readability.

The file name should match the primary type:

```text
Tables/HeadTable.cs
Tables/CmapTable.cs
Tables/Variations/GvarTable.cs
```

A small set of tightly coupled types may share a file:

```text
Tables/CmapTable.cs
    CmapTable
    CmapFormat0
    CmapFormat4
    CmapFormat6
    ...
```

Do not create files named `Helpers.cs`, `Utilities.cs`, `Common.cs`, or similar catch-all names. If a helper is needed, either nest it inside the type that owns it or give it a descriptive name and its own file.

The directory structure should communicate the conceptual organization of the library.

---

### Namespaces

Namespaces are **file-scoped**:

```csharp
namespace Mubarrat.Fonts.OpenType.Tables;

public sealed record HeadTable : IOpenTypeTable<HeadTable>
{
    // ...
}
```

Never use block-scoped namespaces. Never place more than one namespace in a file.

The namespace should reflect the folder:

| Folder                            | Namespace                                       |
| --------------------------------- | ----------------------------------------------- |
| `OpenType/Binary/`                | `Mubarrat.Fonts.OpenType.Binary`                |
| `OpenType/Primitives/`            | `Mubarrat.Fonts.OpenType.Primitives`            |
| `OpenType/Tables/`                | `Mubarrat.Fonts.OpenType.Tables`                |
| `OpenType/Tables/Bitmap/`         | `Mubarrat.Fonts.OpenType.Tables.Bitmap`         |
| `OpenType/Diagnostics/`           | `Mubarrat.Fonts.OpenType.Diagnostics`           |
| `OpenType/Diagnostics/Analyzers/` | `Mubarrat.Fonts.OpenType.Diagnostics.Analyzers` |

`using` directives belong outside the namespace.

---

### Type Kinds

The library uses three primary type kinds, each for a specific purpose.

#### `sealed record`

Use `sealed record` for tables, subtables, and parsed data structures with reference semantics. Parsed semantic properties generally use `{ get; init; }`.

```csharp
public sealed record HeadTable : IOpenTypeTable<HeadTable>
{
    public static Tag Tag => "head";

    public ushort MajorVersion { get; init; }
    public ushort MinorVersion { get; init; }
    // ...
}
```

Tables and semantic records should generally be immutable after construction.

---

#### `record struct`

Use `record struct` for raw binary layouts, headers, and other small fixed-size transfer representations that may need to be filled field-by-field.

Fields are **public and mutable**.

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Header : IBigEndianStruct<Header>
{
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint NumTables;

    public static Header ReverseEndianness(Header value) => new()
    {
        MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
        MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
        NumTables = BinaryPrimitives.ReverseEndianness(value.NumTables),
    };
}
```

A raw wire struct represents the physical layout of bytes in the font. Its fields therefore remain directly accessible to parsing and writing infrastructure.

---

#### `readonly record struct`

Use `readonly record struct` for fully decoded value types such as points, ranges, coordinates, and other semantic values that do not need field-by-field mutation.

Prefer properties in these types:

```csharp
public readonly record struct CffPoint(double X, double Y);
public readonly record struct SourceSpan(long Offset, int Length);
```

Do not use a mutable wire-layout `record struct` merely because the type happens to be small. The distinction is semantic:

* raw on-disk representation → mutable `record struct`
* decoded immutable value → `readonly record struct`

Do not use plain `class` or `struct` unless there is a concrete reason that the record patterns cannot express the type correctly.

---

### Braces and Formatting

Single-statement bodies do not get braces. The body follows the condition on the same line when it fits, or on the next line when it does not.

```csharp
// Correct
if (offset < 0)
    throw new ArgumentOutOfRangeException(nameof(offset));

// Correct, wrapped
if (destination.Length > _length || offset > _length - destination.Length)
    throw new EndOfStreamException(
        $"Read of {destination.Length} bytes at offset {offset} exceeds length {_length}.");

// Wrong
if (offset < 0)
{
    throw new ArgumentOutOfRangeException(nameof(offset));
}
```

Braces are required when the body contains more than one statement:

```csharp
if (_isolated)
{
    ArgumentOutOfRangeException.ThrowIfNegative(offset);

    if (destination.Length > _length || offset > _length - destination.Length)
        throw new EndOfStreamException(...);
}
```

The same rule applies to `for`, `foreach`, `while`, `do`, `using`, `lock`, and `fixed`.

Other formatting rules:

* **Four-space indentation**, no tabs.
* `var` is permitted when the type is obvious from the right-hand side (`var cursor = source.CreateCursor()`).
* Use explicit types when the type is not obvious or when they improve readability.
* One statement per line.
* Trailing commas in multi-line initializers are preferred.
* Use `ArgumentNullException.ThrowIfNull` and `ArgumentOutOfRangeException.ThrowIf*` instead of manual argument checks.
* File-scoped namespaces only.
* `using` directives go outside the namespace.
* `System.*` namespaces come before other namespaces.
* Avoid unnecessary temporary variables and redundant conversions.
* Do not use formatting tricks that obscure the binary layout of a type.

The repository `.editorconfig` configures `csharp_prefer_braces = false` so IDEs do not suggest braces for single-statement bodies.

---

### XML Documentation

Every public type, method, property, field, and enum member carries an XML `<summary>` unless the member is compiler-generated or otherwise intentionally excluded from documentation.

Document parameters when their meaning is not obvious. Document exceptions that callers can reasonably encounter.

```csharp
/// <summary>Reads the table header and constructs the parsed table.</summary>
/// <param name="cursor">The cursor positioned at the table's first byte.</param>
/// <param name="context">The containing parse context, or <c>null</c> when none is required.</param>
/// <exception cref="InvalidDataException">The table version or required header value is invalid.</exception>
static HeadTable IRecord<HeadTable>.Parse(ref Cursor cursor, object? context)
{
    // ...
}
```

Do not write documentation that merely repeats a member name:

```csharp
// Bad
/// <summary>Gets the MajorVersion.</summary>
public ushort MajorVersion { get; init; }
```

Instead explain what the member means:

```csharp
/// <summary>Gets the major version of the table format.</summary>
public ushort MajorVersion { get; init; }
```

Good documentation describes:

* semantic meaning,
* units,
* valid ranges,
* offsets,
* version requirements,
* relationships to other tables,
* or relevant OpenType specification rules.

When useful, include specification links in `<seealso href="..."/>` or inline `<see href="...">...</see>` elements.

Keep XML documentation concise and technically precise. Do not turn straightforward members into essays.

---

### Visibility

Data types that describe font data are generally **public**, including:

* tables,
* subtables,
* enums,
* wire-layout structs,
* context records,
* public helper value types,
* and their corresponding data members.

Runtime implementation state may remain private or internal when exposing it would break invariants or unnecessarily expand the API.

```csharp
public sealed record HeadTable : IOpenTypeTable<HeadTable>
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        public ushort MajorVersion;
        // ...
    }
}
```

A stateful parser or interpreter may keep its mutable state private:

```csharp
public sealed class CffInterpreter
{
    private readonly double[] _stack = new double[StackLimit];
    private int _stackDepth;
}
```

The distinction is:

> If a type describes the shape or meaning of font data, it should generally be public. If a member exists only to maintain runtime state or an implementation invariant, it may be private.

This is particularly important for a bidirectional library. A writer should be able to reuse the same public wire-layout structures that the parser reads. Hiding a header struct forces serialization code to duplicate the layout and creates two representations of the same binary contract.

When unsure, ask:

> Could code that writes or constructs this part of the OpenType data reasonably need to name this type or member?

If yes, it should generally be public.

---

### Interface Implementation

The library intentionally distinguishes between **semantic/public members** and **parsing infrastructure members**.

The implementation rule is:

| Member                                  | Implementation                                                      | Reason                                                                                              |
| --------------------------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `IRecord<T>.Parse`                      | **Explicit**                                                        | Parsing is an infrastructure operation, not a convenience API on the concrete record.               |
| `IHeaderRecord<T, THeader>.FromHeader`  | **Explicit**                                                        | Conversion from the raw header to the semantic record belongs to the header-record infrastructure.  |
| Derived-record dispatch interfaces      | **Explicit where the interface defines the construction mechanism** | Keeps parser machinery out of the semantic public API.                                              |
| `IBigEndianStruct<T>.ReverseEndianness` | **Implicit/public**                                                 | It is a meaningful operation on the wire-layout type and is used directly by binary infrastructure. |
| `IOpenTypeTable<T>.Tag`                 | **Implicit/public**                                                 | The table tag is public type metadata and is used directly for table lookup.                        |

These are deliberate API decisions.

#### `Parse` is explicit

For a record implementing `IRecord<T>`, the parser is written as:

```csharp
static MyRecord IRecord<MyRecord>.Parse(ref Cursor cursor, object? context)
{
    // ...
}
```

Do **not** expose an additional public `Parse` method just to forward to the explicit implementation.

This keeps parsing construction behind the common record infrastructure while still allowing every record to satisfy the same static interface contract.

#### `FromHeader` is explicit

Header-to-record conversion is also explicit:

```csharp
static MyRecord IHeaderRecord<MyRecord, Header>.FromHeader(
    in Header header,
    object? context) => new()
{
    // ...
};
```

Do not make `FromHeader` a public static method.

It is an internal construction mechanism between the raw wire representation and the semantic record representation.

#### `ReverseEndianness` is implicit

Do not make `ReverseEndianness` explicit.

It is intentionally exposed:

```csharp
public static Header ReverseEndianness(Header value) => new()
{
    MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
    MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
};
```

The binary layer may invoke it through the `IBigEndianStruct<T>` constraint, while other code may also use it directly when constructing or examining a wire representation.

#### `Tag` is implicit

Table tags are semantic public metadata:

```csharp
public static Tag Tag => "head";
```

Do not hide `Tag` behind an explicit interface implementation.

The tag is used directly by table lookup, registration, diagnostics, and other code that needs to identify the table.

---

## The Record Pattern

The library has one dominant structural pattern. Tables, subtables, and semantic records follow it consistently.

### Leaf Records

A leaf record is a table or subtable with no internal discriminant — the parser reads a fixed set of fields and returns the value.

```csharp
public sealed record HeadTable : IOpenTypeTable<HeadTable>
{
    public static Tag Tag => "head";

    public ushort MajorVersion { get; init; }
    public ushort MinorVersion { get; init; }

    static HeadTable IRecord<HeadTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        return new HeadTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
        };
    }
}
```

Rules:

* Use `sealed record`.
* Implement `IOpenTypeTable<T>` for top-level tables or `IRecord<T>` for subtables.
* `Tag` is public and implicit.
* `Parse` is an **explicit** `IRecord<T>.Parse` implementation.
* `Parse` takes `ref Cursor` and `object? context`.
* Read what is needed from the cursor and construct the semantic record with an object initializer.
* Do not add a constructor solely to assign parsed properties.
* If construction logic genuinely needs more structure than an object initializer, use a named factory or another explicit mechanism rather than inventing an unrelated constructor pattern.

A typical table therefore exposes its **data**, not its **parser implementation**.

---

### Base and Derived Records

When a table or subtable has a discriminant that selects between multiple layouts — a format number, lookup type, version, or similar discriminator — use the base/derived record pattern.

```csharp
public abstract record BaseCoord : IRecord<BaseCoord>, IBaseRecord<BaseCoord>
{
    public ushort Format { get; init; }

    static BaseCoord IRecord<BaseCoord>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();

        return format switch
        {
            1 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat1>(ref cursor),
            2 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat2>(ref cursor),
            3 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat3>(ref cursor),
            _ => throw new InvalidDataException(
                $"BaseCoord format {format} is not defined."),
        };
    }
}
```

The base record owns the invariant fields and dispatches to the appropriate derived layout.

Derived records carry their layout-specific fields:

```csharp
public sealed record BaseCoordFormat1
    : BaseCoord,
      IBigEndianHeaderDerivedRecord<BaseCoord, BaseCoordFormat1, BaseCoordFormat1.Header>
{
    public override short Coordinate { get; init; }

    static BaseCoordFormat1 IHeaderDerivedRecord<
        BaseCoord,
        BaseCoordFormat1,
        Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        Coordinate = header.Coordinate,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        public short Coordinate;

        public static Header ReverseEndianness(Header value) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(value.Coordinate),
        };
    }
}
```

The rules are:

* the base parser is explicit;
* header-to-derived-record construction is explicit;
* `ReverseEndianness` is public and implicit;
* raw headers remain public;
* inheritance is used for format/layout dispatch only.

Do not introduce inheritance for unrelated semantic relationships. The library does not use class hierarchies merely to share convenience methods.

---

### Header Records

When a record is represented on disk by a fixed-layout header followed by additional data, implement the appropriate header-record interface:

```csharp
public sealed record BaseGlyphPaintRecord
    : IBigEndianHeaderRecord<BaseGlyphPaintRecord, BaseGlyphPaintRecord.Header>
{
    public ushort GlyphId { get; init; }
    public Paint Paint { get; init; } = null!;

    static BaseGlyphPaintRecord IHeaderRecord<BaseGlyphPaintRecord, Header>.FromHeader(
        in Header header,
        object? context) => new()
    {
        GlyphId = header.GlyphId,
        Paint = ((ParentContext)context!).ParentSource.ParseRecordAt<Paint>(
            header.PaintOffset),
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        public ushort GlyphId;
        public uint PaintOffset;

        public static Header ReverseEndianness(Header value) => new()
        {
            GlyphId = BinaryPrimitives.ReverseEndianness(value.GlyphId),
            PaintOffset = BinaryPrimitives.ReverseEndianness(value.PaintOffset),
        };
    }
}
```

The rules are strict:

* `Header` is **public**.
* Header fields are **public and mutable**.
* `FromHeader` is **explicit**.
* `ReverseEndianness` is **public and implicit**.
* Parsed semantic properties are normally `{ get; init; }`.
* Relative offsets are resolved through the existing source/cursor infrastructure instead of manually manipulating positions.

The `Header` type is part of the public binary contract. It is not merely a parser implementation detail.

---

## Big-Endian Handling

OpenType data is big-endian on disk. Every multi-byte field must be interpreted accordingly.

The library provides three primary mechanisms.

### Scalar reads

Use methods such as:

```csharp
cursor.ReadUInt16();
cursor.ReadInt32();
cursor.ReadUInt32();
```

These perform the required byte-order conversion automatically.

Use scalar reads when only a small number of fields are being consumed individually.

### Struct reads

Use:

```csharp
cursor.ReadBigEndianStruct<T>();
```

when a fixed-layout struct represents multiple fields.

`T` must implement `IBigEndianStruct<T>`.

The struct's public `ReverseEndianness` implementation performs the field-by-field conversion.

### Array reads

Use:

```csharp
cursor.ReadBigEndianStructArray<T>(count);
cursor.ReadUInt16Array(count);
```

and the corresponding primitive helpers for arrays.

These APIs are preferred over manually reading individual elements when the data is already represented as a homogeneous binary sequence.

---

### `ReverseEndianness`

Every wire-layout struct containing multi-byte data implements `IBigEndianStruct<T>` with a **public, implicit** `ReverseEndianness` method.

```csharp
public static Header ReverseEndianness(Header value) => new()
{
    MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
    MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
    NumTables = BinaryPrimitives.ReverseEndianness(value.NumTables),
};
```

Do **not** explicitly implement `ReverseEndianness`.

The rule is:

> If a struct contains any field larger than one byte, or any nested struct containing such a field, it must implement `IBigEndianStruct<T>` and reverse every multi-byte field.

Structs whose fields are all `byte` or `sbyte` do not need the interface and may use `ReadStruct<T>()` instead.

For example:

* `CpalTable.ColorRecord` contains only bytes and does not need `IBigEndianStruct<T>`.
* `HeadTable.Header` contains multiple multi-byte fields and does need it.

If a struct contains nested wire types, recurse:

```csharp
public static Header ReverseEndianness(Header value) => new()
{
    FontRevision = Fixed.ReverseEndianness(value.FontRevision),
    // ...
};
```

Missing a nested reversal is a common and serious parsing bug.

When reviewing a wire struct, compare:

1. the number of multi-byte fields in the declaration;
2. the number of corresponding conversions in `ReverseEndianness`.

They should match exactly, including nested wire types.

---

## Adding a New Table

Walk through the following steps when adding a table that is not currently supported.

### 1. Create the file

Place the file in the appropriate folder under `Tables/`.

Use the specification's conceptual grouping:

```text
Tables/
Tables/Bitmap/
Tables/Color/
Tables/Layout/
Tables/Metadata/
Tables/Outlines/
Tables/Hinting/
Tables/Variations/
```

Global tables such as `head` may live directly under `Tables/`.

---

### 2. Declare the type

A top-level table is normally a sealed record implementing `IOpenTypeTable<T>`:

```csharp
public sealed record MyNewTable : IOpenTypeTable<MyNewTable>
{
    public static Tag Tag => "XXXX";

    // ...
}
```

`Tag` is public and implicit.

---

### 3. Add the header struct

If the table has a fixed-size header, add:

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Header : IBigEndianStruct<Header>
{
    // fields in wire order

    public static Header ReverseEndianness(Header value) => new()
    {
        // reverse every multi-byte field
    };
}
```

The declaration must match the specification exactly:

* field order must match;
* field sizes must match;
* packing must be `Pack = 1`;
* the resulting size must match the specification;
* every multi-byte field must be reversed.

Use `Unsafe.SizeOf<Header>()` where appropriate to validate the actual layout.

---

### 4. Declare the semantic fields

Each semantically meaningful field becomes a public property:

```csharp
public ushort MajorVersion { get; init; }
public IReadOnlyList<Record> Records { get; init; } = [];
```

Use the specification's terminology, translated to normal C# naming conventions.

If a value is optional by specification, model its absence explicitly rather than inventing a sentinel unless the specification itself defines one.

---

### 5. Implement `Parse` explicitly

The parser is an explicit interface implementation.

```csharp
static MyNewTable IRecord<MyNewTable>.Parse(
    ref Cursor cursor,
    object? context)
{
    Header header = cursor.ReadBigEndianStruct<Header>();

    return new MyNewTable
    {
        // ...
    };
}
```

Do **not** expose a second public `Parse` method.

If the table uses `IHeaderRecord<T, THeader>`, keep `FromHeader` explicit as well:

```csharp
static MyNewTable IHeaderRecord<MyNewTable, Header>.FromHeader(
    in Header header,
    object? context) => new()
{
    // ...
};
```

`ReverseEndianness` remains public and implicit:

```csharp
public static Header ReverseEndianness(Header value) => new()
{
    // ...
};
```

When a table contains subtables at offsets, use the existing source abstractions:

```csharp
cursor.Source.ParseRecordAt<T>(offset, context);
```

Do not manually reconstruct cursors or duplicate offset-resolution logic.

---

### 6. Write a test

At minimum, add a real-font test:

```csharp
[Fact]
public void MyNewTable_FromRoboto_Parses()
{
    var face = Parse.Face(TestFonts.Bytes("Roboto-Regular.ttf"));
    var table = face.GetTable<MyNewTable>();

    Assert.Equal(expected, table.SomeField);
}
```

If the table has version variants, offset edge cases, or branches that real fonts do not exercise, add byte-level tests using a minimal `byte[]`.

---

### 7. Add documentation

Add XML documentation to every new public type and member.

Use specification links where appropriate and document:

* wire offsets;
* sizes;
* version rules;
* relationships to other tables;
* valid ranges;
* optionality;
* and unusual implementation details.

---

### 8. Update the README

Add the table to the supported-tables list and update any related documentation or feature matrix.

---

## Adding a New Analyzer

Adding an analyzer for a table already covered by the parser follows this pattern.

### 1. Create the file

Create:

```text
Diagnostics/Analyzers/<TableName>Analyzer.cs
```

The class is `<TableName>Analyzer`.

### 2. Implement `IFontAnalyzer`

The analyzer is normally sealed, stateless, and exposed through a singleton:

```csharp
public sealed class MyNewTableAnalyzer : IFontAnalyzer
{
    private MyNewTableAnalyzer() { }

    public static MyNewTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        // ...
    }
}
```

The analyzer contains runtime behavior, not font-data representation. Its internal state may therefore remain private.

### 3. Declare descriptors

Each rule gets a:

```csharp
public static readonly DiagnosticDescriptor
```

Use the rule ID format defined in [Rule IDs](#rule-ids).

The severity reflects the meaning of the rule:

* `Error` — definite specification violation.
* `Warning` — suspicious or potentially problematic data that remains legal.
* `Information` — notable information that is not itself an error.

### 4. Write the checks

Group related checks into private static methods by concern.

Before requesting another table, verify that it exists:

```csharp
if (!face.Directory.Contains(OtherTable.Tag))
    return;
```

Do not assume a cross-referenced table is present merely because the current table normally occurs beside it.

### 5. Report diagnostics

Report through the shared diagnostic abstractions:

```csharp
bag.Add(descriptor.Create(args, ...));
```

The shared creation path should populate the severity, message arguments, table, field, and span information consistently.

### 6. Register the analyzer

Add it to `FontAnalyzer.Analyze`:

```csharp
Run<MyNewTable>(face, MyNewTableAnalyzer.Instance, bag);
```

### 7. Write a corpus test

Every analyzer gets a corpus test:

```csharp
[Theory]
[MemberData(nameof(AllFonts))]
public void AllFonts_HaveNoSevereMyNewTableDiagnostics(string path)
{
    var face = Parse.Face(TestFonts.Bytes(path));

    if (!face.Directory.Contains(MyNewTable.Tag))
        return;

    var bag = new DiagnosticBag();
    MyNewTableAnalyzer.Instance.Analyze(face, bag);

    var severe = bag.Diagnostics
        .Where(d => d.Severity <= DiagnosticSeverity.Warning)
        .ToList();

    Assert.Empty(severe);
}
```

Information-level diagnostics are expected to occur and are therefore excluded from the severe-diagnostic assertion.

---

## Rule IDs

Rule IDs use:

```text
OT.<table>.<slug>
```

where:

* `OT` is the library prefix.
* `<table>` is the lowercase sfnt tag with non-alphanumeric characters removed.
* `<slug>` is a short kebab-case description of what the rule checks.

Examples:

| Rule                               | ID                         |
| ---------------------------------- | -------------------------- |
| `head.magicNumber != 0x5F0F3CF5`   | `OT.head.magic-number`     |
| `head.unitsPerEm` out of range     | `OT.head.units-per-em`     |
| `cmap` encoding records unsorted   | `OT.cmap.records-unsorted` |
| `hmtx` count disagrees with `maxp` | `OT.hmtx.count-vs-maxp`    |
| `OS/2` weight class invalid        | `OT.os2.weight-class`      |
| `cvt ` data malformed              | `OT.cvt.data`              |

Slugs describe the **failure or invariant**, not merely the field.

Prefer:

```text
OT.cmap.records-unsorted
```

over:

```text
OT.cmap.encoding-records
```

### Cross-table rules

Rules that span multiple tables use the table of the **dependent** — the table that is considered wrong if the rule fires.

For example:

```text
OT.hmtx.count-vs-maxp
```

belongs to `hmtx` when the rule checks that the number of horizontal metrics is consistent with `maxp.numGlyphs`.

### Stability

Rule IDs are stable API identifiers.

Once a rule has been published:

* do not rename its ID;
* do not silently reuse it for another invariant;
* changing its message does not change its ID;
* changing its severity does not change its ID;
* changing its implementation does not change its ID.

If a rule is removed, its ID is retired and should never be reused for a different rule.

Run `RuleIdConventionTests` to validate the convention automatically.

---

## Testing

Three kinds of tests are used, roughly in order of breadth.

### Corpus tests

Corpus tests run against the real font collection under `Fonts/`.

They are the preferred first-line tests because they exercise the parser against real OpenType data.

Every analyzer and every parser should have at least one meaningful corpus test.

### Byte-level tests

Use a minimal `byte[]` when you need to cover:

* version-specific branches;
* malformed headers;
* offset arithmetic;
* absent optional data;
* invalid counts;
* unusual boundary conditions;
* or specification branches not represented in the real font corpus.

These tests should remain small and targeted.

### Property tests

Use property-style tests selectively for structural invariants such as:

* reversing endianness twice restores the original value;
* a fixed-size struct has the expected size;
* encoding and decoding preserve a known value;
* range boundaries remain inclusive/exclusive as defined by the specification.

Not every type needs property tests.

Run the full suite with:

```bash
dotnet test
```

Avoid adding tests that perform redundant parsing or repeated font loading. Test setup should be shared where the existing framework provides suitable helpers.

---

### Test Font Conventions

Test fonts live in:

```text
Mubarrat.Fonts.Tests/Fonts/
```

The test project should copy them to the output directory through its project configuration:

```xml
<None Include="Fonts\**\*" CopyToOutputDirectory="PreserveNewest" />
```

Do not commit fonts that are not required by the test suite.

Every font in the corpus should exist for a reason and should be referenced by at least one test.

When adding a new font:

1. give it a descriptive filename such as `NotoSansCJK-Regular.otf`;
2. verify that its license permits redistribution;
3. prefer fonts whose licensing terms are clear and compatible with repository distribution;
4. add or update the corresponding helper in `TestFonts.cs`;
5. add the test that actually requires the font.

Google Fonts commonly contains fonts licensed under OFL or Apache-2.0, but always verify the specific font before committing it.

---

## Performance

The library's performance depends on several properties that are easy to break accidentally.

### 1. Keep parsing allocation-conscious

A `Parse` method should allocate only the data structures it actually needs to return.

Avoid unnecessary allocations such as:

```csharp
var list = new List<Record>();
// ...
return list.ToArray();
```

when a direct array allocation or an existing binary-reader helper can produce the final representation directly.

Avoid:

* unnecessary LINQ on hot paths;
* boxing value types;
* temporary strings created for every record;
* redundant arrays;
* repeated encoding/decoding;
* repeated cursor construction when a source helper already exists.

If a temporary array of structs is required, prefer the existing `ReadBigEndianStructArray<T>` infrastructure.

If a temporary array of primitives is required, use the appropriate primitive-array reader.

### 2. Avoid retaining `Source` unnecessarily

Most tables should materialize their semantic data during parsing and release the underlying `Source`.

A table should retain a `Source` only when on-demand access is genuinely part of its design.

If a new table needs to retain source data, document why and consider the lifetime and memory consequences before introducing it.

### 3. Do not introduce reflection

Do not add:

```text
System.Reflection
System.Linq.Expressions
```

to the parsing architecture.

The library aims to remain compatible with trimming and AOT-oriented scenarios.

Prefer:

* static abstract interface members;
* generic constraints;
* compile-time dispatch;
* direct data structures.

### 4. Measure changes to hot paths

If you change a commonly executed operation such as:

```text
CmapTable.GetGlyphId
GlyfTable[int]
Cursor.Read*
Source.Read*
```

measure the effect before and after when practical.

There is no requirement to benchmark every change, but performance-sensitive changes should be based on measurement rather than intuition.

---

## Commit Messages

Use the **present tense and imperative mood**.

The subject describes what the commit does to the repository:

```text
Add CFF2 analyzer with 13 rules
```

```text
Fix the SliceSource collapse when the outer slice is isolated
```

```text
Rename OffsetSource to SliceSource and unify the length semantics
```

Do not write:

```text
Added CFF2 analyzer
Fixed SliceSource
Renamed OffsetSource
```

The subject line should be no more than 72 characters where practical.

For a non-trivial change, add a body after a blank line:

```text
Add CFF2 analyzer with 13 rules

Covers header validation, FDArray/FDSelect cross-references, and the
variation store presence check. Each rule is documented in the analyzer
file with a specification reference.
```

Do not use prefixes such as:

```text
[fix]
feat:
fix:
WIP
```

If a commit resolves an issue, add the issue reference:

```text
Fixes #42.
```

A commit message should describe the patch itself rather than serve as a diary entry about when the work was performed.

---

## Pull Requests

A PR should be focused.

If you discover an unrelated bug while implementing a feature, open a separate PR unless the two changes are inseparable.

Before opening the PR, confirm:

* [ ] `dotnet build` succeeds with zero warnings.
* [ ] `dotnet test` passes.
* [ ] Every new public type has XML documentation.
* [ ] Every new public member has meaningful XML documentation.
* [ ] Every new data type representing font data is `public`.
* [ ] Every raw wire-layout struct uses the appropriate packing and endianness pattern.
* [ ] Every `Parse` implementation follows the explicit-interface rule.
* [ ] Every `FromHeader` implementation follows the explicit-interface rule.
* [ ] Every `ReverseEndianness` implementation is public and implicit.
* [ ] Every new table has a real-font corpus test.
* [ ] Every new analyzer has an `AllFonts_HaveNoSevere*Diagnostics` test.
* [ ] Every new rule ID follows `OT.<table>.<slug>`.
* [ ] The README supported-tables list is updated when a new table is added.
* [ ] No unnecessary `System.Reflection` or `System.Linq.Expressions` usage was introduced.
* [ ] No `unsafe` code was introduced without a documented and justified reason.
* [ ] No unrelated formatting churn obscures the actual change.

The PR description should include:

1. **What the change does.**
2. **Why it is needed.**
3. **How it was tested.**

For example:

```text
What:
Adds parsing support for the VVAR table.

Why:
Variable fonts using vertical metric variations were previously ignored.

Testing:
Added a VVAR corpus test using the existing variable-font fixture and
byte-level tests for optional DeltaSetIndexMap offsets.
```

A maintainer may ask questions about whether the change fits the existing architecture. That is a normal part of maintaining consistency across the library.

---

## Using AI Assistants

AI tools are welcome during development. You may use Copilot, Claude, ChatGPT, Cursor, or another tool without being required to disclose its use.

The requirement is the same as for manually written code:

> The contribution must follow the repository's conventions and must be reviewed by the contributor before submission.

AI-generated code is not exempt from any architectural rule.

In particular, verify:

* **Record pattern**

  * `sealed record` for semantic tables and records;
  * `public record struct Header : IBigEndianStruct<Header>` for fixed-layout headers;
  * `readonly record struct` for immutable decoded values.

* **Interface implementation**

  * `Parse` is explicitly implemented;
  * `FromHeader` is explicitly implemented;
  * `ReverseEndianness` is publicly and implicitly implemented;
  * `Tag` is publicly and implicitly implemented.

* **Visibility**

  * font-data types are public;
  * runtime state remains private where appropriate.

* **Binary correctness**

  * field order matches the specification;
  * offsets are measured from the correct origin;
  * sizes match the specification;
  * all multi-byte fields are reversed correctly;
  * nested wire structs are reversed recursively.

* **Documentation**

  * every public member is documented meaningfully;
  * specification links are correct;
  * documentation does not merely repeat member names.

* **Testing**

  * each parser has real-font coverage;
  * each analyzer has corpus coverage;
  * edge cases have byte-level tests where necessary.

* **Architecture**

  * no unnecessary dependencies;
  * no reflection-based parsing;
  * no duplicated offset logic;
  * no unnecessary source retention;
  * no unrelated abstraction introduced merely because an AI suggested it.

AI-generated code that follows these conventions is welcome. AI-generated code that does not follow them should be changed before the PR is submitted.

The source of the code does not change the review standard. The maintainer will review the resulting code, its tests, its documentation, and its fit with the architecture.

---

## Questions

If something in this document is unclear, or if you are unsure whether a proposed change fits the conventions, open an issue or start a discussion.

The conventions exist to keep the codebase readable, predictable, and internally consistent. They are not intended to prevent legitimate new designs; unusual cases should be discussed rather than forcing them into an existing pattern incorrectly.

Thank you for contributing.
