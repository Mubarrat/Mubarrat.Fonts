using Microsoft.Win32.SafeHandles;

namespace Mubarrat.Fonts.Binary;

/// <summary>A <see cref="Source"/> backed by a file. Uses <see cref="RandomAccess"/> so concurrent reads at different offsets do not contend on a shared file cursor.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every read is issued through <see cref="RandomAccess.Read(SafeFileHandle, Span{byte}, long)"/>, which takes an absolute offset and therefore does not rely on the file handle's seek position.</description></item>
/// <item><description>Because no shared cursor exists, multiple <see cref="Cursor"/> instances may read from the same <see cref="FileSource"/> concurrently without synchronisation.</description></item>
/// <item><description>The handle is owned by this instance and is disposed by <see cref="Dispose"/>.</description></item>
/// <item><description>Font files are addressed by absolute byte offset throughout parsing; this is the primary reason <see cref="Source"/> exposes an offset-based API rather than a stream.</description></item>
/// </list>
/// <para>For the composition of an OpenType font file and the offsets used to locate its tables, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</para>
/// </remarks>
/// <seealso cref="Source"/>
/// <seealso cref="SliceSource"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess">.NET API: <c>RandomAccess</c></seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.safehandles.safefilehandle">.NET API: <c>SafeFileHandle</c></seealso>
public sealed class FileSource : Source
{
    private readonly SafeFileHandle _handle;

    /// <summary>Creates a file-backed source.</summary>
    /// <param name="handle">A handle opened for random access.</param>
    /// <param name="length">The file length in bytes.</param>
    /// <remarks>The instance takes ownership of <paramref name="handle"/>; disposing this <see cref="FileSource"/> closes it. Callers that obtained the handle from <see cref="File.OpenHandle(string, FileMode, FileAccess, FileShare, FileOptions, long)"/> must not use it after this constructor returns.</remarks>
    /// <seealso cref="Open(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.safehandles.safefilehandle">.NET API: <c>SafeFileHandle</c></seealso>
    public FileSource(SafeFileHandle handle, long length)
    {
        _handle = handle;
        Length = length;
    }

    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
    public override long Length { get; }

    /// <inheritdoc/>
    /// <remarks>Reads are retried until <paramref name="destination"/> is full or the file reports end-of-stream, because a single <see cref="RandomAccess.Read(SafeFileHandle, Span{byte}, long)"/> call may return fewer bytes than requested.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess.read">.NET API: <c>RandomAccess.Read</c></seealso>
    protected override void ReadAtCore(long offset, Span<byte> destination)
    {
        int total = 0;
        while (total < destination.Length)
        {
            int read = RandomAccess.Read(_handle, destination[total..], offset + total);
            if (read == 0)
                throw new EndOfStreamException(
                    $"Unexpected end of file while reading at offset {offset + total}.");
            total += read;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Disposes the underlying <see cref="SafeFileHandle"/>. After this call returns, the file is closed and any further read will throw <see cref="ObjectDisposedException"/>.</remarks>
    /// <seealso cref="Source.Dispose"/>
    /// <seealso cref="System.IDisposable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    public override void Dispose() => _handle.Dispose();

    /// <summary>Opens a file at <paramref name="path"/> for random-access reading.</summary>
    /// <param name="path">The path of the file to open.</param>
    /// <returns>A <see cref="FileSource"/> over the opened file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The file could not be opened for reading.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The file is opened with <see cref="FileShare.Read"/> so other readers may access it concurrently, but writers are excluded for the lifetime of this source.</description></item>
    /// <item><description><see cref="FileOptions.RandomAccess"/> is set as a hint to the operating system that reads will not be sequential.</description></item>
    /// <item><description>The length is captured once at open time; if the file grows or shrinks during parsing, <see cref="Length"/> will not reflect the change.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FileSource(SafeFileHandle, long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.file.openhandle">.NET API: <c>File.OpenHandle</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileoptions">.NET API: <c>FileOptions</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileshare">.NET API: <c>FileShare</c></seealso>
    public static FileSource Open(string path)
    {
        var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            FileOptions.RandomAccess);

        long length = RandomAccess.GetLength(handle);
        return new FileSource(handle, length);
    }
}
