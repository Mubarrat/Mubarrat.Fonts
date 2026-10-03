namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>Marker for any type that is decodable from a <see cref="Source"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>This interface carries no members. It exists so that generic infrastructure — a record walker, a validator, a debugger visualizer, a table enumerator — has a type to bind to when it needs to refer to "a record" without knowing its concrete shape.</description></item>
/// <item><description>Two interfaces extend it: <see cref="IRecord{T}"/> for cursor-driven records and <see cref="IHeaderRecord{T, THeader}"/> for records built from an already-read header.</description></item>
/// <item><description>A type implements one of those, not this marker directly.</description></item>
/// <item><description>The OpenType format is a tree of records rooted at the table directory; for the composition of the file, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IRecord{T}"/>
/// <seealso cref="IHeaderRecord{T, THeader}"/>
/// <seealso cref="IBigEndianHeaderRecord{T, THeader}"/>
/// <seealso cref="IDerivedRecord"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="Source"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
public interface IRecord;

/// <summary>A record decoded by consuming bytes from a <see cref="Cursor"/>.</summary>
/// <typeparam name="T">The implementing type. This is the F-bound that lets callers write <c>T.Parse(ref cursor, context)</c> and get back exactly <typeparamref name="T"/> without a cast.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>The cursor is entered at the record's first byte and advanced past the record's last byte.</description></item>
/// <item><description>Positions declared inside the record are relative to the cursor's base, which for a table-scoped cursor is the table's start.</description></item>
/// <item><description><c>context</c> carries any external values the record needs. Records that need nothing ignore it. Records that need a dependency cast it to the type they expect and fail fast when the cast is wrong.</description></item>
/// <item><description>Records that read a single header and convert it to a semantic object should implement <see cref="IHeaderRecord{T, THeader}"/> or <see cref="IBigEndianHeaderRecord{T, THeader}"/> instead. Those supply <c>Parse</c> via a default interface method.</description></item>
/// </list>
/// <para>The context slot is untyped because records across OpenType need different things — a <c>FontFace</c> for cross-table lookups, a small parameter bundle for values the parent computes, or <c>null</c> when nothing external is required.</para>
/// <para>For the on-disk composition that determines what a record will read, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</para>
/// </remarks>
/// <seealso cref="IHeaderRecord{T, THeader}"/>
/// <seealso cref="IBigEndianHeaderRecord{T, THeader}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="Cursor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces">C# reference: Static abstract members in interfaces</seealso>
public interface IRecord<out T> : IRecord where T : IRecord<T>
{
    /// <summary>Reads one <typeparamref name="T"/> from <paramref name="cursor"/> and advances the cursor past the bytes it consumed.</summary>
    /// <param name="cursor">The cursor positioned at the record's first byte. On return, its <see cref="Cursor.Position"/> is the byte immediately after the record.</param>
    /// <param name="context">External values the record needs, or <c>null</c> when the record is self-contained. The record must not modify the context.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="EndOfStreamException">The record extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException">The record's declared structure is invalid for the bytes present, or the context does not supply a value the record requires.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a <c>static abstract</c> member: implementers supply the body, callers invoke it through a <c>where T : IRecord&lt;T&gt;</c> constraint.</description></item>
    /// <item><description>Records whose parse is exactly "read a header, convert it" should not implement this directly — implement <see cref="IHeaderRecord{T, THeader}"/> or <see cref="IBigEndianHeaderRecord{T, THeader}"/> instead and inherit the <c>Parse</c> default implementation.</description></item>
    /// <item><description>On success the cursor is advanced by however many bytes the record consumed. On failure it is left unchanged, because the underlying reads throw before mutating <see cref="Cursor.Position"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}.FromHeader(in THeader, object?)"/>
    /// <seealso cref="Cursor"/>
    /// <seealso cref="Source"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces">C# reference: Static abstract members in interfaces</seealso>
    static abstract T Parse(ref Cursor cursor, object? context = null);
}

/// <summary>A record constructed from an already-read header. No bytes are consumed.</summary>
/// <typeparam name="T">The implementing type. This is the F-bound that lets callers write <c>T.FromHeader(in header, context)</c> and get back exactly <typeparamref name="T"/>.</typeparam>
/// <typeparam name="THeader">The header type. A blittable struct that has already been read from the source, typically by <see cref="Source.ReadStructAt{T}(long)"/> or equivalent. Constrained to <c>unmanaged</c> so it can be passed by <c>in</c> reference without allocation, boxing, or GC interaction.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>This interface models the second phase of a two-phase parse: a low-level struct has already been read from the source, and the record is a semantic view over it.</description></item>
/// <item><description>The interface supplies a default implementation of <see cref="IRecord{T}.Parse"/> that reads a <typeparamref name="THeader"/> with <see cref="Cursor.ReadStruct{T}"/> and forwards it to <see cref="FromHeader(in THeader, object?)"/>.</description></item>
/// <item><description>A record that fits the "read header, convert" shape only declares <see cref="FromHeader(in THeader, object?)"/>; every <c>IRecord&lt;T&gt;.Parse</c> call site works without change.</description></item>
/// <item><description>When <typeparamref name="THeader"/>'s multi-byte fields are stored big-endian on disk, implement <see cref="IBigEndianHeaderRecord{T, THeader}"/> instead so the default <c>Parse</c> reverses them.</description></item>
/// </list>
/// <para>The <c>context</c> argument carries anything the header does not — most commonly a <see cref="Source"/> plus a base offset for resolving an offset the header declares, or a parent record whose fields the header alone does not provide.</para>
/// </remarks>
/// <seealso cref="IRecord{T}"/>
/// <seealso cref="IBigEndianHeaderRecord{T, THeader}"/>
/// <seealso cref="Cursor.ReadStruct{T}"/>
/// <seealso cref="Source.ReadStructAt{T}(long)"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods">C# reference: Default interface methods</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces">C# reference: Static abstract members in interfaces</seealso>
public interface IHeaderRecord<out T, THeader> : IRecord<T> where T : IHeaderRecord<T, THeader> where THeader : unmanaged
{
    /// <summary>Builds one <typeparamref name="T"/> from <paramref name="header"/>.</summary>
    /// <param name="header">The already-read header. Passed by readonly reference; not copied.</param>
    /// <param name="context">External values the record needs, or <c>null</c> when the header is sufficient. The record must not modify the context.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidDataException">The header's fields describe a structure the record cannot represent, or the context does not supply a value the record requires.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Implementers supply the conversion. The interface's default <see cref="IRecord{T}.Parse"/> reads the header and forwards to this method.</description></item>
    /// <item><description>The <c>in</c> parameter avoids a copy of the header struct when it is larger than a pointer, without the boxing penalty of a by-value interface method.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}.Parse(ref Cursor, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier">C# reference: <c>in</c> parameter modifier</seealso>
    static abstract T FromHeader(in THeader header, object? context = null);

    /// <summary>Reads a <typeparamref name="THeader"/> from the cursor and forwards it to <see cref="FromHeader(in THeader, object?)"/>. This default implementation is what lets a header record satisfy <see cref="IRecord{T}"/> without writing its own <c>Parse</c>.</summary>
    /// <param name="cursor">The cursor positioned at the header's first byte. On return, its <see cref="Cursor.Position"/> is the byte immediately after the header.</param>
    /// <param name="context">External values the record needs. Passed unchanged to <see cref="FromHeader(in THeader, object?)"/>.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="EndOfStreamException">The header extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException"><see cref="FromHeader(in THeader, object?)"/> rejects the header or the context.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is an <em>explicit</em> implementation of <see cref="IRecord{T}.Parse"/>: it is reachable only through the interface, not as a public method on the implementing type.</description></item>
    /// <item><description>The header is read with no endianness conversion. If the on-disk header is big-endian, use <see cref="IBigEndianHeaderRecord{T, THeader}"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cursor.ReadStruct{T}"/>
    /// <seealso cref="FromHeader(in THeader, object?)"/>
    /// <seealso cref="IBigEndianHeaderRecord{T, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods">C# reference: Default interface methods</seealso>
    static T IRecord<T>.Parse(ref Cursor cursor, object? context)
    {
        THeader header = cursor.ReadStruct<THeader>();
        return T.FromHeader(in header, context);
    }
}

/// <summary>A header-backed record whose header stores its multi-byte fields in big-endian order on disk.</summary>
/// <typeparam name="T">The implementing type. This is the F-bound that lets callers write <c>T.FromHeader(in header, context)</c> and get back exactly <typeparamref name="T"/>.</typeparam>
/// <typeparam name="THeader">The header type. A blittable struct that has already been read from the source and implements <see cref="IBigEndianStruct{THeader}"/> so its multi-byte fields can be reversed on little-endian hosts. Constrained to <c>unmanaged</c> so it can be passed by <c>in</c> reference without allocation, boxing, or GC interaction.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>This interface is to <see cref="IHeaderRecord{T, THeader}"/> what <see cref="Source.ReadBigEndianStructAt{T}(long)"/> is to <see cref="Source.ReadStructAt{T}(long)"/>: the same pattern, with the header's multi-byte fields reversed on little-endian hosts.</description></item>
/// <item><description>The interface supplies a default implementation of <see cref="IRecord{T}.Parse"/> that reads a <typeparamref name="THeader"/> with <see cref="Cursor.ReadBigEndianStruct{T}"/> and forwards it to <see cref="IHeaderRecord{T, THeader}.FromHeader(in THeader, object?)"/>.</description></item>
/// <item><description>The reversal is applied before the header reaches <c>FromHeader</c>, so the record sees native-order field values and does not need to reverse anything itself.</description></item>
/// <item><description>Use this interface for any header whose on-disk layout includes multi-byte fields stored big-endian. For headers that are all single bytes, implement <see cref="IHeaderRecord{T, THeader}"/> directly.</description></item>
/// </list>
/// <para>Every OpenType table header is defined as big-endian on disk; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</para>
/// </remarks>
/// <seealso cref="IHeaderRecord{T, THeader}"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
/// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
public interface IBigEndianHeaderRecord<out T, THeader> : IHeaderRecord<T, THeader> where T : IBigEndianHeaderRecord<T, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
{
    /// <summary>Reads a big-endian <typeparamref name="THeader"/> from the cursor, reverses its multi-byte fields on little-endian hosts, and forwards it to <see cref="IHeaderRecord{T, THeader}.FromHeader(in THeader, object?)"/>.</summary>
    /// <param name="cursor">The cursor positioned at the header's first byte. On return, its <see cref="Cursor.Position"/> is the byte immediately after the header.</param>
    /// <param name="context">External values the record needs. Passed unchanged to <c>FromHeader</c>.</param>
    /// <returns>The decoded record, with the header's multi-byte fields in native byte order.</returns>
    /// <exception cref="EndOfStreamException">The header extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException"><c>FromHeader</c> rejects the header or the context.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is an <em>explicit</em> implementation of <see cref="IRecord{T}.Parse"/>: it is reachable only through the interface, not as a public method on the implementing type.</description></item>
    /// <item><description>The reversal is unconditional inside <see cref="IBigEndianStruct{T}.ReverseEndianness(T)"/>; the host-endianness check is applied by <see cref="Cursor.ReadBigEndianStruct{T}"/>, not by this method.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso cref="IHeaderRecord{T, THeader}.FromHeader(in THeader, object?)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    static T IRecord<T>.Parse(ref Cursor cursor, object? context)
    {
        THeader header = cursor.ReadBigEndianStruct<THeader>();
        return T.FromHeader(in header, context);
    }
}

/// <summary>Marker for any type in a format-dispatched hierarchy that is a format of some base.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A derived record is not an <see cref="IRecord{T}"/>; it cannot be parsed by the cursor-record entry points (<c>ReadRecord&lt;T&gt;</c>, <c>ParseRecordAt&lt;T&gt;</c>).</description></item>
/// <item><description>It is reachable only through its base's dispatcher, which calls it through the <see cref="IDerivedRecord{TBase, TDerived}"/> constraint.</description></item>
/// <item><description>Format dispatch is a common OpenType pattern — subtable layouts often differ by a leading format discriminant; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
public interface IDerivedRecord : IRecord;

/// <summary>A cursor-driven format of a format-dispatched hierarchy.</summary>
/// <typeparam name="TBase">The base type whose dispatcher constructs this format.</typeparam>
/// <typeparam name="TDerived">The implementing format type. Constrained to inherit from <typeparamref name="TBase"/> so <c>Parse&lt;TBase, WrongFormat&gt;</c> cannot compile.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>Derived records are a separate family from <see cref="IRecord{T}"/>. They do not satisfy the <c>where T : IRecord&lt;T&gt;</c> constraint, so the cursor-record helpers do not accept them.</description></item>
/// <item><description>The only dispatch route is <c>Cursor.ReadDerived&lt;TBase, TDerived&gt;</c>, which the base's dispatcher calls through <see cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>.</description></item>
/// <item><description><typeparamref name="TBase"/> is used only in the constraint graph and for readability; it is never a runtime value. The class hierarchy already places the derived type under its base; this interface makes the relationship visible to generic code.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IDerivedRecord"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
public interface IDerivedRecord<TBase, TDerived> : IDerivedRecord where TDerived : TBase, IDerivedRecord<TBase, TDerived>
{
    /// <summary>Reads one <typeparamref name="TDerived"/> from <paramref name="cursor"/>. Invoked only by the base dispatcher through the derived-record helper.</summary>
    /// <param name="cursor">The cursor at the format's first byte. On return, its position is the byte immediately after the format.</param>
    /// <param name="context">External values, or <c>null</c>.</param>
    /// <returns>The decoded format.</returns>
    /// <exception cref="EndOfStreamException">The format extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException">The format's declared structure is invalid for the bytes present, or the context does not supply a value the format requires.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a <c>static abstract</c> member: implementers supply the body, the base dispatcher invokes it through the constraint on <see cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>.</description></item>
    /// <item><description>Formats whose parse is "read a header, convert it" should not implement this directly — implement <see cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/> or <see cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>
    /// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>
    /// <seealso cref="Cursor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces">C# reference: Static abstract members in interfaces</seealso>
    static abstract TDerived Parse(ref Cursor cursor, object? context = null);
}

/// <summary>A header-driven format of a format-dispatched hierarchy.</summary>
/// <typeparam name="TBase">The base type whose dispatcher constructs this format.</typeparam>
/// <typeparam name="TDerived">The implementing format type.</typeparam>
/// <typeparam name="THeader">The header struct. Blittable, unpadded, native-endian in memory.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>Supplies the <c>Parse</c> implementation as a default interface method that reads the header with <see cref="Cursor.ReadStruct{T}"/> and forwards to <see cref="FromHeader(in THeader, object?)"/>.</description></item>
/// <item><description>A format whose parse is exactly "read a struct, convert it" implements only <c>FromHeader</c>.</description></item>
/// <item><description>Use <see cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/> when the header's multi-byte fields are big-endian on disk.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso cref="IHeaderRecord{T, THeader}"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods">C# reference: Default interface methods</seealso>
public interface IHeaderDerivedRecord<TBase, TDerived, THeader> : IDerivedRecord<TBase, TDerived> where TDerived : TBase, IHeaderDerivedRecord<TBase, TDerived, THeader> where THeader : unmanaged
{
    /// <summary>Builds a format record from its already-read header.</summary>
    /// <param name="header">The already-read header. Passed by readonly reference; not copied.</param>
    /// <param name="context">External values the format needs, or <c>null</c> when the header is sufficient.</param>
    /// <returns>The decoded format.</returns>
    /// <exception cref="InvalidDataException">The header's fields describe a structure the format cannot represent, or the context does not supply a value the format requires.</exception>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}.Parse(ref Cursor, object?)"/>
    /// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier">C# reference: <c>in</c> parameter modifier</seealso>
    static abstract TDerived FromHeader(in THeader header, object? context = null);

    /// <summary>Reads a <typeparamref name="THeader"/> from the cursor and forwards it to <see cref="FromHeader(in THeader, object?)"/>.</summary>
    /// <param name="cursor">The cursor positioned at the header's first byte. On return, its position is the byte immediately after the header.</param>
    /// <param name="context">External values the format needs. Passed unchanged to <c>FromHeader</c>.</param>
    /// <returns>The decoded format.</returns>
    /// <exception cref="EndOfStreamException">The header extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException"><c>FromHeader</c> rejects the header or the context.</exception>
    /// <remarks>The header is read with no endianness conversion. Use <see cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/> when the header's multi-byte fields are big-endian on disk.</remarks>
    /// <seealso cref="Cursor.ReadStruct{T}"/>
    /// <seealso cref="FromHeader(in THeader, object?)"/>
    /// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods">C# reference: Default interface methods</seealso>
    static TDerived IDerivedRecord<TBase, TDerived>.Parse(ref Cursor cursor, object? context)
    {
        THeader header = cursor.ReadStruct<THeader>();
        return TDerived.FromHeader(in header, context);
    }
}

/// <summary>A big-endian header-driven format of a format-dispatched hierarchy.</summary>
/// <typeparam name="TBase">The base type whose dispatcher constructs this format.</typeparam>
/// <typeparam name="TDerived">The implementing format type.</typeparam>
/// <typeparam name="THeader">The header struct. Multi-byte fields are big-endian on disk; must implement <see cref="IBigEndianStruct{THeader}"/>.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>, with the header reversed on little-endian hosts before <c>FromHeader</c> sees it.</description></item>
/// <item><description>The reversal is supplied by <see cref="Cursor.ReadBigEndianStruct{T}"/>, which consults <see cref="System.BitConverter.IsLittleEndian"/> and calls <see cref="IBigEndianStruct{THeader}.ReverseEndianness(THeader)"/> conditionally.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
public interface IBigEndianHeaderDerivedRecord<TBase, TDerived, THeader> : IHeaderDerivedRecord<TBase, TDerived, THeader> where TDerived : TBase, IBigEndianHeaderDerivedRecord<TBase, TDerived, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
{
    /// <summary>Reads a big-endian <typeparamref name="THeader"/> from the cursor, reverses its multi-byte fields on little-endian hosts, and forwards it to <see cref="IHeaderDerivedRecord{TBase, TDerived, THeader}.FromHeader(in THeader, object?)"/>.</summary>
    /// <param name="cursor">The cursor positioned at the header's first byte. On return, its position is the byte immediately after the header.</param>
    /// <param name="context">External values the format needs. Passed unchanged to <c>FromHeader</c>.</param>
    /// <returns>The decoded format, with the header's multi-byte fields in native byte order.</returns>
    /// <exception cref="EndOfStreamException">The header extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException"><c>FromHeader</c> rejects the header or the context.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is an <em>explicit</em> implementation of <see cref="IDerivedRecord{TBase, TDerived}.Parse"/>: reachable only through the interface.</description></item>
    /// <item><description>The host-endianness check is applied by <see cref="Cursor.ReadBigEndianStruct{T}"/>; <see cref="IBigEndianStruct{THeader}.ReverseEndianness(THeader)"/> itself is unconditional.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}.FromHeader(in THeader, object?)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    static TDerived IDerivedRecord<TBase, TDerived>.Parse(ref Cursor cursor, object? context)
    {
        THeader header = cursor.ReadBigEndianStruct<THeader>();
        return TDerived.FromHeader(in header, context);
    }
}

/// <summary>Marks a base class in a format-dispatched hierarchy and provides the protected dispatch helper through which its derived formats are read.</summary>
/// <typeparam name="TBase">The base type itself. The F-bound (<c>TBase : IBaseRecord&lt;TBase&gt;</c>) ensures a type can only be the base of its own hierarchy — an <c>Anchor</c> cannot be declared as the base of a <c>GposSubtable</c> hierarchy, or vice versa.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>A base record is an abstract class with an internal format dispatcher: it reads a format discriminant and routes to the matching concrete format.</description></item>
/// <item><description>Implementations declare <c>IBaseRecord&lt;TSelf&gt;</c> alongside <c>IRecord&lt;TSelf&gt;</c>, as shown in the example below.</description></item>
/// <item><description>Derived formats declare <see cref="IDerivedRecord{TBase, TDerived}"/> and are not reachable through <see cref="IRecord{T}"/>. The only route to a derived format is <see cref="Parse{TDerived}(ref Cursor, object?)"/>, which the base's dispatcher calls.</description></item>
/// <item><description>The compiler enforces the pairing through the <c>where TDerived : TBase</c> constraint on <see cref="Parse{TDerived}(ref Cursor, object?)"/>.</description></item>
/// <item><description>Derived formats may also implement one of the header-record interfaces (<see cref="IHeaderRecord{T, THeader}"/> or <see cref="IBigEndianHeaderRecord{T, THeader}"/>) in place of a hand-written <c>Parse</c>. The interface's default <c>IRecord&lt;T&gt;.Parse</c> reads the header and forwards to <c>FromHeader</c>, and <see cref="Parse{TDerived}(ref Cursor, object?)"/> reaches it through the same constraint.</description></item>
/// </list>
/// <para>Format dispatch appears throughout OpenType: the <c>GSUB</c>/<c>GPOS</c> lookup subtable formats, the <c>cmap</c> subtable formats, and many others; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</para>
/// </remarks>
/// <example>
/// <code>
/// public abstract record Anchor : IRecord&lt;Anchor&gt;, IBaseRecord&lt;Anchor&gt;
/// {
///     public ushort Format { get; init; }
///     public static Anchor Parse(ref Cursor cursor, object? context = null) { /* dispatch on Format */ }
/// }
/// </code>
/// </example>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso cref="IDerivedRecord"/>
/// <seealso cref="IRecord{T}"/>
/// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected">C# reference: <c>protected</c></seealso>
public interface IBaseRecord<TBase> : IRecord where TBase : IBaseRecord<TBase>
{
    /// <summary>Reads one derived format of this base from the cursor. Called by the base's dispatcher; not part of the public API of any format class.</summary>
    /// <typeparam name="TDerived">The concrete format to read. Must be a derived record of this base: the constraint <c>TDerived : TBase, IDerivedRecord&lt;TBase, TDerived&gt;</c> ties the two types together, so <c>Parse&lt;WrongFormat&gt;</c> does not compile when the wrong type is supplied.</typeparam>
    /// <param name="cursor">The cursor positioned at the format's first byte. On return, its position is the byte immediately after the format. The cursor's source determines the base against which the format's internal offsets resolve; when called from the base dispatcher, that base is the format subtable's start.</param>
    /// <param name="context">External values the format needs, or <c>null</c>. Passed unchanged to <c>TDerived.Parse</c>. A format that ignores context is called with <c>null</c>; a format that depends on a parent source or a value format receives the corresponding context record.</param>
    /// <returns>The decoded format.</returns>
    /// <exception cref="EndOfStreamException">The format extends past the end of <see cref="Cursor.Source"/>.</exception>
    /// <exception cref="InvalidDataException">The format's declared structure is invalid for the bytes present, or the context does not supply a value the format requires.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This helper is <c>protected</c>: it is callable from the base's <c>Parse</c> method (which is a static member of the base type and therefore part of the interface's "protected" surface), but not from user code.</description></item>
    /// <item><description>The helper does not dispatch — the base's <c>Parse</c> reads the format discriminant and decides which <typeparamref name="TDerived"/> to invoke.</description></item>
    /// <item><description>This is the only sanctioned route to a derived record. Calling <c>TDerived.Parse</c> directly is legal C# but bypasses the base's invariants; the protected helper exists to keep the two in step.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}.Parse(ref Cursor, object?)"/>
    /// <seealso cref="IHeaderDerivedRecord{TBase, TDerived, THeader}"/>
    /// <seealso cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected">C# reference: <c>protected</c></seealso>
    protected static TDerived Parse<TDerived>(ref Cursor cursor, object? context = null) where TDerived : TBase, IDerivedRecord<TBase, TDerived>
        => TDerived.Parse(ref cursor, context);
}
