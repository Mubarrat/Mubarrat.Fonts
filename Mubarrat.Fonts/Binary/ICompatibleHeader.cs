namespace Mubarrat.Fonts.Binary;

/// <summary>
/// Defines a compatibility contract for exposing readable and writable
/// header storage from an instance of a reference type.
/// </summary>
/// <typeparam name="T">
/// The reference type whose instance contains the compatible header storage.
/// </typeparam>
/// <typeparam name="THeader">
/// The unmanaged structure representing the header.
/// </typeparam>
/// <remarks>
/// Implementations must return references to the header storage associated
/// with the specified instance. The storage must be large enough and
/// correctly aligned for <typeparamref name="THeader"/>, and each returned
/// reference must remain valid for the duration of its use.
///
/// This interface is intended for low-level binary operations that access
/// header storage directly, avoiding intermediate copies where possible.
/// It does not guarantee any particular memory layout; implementations are
/// responsible for ensuring that their storage is compatible with
/// <typeparamref name="THeader"/>.
/// </remarks>
public interface ICompatibleHeader<T, THeader> where T : class where THeader : unmanaged
{
    /// <summary>
    /// Gets a writable reference to the header storage associated with
    /// the specified instance.
    /// </summary>
    /// <param name="instance">
    /// The instance whose header storage is to be accessed.
    /// </param>
    /// <returns>
    /// A writable reference to the instance's header storage.
    /// </returns>
    /// <remarks>
    /// The returned reference aliases the underlying storage. Modifications
    /// made through it directly affect that storage. The implementation
    /// must ensure that the reference identifies valid, writable storage
    /// for the duration of its use.
    /// </remarks>
    static abstract ref THeader GetWritableReference(T instance);

    /// <summary>
    /// Gets a read-only reference to the header storage associated with
    /// the specified instance.
    /// </summary>
    /// <param name="instance">
    /// The instance whose header storage is to be accessed.
    /// </param>
    /// <returns>
    /// A read-only reference to the instance's header storage.
    /// </returns>
    /// <remarks>
    /// The returned reference aliases the underlying storage without
    /// permitting modification through that reference. The underlying
    /// storage may still be modified through other references.
    /// The implementation must ensure that the reference identifies valid
    /// storage for the duration of its use.
    /// </remarks>
    static abstract ref readonly THeader GetReadOnlyReference(T instance);
}
