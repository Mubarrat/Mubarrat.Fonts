using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>Context carrying a parent <see cref="Source"/>, used by records whose layout is chosen by the parent context.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Records whose layout depends on the enclosing table implement <see cref="IRecord{T}"/> and inspect the context argument to decide which format to parse.</description></item>
/// <item><description>A record that needs the parent source casts the <c>object?</c> context to <see cref="IParentContext"/> and reads <see cref="ParentSource"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ParentContext"/>
/// <seealso cref="TagContext"/>
/// <seealso cref="Source"/>
public interface IParentContext
{
    /// <summary>Gets the parent source, which is the source from which the current record was read.</summary>
    /// <value>The <see cref="Source"/> that was used to create the cursor passed to <see cref="IRecord{T}.Parse"/>.</value>
    /// <remarks>Callers must not dispose the parent source while any record obtained from it is still in use. See <see cref="Source.Dispose"/>.</remarks>
    /// <seealso cref="Source"/>
    /// <seealso cref="ParentContext.ParentSource"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    Source ParentSource { get; }
}

/// <summary>Context carrying a parent <see cref="Source"/>, used by records whose layout is chosen by the parent context.</summary>
/// <param name="ParentSource">The parent source.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The primary-constructor parameter becomes the <see cref="Source"/> stored by the generated <c>ParentSource</c> property.</description></item>
/// <item><description>This is a reference type; passing it through <see cref="IRecord{T}.Parse"/> does not copy the underlying <see cref="Source"/>.</description></item>
/// <item><description>The compiler also synthesizes a <c>&lt;Clone&gt;$()</c> method, which must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IParentContext"/>
/// <seealso cref="TagContext"/>
/// <seealso cref="Source"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/record">C# reference: <c>record</c></seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/using-constructors">C# programming guide: Constructors</seealso>
public record ParentContext(Source ParentSource) : IParentContext;

/// <summary>Context carrying a feature <see cref="Primitives.Tag"/>, used by records whose layout is chosen by the enclosing feature's tag rather than by a discriminant stored in the record itself.</summary>
/// <param name="Tag">The feature tag the record belongs to.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>Examples of tag-driven layouts in OpenType: <c>GSUB</c>/<c>GPOS</c> feature subtables, whose format is selected by the feature tag (e.g. <c>liga</c>, <c>kern</c>, <c>mark</c>).</description></item>
/// <item><description>Tags are compared with <see cref="Tag.Equals(Tag)"/>; see <see cref="Tag"/> for the four-character representation.</description></item>
/// <item><description>This is a reference type; passing it through <see cref="IRecord{T}.Parse"/> does not copy the underlying <see cref="Primitives.Tag"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Tag"/>
/// <seealso cref="IParentContext"/>
/// <seealso cref="ParentContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/record">C# reference: <c>record</c></seealso>
public record TagContext(Tag Tag);
