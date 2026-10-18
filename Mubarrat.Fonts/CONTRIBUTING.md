# Contributing to Mubarrat.Fonts

This guide describes the implementation conventions, architectural patterns, and invariants used by the `Mubarrat.Fonts` library. It is intended for contributors working inside this project directory, especially when implementing binary readers, primitive value types, SFNT structures, or OpenType and Apple Advanced Typography (AAT) tables.

The goal is not to enforce a uniform implementation on every file. Different font structures have different layouts and parsing requirements. Instead, contributions should follow the established abstractions where they fit, preserve the semantics of the font specification, and make any new patterns explicit when existing ones are insufficient.

## 1. Project boundaries and architecture

The project separates binary access, primitive representations, SFNT container structures, and font tables into distinct layers.

| Area | Responsibility |
|---|---|
| `Binary/` | Random-access byte sources, cursor-based parsing, endianness conversion, record contracts, and parsing helpers |
| `Primitives/` | Strongly typed representations of OpenType primitive data types and values |
| `Sfnt/` | SFNT font faces, table directories, and directory records |
| `Tables/` | Concrete table models, nested records, format-specific structures, and higher-level interpretation |
| Project root | Shared font-face and top-level table contracts |

Keep these responsibilities separate. A table parser should not need to know whether its bytes came from a file, a memory buffer, or a lazily buffered stream. Conversely, the binary layer should not need to understand the semantics of glyph substitutions, variation regions, or mathematical layout.

The current design uses static interface members and generic constraints to connect these layers. Preserve that design rather than introducing a runtime registration system, reflection-based parser discovery, or a parallel parsing framework.

## 2. Binary parsing: `Source` and `Cursor`

### 2.1 `Source` is the random-access abstraction

`Binary/Source.cs` defines the common abstraction for reading bytes from a known coordinate system. It exposes a `Length`, an offset-based read operation, and helpers for interpreting primitive values, arrays, and structured data.

The important distinction is that a `Source` is not a sequential stream. A caller can request bytes at an absolute offset without first consuming all preceding bytes. This matches font formats, where table directories contain offsets and nested structures frequently refer to locations elsewhere in the same table.

When implementing a new source, implement the underlying random-access operation and let the shared `Source` implementation provide the common bounds checking and typed reads. Do not duplicate primitive decoding logic in every storage implementation.

Existing source implementations have different ownership and storage characteristics:

- **`MemorySource`** reads from an existing memory buffer without copying it. Its lifetime and mutability implications follow from retaining the caller's buffer.
- **`FileSource`** uses `RandomAccess` with a file handle, avoiding dependence on a shared seek position. Its handle ownership and disposal behavior must remain explicit.
- **`StreamMemorySource`** reads from a stream on demand and grows a reusable memory buffer as data becomes necessary. Its buffering, synchronization, and stream-ownership behavior are part of its contract.
- **`SliceSource`** exposes a bounded region of another source under a new coordinate system. It is particularly important for parsing table-relative offsets.

A new `Source` implementation should preserve the same observable semantics for valid and invalid reads. If it introduces caching, buffering, or ownership rules, document those rules and test them independently.

### 2.2 `SliceSource` establishes a new origin

Suppose a font's `cmap` table begins at file offset \(B\), and a subtable has an offset \(O\) relative to the beginning of `cmap`. The intended absolute file location is \(B + O\).

A table-scoped `SliceSource` makes the subtable's offset simply \(O\). The source translates that request into the corresponding location in its outer source.

This is a fundamental design pattern in the library: **use the correct source coordinate system instead of manually adding parent offsets throughout the parser**.

When parsing nested structures:

1. Identify the origin specified by the format.
2. Ensure the `Source` represents that origin, or use the existing offset helpers with the appropriate parent source.
3. Interpret the stored offset according to the specification.
4. Avoid mixing file-relative, table-relative, and structure-relative offsets.

Do not assume every offset has the same origin. A nested format may define offsets relative to its own header, its containing table, or another explicitly named structure. Read the relevant specification before choosing the source or helper.

`SliceSource` also has an isolation option. Preserve the intended distinction between a normal view and an isolated source; do not silently change ownership or disposal behavior when modifying source composition.

### 2.3 `Cursor` is a positional parsing tool

`Binary/Cursor.cs` defines `Cursor` as a `ref struct` over a `Source`. It maintains a position and offers sequential reads, peeks, array reads, and typed helpers for OpenType values.

Use a cursor when the format describes a record as a sequence of fields. Its advancing methods express consumption of the wire format:

```csharp
ushort majorVersion = cursor.ReadUInt16();
ushort minorVersion = cursor.ReadUInt16();
uint tableOffset = cursor.ReadUInt32();
```

The example illustrates the reading pattern; actual fields and validation must follow the relevant specification.

Use `Peek...` operations when the current bytes must be inspected without advancing the cursor. Do not simulate a peek by advancing and manually restoring the position unless the operation genuinely requires that behavior.

A cursor created through `At` or `Here` is an independent cursor. It does not share a mutable position with the original cursor. This makes nested parsing possible without accidentally consuming bytes from the caller's sequential position.

The distinction between cursor and source operations matters:

- `cursor.Read...()` consumes bytes at the current position.
- `cursor.Peek...()` observes bytes at the current position without consuming them.
- `source.Read...At(offset)` reads at an explicit offset without relying on a cursor's position.
- Offset-based record helpers should be preferred when a structure points elsewhere in the source.

A cursor is a sequential reader first. When a parser enters a region and consumes fields in on-disk order, use the cursor for those reads. Use `cursor.Position = offset` to jump to a new region within the same logical structure and let subsequent reads advance from there. Use `cursor.Source.Read...At(offset)` when the target is at an arbitrary source offset with no relationship to where the cursor is currently pointing.

Do not replace sequential reads with hand-written offset arithmetic. A loop of `pos = pos + n; source.Read...At(pos)` duplicates what the cursor already does and is easier to get wrong. Conversely, do not mutate the cursor to peek at a value that could be read with `Source.Read...At(offset)`; that is a peek, and it should be expressed as a peek.

An `IRecord<T>.Parse` implementation is entered with the cursor at position `0` by contract. Do not capture `long start = cursor.Position` in a top-level parser: the start is already `0`, and reading subsequent fields from `start + offset` is a no-op that misleads readers into thinking the parser was entered at an arbitrary position. A dispatched derived parser — one invoked through `IBaseRecord<TBase>.Parse<TDerived>` — may capture the cursor's position to use as the base of its offsets, because the base format may have advanced the cursor past a discriminant or intervening bytes before delegating.

A cursor is a parsing-time value, not a persistent representation of a parsed record. Do not store it in ordinary objects, capture it in callbacks, or make parsed table models depend on a live cursor.

### 2.4 Bulk reads, spans, and allocation

The binary layer provides array-returning and destination-span forms of many operations. Use the destination form when the caller already owns suitable storage or when avoiding an unnecessary intermediate allocation is worthwhile.

For a collection whose count comes from the input, validate the count and ensure the corresponding byte range is available before relying on it. Do not allocate a large buffer solely because an untrusted file declares a large count.

Bulk reads are often preferable to repeated random-access reads. They can reduce call overhead and make endianness conversion and array interpretation easier to centralize. However, a bulk read is not automatically the best solution for every format: sparse offsets, variable-length records, and nested structures may require separate reads.

When changing these methods, preserve their distinctions between:

- A newly allocated array returned to the caller.
- A caller-provided destination span.
- A read that advances a cursor.
- A read at an explicit source offset.
- A read that converts wire endianness and one that does not.

Do not silently change one operation's cursor or allocation semantics while optimizing another.

When a struct carries a fixed-size buffer — a `byte` name field of a specific width, for example — declare it with `[StructLayout(LayoutKind.Sequential, Size = N, Pack = 1)]` and a single `byte First` field. Expose the buffer to callers through `MemoryMarshal.CreateSpan(ref First, N)` rather than a `fixed` statement and a `new Span<byte>(p, N)` over a raw pointer. The `MemoryMarshal.CreateSpan` form produces a ref-based span the GC tracks; the pointer-based form does not. The unsafe context stays confined to the struct's type definition, not the accessor.

## 3. Record contracts and static dispatch

`Binary/IRecord.cs` defines the record contracts used throughout the project. These contracts are related, but they are not interchangeable.

### 3.1 `IRecord<T>`: cursor-driven records

Use `IRecord<T>` for a type that can construct its parsed representation from a cursor and optional context. The parsing entry point is a static abstract member:

```csharp
static abstract T Parse(ref Cursor cursor, object? context = null);
```

Implement the entry point explicitly, as `static ExampleTable IRecord<ExampleTable>.Parse(ref Cursor cursor, object? context)`. Do not also expose a public static `Parse` method; the interface member is the contract and it should be the only parser entry point. This keeps the parse signature under interface control and prevents callers from binding to a concrete parser shape that may not be stable.

A top-level table generally follows this pattern:

```csharp
public sealed record ExampleTable : IFontTable<ExampleTable>
{
    public static Tag Tag => "TEST";

    public ushort Version { get; init; }

    static ExampleTable IRecord<ExampleTable>.Parse(
        ref Cursor cursor,
        object? context)
    {
        // Read and validate the table according to its specification.
        throw new NotImplementedException();
    }
}
```

This is a structural example, not a complete parser. Replace the placeholder with real parsing and validation before introducing a table implementation.

The self-referential generic constraint lets the compiler associate a concrete type with its own parser. It avoids a central dictionary of parser delegates and allows generic callers to use static interface dispatch without reflection.

Do not place XML documentation on explicit interface implementations. The interface member carries its own documentation; the implementer's body is not part of the type's public surface. Public members — properties, methods, constants, nested types — are documented. Explicit implementations, private helpers, and internal methods are not.

### 3.2 Header-based records

Some structures have a fixed-size header that is naturally read as a value type before the semantic object is constructed. For these structures, the library provides `IHeaderRecord<T, THeader>`.

The header is a transport representation of the bytes. The resulting record is the semantic representation consumed by the rest of the library.

A header-based parser typically follows this sequence:

1. Read the header using the appropriate binary helper.
2. Convert the header's multibyte fields from wire byte order when required.
3. Validate header fields that control subsequent parsing.
4. Construct the semantic record through `FromHeader`.
5. Parse any dependent or variable-length data that is not contained in the header.

`IEndianReversibleHeaderRecord<T, THeader>` combines this pattern with `IEndianReversibleStruct<THeader>`. Its parsing contract can read the header, perform the required endianness transformation, and delegate record construction to `FromHeader`.

Use this pattern when the header has a stable, fixed layout. Do not force variable-length or highly format-dependent structures into it merely to avoid writing a parser.

### 3.3 Derived records and format dispatch

Many font structures have a shared logical meaning but several wire formats. The library models these using `IDerivedRecord`, `IDerivedRecord<TBase, TDerived>`, `IBaseRecord<TBase>`, and their header-based variants.

The base type reads the format discriminator and dispatches to the appropriate derived type. The derived type is responsible for the fields specific to its format.

For example, `Coverage` has two formats. Its parser selects `CoverageFormat1` or `CoverageFormat2` from the format field. `CmapSubtable`, `GsubSubtable`, and several GPOS structures follow the same broad pattern.

Conceptually:

```csharp
return format switch
{
    1 => IBaseRecord<Coverage>.Parse<CoverageFormat1>(
        ref cursor, context),
    2 => IBaseRecord<Coverage>.Parse<CoverageFormat2>(
        ref cursor, context),
    _ => throw new InvalidDataException(
        $"Unsupported coverage format: {format}.")
};
```

Use the project's existing base-record dispatch contract and helpers when implementing a real type. The snippet illustrates the pattern; it is not a replacement for the actual parsing sequence.

The dispatcher should peek the format discriminator with the correct wire byte order and leave the cursor at the derived format's first byte. The derived parser is responsible for reading the discriminator as part of its own header if the spec defines it that way; the base must not consume bytes the derived parser needs.

When the derived format's parse is exactly "read a fixed-layout header, convert it to a semantic record," implement `IHeaderDerivedRecord<TBase, TDerived, THeader>` (or `IEndianReversibleHeaderDerivedRecord<TBase, TDerived, THeader>` for big-endian headers) rather than `IDerivedRecord<TBase, TDerived>.Parse` directly. The interface supplies the default `Parse` body; the record supplies only `FromHeader`.

When the base dispatcher needs the discriminator to decide which format to construct anyway, it can forward the already-read header through `IBaseRecord<TBase>.FromHeader<TDerived, THeader>(in THeader, context)` instead of the cursor-based helper. This avoids re-reading the same header in the derived parser:

```csharp
static Base IRecord<Base>.Parse(ref Cursor cursor, object? context)
{
    Header header = cursor.ReadEndianReversibleStruct<Header>();
    return header.IsFormat1
        ? IBaseRecord<Base>.FromHeader<Format1, Header>(in header, context)
        : IBaseRecord<Base>.FromHeader<Format2, Header>(in header, context);
}
```

A shallow hierarchy whose formats are fixed-size, whose dispatch is expressible as a single ternary on a header field, and whose derived bodies have no data beyond what `FromHeader` can construct may skip the `IBaseRecord`/`IDerivedRecord` interface machinery entirely. Keep the class hierarchy — an abstract base with two or three `sealed record` subclasses — and expose an `internal static FromHeader` factory on each subclass; the base's `IRecord<T>.Parse` invokes those factories through the C# inheritance relationship rather than through the constraint graph. Introduce the full interface family when the derived formats read variable-length data, when their number exceeds two or three, when their headers diverge, or when the base's dispatch stops being a one-line expression.

Keep format-specific behavior in the derived implementation. Do not scatter repeated format switches throughout unrelated callers, and do not silently interpret an unknown format as a known one.

A new format should be added as a new supported branch when the format warrants its own representation. If the specification defines a format as invalid or unsupported, fail explicitly rather than returning an unrelated default object.

### 3.4 Context is explicit parsing input

The parser's optional `object? context` allows a record to receive information that is not encoded in its own bytes. The project includes context types such as `ParentContext` and `TagContext`, as well as table-specific contexts.

Examples of legitimate context include:

- The parent source required to resolve a structure.
- A lookup type that determines the interpretation of a subtable.
- A value format that determines which fields exist in a value record.
- A parent table's metadata needed to interpret a nested record.

Use an existing context type when it expresses the required relationship. If a new context is necessary, define a small, named type that communicates what the parser needs.

A parser should not obtain this information through global mutable state or an implicit dependency on whichever table happened to be parsed earlier. Validate the context when it is required; do not silently substitute unrelated defaults when the context is missing or of the wrong type.

`ParentContext(cursor.Source)` is the standard way to carry the table-scoped source to a nested record's `FromHeader`. A nested record whose own header declares an offset relative to the parent table resolves that offset by reading from `ParentContext.ParentSource`, not from the cursor's source — the cursor's source may be a slice that reflects a different origin.

### 3.5 `IFontTable<T>` identifies top-level tables

`IFontTable<T>` extends `IRecord<T>` and supplies a static `Tag`. A tag identifies the kind of table, while a directory entry identifies a particular occurrence of that table in a font.

Only structures represented as top-level table-directory entries should normally implement `IFontTable<T>`. A lookup, coverage record, glyph variation, nested list, or subtable should use the appropriate record contract instead.

The tag must match the specification exactly, including capitalization and required trailing spaces. The `Tag` type does not automatically repair a malformed tag.

## 4. Endianness and fixed-layout structures

### 4.1 Wire representation is not host representation

OpenType structures are generally serialized in big-endian order. The machine running the library may use a different native byte order.

The library centralizes this distinction in `Source`, `Cursor`, `IEndianReversibleStruct<T>`, and the binary helper methods. A parser must know whether it is reading raw bytes, already converted values, or a fixed-layout header whose fields still require conversion.

Do not reverse the endianness of a value twice. Equally, do not assume that reading a raw structure into memory automatically produces correctly interpreted multibyte fields.

When implementing a fixed-layout header, implement `IEndianReversibleStruct<T>` if the structure's fields need byte-order conversion. Its static `ReverseEndianness` operation should return the transformed structure. For headers containing nested reversible values, reverse the relevant fields recursively according to their representation.

A typical header is a mutable `record struct`:

```csharp
public record struct Header : IEndianReversibleStruct<Header>
{
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint DataOffset;

    public static Header ReverseEndianness(Header value) => new()
    {
        MajorVersion = BinaryPrimitives.ReverseEndianness(
            value.MajorVersion),
        MinorVersion = BinaryPrimitives.ReverseEndianness(
            value.MinorVersion),
        DataOffset = BinaryPrimitives.ReverseEndianness(
            value.DataOffset)
    };
}
```

This example shows the convention used by many table headers. The actual header fields and conversion must match the corresponding specification.

### 4.2 Header records versus semantic records

The mutable header and the parsed model serve different purposes.

A header is a compact, fixed-layout representation used to read and transform fields efficiently. A semantic record exposes meaningful properties and may contain lists, nested records, or computed values. It should not be treated as a raw memory image of the file.

Do not expose an in-memory struct layout as if it were a portable serialization contract unless the layout, padding, and endianness requirements have been established. When a record's representation is not a direct fixed-size mapping, parse it field by field or through the relevant helpers.

A fixed-layout header is declared `public`, never `internal`, regardless of whether the semantic record exposes it as a property. Headers are wire-format transport types that a writer fills in memory, reverses for endianness, and then writes by blittable cast; a diagnostics analyzer or an external writer assembly must be able to construct and pass the type by value. Visibility is part of the write contract, not a stylistic choice. A header that is only consumed by one parser should still be public — it is the parser's *property exposure* that should be minimal, not the header's visibility.

## 5. Primitive types in `Primitives/`

The primitive types encode format-specific semantics that ordinary C# types do not express. Reuse them where the specification calls for them rather than converting everything to `int`, `uint`, `double`, or `string` at the parsing boundary.

### 5.1 `Tag`: four-byte identifiers

`Tag` represents a four-byte identifier and supports comparison, equality, and conversion to and from its underlying value. It also implements `IEndianReversibleStruct<Tag>`.

Use `Tag` for table identifiers and other specification-defined four-byte tags. Preserve case and all four characters. In particular, do not trim a trailing space from a tag such as `cvt `.

Do not compare tag names using culture-sensitive string operations when the intended operation is tag equality or ordering. Use the strongly typed value and its operators.

### 5.2 `UInt24` and `Int24`: three-byte integers

C# has no built-in 24-bit integer type, so `UInt24` and `Int24` provide the corresponding unsigned and signed value domains.

These types use three bytes of storage and expose explicit big-endian read and write operations. Their internal representation follows the host-native convention, whereas font files store their multibyte values in wire order. That distinction is why byte-order conversion must remain explicit at the binary boundary.

Their arithmetic, conversion operators, checked operations, and parsing methods are part of their public behavior. When modifying them:

- Preserve the 24-bit range and signed two's-complement interpretation.
- Distinguish checked conversion from truncating or unchecked conversion.
- Avoid narrowing an intermediate value before its range has been validated.
- Keep `ReadBigEndian`, `TryReadBigEndian`, and `WriteBigEndian` consistent with the documented wire representation.
- Test boundary values, negative values for `Int24`, and both valid and invalid offsets.

Do not replace these types with a plain 32-bit integer in a public API merely because the underlying calculations use 32-bit arithmetic.

### 5.3 `Fixed` and `F2Dot14`: fixed-point numbers

`Fixed` and `F2Dot14` represent fixed-point numeric formats with different fractional widths. `Fixed` uses 16 fractional bits; `F2Dot14` uses 14.

The stored integer bits are the exact format representation. The floating-point value is a convenient interpretation of those bits, not the serialized representation itself.

When adding calculations, be explicit about whether the operation is performed in the encoded integer domain or the floating-point domain. Scaling, multiplication, division, rounding, and overflow can produce different results depending on that choice.

Preserve the distinction between constructing a value from raw bits and constructing it from a numeric value. Use the existing conversion methods and operators instead of repeating scaling arithmetic in table parsers.

For a new fixed-point type, document its fractional width, range, overflow behavior, endianness, and conversion semantics. Follow the existing type's formatting and comparison conventions where appropriate.

## 6. Table implementation patterns

Table parsers should be designed around the actual structure of the format. The following patterns occur repeatedly in `Tables/` and should guide new implementations.

### 6.1 A conventional top-level table

A table with a fixed header and a small number of offset-referenced structures generally has the following responsibilities:

1. Declare its top-level tag through `IFontTable<T>`.
2. Read and validate its header.
3. Resolve any child offsets using the correct source origin.
4. Parse each child with its own record contract.
5. Construct the table's semantic representation.

`GsubTable` and `MathTable` illustrate this general organization. Both have version information and headers containing offsets to other structures. Their child records remain distinct types instead of being flattened into a single enormous parsing method.

Keep the top-level parser focused on table-level validation and composition. Put the interpretation of each child structure in its own record or helper when that structure has independent semantics, reuse potential, or a substantial parsing procedure.

### 6.2 Offset-referenced structures

Font tables frequently contain offsets instead of embedding every child directly after its parent. The binary helpers support reading arrays of offsets and parsing the referenced records.

For example, contextual subtables and lookup lists use helpers such as `ReadOffset16ArrayPeekRecord<T>` to read offset arrays and construct their target records. The important point is that the offset list and the records it refers to are separate parts of the serialized representation.

When implementing this pattern:

- Determine the exact origin of every offset from the specification.
- Preserve the distinction between reading an offset and parsing its target.
- Use the existing offset-record helpers when their semantics match the format.
- Validate counts and ranges before processing attacker-controlled input.
- Preserve the original record order unless the specification defines a different interpretation.

Do not assume that an offset points to the byte immediately after the offset field. Do not assume that all referenced structures are contiguous, unique, or stored in the same order as their references.

`Source` and `Cursor` expose F-bounded CRTP helpers such as `ReadOffset32ArrayParseRecordAt<T>(long, int, bool)`, `ParseRecordArrayAt<T>(long, int, object?)`, `ParseRecordAt<T>(long, object?)`, `ReadEndianReversibleHeaderRecordArray<T, THeader>(int, object?)`, and `ReadEndianReversibleHeaderRecordArrayAt<T, THeader>(long, int, object?)`. These helpers create the correct parser entry point, invoke the static abstract `Parse` through the contract, apply the correct slice origin where applicable, and return the materialized array. Prefer them over hand-written loops that reposition a cursor and call `Parse` per element; the helpers handle array shape, bounds, slice creation, and static dispatch in one place.

When an offset array is measured from a base other than the containing table's start, slice the source at that base with `WithOffset` (or `WithOffsetAndLength`) before passing it to the array helper. The helper's offset parameters then apply to the slice's coordinate system, and each value resolves relative to the base the specification names.

### 6.3 Variable formats and nested dispatch

`CmapTable`, `GsubTable`, and `GposTable` contain structures whose format fields select different representations. `Coverage` is a compact example: its formats represent glyph coverage through either a glyph array or a range array.

The preferred pattern is a small dispatcher plus independent format-specific implementations. The dispatcher reads enough information to identify the representation and delegates parsing. Each derived implementation owns its own field layout and behavior.

This is particularly important when formats have different lookup semantics. For example, `CoverageFormat1` searches a glyph array, whereas `CoverageFormat2` interprets ranges and coverage indexes. They share an abstract API but should not be forced into one storage representation.

The same principle applies to GSUB/GPOS lookups, anchors, contextual rules, and other format families. Keep the shared semantic contract on the base type and the representation-specific fields on the derived types.

### 6.4 Shared nested structures

Types such as `Coverage`, `ClassDef`, `Device`, `FeatureList`, `ScriptList`, and `LookupList<TSubtable>` are reused by multiple tables.

Reuse a shared structure when the wire format and its meaning are genuinely shared. Do not duplicate a nearly identical parser in each consuming table simply because the structures appear in different contexts.

At the same time, do not merge structures merely because their bytes look similar. If two specifications give a field different units, offset origins, version rules, or semantic constraints, those differences must remain explicit.

A generic type such as `LookupList<TSubtable>` is appropriate when the container layout is shared while the concrete subtable family varies by its containing table. Keep the type parameter and context contract intact rather than introducing runtime type discovery.

### 6.5 Context-dependent records

Some records cannot be interpreted correctly from their own bytes alone. For example, `GposValueRecord` uses a value-format context to determine which fields are present. Bitmap structures can use the parent source and contextual metadata to resolve nested structures. Lookup subtables can depend on their lookup type.

Use a dedicated context type to make this dependency explicit. Do not infer a value format from the bytes that happen to follow it, and do not parse every possible field when the controlling flags indicate that some fields are absent.

The context must describe the parent specification's rules, not merely provide convenient access to arbitrary state. If the record is impossible to interpret without the context, reject a missing or invalid context rather than constructing a misleading object.

### 6.6 Version-dependent tables

Tables such as `COLR` and `GSUB` contain version-dependent fields or structures. `ColrTable` is an especially important example because version 0 has base-glyph and layer records, while version 1 adds paint-oriented structures and variation-related data.

A version-aware parser should:

1. Read the version field.
2. Reject unsupported versions explicitly.
3. Parse the structures defined for that version.
4. Avoid reading fields that do not exist in that version.
5. Represent optional or version-specific data clearly in the parsed model.

Do not interpret an unknown future version as the nearest known version unless the specification explicitly permits that behavior. Do not treat an absent version-specific structure as an empty but valid structure when absence has a different meaning.

Keep version-specific parsing readable. A conditional branch at the table level is often better than a single parser filled with unrelated conditionals across every child record.

### 6.7 Simple and composite glyphs

`GlyfTable.cs` demonstrates a different pattern: a common logical object with materially different encodings.

The base `Glyph` parser dispatches between simple and composite glyph representations. A simple glyph contains contour endpoints, point flags, and coordinate data. A composite glyph describes components that refer to other glyphs and may apply transformations or positioning rules.

Do not treat a composite glyph as a simple glyph with an unusual point count. Their data layouts and interpretation differ. Keep format-specific parsing and component flags associated with the appropriate derived representation.

When working on glyph parsing, pay particular attention to:

- Contour endpoints and point counts.
- Repeated point flags and packed coordinate deltas.
- Signed versus unsigned coordinate deltas.
- Component argument interpretation.
- Transformation flags and optional fields.
- The distinction between glyph IDs and offsets into glyph data.

Counts and cumulative lengths must be validated before indexing arrays or allocating storage. Test malformed and truncated glyph data, not only glyphs from well-formed fonts.

### 6.8 CFF and CFF2: variable-width indexes and interpreters

`CffTable.cs` and `Cff2Table.cs` contain substantially more than conventional fixed-header parsing. They include index structures, dictionaries, font dictionaries, character strings, and interpretation of compact instruction streams.

A CFF index uses an offset-size field and a sequence of offsets to identify variable-length objects. The parser must respect the selected offset width, the index's count, the offset convention, and the data region. Do not assume all entries have equal size or use a fixed-width integer for every offset encoded in the stream.

Dictionary parsing and charstring interpretation are also distinct responsibilities. Keep the representation of parsed dictionary operands and operators separate from the higher-level interpretation of those values. Keep instruction decoding separate from executing or interpreting the instructions when those are separate stages in the existing design.

CFF2 extends the model with variation-related structures and font-dictionary selection. Do not assume that CFF and CFF2 have identical headers, index layouts, or termination rules simply because they share concepts.

When modifying these parsers, validate indexes and offsets before slicing data. An index or font-dictionary selection derived from untrusted bytes must never be allowed to become an unchecked array access.

### 6.9 Variation data and delta sets

Variation-related types include `GvarTable`, `ItemVariationStore`, `TupleVariationStore`, `DeltaSetIndexMap`, and the supporting variation records.

These structures represent relationships between axes, regions, indexes, and delta values. Their data is not merely a list of independent numbers. The meaning of a delta depends on the correct region, index mapping, and interpretation of the encoded values.

Keep parsing separate from evaluation. A parser should establish the stored structure faithfully; a method that calculates a variation-dependent value should apply the specified interpretation to that structure.

Be careful with packed values, flags that alter field widths, shared tuples, variable-length offsets, and the mapping between outer and inner indexes. Validate the indexes and dimensions used to connect the structures. Do not silently substitute zero for an invalid reference unless the specification defines that fallback.

### 6.10 Bitmap tables and shared bitmap parsing

The bitmap-related files include shared structures and parsers for location, image data, and index subtables. `BitmapLocationParser` illustrates why shared parsing logic belongs outside individual top-level table wrappers when multiple tables use the same format family.

The bitmap format can select different indexing strategies and image representations. A glyph's bitmap location is therefore not necessarily a simple multiplication of its glyph ID by a fixed image size.

Keep the distinction between strike metadata, index-subtable selection, and bitmap image decoding. When adding support for an index or image format, add the format-specific behavior at the appropriate layer rather than embedding all cases into each table parser.

Validate glyph ranges, offsets, image lengths, and dimensions before exposing a slice of image data. Malformed bitmap metadata must not cause out-of-range reads or oversized allocations.

### 6.11 Color paint graphs

`ColrTable.cs` models both the older layer-oriented representation and the newer paint-oriented representation. The latter can form a graph of paint structures rather than a flat list of layers.

Do not assume every color glyph is represented by a sequence of layer records. Keep version 0's layer semantics separate from version 1's paint structures, and use the existing paint record model for version 1.

For graph-like structures, preserve the distinction between parsing a reference and traversing the referenced graph. Be mindful of cycles, invalid offsets, and repeated references when implementing traversal or evaluation. Parsing should not recursively follow arbitrary untrusted references without a defined bound or validation strategy.

### 6.12 TrueType instruction streams

`TrueTypeProgram.cs` demonstrates the distinction between encoded instructions and their decoded representation. The raw opcode may contain variant bits, and push instructions can include inline data of variable length.

Do not assume one opcode always corresponds to one byte of total instruction length. Some instructions consume additional bytes or words from the instruction stream.

Keep opcode decoding consistent with the instruction format and ensure the decoder advances by the correct amount for both the opcode and its operands. A decoder that returns plausible instructions for a valid program but loses synchronization on malformed input is not robust.

If adding execution semantics, keep them separate from byte decoding unless the surrounding design explicitly combines those operations.

## 7. Correctness and malformed input

Font files are external input. They may be truncated, internally inconsistent, or deliberately malformed. A parser must not rely on all offsets, counts, versions, or indexes being valid merely because the format specification describes valid files.

Validate data before using it to allocate arrays, calculate byte ranges, or index other structures. Pay attention to arithmetic overflow when multiplying a count by an element size or adding a relative offset to a base position. Validate signed indexes before converting them to unsigned values or using them to index collections.

Use `InvalidDataException` for malformed data when that is consistent with the surrounding parser. Use argument exceptions for invalid caller arguments, and preserve the existing distinction between input errors and programming errors.

Do not catch an exception merely to return a default record. If a fallback is required by the specification, implement it deliberately and document the condition. Otherwise, make the invalid condition visible.

A successful parse should mean that the data consumed by that parser is structurally valid enough for the resulting model's documented operations. It does not automatically mean that every semantic rule of the entire font has been validated; keep that distinction clear.

## 8. Performance and allocation

This library uses direct source access, spans, value types, static interface dispatch, and generic parsing contracts to keep binary parsing efficient and compatible with trimming and ahead-of-time compilation.

Performance improvements should preserve the abstractions rather than bypassing them without evidence. In particular:

- Prefer bulk reads for contiguous fixed-width arrays when the existing helpers fit the format.
- Use destination spans when they eliminate a useful-to-avoid intermediate array.
- Avoid unnecessary copies of large byte regions.
- Keep fixed-layout headers compact and free from avoidable allocations.
- Do not add reflection-based dispatch or runtime parser registries.
- Do not add dependencies outside the project's established dependency policy without a compelling reason.

Fixed-layout buffers inside structs use `[StructLayout(Size = N, Pack = 1)]` with a single `byte First` field and expose the range through `MemoryMarshal.CreateSpan(ref First, N)`. Do not use a `fixed` statement and a raw pointer to construct the span; the `MemoryMarshal.CreateSpan` form preserves the same zero-copy semantics while keeping the span backed by a GC-tracked ref instead of a bare pointer.

Do not add unsafe code merely because the project permits it. An unsafe implementation should have a specific benefit that can be measured and should retain the same bounds and representation guarantees as the safe implementation.

Likewise, do not assume that a hand-written optimization is faster than a straightforward implementation. Benchmark the relevant workload and account for realistic font sizes, malformed-input handling, and the cost of allocations.

## 9. Naming, models, and API shape

Follow the naming and model conventions already established in the relevant family of types.

Top-level table models generally use names such as `HeadTable`, `CmapTable`, and `GvarTable`. Supporting records use names that describe their role, such as `EncodingRecordHeader`, `SequenceRule`, `GlyphVariation`, or `VariationRegionList`.

Use a `record struct` for a small fixed-layout header when that matches the existing header pattern. Use a semantic `record` for parsed objects whose fields may include lists, child records, or computed properties. Do not choose a record or struct solely for its syntax; consider mutability, copying, lifetime, and representation.

Use strongly typed properties when a field has a known semantic type. Use computed properties for values that can be derived reliably from the stored representation. Avoid maintaining duplicate mutable state for a value that can be derived from another property unless caching or a format-specific requirement justifies it.

Array-valued properties follow a type-based convention:

- When the element type is unmanaged (`ushort`, `float`, a header struct, or another unmanaged `record struct`), expose `ReadOnlyMemory<T>`. Callers obtain a `ReadOnlySpan<T>` via `.Span`, which the JIT compiles to a pointer and a length with no additional allocation. `ReadOnlyMemory<T>` on unmanaged element types carries no wrapper object beyond the array itself.
- When the element type is a reference type (a semantic record, an abstract base, or any other class), expose `IReadOnlyList<T>`. `ReadOnlyMemory<T>` on a reference type does not carry a performance advantage — the underlying array is the same — and communicates the wrong lifetime model for reference-typed data.

Wrap the underlying array with `Array.AsReadOnly` when exposing an `IReadOnlyList<T>`, or construct `ReadOnlyMemory<T>` directly from the array when exposing an unmanaged collection. Do not expose mutable arrays through public properties; the type's read-only surface is what a caller binds to. Do not expose a reference-typed collection as `ReadOnlyMemory<T>` just because some of the record's other fields are unmanaged; apply the rule per property.

Do not claim deep immutability merely because a property is typed as `IReadOnlyList<T>` or `ReadOnlyMemory<T>` — the backing array may still be reachable through other references. Document what the type actually guarantees.

## 10. CommunityToolkit.HighPerformance

Use `CommunityToolkit.HighPerformance` when its memory abstractions accurately represent the data being parsed or exposed. Do not introduce a toolkit type merely because it is described as high-performance; the type must fit the underlying data layout and the library's ownership and lifetime semantics.

### 1. Two-dimensional memory

Use `ReadOnlyMemory2D<T>` when a parsed structure is naturally represented as a rectangular, read-only region of unmanaged elements and callers benefit from explicit row and column indexing.

The classic AAT state array is an example. Its dimensions are `StateCount × StateSize`, and the element at `[state, class]` is a zero-based index into the subtable's entry table. Representing this array as `ReadOnlyMemory2D<byte>` expresses its structure directly and avoids requiring callers to calculate flattened indexes.

Derive dimensions from the two-dimensional view when they are already represented by it. For example, use `StateArray.Height` for the number of states and `StateArray.Width` for the number of bytes per state row rather than maintaining duplicate mutable dimension fields.

Use `ReadOnlyMemory2D<T>` only when the data is genuinely rectangular. Do not use it for ordinary one-dimensional arrays, offset lists, variable-length records, or data whose logical structure is better represented by a different abstraction. A two-dimensional view does not itself validate the binary format or guarantee that its contents are semantically valid.

### 2. Preserve the existing storage when constructing views

When a suitable array has already been materialized, construct the two-dimensional view over that array instead of copying the elements into a second array solely to obtain a two-dimensional representation.

Before constructing a view from untrusted binary data, validate the dimensions and the relationship between the buffer length and those dimensions. For a dense array with `height` rows and `width` columns, ensure that the dimensions are valid and that their product matches the intended element count. Perform overflow-safe validation before calculating sizes or allocating buffers.

Do not infer that a two-dimensional view owns its backing storage. Preserve the lifetime and ownership rules of the underlying memory, and do not expose mutable access when the public contract promises read-only access.

### 3. Dependency and API boundaries

`CommunityToolkit.HighPerformance` is an external dependency. Use its types when they provide a meaningful abstraction that would otherwise require unnecessary custom infrastructure, but do not add unrelated toolkit APIs merely because the package is already referenced.

A toolkit type exposed by a public member becomes part of the public API contract. Before exposing one, consider whether the dependency is appropriate for consumers of `Mubarrat.Fonts`, including its compatibility, versioning, trimming, and ahead-of-time compilation requirements.

Prefer the existing BCL abstractions for ordinary byte ranges, spans, and one-dimensional memory. Use toolkit abstractions when the data model requires their additional capabilities, such as rectangular memory views. Do not assume that a toolkit abstraction is inherently faster than a direct `Span<T>` or `ReadOnlyMemory<T>` implementation; performance claims should be supported by measurements of the relevant workload.

## 11. Documentation and public API compatibility

Public types and members are part of the library's contract. Document non-obvious format semantics, units, offset origins, endianness, nullability, and exception behavior where those details matter to callers.

Do not document explicit interface implementations. The interface member carries its own documentation, and the implementer's body is not part of the type's public surface. Do not document private helpers, internal methods, or protected members that are reachable only through the type's own assembly. Document public properties, public methods, public constants, and public nested types in full — summary, value, parameters, returns, exceptions, remarks, and cross-references to the specification as the surrounding code does.

The project includes `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, and treats compiler warnings as errors. Public API changes must therefore be deliberate. `record struct` declarations generate a `<Clone>$()` method that appears in the public API and must be added to the baseline alongside any other public member.

When adding a public member, update the public API baseline through the project's intended workflow. Do not suppress a public API or compiler diagnostic just to make a build pass. If a suppression is genuinely necessary, scope it narrowly and explain the reason.

Avoid exposing a helper publicly when it is only an implementation detail of a parser. Conversely, do not make a shared abstraction private to one table if multiple format implementations genuinely need the same contract.

A public API change should consider source compatibility, behavior on malformed input, AOT and trimming behavior, and whether the type can be used without reflection.

## 12. Tests and validation

A parser change should include tests that exercise the actual representation being changed. A test that merely constructs a record in memory does not prove that its wire-format parser handles the corresponding bytes correctly.

Where practical, cover:

- A valid minimal input.
- Representative values and format variants.
- Boundary values for counts, offsets, and indexes.
- Truncated or malformed data.
- Endianness-sensitive fields.
- Multiple source implementations when the change affects `Source`.
- Cursor advancement and peek behavior when the change affects `Cursor`.
- Public API and build compatibility for new public types.

For a new table format, test the dispatcher and the derived parser. For offset-based structures, verify that the origin is correct and that out-of-range references are handled safely. For variable-width indexes, test the supported widths and the boundary cases of the offset sequence.

Use real font files as integration fixtures when they provide coverage that synthetic data cannot. However, do not rely exclusively on a corpus: malformed inputs and precise edge cases are often easier to test with small byte sequences whose expected layout is explicit.

Run the project build and the relevant test project before submitting changes. A change that introduces compiler warnings is not complete while the project treats warnings as errors.

## 13. A practical implementation workflow

When adding a new table or parser, begin by identifying the table's tag, supported versions, fixed header layout, and offset origins. Record which structures are fixed-width, which depend on counts or flags, and which select a derived format.

Then inspect the closest existing implementation. Reuse the relevant binary helpers, record contracts, context types, and nested structures rather than inventing a parallel approach. If no existing abstraction fits, explain why before introducing a new one.

Implement the parser in layers: read the header, validate it, resolve child structures, parse each child, and construct the semantic model. Keep format dispatch explicit, and avoid mixing parsing with higher-level interpretation unless the existing type deliberately serves both purposes.

Finally, add focused tests, run the build and relevant tests, review the public API baseline if necessary, and verify that no accidental changes to ownership, offset semantics, endianness, or cursor advancement have been introduced.

## 14. Contribution checklist

Before submitting a change to `Mubarrat.Fonts`, verify the following:

- [ ] The implementation follows the existing binary and record contracts where applicable.
- [ ] Every offset is interpreted relative to the origin defined by the specification.
- [ ] When an offset array's values are not measured from the containing table's start, the source is sliced at the correct base before the helper is invoked.
- [ ] Wire endianness is handled exactly once.
- [ ] Header structures and semantic records remain distinct where appropriate.
- [ ] Fixed-layout headers are `public` even when no public member exposes them as a property, so that writer assemblies and diagnostics analyzers can construct, reverse, and write them by blittable cast.
- [ ] `IRecord<T>.Parse` implementations are explicit interface implementations and carry no XML documentation.
- [ ] `IRecord<T>.Parse` implementations do not capture `cursor.Position` as a base offset; the contract guarantees it is `0` on entry.
- [ ] Sequential field reads use `cursor.Read...`; arbitrary offset reads use `Source.Read...At` or `Source.Parse...At`, and neither pattern substitutes for the other.
- [ ] Version and format dispatch reject unsupported representations explicitly.
- [ ] Counts, offsets, lengths, and indexes are validated before use.
- [ ] Context-dependent records receive the correct explicit context; `ParentContext` carries the table-scoped source when a nested record resolves offsets against the parent.
- [ ] Arrays and spans are used with appropriate allocation and lifetime semantics; header-record arrays are read through the CRTP helpers rather than hand-written loops.
- [ ] Unmanaged collections are exposed as `ReadOnlyMemory<T>`; reference-typed collections as `IReadOnlyList<T>`.
- [ ] Fixed-size buffers are exposed through `MemoryMarshal.CreateSpan`, not a `fixed` statement and raw pointer.
- [ ] Tests cover valid data and relevant malformed or boundary cases.
- [ ] The project builds without warnings.
- [ ] Public API baselines and documentation are updated when necessary.
- [ ] No unnecessary dependencies, reflection-based dispatch, or unrelated refactoring have been introduced.

The guiding principle is to preserve the relationship between the serialized font format and the library's semantic model. A good contribution does more than parse a valid example: it makes the representation explicit, keeps parsing responsibilities separated, and behaves predictably when the input is invalid.

## 15. Keeping this guide and the codebase in sync

This guide is a description of the project's current conventions, not an aspiration. If a section of this document describes a pattern that the codebase does not follow, or if a section of the codebase follows a pattern this document does not describe, then at least one of the two is wrong.

When you encounter that divergence in a file that has already shipped in a commit:

- If the code is correct and this guide describes a pattern that has been intentionally superseded, update this guide in the same change. Explain the new convention, its rationale, and where in the codebase it is used.
- If the code violates a pattern this guide still describes as correct, fix the code. Do not update this guide to match a violation merely because the violation already exists.
- If both are partially correct and the divergence is a genuine trade-off, reconcile them explicitly. State which sections of each are authoritative, and update the other side to match.

Do not carry a silent divergence forward. A commit that touches a file whose contents disagree with this guide is the natural moment to resolve the disagreement, whether by fixing the code or amending the guide. Do not defer the resolution to a future change on the grounds that the divergence predates your work.

A newly added contribution is expected to conform to this guide in its final form. If a contribution introduces a new pattern that this guide does not yet describe, update this guide in the same change so the pattern is documented alongside the code that adopts it.
