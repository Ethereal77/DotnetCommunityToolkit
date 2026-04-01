// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.HighPerformance.Helpers;

/// <summary>
/// Helpers to process sequences of values by reference with a given stride in bytes.
/// </summary>
internal static class MemoryViewHelper
{
    // Most of these methods are all essentially copies of the methods in RefEnumerableHelper,
    // with the difference that these versions call Unsafe.AddByteOffset instead of Unsafe.Add.

    /// <summary>
    /// Returns the number of elements of type <typeparamref name="T"/> that fit in
    /// <paramref name="byteLength"/> bytes assuming elements are separated by
    /// a specific <paramref name="stride"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="stride">
    /// The stride between elements, i.e., the distance in bytes between the start of an element
    /// and the start of the next element.
    /// </param>
    /// <param name="byteLength">The available length in bytes.</param>
    /// <returns>The actual number of elements that fit in the specified length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetLength<T>(int stride, int byteLength)
        where T : unmanaged
    {
        return Math.DivRem(byteLength, stride, out int rem) + (rem >= sizeof(T) ? 1 : 0);
    }

    /// <summary>
    /// Returns the byte length occupied by <paramref name="length"/> consecutive elements of
    /// type <typeparamref name="T"/> separated by a specific <paramref name="stride"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="stride">
    /// The stride between elements, i.e., the distance in bytes between the start of an element
    /// and the start of the next element.
    /// </param>
    /// <param name="length">
    /// The number of elements of type <typeparamref name="T"/>.
    /// </param>
    /// <returns>The actual length in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetByteLength<T>(int stride, int length)
        where T : unmanaged
    {
        return length switch
        {
            0 => 0,
            1 => sizeof(T),
            _ => sizeof(T) + (length - 1) * stride
        };
    }

    /// <summary>
    /// Clears a target memory area.
    /// </summary>
    /// <typeparam name="T">The type of the elements to clear.</typeparam>
    /// <param name="r0">A <typeparamref name="T"/> reference to the start of the memory area.</param>
    /// <param name="length">The number of elements in the memory area.</param>
    /// <param name="stride">The distance in bytes between each consecutive target element.</param>
    public static void Clear<T>(ref T r0, nint length, nint stride)
    {
        nint offset = 0;

        // Main loop with 8 unrolled iterations
        while (length >= 8)
        {
            Unsafe.AddByteOffset(ref r0, offset) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;

            length -= 8;
            offset += stride;
        }

        if (length >= 4)
        {
            Unsafe.AddByteOffset(ref r0, offset) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;
            Unsafe.AddByteOffset(ref r0, offset += stride) = default!;

            length -= 4;
            offset += stride;
        }

        // Clear the remaining values
        while (length > 0)
        {
            Unsafe.AddByteOffset(ref r0, offset) = default!;

            length -= 1;
            offset += stride;
        }
    }

    /// <summary>
    /// Copies a sequence of discontiguous elements from one memory area to another.
    /// </summary>
    /// <typeparam name="T">The type of the elements to copy.</typeparam>
    /// <param name="sourceRef">The source reference to copy from.</param>
    /// <param name="destinationRef">The target reference to copy to.</param>
    /// <param name="length">The total number of elements to copy.</param>
    /// <param name="sourceStride">The distance in bytes between consecutive elements in the memory area pointed to by <paramref name="sourceRef"/>.</param>
    public static void CopyTo<T>(ref T sourceRef, ref T destinationRef, nint length, nint sourceStride)
    {
        nint sourceOffset = 0;
        nint destinationOffset = 0;

        while (length >= 8)
        {
            Unsafe.Add(ref destinationRef, destinationOffset + 0) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);
            Unsafe.Add(ref destinationRef, destinationOffset + 1) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 2) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 3) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 4) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 5) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 6) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 7) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);

            length -= 8;
            sourceOffset += sourceStride;
            destinationOffset += 8;
        }

        if (length >= 4)
        {
            Unsafe.Add(ref destinationRef, destinationOffset + 0) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);
            Unsafe.Add(ref destinationRef, destinationOffset + 1) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 2) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.Add(ref destinationRef, destinationOffset + 3) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);

            length -= 4;
            sourceOffset += sourceStride;
            destinationOffset += 4;
        }

        while (length > 0)
        {
            Unsafe.Add(ref destinationRef, destinationOffset) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);

            length -= 1;
            sourceOffset += sourceStride;
            destinationOffset += 1;
        }
    }

    /// <summary>
    /// Copies a sequence of discontiguous elements from one memory area to another.
    /// </summary>
    /// <typeparam name="T">The type of the elements to copy.</typeparam>
    /// <param name="sourceRef">The source reference to copy from.</param>
    /// <param name="destinationRef">The target reference to copy to.</param>
    /// <param name="length">The total number of elements to copy.</param>
    /// <param name="sourceStride">The distance in bytes between consecutive elements in the memory area pointed to by <paramref name="sourceRef"/>.</param>
    /// <param name="destinationStride">The distance in bytes between consecutive elements in the memory area pointed to by <paramref name="destinationRef"/>.</param>
    public static void CopyTo<T>(ref T sourceRef, ref T destinationRef, nint length, nint sourceStride, nint destinationStride)
    {
        nint sourceOffset = 0;
        nint destinationOffset = 0;

        while (length >= 8)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);

            length -= 8;
            sourceOffset += sourceStride;
            destinationOffset += destinationStride;
        }

        if (length >= 4)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset += sourceStride);

            length -= 4;
            sourceOffset += sourceStride;
            destinationOffset += destinationStride;
        }

        while (length > 0)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.AddByteOffset(ref sourceRef, sourceOffset);

            length -= 1;
            sourceOffset += sourceStride;
            destinationOffset += destinationStride;
        }
    }

    /// <summary>
    /// Copies a sequence of discontiguous elements from one memory area to another.
    /// </summary>
    /// <typeparam name="T">The type of the elements to copy.</typeparam>
    /// <param name="sourceRef">The source reference to copy from.</param>
    /// <param name="destinationRef">The target reference to copy to.</param>
    /// <param name="length">The total number of elements to copy.</param>
    /// <param name="destinationStride">The distance in bytes between consecutive elements in the memory area pointed to by <paramref name="destinationRef"/>.</param>
    public static void CopyFrom<T>(ref T sourceRef, ref T destinationRef, nint length, nint destinationStride)
    {
        nint sourceOffset = 0;
        nint destinationOffset = 0;

        while (length >= 8)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.Add(ref sourceRef, sourceOffset);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 1);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 2);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 3);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 4);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 5);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 6);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 7);

            length -= 8;
            sourceOffset += 8;
            destinationOffset += destinationStride;
        }

        if (length >= 4)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.Add(ref sourceRef, sourceOffset);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 1);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 2);
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset += destinationStride) = Unsafe.Add(ref sourceRef, sourceOffset + 3);

            length -= 4;
            sourceOffset += 4;
            destinationOffset += destinationStride;
        }

        while (length > 0)
        {
            Unsafe.AddByteOffset(ref destinationRef, destinationOffset) = Unsafe.Add(ref sourceRef, sourceOffset);

            length -= 1;
            sourceOffset += 1;
            destinationOffset += destinationStride;
        }
    }

    /// <summary>
    /// Fills a target memory area.
    /// </summary>
    /// <typeparam name="T">The type of the elements to fill.</typeparam>
    /// <param name="r0">A <typeparamref name="T"/> reference to the start of the memory area.</param>
    /// <param name="length">The number of elements in the memory area.</param>
    /// <param name="stride">The distance in bytes between each consecutive target element.</param>
    /// <param name="value">The value to assign to every element in the target memory area.</param>
    public static void Fill<T>(ref T r0, nint length, nint stride, T value)
    {
        nint offset = 0;

        while (length >= 8)
        {
            Unsafe.AddByteOffset(ref r0, offset) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;

            length -= 8;
            offset += stride;
        }

        if (length >= 4)
        {
            Unsafe.AddByteOffset(ref r0, offset) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;
            Unsafe.AddByteOffset(ref r0, offset += stride) = value;

            length -= 4;
            offset += stride;
        }

        while (length > 0)
        {
            Unsafe.AddByteOffset(ref r0, offset) = value;

            length -= 1;
            offset += stride;
        }
    }
}