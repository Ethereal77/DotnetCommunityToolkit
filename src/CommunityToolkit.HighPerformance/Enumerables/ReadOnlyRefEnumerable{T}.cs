// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Helpers.Internals;
using CommunityToolkit.HighPerformance.Memory.Internals;

namespace CommunityToolkit.HighPerformance.Enumerables;

/// <summary>
/// A <see langword="ref"/> <see langword="struct"/> that iterates readonly items from arbitrary memory locations.
/// </summary>
/// <typeparam name="T">The type of items to enumerate.</typeparam>
public readonly ref struct ReadOnlyRefEnumerable<T>
{
    /// <summary>
    /// The <typeparamref name="T"/> reference for the <see cref="ReadOnlyRefEnumerable{T}"/> instance.
    /// </summary>
    private readonly ref readonly T reference;

    /// <summary>
    /// The length of the current sequence.
    /// </summary>
    private readonly int length;

    /// <summary>
    /// The distance between items in the sequence to enumerate.
    /// </summary>
    /// <remarks>The distance refers to <typeparamref name="T"/> items, not byte offset.</remarks>
    private readonly int step;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyRefEnumerable{T}"/> struct.
    /// </summary>
    /// <param name="reference">A reference to the first item of the sequence.</param>
    /// <param name="length">The number of items in the sequence.</param>
    /// <param name="step">The distance between items in the sequence to enumerate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyRefEnumerable(in T reference, int length, int step)
    {
        this.reference = ref reference;
        this.length = length;
        this.step = step;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ReadOnlyRefEnumerable{T}"/> struct with the specified parameters.
    /// </summary>
    /// <param name="value">The reference to the first <typeparamref name="T"/> item to map.</param>
    /// <param name="length">The number of items in the sequence.</param>
    /// <param name="step">The distance between items in the sequence to enumerate.</param>
    /// <returns>A <see cref="ReadOnlyRefEnumerable{T}"/> instance with the specified parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the parameters are negative.</exception>
    public static ReadOnlyRefEnumerable<T> DangerousCreate(in T value, int length, int step)
    {
        if (length < 0)
        {
            ThrowArgumentOutOfRangeExceptionForLength();
        }

        if (step < 0)
        {
            ThrowArgumentOutOfRangeExceptionForStep();
        }

        OverflowHelper.EnsureIsInNativeIntRange(length, 1, step);

        return new(in value, length, step);
    }

    /// <summary>
    /// Gets the total available length for the sequence.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.length;
    }

    /// <summary>
    /// Gets the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when <paramref name="index"/> is invalid.
    /// </exception>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Length)
            {
                ThrowHelper.ThrowIndexOutOfRangeException();
            }

            ref T r0 = ref Unsafe.AsRef(in this.reference);
            nint offset = (nint)(uint)index * (nint)(uint)this.step;
            ref T ri = ref Unsafe.Add(ref r0, offset);

            return ref ri;
        }
    }

    /// <summary>
    /// Gets the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when <paramref name="index"/> is invalid.
    /// </exception>
    public ref readonly T this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this[index.GetOffset(Length)];
    }

    /// <inheritdoc cref="System.Collections.IEnumerable.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        return new(in this.reference, this.length, this.step);
    }

    /// <summary>
    /// Copies the contents of this <see cref="ReadOnlyRefEnumerable{T}"/> into a destination <see cref="RefEnumerable{T}"/> instance.
    /// </summary>
    /// <param name="destination">The destination <see cref="RefEnumerable{T}"/> instance.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> is shorter than the source <see cref="ReadOnlyRefEnumerable{T}"/> instance.
    /// </exception>
    public void CopyTo(RefEnumerable<T> destination)
    {
        if (this.step == 1)
        {
            destination.CopyFrom(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.reference), this.length));

            return;
        }

        if (destination.Step == 1)
        {
            CopyTo(MemoryMarshal.CreateSpan(ref destination.Reference, destination.Length));

            return;
        }

        ref T sourceRef = ref Unsafe.AsRef(in this.reference);
        ref T destinationRef = ref destination.Reference;
        int sourceLength = this.length;
        int destinationLength = destination.Length;

        if ((uint)destinationLength < (uint)sourceLength)
        {
            ThrowArgumentExceptionForDestinationTooShort();
        }

        RefEnumerableHelper.CopyTo(ref sourceRef, ref destinationRef, (nint)(uint)sourceLength, (nint)(uint)this.step, (nint)(uint)destination.Step);
    }

    /// <summary>
    /// Attempts to copy the current <see cref="ReadOnlyRefEnumerable{T}"/> instance to a destination <see cref="RefEnumerable{T}"/>.
    /// </summary>
    /// <param name="destination">The target <see cref="RefEnumerable{T}"/> of the copy operation.</param>
    /// <returns>Whether or not the operation was successful.</returns>
    public bool TryCopyTo(RefEnumerable<T> destination)
    {
        int sourceLength = this.length;
        int destinationLength = destination.Length;

        if (destinationLength >= sourceLength)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies the contents of this <see cref="RefEnumerable{T}"/> into a destination <see cref="Span{T}"/> instance.
    /// </summary>
    /// <param name="destination">The destination <see cref="Span{T}"/> instance.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> is shorter than the source <see cref="RefEnumerable{T}"/> instance.
    /// </exception>
    public void CopyTo(Span<T> destination)
    {
        if (this.step == 1)
        {
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.reference), this.length).CopyTo(destination);

            return;
        }

        ref T sourceRef = ref Unsafe.AsRef(in this.reference);
        int length = this.length;

        if ((uint)destination.Length < (uint)length)
        {
            ThrowArgumentExceptionForDestinationTooShort();
        }

        ref T destinationRef = ref destination.DangerousGetReference();

        RefEnumerableHelper.CopyTo(ref sourceRef, ref destinationRef, (nint)(uint)length, (nint)(uint)this.step);
    }

    /// <summary>
    /// Attempts to copy the current <see cref="RefEnumerable{T}"/> instance to a destination <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="destination">The target <see cref="Span{T}"/> of the copy operation.</param>
    /// <returns>Whether or not the operation was successful.</returns>
    public bool TryCopyTo(Span<T> destination)
    {
        int length = this.length;

        if (destination.Length >= length)
        {
            CopyTo(destination);

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="RefEnumerable{T}.ToArray"/>
    public T[] ToArray()
    {
        int length = this.length;

        // Empty array if no data is mapped
        if (length == 0)
        {
            return Array.Empty<T>();
        }

        T[] array = new T[length];

        CopyTo(array);

        return array;
    }

    /// <summary>
    /// Implicitly converts a <see cref="RefEnumerable{T}"/> instance into a <see cref="ReadOnlyRefEnumerable{T}"/> one.
    /// </summary>
    /// <param name="enumerable">The input <see cref="RefEnumerable{T}"/> instance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyRefEnumerable<T>(RefEnumerable<T> enumerable)
    {
        return new(in enumerable.Reference, enumerable.Length, enumerable.Step);
    }

    /// <summary>
    /// A custom enumerator type to traverse items within a <see cref="ReadOnlyRefEnumerable{T}"/> instance.
    /// </summary>
    public ref struct Enumerator
    {
        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.reference"/>
        private readonly ref readonly T reference;

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.length"/>
        private readonly int length;

        /// <inheritdoc cref="ReadOnlyRefEnumerable{T}.step"/>
        private readonly int step;

        /// <summary>
        /// The current position in the sequence.
        /// </summary>
        private int position;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="reference">The <typeparamref name="T"/> reference to the first item of the sequence.</param>
        /// <param name="length">The length of the sequence.</param>
        /// <param name="step">The distance between items in the sequence to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(in T reference, int length, int step)
        {
            this.reference = ref reference;
            this.length = length;
            this.step = step;
            this.position = -1;
        }

        /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++this.position < this.length;
        }

        /// <inheritdoc cref="System.Collections.Generic.IEnumerator{T}.Current"/>
        public readonly ref readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref T r0 = ref Unsafe.AsRef(in this.reference);
                nint offset = (nint)(uint)this.position * (nint)(uint)this.step;
                ref T ri = ref Unsafe.Add(ref r0, offset);

                return ref ri;
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when the "length" parameter is invalid.
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForLength()
    {
        throw new ArgumentOutOfRangeException("length");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when the "step" parameter is invalid.
    /// </summary>
    private static void ThrowArgumentOutOfRangeExceptionForStep()
    {
        throw new ArgumentOutOfRangeException("step");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when the target span is too short.
    /// </summary>
    private static void ThrowArgumentExceptionForDestinationTooShort()
    {
        throw new ArgumentException("The target span is too short to copy all the current items to.");
    }
}
