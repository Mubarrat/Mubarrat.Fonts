using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Variations;

namespace Mubarrat.Fonts.OpenType.Tables.Hinting;

/// <summary>The <c>cvt </c> table: Control Value Table. An indexed list of FWORD values that TrueType instructions reference during hinting. Example values include serif height, x-height, and stem widths.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table has no internal structure — it is a flat array of <c>int16</c> values. The length must be an integral number of FWORD units. The number of entries is declared by <c>maxp.maxCvtValues</c>.</description></item>
/// <item><description>In a variable font, the <c>cvar</c> table supplies per-region deltas that adjust these values at different variation instances. Use <see cref="Evaluate(CvarTable?, ReadOnlySpan{F2Dot14})"/> to retrieve values at a specific instance.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt">cvt table</see> and the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">cvar table</see> chapters in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CvarTable"/>
/// <seealso cref="Evaluate(CvarTable?, ReadOnlySpan{F2Dot14})"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt">OpenType specification: cvt table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">OpenType specification: cvar table</seealso>
public sealed record CvtTable : IOpenTypeTable<CvtTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt">OpenType specification: cvt table</seealso>
    public static Tag Tag => "cvt ";

    /// <summary>Gets the control values, in the order instructions index them.</summary>
    /// <value>The flat array of FWORD values. Length matches <c>maxp.maxCvtValues</c> in a conforming font.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array order is the index order used by TrueType instructions; a value's position is its CVT index.</description></item>
    /// <item><description>Values are stored as-is; the base array is not adjusted for variations. Use <see cref="Evaluate(CvarTable?, ReadOnlySpan{F2Dot14})"/> for instance-adjusted values.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="this[int]"/>
    /// <seealso cref="Evaluate(CvarTable?, ReadOnlySpan{F2Dot14})"/>
    public IReadOnlyList<short> Values { get; init; } = [];

    /// <summary>Gets the number of control values.</summary>
    /// <value>The size of the <see cref="Values"/> array.</value>
    /// <seealso cref="Values"/>
    /// <seealso cref="this[int]"/>
    public int Count => Values.Count;

    /// <summary>Gets the control value at <paramref name="index"/>.</summary>
    /// <param name="index">The zero-based CVT index; must be in <c>[0, <see cref="Count"/>)</c>.</param>
    /// <returns>The FWORD value at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or at least <see cref="Count"/>.</exception>
    /// <remarks>The indexer validates both bounds before the array access, so the exception is always <see cref="ArgumentOutOfRangeException"/> rather than the raw <see cref="IndexOutOfRangeException"/> from the backing list.</remarks>
    /// <seealso cref="Values"/>
    /// <seealso cref="Count"/>
    public short this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Values.Count);
            return Values[index];
        }
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the cvt table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose table directory provides the declared table length.</param>
    /// <returns>The parsed cvt table.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The table's declared length is not a multiple of two.</exception>
    /// <exception cref="EndOfStreamException">The value array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The entry count is not stored in the table itself; it is derived from the declared table length divided by <c>sizeof(int16)</c>.</description></item>
    /// <item><description>The declared length from the sfnt directory is the authoritative bound; <c>cursor.Source</c> is table-scoped, so the reader consumes exactly the bytes the table declares.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt">cvt table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FontFace.GetTableLength(Tag)"/>
    /// <seealso cref="Values"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt">OpenType specification: cvt table</seealso>
    static CvtTable IRecord<CvtTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(CvtTable)}.Parse requires a {nameof(FontFace)} context " +
                "to determine the table's byte length.");

        int length = face.GetTableLength(Tag);
        if (length % 2 != 0)
            throw new InvalidDataException(
                $"'cvt ' table length is {length}, not a multiple of 2.");

        return new CvtTable
        {
            Values = cursor.ReadInt16Array(length / 2),
        };
    }

    /// <summary>Evaluates the CVT values at a specific variation instance by applying the deltas supplied by a <c>cvar</c> table.</summary>
    /// <param name="cvar">The CVT variations table, or <c>null</c> for a static font.</param>
    /// <param name="normalizedCoords">The normalized axis coordinates. Length must equal the number of axes in <c>fvar</c>.</param>
    /// <returns>One adjusted value per CVT entry. The array has the same length as <see cref="Values"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The algorithm follows the tuple variation store interpolation rules. Each tuple produces a scalar from its peak and intermediate region coordinates; the scalar multiplies the tuple's packed deltas, and the results are summed onto the base values.</description></item>
    /// <item><description>The <c>cvar</c> table has exactly one delta per CVT index; when the tuple's point numbers mark a sparse set, only the listed indices receive deltas and the rest are left unchanged.</description></item>
    /// <item><description>When <paramref name="cvar"/> is <c>null</c> or its store has zero tuple variations, the returned array is a copy of <see cref="Values"/> as <see cref="double"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">cvar table</see> and the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#tuplevariationstore">tuple variation store</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var cvar = face.GetTable&lt;CvarTable&gt;();
    /// F2Dot14[] coords = /* normalized axis positions */;
    /// double[] values = cvt.Evaluate(cvar, coords);
    /// </code>
    /// </example>
    /// <seealso cref="Values"/>
    /// <seealso cref="CvarTable"/>
    /// <seealso cref="CvarTable.Store"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">OpenType specification: cvar table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#tuplevariationstore">OpenType specification: TupleVariationStore</seealso>
    public double[] Evaluate(CvarTable? cvar, ReadOnlySpan<F2Dot14> normalizedCoords)
    {
        var result = new double[Values.Count];
        for (int i = 0; i < result.Length; i++)
            result[i] = Values[i];

        if (cvar is null || cvar.Store.TupleVariationCount == 0)
            return result;

        var store = cvar.Store;

        for (int t = 0; t < store.TupleVariationCount; t++)
        {
            double scalar = store.GetScalar(t, normalizedCoords);
            if (scalar == 0.0) continue;

            var data = store.Data[t];
            var pointNumbers = data.PrivatePointNumbers ?? store.SharedPointNumbers;
            // 'cvar' has exactly one delta per CVT index.
            var deltas = data.Deltas[0];

            if (pointNumbers is { IsAllPoints: false } points)
            {
                // Deltas apply only to the listed CVT indices.
                for (int i = 0; i < points.Points.Count && i < deltas.Length; i++)
                {
                    int cvtIndex = points.Points[i];
                    if ((uint)cvtIndex < (uint)result.Length)
                        result[cvtIndex] += deltas[i] * scalar;
                }
            }
            else
            {
                // Deltas apply to every CVT entry in order.
                int n = Math.Min(deltas.Length, result.Length);
                for (int i = 0; i < n; i++)
                    result[i] += deltas[i] * scalar;
            }
        }

        return result;
    }
}
