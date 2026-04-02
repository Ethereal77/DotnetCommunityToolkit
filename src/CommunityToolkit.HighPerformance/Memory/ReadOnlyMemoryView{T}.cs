// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Helpers;
using CommunityToolkit.HighPerformance.Memory.Internals;
using CommunityToolkit.HighPerformance.Memory.Views;

#pragma warning disable CA2231

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// <see cref="ReadOnlyMemoryView{T}"/> represents a discontinuous typed view over a <see cref="ReadOnlyMemory{T}"/> of bytes,
/// similar to <see cref="ReadOnlySpanView{T}"/>.
/// Unlike <see cref="ReadOnlySpanView{T}"/>, it is not a byref-like type.
/// </summary>
/// <typeparam name="T">
/// The type of elements in the <see cref="ReadOnlyMemoryView{T}"/>.
/// Elements must be unmanaged types (primitive or blittable).
/// </typeparam>
/// <remarks>
/// <see cref="ReadOnlyMemoryView{T}"/> represents a view over a memory region, from a specified
/// offset onwards, of a sequence of consecutive --although separated by a stride-- typed elements.
/// <list type="bullet">
/// <item>
/// If the stride is the size of the elements' type, it behaves like a regular <see cref="ReadOnlyMemory{T}"/>.
/// </item>
/// <item>
/// <para>
/// If the stride is higher than the size of the elements' type, it allows (in combination with
/// the custom offset) to "view" only a specific part of elements of a bigger structure in the
/// underlying memory.
/// </para>
/// <para>
/// For example, to view as a <c>ReadOnlyMemoryView&lt;Vector3&gt;</c> only the
/// <c>Position</c> fields of a bigger <c>Transform</c> structure (composed of position, rotation,
/// and scale) from a <c>ReadOnlyMemory&lt;Transform&gt;</c>.
/// </para>
/// </item>
/// </list>
/// </remarks>
[DebuggerTypeProxy(typeof(MemoryViewDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly struct ReadOnlyMemoryView<T> : IEquatable<ReadOnlyMemoryView<T>>
    where T : unmanaged
{
    /// <summary>
    /// The underlying memory this view is constructed upon.
    /// </summary>
    private readonly ReadOnlyMemory<byte> memory;

    /// <summary>
    /// Creates a new <see cref="ReadOnlyMemoryView{T}"/> over the specified <see cref="ReadOnlyMemory{T}"/> of bytes
    /// beginning at <paramref name="offset"/> and with a specific <paramref name="stride"/>.
    /// </summary>
    /// <param name="memory">The target <see cref="ReadOnlyMemory{T}"/> of bytes to wrap.</param>
    /// <param name="offset">The initial offset in bytes within <paramref name="memory"/>.</param>
    /// <param name="stride">The stride in bytes between consecutive elements in <paramref name="memory"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="stride"/> specified is less than the size of the
    /// <typeparamref name="T"/> type, or when the offset is outside the wrapped <paramref name="memory"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlyMemoryView(ReadOnlyMemory<byte> memory, int offset, int stride)
    {
        if ((uint)offset > (uint)memory.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
        }

        if (stride < sizeof(T))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStride();
        }

        this.memory = memory.Slice(start: offset);
        Stride = stride;
        Length = MemoryViewHelper.GetLength<T>(stride, this.memory.Length);
    }

    /// <summary>
    /// Creates a new <see cref="ReadOnlyMemoryView{T}"/> over the specified <see cref="ReadOnlyMemory{T}"/> of bytes
    /// beginning at <paramref name="offset"/> and with a specific <paramref name="stride"/>,
    /// but only considering a number of elements equal to <paramref name="length"/>.
    /// </summary>
    /// <param name="memory">The target memory to wrap.</param>
    /// <param name="offset">The initial offset in bytes within <paramref name="memory"/>.</param>
    /// <param name="stride">The stride in bytes between consecutive elements in <paramref name="memory"/>.</param>
    /// <param name="length">The number of elements in the view.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="stride"/> specified is less than the size of the
    /// <typeparamref name="T"/> type, or when <paramref name="length"/> specifies more
    /// elements than available in the wrapped <paramref name="memory"/>, or when
    /// the offset is outside the wrapped <paramref name="memory"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ReadOnlyMemoryView(ReadOnlyMemory<byte> memory, int offset, int stride, int length)
    {
        if ((uint)offset > (uint)memory.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
        }

        ReadOnlyMemory<byte> memoryFromOffset = memory.Slice(start: offset);

        if (stride < sizeof(T))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStride();
        }

        if (length > MemoryViewHelper.GetLength<T>(stride, memoryFromOffset.Length))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForLength();
        }

        this.memory = memoryFromOffset;
        Stride = stride;
        Length = length;
    }

    /// <summary>
    /// Creates a new <see cref="ReadOnlyMemoryView{T}"/> instance over a buffer, representing the values
    /// at a given offset from each element.
    /// </summary>
    /// <typeparam name="TBuffer">The type of the elements in the memory buffer to wrap.</typeparam>
    /// <param name="buffer">
    /// The <see cref="ReadOnlyMemory{T}"/> of <typeparamref name="TBuffer"/> over which to create a view.
    /// </param>
    /// <param name="field">
    /// A reference to a field or value of type <typeparamref name="T"/> in the first element
    /// of type <typeparamref name="TBuffer"/> of <paramref name="buffer"/>.
    /// </param>
    /// <returns>A <see cref="ReadOnlyMemoryView{T}"/> instance with the specified parameters.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="DangerousCreate{TBuffer}(ReadOnlyMemory{TBuffer}, ref readonly T)"/> method creates a view over
    /// a buffer of elements of type <typeparamref name="TBuffer"/> as if you had a <see cref="ReadOnlyMemory{T}"/>
    /// of <typeparamref name="T"/>, but only exposes the referenced <paramref name="field"/> within each element in
    /// <paramref name="buffer"/>.
    /// </para>
    /// <para>
    /// Caution when using this method. It does not validate that the <paramref name="field"/>
    /// actually refers to a field within the first element of <paramref name="buffer"/>.
    /// It is the responsibility of the caller to ensure the passed <paramref name="buffer"/>
    /// and <paramref name="field"/> reference are valid.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlyMemoryView<T> DangerousCreate<TBuffer>(ReadOnlyMemory<TBuffer> buffer, ref readonly T field)
        where TBuffer : unmanaged
    {
        ReadOnlyMemory<byte> memory = buffer.Cast<TBuffer, byte>();
        nint offset = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(memory.Span), ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in field)));
        return new ReadOnlyMemoryView<T>(memory, (int)offset, sizeof(TBuffer));
    }

    /// <summary>
    /// Creates a new <see cref="ReadOnlyMemoryView{T}"/> instance over a buffer, representing the values
    /// at a given offset from each element.
    /// </summary>
    /// <typeparam name="TBuffer">The type of the elements in the memory buffer to wrap.</typeparam>
    /// <param name="buffer">
    /// The <see cref="ReadOnlyMemory{T}"/> of <typeparamref name="TBuffer"/> over which to create a view.
    /// </param>
    /// <param name="offset">
    /// The offset from the start of <paramref name="buffer"/> to a field or value of type <typeparamref name="T"/>
    /// in the first element of type <typeparamref name="TBuffer"/> of <paramref name="buffer"/>.
    /// </param>
    /// <returns>A <see cref="ReadOnlyMemoryView{T}"/> instance with the specified parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the offset is outside the wrapped <paramref name="buffer"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="DangerousCreate{TBuffer}(ReadOnlyMemory{TBuffer}, int)"/> method creates a view over a buffer of elements
    /// of type <typeparamref name="TBuffer"/> as if you had a <see cref="ReadOnlyMemory{T}"/> of <typeparamref name="T"/>,
    /// but only exposes the field at the specified <paramref name="offset"/> within each element in <paramref name="buffer"/>.
    /// </para>
    /// <para>
    /// Caution when using this method. It does not validate that the <paramref name="offset"/>
    /// actually refers to a field within the first element of <paramref name="buffer"/>.
    /// It is the responsibility of the caller to ensure the passed <paramref name="buffer"/> and
    /// <paramref name="offset"/> are valid.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlyMemoryView<T> DangerousCreate<TBuffer>(ReadOnlyMemory<TBuffer> buffer, int offset)
        where TBuffer : unmanaged
    {
        return new ReadOnlyMemoryView<T>(buffer.Cast<TBuffer, byte>(), offset, sizeof(TBuffer));
    }

    /// <summary>
    /// Gets the underlying memory of the current view.
    /// </summary>
    internal ReadOnlyMemory<byte> Memory => this.memory;

    /// <summary>
    /// Gets an empty <see cref="ReadOnlyMemoryView{T}"/>.
    /// </summary>
    public static ReadOnlyMemoryView<T> Empty => default;

    /// <summary>
    /// Gets a value that indicates if the current memory view is empty.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when the current memory view is empty; otherwise <see langword="false"/>.
    /// </value>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Length <= 0;
    }

    /// <summary>
    /// Gets the number of elements in the memory view.
    /// </summary>
    public nint Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Gets the stride in bytes of the current memory view.
    /// </summary>
    /// <remarks>
    /// The stride is the distance in bytes between the start of consecutive elements as
    /// seen by the memory view.
    /// </remarks>
    public int Stride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Gets a <see cref="ReadOnlySpanView{T}"/> from the memory view.
    /// </summary>
    public ReadOnlySpanView<T> SpanView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.memory.Span, 0, Stride, (int)Length);
    }

    /// <summary>
    /// Forms a slice out of the memory view, beginning at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The index at which to begin the slice.</param>
    /// <returns>The sliced <see cref="ReadOnlyMemoryView{T}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range
    /// (&lt; 0 or &gt; <see cref="Length"/>).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemoryView<T> Slice(int start)
    {
        if ((uint)start > (uint)Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStart();
        }

        return new ReadOnlyMemoryView<T>(this.memory, start * Stride, Stride, (int)(Length - start));
    }

    /// <summary>
    /// Forms a slice out of the memory view, beginning at <paramref name="start"/>,
    /// of given <paramref name="length"/>.
    /// </summary>
    /// <param name="start">The index at which to begin the slice.</param>
    /// <param name="length">The desired length for the slice.</param>
    /// <returns>The sliced <see cref="ReadOnlyMemoryView{T}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in range
    /// (&lt; 0 or &gt; <see cref="Length"/>), or if the requested <paramref name="length"/>
    /// is larger than the available length from <paramref name="start"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemoryView<T> Slice(int start, int length)
    {
        if ((uint)start > (uint)Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStart();
        }

        if ((uint)length > (uint)(Length - start))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForLength();
        }

        int byteOffset = start * Stride;
        int byteLength = MemoryViewHelper.GetByteLength<T>(Stride, length);
        ReadOnlyMemory<byte> slicedMemory = this.memory.Slice(byteOffset, byteLength);

        return new ReadOnlyMemoryView<T>(slicedMemory, offset: 0, Stride, length);
    }

    /// <summary>
    /// Copies the contents of the memory view into the <paramref name="destination"/>. If the source
    /// and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The <see cref="MemoryView{T}"/> to copy items into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="destination"/> is shorter than the source.
    /// </exception>
    public void CopyTo(MemoryView<T> destination)
    {
        SpanView.CopyTo(destination.SpanView);
    }

    /// <summary>
    /// Copies the contents of the memory view into the <paramref name="destination"/>.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The <see cref="MemoryView{T}"/> to copy items into.</param>
    /// <returns>
    /// If the <paramref name="destination"/> is shorter than the source, this method
    /// returns <see langword="false"/> and no data is written to the destination.
    /// </returns>
    public bool TryCopyTo(MemoryView<T> destination)
    {
        return SpanView.TryCopyTo(destination.SpanView);
    }

    /// <summary>
    /// Copies the contents from the memory view into a new array. This heap
    /// allocates, so should generally be avoided, however it is sometimes
    /// necessary to bridge the gap with APIs written in terms of arrays.
    /// </summary>
    public T[] ToArray() => SpanView.ToArray();

    /// <summary>
    /// Creates a handle for the memory view.
    /// The GC will not move the memory until the returned <see cref="MemoryHandle"/>
    /// is disposed, enabling taking and using the memory's address.
    /// </summary>
    public MemoryHandle Pin() => this.memory.Pin();

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// Returns <see langword="true"/> if the object is <see cref="MemoryView{T}"/> or <see cref="ReadOnlyMemoryView{T}"/>
    /// and if both objects point to the same memory and have the same length and stride.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            MemoryView<T> view => Equals(view),
            ReadOnlyMemoryView<T> readOnlyView => readOnlyView.Equals(this),
            _ => false
        };
    }

    /// <summary>
    /// Returns <see langword="true"/> if the memory view points to the same memory and has the same length and stride.
    /// Note that this does <strong>not</strong> check to see if the <strong>contents</strong> are equal.
    /// </summary>
    public bool Equals(ReadOnlyMemoryView<T> other)
    {
        return Length == other.Length
            && Stride == other.Stride
            && Unsafe.AreSame(
                ref MemoryMarshal.GetReference(this.memory.Span),
                ref MemoryMarshal.GetReference(other.memory.Span));
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="ReadOnlyMemory{T}"/> to a <see cref="ReadOnlyMemoryView{T}"/>.
    /// </summary>
    public static unsafe implicit operator ReadOnlyMemoryView<T>(ReadOnlyMemory<T> memory)
    {
        return new ReadOnlyMemoryView<T>(memory.Cast<T, byte>(), 0, sizeof(T), memory.Length);
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="Memory{T}"/> to a <see cref="ReadOnlyMemoryView{T}"/>.
    /// </summary>
    public static unsafe implicit operator ReadOnlyMemoryView<T>(Memory<T> memory)
    {
        return new ReadOnlyMemoryView<T>(memory.Cast<T, byte>(), 0, sizeof(T), memory.Length);
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="MemoryView{T}"/> to a <see cref="ReadOnlyMemoryView{T}"/>.
    /// </summary>
    public static implicit operator ReadOnlyMemoryView<T>(MemoryView<T> view)
    {
        return new ReadOnlyMemoryView<T>(view.Memory, 0, view.Stride, (int)view.Length);
    }

    /// <summary>
    /// Returns the hash code for the memory view.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe override int GetHashCode()
    {
        // We hash the raw pointer to the first byte of the span rather than the Memory<byte>
        // field itself, because each Cast() call wraps the same backing array in a new
        // MemoryManager object — so two independently created views over the same array would
        // produce different hashes via memory.GetHashCode(), violating the Equals/GetHashCode
        // contract.
        //
        // Note: like any pointer-based hash, this value may change across GC
        // compactions, so ReadOnlyMemoryView<T> should not be used as a long-lived dictionary
        // key without first pinning the underlying memory.
        return HashCode.Combine(
            (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(this.memory.Span)),
            Stride,
            Length);
    }

    /// <summary>
    /// For <see cref="ReadOnlyMemoryView{Char}"/>, returns a new instance of string that represents
    /// the characters pointed to by the memory view.
    /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
    /// </summary>
    public override string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            return SpanView.ToString();
        }

        return nameof(ReadOnlyMemoryView<T>) + $"<{typeof(T).Name}>[{Length}]";
    }
}