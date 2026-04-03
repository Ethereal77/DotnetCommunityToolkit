// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Helpers;
using CommunityToolkit.HighPerformance.Memory.Internals;
using CommunityToolkit.HighPerformance.Memory.Views;

#pragma warning disable 0809 // Obsolete member 'SpanView<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// <see cref="SpanView{T}"/> represents a discontinuous typed view over a <see cref="Span{T}"/> of bytes.
/// </summary>
/// <typeparam name="T">
/// The type of elements in the <see cref="SpanView{T}"/>.
/// Elements must be unmanaged types (primitive or blittable).
/// </typeparam>
/// <remarks>
/// <see cref="SpanView{T}"/> represents a view over a memory region, from a specified
/// offset onwards, of a sequence of consecutive --although separated by a stride-- typed elements.
/// <list type="bullet">
/// <item>
/// If the stride is the size of the elements' type, it behaves like a regular <see cref="Span{T}"/>.
/// </item>
/// <item>
/// <para>
/// If the stride is higher than the size of the elements' type, it allows (in combination with
/// the custom offset) to "view" only a specific part of elements of a bigger structure in the
/// underlying memory.
/// </para>
/// <para>
/// For example, to view as a <c>SpanView&lt;Vector3&gt;</c> only the
/// <c>Position</c> fields of a bigger <c>Transform</c> structure (composed of position, rotation,
/// and scale) from a <c>Span&lt;Transform&gt;</c>.
/// </para>
/// </item>
/// </list>
/// </remarks>
[DebuggerTypeProxy(typeof(MemoryViewDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly ref struct SpanView<T>
    where T : unmanaged
{
    /// <summary>
    /// The underlying span this view is constructed upon.
    /// </summary>
    private readonly Span<byte> span;

    /// <summary>
    /// Creates a new <see cref="SpanView{T}"/> from a specified number of <typeparamref name="T"/> elements
    /// starting at a specified memory address and with a specific <paramref name="stride"/>.
    /// </summary>
    /// <param name="pointer">
    /// A pointer to the starting address of a specified number of <typeparamref name="T"/> elements in memory.
    /// </param>
    /// <param name="stride">The stride in bytes between consecutive elements.</param>
    /// <param name="length">The number of T elements to be included in the view.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="stride"/> specified is less than the size of the
    /// <typeparamref name="T"/> type, or when <paramref name="length"/> is negative.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SpanView(void* pointer, int stride, int length)
    {
        if (stride < sizeof(T))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStride();
        }

        if (length < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForLength();
        }

        this.span = new Span<byte>(pointer, MemoryViewHelper.GetByteLength<T>(stride, length));
        Stride = stride;
        Length = length;
    }

    /// <summary>
    /// Creates a new <see cref="SpanView{T}"/> over the specified <see cref="Span{T}"/> of bytes
    /// beginning at <paramref name="offset"/> and with a specific <paramref name="stride"/>.
    /// </summary>
    /// <param name="span">The target span to wrap.</param>
    /// <param name="offset">The initial offset in bytes within <paramref name="span"/>.</param>
    /// <param name="stride">The stride in bytes between consecutive elements in <paramref name="span"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="stride"/> specified is less than the size of the
    /// <typeparamref name="T"/> type, or when the offset is outside the wrapped <paramref name="span"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SpanView(Span<byte> span, int offset, int stride)
    {
        if (stride < sizeof(T))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStride();
        }

        this.span = span.Slice(offset);
        Stride = stride;
        Length = MemoryViewHelper.GetLength<T>(stride, this.span.Length);
    }

    /// <summary>
    /// Creates a new <see cref="SpanView{T}"/> over the specified <see cref="Span{T}"/> of bytes
    /// beginning at <paramref name="offset"/> and with a specific <paramref name="stride"/>,
    /// but only considering a number of elements equal to <paramref name="length"/>.
    /// </summary>
    /// <param name="span">The target span to wrap.</param>
    /// <param name="offset">The initial offset in bytes within <paramref name="span"/>.</param>
    /// <param name="stride">The stride in bytes between consecutive elements in <paramref name="span"/>.</param>
    /// <param name="length">The number of elements in the view.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="stride"/> specified is less than the size of the
    /// <typeparamref name="T"/> type, or when <paramref name="length"/> specifies more
    /// elements than available in the wrapped <paramref name="span"/>, or when
    /// the offset is outside the wrapped <paramref name="span"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe SpanView(Span<byte> span, int offset, int stride, int length)
    {
        if ((uint)offset > (uint)span.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
        }

        Span<byte> spanFromOffset = span.Slice(offset);

        if (stride < sizeof(T))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStride();
        }

        if (length > MemoryViewHelper.GetLength<T>(stride, spanFromOffset.Length))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForLength();
        }

        this.span = spanFromOffset;
        Stride = stride;
        Length = length;
    }

    /// <summary>
    /// Creates a new <see cref="SpanView{T}"/> instance over a span, representing the values
    /// at a given offset from each element.
    /// </summary>
    /// <typeparam name="TBuffer">The type of the elements in the span to wrap.</typeparam>
    /// <param name="buffer">
    /// The <see cref="Span{T}"/> of <typeparamref name="TBuffer"/> over which to create a view.
    /// </param>
    /// <param name="field">
    /// A reference to a field or value of type <typeparamref name="T"/> in the first element
    /// of type <typeparamref name="TBuffer"/> of <paramref name="buffer"/>.
    /// </param>
    /// <returns>A <see cref="SpanView{T}"/> instance with the specified parameters.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="DangerousCreate{TBuffer}(Span{TBuffer}, ref T)"/> method creates a view over a span of elements
    /// of type <typeparamref name="TBuffer"/> as if you had a <see cref="Span{T}"/> of <typeparamref name="T"/>,
    /// but only exposes the referenced <paramref name="field"/> within each element in <paramref name="buffer"/>.
    /// </para>
    /// <para>
    /// Caution when using this method. It does not validate that the <paramref name="field"/>
    /// actually refers to a field within the first element of <paramref name="buffer"/>.
    /// It is the responsibility of the caller to ensure the passed <paramref name="buffer"/>
    /// and <paramref name="field"/> reference are valid.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe SpanView<T> DangerousCreate<TBuffer>(Span<TBuffer> buffer, ref T field)
        where TBuffer : unmanaged
    {
        Span<byte> span = MemoryMarshal.AsBytes(buffer);
        IntPtr offset = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref Unsafe.As<T, byte>(ref field));
        return new SpanView<T>(span, (int)offset, sizeof(TBuffer));
    }

    /// <summary>
    /// Creates a new <see cref="SpanView{T}"/> instance over a span, representing the values
    /// at a given offset from each element.
    /// </summary>
    /// <typeparam name="TBuffer">The type of the elements in the span to wrap.</typeparam>
    /// <param name="buffer">
    /// The <see cref="Span{T}"/> of <typeparamref name="TBuffer"/> over which to create a view.
    /// </param>
    /// <param name="offset">
    /// The offset from the start of <paramref name="buffer"/> to a field or value of type <typeparamref name="T"/>
    /// in the first element of type <typeparamref name="TBuffer"/> of <paramref name="buffer"/>.
    /// </param>
    /// <returns>A <see cref="SpanView{T}"/> instance with the specified parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the offset is outside the wrapped <paramref name="buffer"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="DangerousCreate{TBuffer}(Span{TBuffer}, int)"/> method creates a view over a buffer of elements
    /// of type <typeparamref name="TBuffer"/> as if you had a <see cref="Span{T}"/> of <typeparamref name="T"/>,
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
    public static unsafe SpanView<T> DangerousCreate<TBuffer>(Span<TBuffer> buffer, int offset)
        where TBuffer : unmanaged
    {
        return new SpanView<T>(MemoryMarshal.AsBytes(buffer), offset, sizeof(TBuffer));
    }

    /// <summary>
    /// Gets the underlying memory of the current view.
    /// </summary>
    internal Span<byte> Span => this.span;

    /// <summary>
    /// Gets an empty <see cref="SpanView{T}"/>.
    /// </summary>
    public static SpanView<T> Empty => default;

    /// <summary>
    /// Gets a value that indicates if the current span view is empty.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when the current span view is empty; otherwise <see langword="false"/>.
    /// </value>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Length <= 0;
    }

    /// <summary>
    /// Gets the number of elements in the span view.
    /// </summary>
    public nint Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Gets the stride in bytes of the current span view.
    /// </summary>
    /// <remarks>
    /// The stride is the distance in bytes between the start of consecutive elements as
    /// seen by the span view.
    /// </remarks>
    public int Stride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Gets an element from the span view at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element from the span view.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is less than zero or greater than or equal to <see cref="Length" />.
    /// </exception>
    public ref T this[int index]
    {
#if NET7_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnscopedRef]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Length)
            {
                ThrowHelper.ThrowIndexOutOfRangeException();
            }

            return ref DangerousGetReferenceAt(index);
        }
    }

    /// <summary>
    /// Gets an enumerator for this <see cref="SpanView{T}"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="Enumerator"/> structure that can be used to iterate over the elements in the span view.
    /// </returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Forms a slice out of the span view, beginning at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The index at which to begin the slice.</param>
    /// <returns>The sliced <see cref="SpanView{T}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range
    /// (&lt; 0 or &gt; <see cref="Length"/>).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanView<T> Slice(int start)
    {
        if ((uint)start > (uint)Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForStart();
        }

        return new SpanView<T>(this.span, start * Stride, Stride, (int)(Length - start));
    }

    /// <summary>
    /// Forms a slice out of the span view, beginning at <paramref name="start"/>,
    /// of given <paramref name="length"/>.
    /// </summary>
    /// <param name="start">The index at which to begin the slice.</param>
    /// <param name="length">The desired length for the slice.</param>
    /// <returns>The sliced <see cref="SpanView{T}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in range
    /// (&lt; 0 or &gt; <see cref="Length"/>), or if the requested <paramref name="length"/>
    /// is larger than the available length from <paramref name="start"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanView<T> Slice(int start, int length)
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
        Span<byte> slicedSpan = this.span.Slice(byteOffset, byteLength);

        return new SpanView<T>(slicedSpan, offset: 0, Stride, length);
    }

    /// <summary>
    /// Clears the contents of this view.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        MemoryViewHelper.Clear(ref DangerousGetReference(), Length, Stride);
    }

    /// <summary>
    /// Fills the contents of this view with the given value.
    /// </summary>
    /// <param name="value">The value to fill the view with.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(T value)
    {
        MemoryViewHelper.Fill(ref DangerousGetReference(), Length, Stride, value);
    }

    /// <summary>
    /// Copies the contents of a <paramref name="source"/> span into this span view.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> to copy elements from.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="source"/> is larger than the view.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(ReadOnlySpan<T> source)
    {
        if ((uint)Length < (uint)source.Length)
        {
            ThrowHelper.ThrowArgumentExceptionForSourceTooLarge();
        }

        MemoryViewHelper.CopyFrom(sourceRef: ref MemoryMarshal.GetReference(source),
                                  destinationRef: ref DangerousGetReference(),
                                  length: source.Length,
                                  destinationStride: Stride);
    }

    /// <summary>
    /// Attempts to copy the contents of a <paramref name="source"/> span into this span view.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> to copy elements from.</param>
    /// <returns>
    /// <see langword="true"/> if the copy succeeded;
    /// <see langword="false"/> otherwise if <paramref name="source"/> is too large,
    /// in which case no data is written.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyFrom(ReadOnlySpan<T> source)
    {
        if ((uint)Length < (uint)source.Length)
        {
            return false;
        }

        MemoryViewHelper.CopyFrom(sourceRef: ref MemoryMarshal.GetReference(source),
                                  destinationRef: ref DangerousGetReference(),
                                  length: source.Length,
                                  destinationStride: Stride);
        return true;
    }

    /// <summary>
    /// Copies the contents of a <paramref name="source"/> span view into the current span view.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpanView{T}"/> to copy elements from.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="source"/> is larger than the current view.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(ReadOnlySpanView<T> source)
    {
        source.CopyTo(this);
    }

    /// <summary>
    /// Attempts to copy the contents of a <paramref name="source"/> span view into the current span view.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpanView{T}"/> to copy elements from.</param>
    /// <returns>
    /// <see langword="true"/> if the copy succeeded;
    /// <see langword="false"/> otherwise if <paramref name="source"/> is too large,
    /// in which case no data is written.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyFrom(ReadOnlySpanView<T> source)
    {
        return source.TryCopyTo(this);
    }

    /// <summary>
    /// Copies the contents of the span view into the <paramref name="destination"/>. If the source
    /// and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The <see cref="SpanView{T}"/> to copy items into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="destination"/> is shorter than the source.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(SpanView<T> destination)
    {
        if ((uint)Length > (uint)destination.Length)
        {
            ThrowHelper.ThrowArgumentExceptionForDestinationTooShort();
        }

        MemoryViewHelper.CopyTo(sourceRef: ref DangerousGetReference(),
                                ref destination.DangerousGetReference(),
                                Length,
                                sourceStride: Stride,
                                destination.Stride);
    }

    /// <summary>
    /// Attempts to copy the contents of the span view into the <paramref name="destination"/>.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The span view to copy items into.</param>
    /// <returns>
    /// <see langword="true"/> if the copy succeeded;
    /// <see langword="false"/> otherwise if <paramref name="destination"/> is shorter than the source,
    /// in which case no data is written.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(SpanView<T> destination)
    {
        if ((uint)Length > (uint)destination.Length)
        {
            return false;
        }

        MemoryViewHelper.CopyTo(sourceRef: ref DangerousGetReference(),
                                ref destination.DangerousGetReference(),
                                Length,
                                sourceStride: Stride,
                                destination.Stride);
        return true;
    }

    /// <summary>
    /// Copies the contents of the span view into the <paramref name="destination"/>. If the source
    /// and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy items into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="destination"/> is shorter than the source.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<T> destination)
    {
        if (Length > destination.Length)
        {
            ThrowHelper.ThrowArgumentExceptionForDestinationTooShort();
        }

        MemoryViewHelper.CopyTo(sourceRef: ref DangerousGetReference(),
                                ref MemoryMarshal.GetReference(destination),
                                Length,
                                sourceStride: Stride);
    }

    /// <summary>
    /// Attempts to copy the contents of the span view into the <paramref name="destination"/>.
    /// If the source and destination overlap, this method behaves as if the original values are in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy items into.</param>
    /// <returns>
    /// <see langword="true"/> if the copy succeeded;
    /// <see langword="false"/> otherwise if <paramref name="destination"/> is shorter than the source,
    /// in which case no data is written.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<T> destination)
    {
        if (Length > destination.Length)
        {
            return false;
        }

        MemoryViewHelper.CopyTo(sourceRef: ref DangerousGetReference(),
                                ref MemoryMarshal.GetReference(destination),
                                Length,
                                sourceStride: Stride);
        return true;
    }

    /// <summary>
    /// Returns a reference to the first element within the span view, with no bounds checks.
    /// </summary>
    /// <returns>A reference to the first element within this span view.</returns>
    /// <remarks>
    /// This method doesn't do any bounds checks, therefore it is the responsibility of the caller
    /// to perform checks in case the returned value is dereferenced.
    /// </remarks>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnscopedRef]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReference()
    {
        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(this.span));
    }

    /// <summary>
    /// Returns a reference to an element at a specified index within the span view, with no bounds checks.
    /// </summary>
    /// <param name="index">The index of the element to retrieve within this span view.</param>
    /// <returns>
    /// A reference to the element within this span view at the index specified by <paramref name="index"/>.
    /// </returns>
    /// <remarks>
    /// This method doesn't do any bounds checks, therefore it is the responsibility of the caller
    /// to ensure the <paramref name="index"/> parameter is valid.
    /// </remarks>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnscopedRef]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReferenceAt(int index)
    {
        return ref Unsafe.AddByteOffset(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(this.span)), (nint)index * Stride);
    }

    /// <summary>
    /// Returns a reference to the first element of the <see cref="SpanView{T}"/>.
    /// If the span view is empty, returns a <see langword="null"/> reference.
    /// It can be used for pinning and is required to support the use of span view within a <see langword="fixed"/> statement.
    /// </summary>
    /// <returns>
    /// A reference to the first element of the span view, or a <see langword="null"/> reference if the span view is empty.
    /// </returns>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnscopedRef]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref T GetPinnableReference()
    {
        return ref Length != 0
            ? ref DangerousGetReference()
            : ref Unsafe.NullRef<T>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SpanView{T}"/> is equal to the current span view.
    /// </summary>
    /// <param name="other">The <see cref="SpanView{T}"/> to compare against.</param>
    /// <returns>
    /// Returns <see langword="true"/> if both span views point at the same memory, have the same length,
    /// and the same stride.
    /// Note that this does <strong>not</strong> check to see if the <strong>contents</strong> are equal.
    /// </returns>
    public bool Equals(SpanView<T> other)
    {
        return Length == other.Length
            && Stride == other.Stride
            && Unsafe.AreSame(ref MemoryMarshal.GetReference(this.span),
                              ref MemoryMarshal.GetReference(other.span));
    }

    /// <summary>
    /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    [Obsolete("Equals() on SpanView will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
    {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        throw new NotSupportedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }

    /// <summary>
    /// Determines whether two <see cref="SpanView{T}"/> instances are equal.
    /// </summary>
    /// <param name="left">The <see cref="SpanView{T}"/> to the left of the operator.</param>
    /// <param name="right">The <see cref="SpanView{T}"/> to the right of the operator.</param>
    /// <returns>
    /// Returns <see langword="true"/> if both span views point at the same memory, have the same length,
    /// and the same stride.
    /// Note that this does <strong>not</strong> check to see if the <strong>contents</strong> are equal.
    /// </returns>
    public static bool operator ==(SpanView<T> left, SpanView<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="SpanView{T}"/> instances are not equal.
    /// </summary>
    /// <param name="left">The <see cref="SpanView{T}"/> to the left of the operator.</param>
    /// <param name="right">The <see cref="SpanView{T}"/> to the right of the operator.</param>
    /// <returns>
    /// Returns <see langword="true"/> if the span views point at different memory, have different lengths,
    /// or have different strides.
    /// Note that this does <strong>not</strong> check to see if the <strong>contents</strong> are equal.
    /// </returns>
    public static bool operator !=(SpanView<T> left, SpanView<T> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// This method is not supported as span views cannot be boxed.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    [Obsolete("GetHashCode() on SpanView will always throw an exception.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
    {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        throw new NotSupportedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }

    /// <summary>
    /// For <see cref="SpanView{Char}"/>, returns a new instance of <see cref="string"/> that represents
    /// the characters pointed to by the span.
    /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
    /// </summary>
    public override unsafe string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            string result = new('\0', (int)Length);

            fixed (char* p = result)
            {
                CopyTo(new Span<T>(p, (int)Length));
            }

            return result;
        }

        return nameof(SpanView<T>) + $"<{typeof(T).Name}>[{Length}]";
    }

    /// <summary>
    /// Copies the contents of this span view into a new array.
    /// This heap allocates, so should generally be avoided, however it is sometimes
    /// necessary to bridge the gap with APIs written in terms of arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray()
    {
        if (Length == 0)
        {
            return [];
        }

        T[] array = new T[Length];
        CopyTo(array);
        return array;
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="Span{T}"/> to a <see cref="SpanView{T}"/>.
    /// </summary>
    public static unsafe implicit operator SpanView<T>(Span<T> span)
    {
        return new SpanView<T>(MemoryMarshal.AsBytes(span), 0, sizeof(T), span.Length);
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="SpanView{T}"/>.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// The span view being enumerated.
        /// </summary>
        private readonly SpanView<T> view;

        /// <summary>
        /// The next position to yield.
        /// </summary>
        private int position;

        /// <summary>
        /// Initializes the enumerator.
        /// </summary>
        /// <param name="view">The span view to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(SpanView<T> view)
        {
            this.view = view;
            this.position = -1;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the <see cref="SpanView{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++this.position < this.view.Length;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly ref T Current
        {
#if NET7_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this.view.DangerousGetReferenceAt(this.position);
        }

        readonly T IEnumerator<T>.Current => Current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        void IEnumerator.Reset() => this.position = -1;

        /// <inheritdoc />
        readonly void IDisposable.Dispose() { }
    }
}