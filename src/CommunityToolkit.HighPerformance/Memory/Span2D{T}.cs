// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Memory.Internals;
using CommunityToolkit.HighPerformance.Memory.Views;

#pragma warning disable CS0809, CA1065

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// <see cref="Span2D{T}"/> represents a 2D region of arbitrary memory. Like the <see cref="Span{T}"/> type,
/// it can point to either managed or native memory, or to memory allocated on the stack. It is type- and memory-safe.
/// One key difference with <see cref="Span{T}"/> and arrays is that the underlying buffer for a <see cref="Span2D{T}"/>
/// instance might not be contiguous in memory: this is supported to enable mapping arbitrary 2D regions even if they
/// require padding between boundaries of sequential rows. All this logic is handled internally by the <see cref="Span2D{T}"/>
/// type and it is transparent to the user, but note that working over discontiguous buffers has a performance impact.
/// </summary>
/// <typeparam name="T">The type of items in the current <see cref="Span2D{T}"/> instance.</typeparam>
[DebuggerTypeProxy(typeof(MemoryDebugView2D<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly ref partial struct Span2D<T>
{
    // Let's consider a representation of a discontiguous 2D memory
    // region within an existing array. The data is represented in
    // row-major order as usual, and the 'XX' grid cells represent
    // locations that are mapped by a given Span2D<T> instance:
    //
    //                _____________________stride_____...
    //  reference__  /________width_________  ________...
    //             \/                       \/
    // | -- | -- | |- | -- | -- | -- | -- | -- | -- | -- |_
    // | -- | -- | XX | XX | XX | XX | XX | XX | -- | -- | |
    // | -- | -- | XX | XX | XX | XX | XX | XX | -- | -- | |
    // | -- | -- | XX | XX | XX | XX | XX | XX | -- | -- | |_height
    // | -- | -- | XX | XX | XX | XX | XX | XX | -- | -- |_|
    // | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- |
    // | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- |
    // ...__pitch__/
    // ...________/
    //
    // The pitch is used to calculate the offset between each
    // discontiguous row, so that any arbitrary memory locations
    // can be used to internally represent a 2D span. This gives
    // users much more flexibility when creating spans from data.

    /// <summary>
    /// The <typeparamref name="T"/> reference for the <see cref="Span2D{T}"/> instance.
    /// </summary>
    private readonly ref T reference;

    /// <summary>
    /// The height of the specified 2D region.
    /// </summary>
    private readonly int height;

    /// <summary>
    /// The width of the specified 2D region.
    /// </summary>
    private readonly int width;

    /// <summary>
    /// The stride of the specified 2D region.
    /// </summary>
    /// <remarks>
    /// This combines both the width and pitch in a single value so that the indexing
    /// logic can be simplified (no need to recompute the sum every time) and be faster.
    /// </remarks>
    internal readonly int Stride;

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct with the specified parameters.
    /// </summary>
    /// <param name="value">The reference to the first <typeparamref name="T"/> item to map.</param>
    /// <param name="height">The height of the 2D memory area to map.</param>
    /// <param name="width">The width of the 2D memory area to map.</param>
    /// <param name="pitch">The pitch of the 2D memory area to map (the distance between each row).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span2D(ref T value, int height, int width, int pitch)
    {
        this.reference = ref value;
        this.height = height;
        this.width = width;
        this.Stride = width + pitch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct with the specified parameters.
    /// </summary>
    /// <param name="pointer">The pointer to the first <typeparamref name="T"/> item to map.</param>
    /// <param name="height">The height of the 2D memory area to map.</param>
    /// <param name="width">The width of the 2D memory area to map.</param>
    /// <param name="pitch">The pitch of the 2D memory area to map (the distance between each row).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the parameters are negative.</exception>
    public unsafe Span2D(void* pointer, int height, int width, int pitch)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            ThrowHelper.ThrowArgumentExceptionForManagedType();
        }

        if (width < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        if (height < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if (pitch < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
        }

        OverflowHelper.EnsureIsInNativeIntRange(height, width, pitch);

        this.reference = ref Unsafe.AsRef<T>(pointer);
        this.height = height;
        this.width = width;
        this.Stride = width + pitch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct.
    /// </summary>
    /// <param name="array">The target array to wrap.</param>
    /// <param name="height">The height of the resulting 2D area.</param>
    /// <param name="width">The width of each row in the resulting 2D area.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either <paramref name="height"/> or <paramref name="width"/> are invalid.
    /// </exception>
    /// <remarks>The total area must match the length of <paramref name="array"/>.</remarks>
    public Span2D(T[] array, int height, int width)
        : this(array, 0, height, width, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct.
    /// </summary>
    /// <param name="array">The target array to wrap.</param>
    /// <param name="offset">The initial offset within <paramref name="array"/>.</param>
    /// <param name="height">The height of the resulting 2D area.</param>
    /// <param name="width">The width of each row in the resulting 2D area.</param>
    /// <param name="pitch">The pitch in the resulting 2D area.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when one of the input parameters is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the requested area is outside of bounds for <paramref name="array"/>.
    /// </exception>
    public Span2D(T[] array, int offset, int height, int width, int pitch)
    {
        if (array.IsCovariant())
        {
            ThrowHelper.ThrowArrayTypeMismatchException();
        }

        if ((uint)offset > (uint)array.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
        }

        if (height < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if (width < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        if (pitch < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
        }

        int area = OverflowHelper.ComputeInt32Area(height, width, pitch);
        int remaining = array.Length - offset;

        if (area > remaining)
        {
            ThrowHelper.ThrowArgumentException();
        }

        this.reference = ref array.DangerousGetReferenceAt(offset);
        this.height = height;
        this.width = width;
        this.Stride = width + pitch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct wrapping a 2D array.
    /// </summary>
    /// <param name="array">The given 2D array to wrap.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    public Span2D(T[,]? array)
    {
        if (array is null)
        {
            this = default;

            return;
        }

        if (array.IsCovariant())
        {
            ThrowHelper.ThrowArrayTypeMismatchException();
        }

        this.reference = ref array.DangerousGetReference();
        this.height = array.GetLength(0);
        this.width = this.Stride = array.GetLength(1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct wrapping a 2D array.
    /// </summary>
    /// <param name="array">The given 2D array to wrap.</param>
    /// <param name="row">The target row to map within <paramref name="array"/>.</param>
    /// <param name="column">The target column to map within <paramref name="array"/>.</param>
    /// <param name="height">The height to map within <paramref name="array"/>.</param>
    /// <param name="width">The width to map within <paramref name="array"/>.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either <paramref name="height"/>, <paramref name="width"/> or <paramref name="height"/>
    /// are negative or not within the bounds that are valid for <paramref name="array"/>.
    /// </exception>
    public Span2D(T[,]? array, int row, int column, int height, int width)
    {
        if (array is null)
        {
            if (row != 0 || column != 0 || height != 0 || width != 0)
            {
                ThrowHelper.ThrowArgumentException();
            }

            this = default;

            return;
        }

        if (array.IsCovariant())
        {
            ThrowHelper.ThrowArrayTypeMismatchException();
        }

        int rows = array.GetLength(0);
        int columns = array.GetLength(1);

        if ((uint)row >= (uint)rows)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
        }

        if ((uint)column >= (uint)columns)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
        }

        if ((uint)height > (uint)(rows - row))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if ((uint)width > (uint)(columns - column))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        this.reference = ref array.DangerousGetReferenceAt(row, column);
        this.height = height;
        this.width = width;
        this.Stride = columns;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct wrapping a layer in a 3D array.
    /// </summary>
    /// <param name="array">The given 3D array to wrap.</param>
    /// <param name="depth">The target layer to map within <paramref name="array"/>.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is invalid.</exception>
    public Span2D(T[,,] array, int depth)
    {
        if (array.IsCovariant())
        {
            ThrowHelper.ThrowArrayTypeMismatchException();
        }

        if ((uint)depth >= (uint)array.GetLength(0))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForDepth();
        }

        this.reference = ref array.DangerousGetReferenceAt(depth, 0, 0);
        this.height = array.GetLength(1);
        this.width = this.Stride = array.GetLength(2);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct wrapping a layer in a 3D array.
    /// </summary>
    /// <param name="array">The given 3D array to wrap.</param>
    /// <param name="depth">The target layer to map within <paramref name="array"/>.</param>
    /// <param name="row">The target row to map within <paramref name="array"/>.</param>
    /// <param name="column">The target column to map within <paramref name="array"/>.</param>
    /// <param name="height">The height to map within <paramref name="array"/>.</param>
    /// <param name="width">The width to map within <paramref name="array"/>.</param>
    /// <exception cref="ArrayTypeMismatchException">
    /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is invalid.</exception>
    public Span2D(T[,,] array, int depth, int row, int column, int height, int width)
    {
        if (array.IsCovariant())
        {
            ThrowHelper.ThrowArrayTypeMismatchException();
        }

        if ((uint)depth >= (uint)array.GetLength(0))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForDepth();
        }

        int rows = array.GetLength(1);
        int columns = array.GetLength(2);

        if ((uint)row >= (uint)rows)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
        }

        if ((uint)column >= (uint)columns)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
        }

        if ((uint)height > (uint)(rows - row))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if ((uint)width > (uint)(columns - column))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        this.reference = ref array.DangerousGetReferenceAt(depth, row, column);
        this.height = height;
        this.width = width;
        this.Stride = columns;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct.
    /// </summary>
    /// <param name="span">The target <see cref="Span{T}"/> to wrap.</param>
    /// <param name="height">The height of the resulting 2D area.</param>
    /// <param name="width">The width of each row in the resulting 2D area.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when either <paramref name="height"/> or <paramref name="width"/> are invalid.
    /// </exception>
    /// <remarks>The total area must match the length of <paramref name="span"/>.</remarks>
    internal Span2D(Span<T> span, int height, int width)
        : this(span, 0, height, width, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Span2D{T}"/> struct.
    /// </summary>
    /// <param name="span">The target <see cref="Span{T}"/> to wrap.</param>
    /// <param name="offset">The initial offset within <paramref name="span"/>.</param>
    /// <param name="height">The height of the resulting 2D area.</param>
    /// <param name="width">The width of each row in the resulting 2D area.</param>
    /// <param name="pitch">The pitch in the resulting 2D area.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when one of the input parameters is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the requested area is outside of bounds for <paramref name="span"/>.
    /// </exception>
    internal Span2D(Span<T> span, int offset, int height, int width, int pitch)
    {
        if ((uint)offset > (uint)span.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
        }

        if (height < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if (width < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        if (pitch < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
        }

        int area = OverflowHelper.ComputeInt32Area(height, width, pitch);
        int remaining = span.Length - offset;

        if (area > remaining)
        {
            ThrowHelper.ThrowArgumentException();
        }

        this.reference = ref span.DangerousGetReferenceAt(offset);
        this.height = height;
        this.width = width;
        this.Stride = width + pitch;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Span2D{T}"/> struct with the specified parameters.
    /// </summary>
    /// <param name="value">The reference to the first <typeparamref name="T"/> item to map.</param>
    /// <param name="height">The height of the 2D memory area to map.</param>
    /// <param name="width">The width of the 2D memory area to map.</param>
    /// <param name="pitch">The pitch of the 2D memory area to map (the distance between each row).</param>
    /// <returns>A <see cref="Span2D{T}"/> instance with the specified parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the parameters are negative.</exception>
    public static Span2D<T> DangerousCreate(ref T value, int height, int width, int pitch)
    {
        if (width < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        if (height < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if (pitch < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
        }

        OverflowHelper.EnsureIsInNativeIntRange(height, width, pitch);

        return new(ref value, height, width, pitch);
    }

    /// <summary>
    /// Gets an empty <see cref="Span2D{T}"/> instance.
    /// </summary>
    public static Span2D<T> Empty => default;

    /// <summary>
    /// Gets a value indicating whether the current <see cref="Span2D{T}"/> instance is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Height == 0 || this.width == 0;
    }

    /// <summary>
    /// Gets the length of the current <see cref="Span2D{T}"/> instance.
    /// </summary>
    public nint Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)(uint)Height * (nint)(uint)this.width;
    }

    /// <summary>
    /// Gets the height of the underlying 2D memory area.
    /// </summary>
    public int Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.height;
    }

    /// <summary>
    /// Gets the width of the underlying 2D memory area.
    /// </summary>
    public int Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.width;
    }

    /// <summary>
    /// Gets the element at the specified zero-based indices.
    /// </summary>
    /// <param name="row">The target row to get the element from.</param>
    /// <param name="column">The target column to get the element from.</param>
    /// <returns>A reference to the element at the specified indices.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when either <paramref name="row"/> or <paramref name="column"/> are invalid.
    /// </exception>
    public ref T this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)row >= (uint)Height ||
                (uint)column >= (uint)this.width)
            {
                ThrowHelper.ThrowIndexOutOfRangeException();
            }

            return ref DangerousGetReferenceAt(row, column);
        }
    }

    /// <summary>
    /// Gets the element at the specified zero-based indices.
    /// </summary>
    /// <param name="row">The target row to get the element from.</param>
    /// <param name="column">The target column to get the element from.</param>
    /// <returns>A reference to the element at the specified indices.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when either <paramref name="row"/> or <paramref name="column"/> are invalid.
    /// </exception>
    public ref T this[Index row, Index column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this[row.GetOffset(Height), column.GetOffset(this.width)];
    }

    /// <summary>
    /// Slices the current instance with the specified parameters.
    /// </summary>
    /// <param name="rows">The target range of rows to select.</param>
    /// <param name="columns">The target range of columns to select.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when either <paramref name="rows"/> or <paramref name="columns"/> are invalid.
    /// </exception>
    /// <returns>A new <see cref="Span2D{T}"/> instance representing a slice of the current one.</returns>
    public Span2D<T> this[Range rows, Range columns]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            (int row, int height) = rows.GetOffsetAndLength(Height);
            (int column, int width) = columns.GetOffsetAndLength(this.width);

            return Slice(row, column, height, width);
        }
    }

    /// <summary>
    /// Clears the contents of the current <see cref="Span2D{T}"/> instance.
    /// </summary>
    public void Clear()
    {
        if (IsEmpty)
        {
            return;
        }

        if (TryGetSpan(out Span<T> span))
        {
            span.Clear();
        }
        else
        {
            // Clear one row at a time
            for (int i = 0; i < Height; i++)
            {
                GetRowSpan(i).Clear();
            }
        }
    }

    /// <summary>
    /// Copies the contents of this <see cref="Span2D{T}"/> into a destination <see cref="Span{T}"/> instance.
    /// </summary>
    /// <param name="destination">The destination <see cref="Span{T}"/> instance.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than the source <see cref="Span2D{T}"/> instance.
    /// </exception>
    public void CopyTo(Span<T> destination)
    {
        if (IsEmpty)
        {
            return;
        }

        if (TryGetSpan(out Span<T> span))
        {
            span.CopyTo(destination);
        }
        else
        {
            if (Length > destination.Length)
            {
                ThrowHelper.ThrowArgumentExceptionForDestinationTooShort();
            }

            // Copy each row individually
            for (int i = 0, j = 0; i < Height; i++, j += this.width)
            {
                GetRowSpan(i).CopyTo(destination.Slice(j));
            }
        }
    }

    /// <summary>
    /// Copies the contents of this <see cref="Span2D{T}"/> into a destination <see cref="Span2D{T}"/> instance.
    /// For this API to succeed, the target <see cref="Span2D{T}"/> has to have the same shape as the current one.
    /// </summary>
    /// <param name="destination">The destination <see cref="Span2D{T}"/> instance.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not have the same shape as the source <see cref="Span2D{T}"/> instance.
    /// </exception>
    public void CopyTo(Span2D<T> destination)
    {
        if (destination.Height != Height ||
            destination.width != this.width)
        {
            ThrowHelper.ThrowArgumentExceptionForDestinationWithNotSameShape();
        }

        if (IsEmpty)
        {
            return;
        }

        if (destination.TryGetSpan(out Span<T> span))
        {
            CopyTo(span);
        }
        else
        {
            // Copy each row individually
            for (int i = 0; i < Height; i++)
            {
                GetRowSpan(i).CopyTo(destination.GetRowSpan(i));
            }
        }
    }

    /// <summary>
    /// Attempts to copy the current <see cref="Span2D{T}"/> instance to a destination <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="destination">The target <see cref="Span{T}"/> of the copy operation.</param>
    /// <returns>Whether or not the operation was successful.</returns>
    public bool TryCopyTo(Span<T> destination)
    {
        if (destination.Length >= Length)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to copy the current <see cref="Span2D{T}"/> instance to a destination <see cref="Span2D{T}"/>.
    /// </summary>
    /// <param name="destination">The target <see cref="Span2D{T}"/> of the copy operation.</param>
    /// <returns>Whether or not the operation was successful.</returns>
    public bool TryCopyTo(Span2D<T> destination)
    {
        if (destination.Height == Height &&
            destination.Width == this.width)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Fills the elements of this span with a specified value.
    /// </summary>
    /// <param name="value">The value to assign to each element of the <see cref="Span2D{T}"/> instance.</param>
    public void Fill(T value)
    {
        if (IsEmpty)
        {
            return;
        }

        if (TryGetSpan(out Span<T> span))
        {
            span.Fill(value);
        }
        else
        {
            // Fill one row at a time
            for (int i = 0; i < Height; i++)
            {
                GetRowSpan(i).Fill(value);
            }
        }
    }

    /// <summary>
    /// Returns a reference to the 0th element of the <see cref="Span2D{T}"/> instance. If the current
    /// instance is empty, returns a <see langword="null"/> reference. It can be used for pinning
    /// and is required to support the use of span within a fixed statement.
    /// </summary>
    /// <returns>A reference to the 0th element, or a <see langword="null"/> reference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe ref T GetPinnableReference()
    {
        ref T r0 = ref Unsafe.AsRef<T>(null);

        if (Length != 0)
        {
            r0 = ref this.reference;
        }

        return ref r0;
    }

    /// <summary>
    /// Returns a reference to the first element within the current instance, with no bounds check.
    /// </summary>
    /// <returns>A reference to the first element within the current instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReference()
    {
        return ref this.reference;
    }

    /// <summary>
    /// Returns a reference to a specified element within the current instance, with no bounds check.
    /// </summary>
    /// <param name="i">The target row to get the element from.</param>
    /// <param name="j">The target column to get the element from.</param>
    /// <returns>A reference to the element at the specified indices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T DangerousGetReferenceAt(int i, int j)
    {
        ref T r0 = ref this.reference;
        nint index = ((nint)(uint)i * (nint)(uint)this.Stride) + (nint)(uint)j;

        return ref Unsafe.Add(ref r0, index);
    }

    /// <summary>
    /// Slices the current instance with the specified parameters.
    /// </summary>
    /// <param name="row">The target row to map within the current instance.</param>
    /// <param name="column">The target column to map within the current instance.</param>
    /// <param name="height">The height to map within the current instance.</param>
    /// <param name="width">The width to map within the current instance.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either <paramref name="height"/>, <paramref name="width"/> or <paramref name="height"/>
    /// are negative or not within the bounds that are valid for the current instance.
    /// </exception>
    /// <returns>A new <see cref="Span2D{T}"/> instance representing a slice of the current one.</returns>
    /// <remarks>
    /// <para>
    /// Contrary to <see cref="Span{T}.Slice(int, int)"/>, this method will throw an <see cref="ArgumentOutOfRangeException"/>
    /// if attempting to perform a slice operation that would result in either axes being 0. That is, trying to call
    /// <see cref="Slice(int, int, int, int)"/> as eg. <c>Slice(row: 1, column: 0, height: 0, width: 2)</c> on an instance
    /// that has 1 row and 2 columns will throw, rather than returning a new <see cref="Span2D{T}"/> instance with 0 rows and
    /// 2 columns. For contrast, trying to eg. call <c>Slice(start: 1, length: 0)</c> on a <see cref="Span{T}"/> instance of
    /// length 1 would return a span of length 0, with the internal reference being set to right past the end of the memory.
    /// </para>
    /// <para>
    /// This is by design, and it is due to the internal memory layout that <see cref="Span2D{T}"/> has. That is, in the case
    /// of <see cref="Span{T}"/>, the only edge case scenario would be to obtain a new span of size 0, referencing the very end
    /// of the backing object (eg. an array or a <see cref="string"/> instance). In that case, the GC can correctly track things.
    /// With <see cref="Span2D{T}"/>, on the other hand, it would be possible to slice an instance with a sizeof 0 in either axis,
    /// but with the computed starting reference pointing well past the end of the internal memory area. Such a behavior would not
    /// be valid if the reference was pointing to a managed object, and it would cause memory corruptions (ie. "GC holes").
    /// </para>
    /// <para>
    /// If you specifically need to be able to obtain empty values from slicing past the valid range, consider performing the range
    /// validation yourself (ie. through some helper method), and then only invoking <see cref="Slice(int, int, int, int)"/> once the
    /// parameters are in the accepted range. Otherwise, consider returning another return explicitly, such as <see cref="Empty"/>.
    /// </para>
    /// </remarks>
    public unsafe Span2D<T> Slice(int row, int column, int height, int width)
    {
        if ((uint)row >= Height)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
        }

        if ((uint)column >= this.width)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
        }

        if ((uint)height > (Height - row))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
        }

        if ((uint)width > (this.width - column))
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
        }

        nint shift = ((nint)(uint)this.Stride * (nint)(uint)row) + (nint)(uint)column;
        int pitch = this.Stride - width;

        ref T r0 = ref Unsafe.Add(ref this.reference, shift);

        return new(ref r0, height, width, pitch);
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> for a specified row.
    /// </summary>
    /// <param name="row">The index of the target row to retrieve.</param>
    /// <exception cref="ArgumentOutOfRangeException">Throw when <paramref name="row"/> is out of range.</exception>
    /// <returns>The resulting row <see cref="Span{T}"/>.</returns>
    public Span<T> GetRowSpan(int row)
    {
        if ((uint)row >= (uint)Height)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
        }

        ref T r0 = ref DangerousGetReferenceAt(row, 0);

        return MemoryMarshal.CreateSpan(ref r0, this.width);
    }

    /// <summary>
    /// Tries to get a <see cref="Span{T}"/> instance, if the underlying buffer is contiguous and small enough.
    /// </summary>
    /// <param name="span">The resulting <see cref="Span{T}"/>, in case of success.</param>
    /// <returns>Whether or not <paramref name="span"/> was correctly assigned.</returns>
    public bool TryGetSpan(out Span<T> span)
    {
        // We can only create a Span<T> if the buffer is contiguous
        if (this.Stride == this.width &&
            Length <= int.MaxValue)
        {
            span = MemoryMarshal.CreateSpan(ref this.reference, (int)Length);

            return true;
        }

        span = default;

        return false;
    }

    /// <summary>
    /// Copies the contents of the current <see cref="Span2D{T}"/> instance into a new 2D array.
    /// </summary>
    /// <returns>A 2D array containing the data in the current <see cref="Span2D{T}"/> instance.</returns>
    public T[,] ToArray()
    {
        T[,] array = new T[Height, this.width];

        CopyTo(array.AsSpan());

        return array;
    }

    /// <inheritdoc cref="Span{T}.Equals(object)"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
    public override bool Equals(object? obj)
    {
        throw new NotSupportedException("CommunityToolkit.HighPerformance.Span2D<T>.Equals(object) is not supported.");
    }

    /// <inheritdoc cref="Span{T}.GetHashCode()"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("GetHashCode() on Span will always throw an exception.")]
    public override int GetHashCode()
    {
        throw new NotSupportedException("CommunityToolkit.HighPerformance.Span2D<T>.GetHashCode() is not supported.");
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"CommunityToolkit.HighPerformance.Span2D<{typeof(T)}>[{Height}, {this.width}]";
    }

    /// <summary>
    /// Checks whether two <see cref="Span2D{T}"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="Span2D{T}"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Span2D{T}"/> instance to compare.</param>
    /// <returns>Whether or not <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
    public static bool operator ==(Span2D<T> left, Span2D<T> right)
    {
        return
            Unsafe.AreSame(ref left.reference, ref right.reference) &&
            left.height == right.height &&
            left.width == right.width &&
            left.Stride == right.Stride;
    }

    /// <summary>
    /// Checks whether two <see cref="Span2D{T}"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="Span2D{T}"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Span2D{T}"/> instance to compare.</param>
    /// <returns>Whether or not <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
    public static bool operator !=(Span2D<T> left, Span2D<T> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Implicitly converts a given 2D array into a <see cref="Span2D{T}"/> instance.
    /// </summary>
    /// <param name="array">The input 2D array to convert.</param>
    public static implicit operator Span2D<T>(T[,]? array) => new(array);
}
