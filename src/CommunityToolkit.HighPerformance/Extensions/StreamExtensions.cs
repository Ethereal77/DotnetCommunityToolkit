// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// Helpers for working with the <see cref="Stream"/> type.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Asynchronously reads a sequence of bytes from a given <see cref="Stream"/> instance.
    /// </summary>
    /// <param name="stream">The source <see cref="Stream"/> to read data from.</param>
    /// <param name="buffer">The destination <see cref="Memory{T}"/> to write data to.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the operation being performed.</returns>
    [Obsolete("This API is only available for binary compatibility, but Stream.ReadAsync should be used instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return stream.ReadAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes a sequence of bytes to a given <see cref="Stream"/> instance.
    /// </summary>
    /// <param name="stream">The destination <see cref="Stream"/> to write data to.</param>
    /// <param name="buffer">The source <see cref="ReadOnlyMemory{T}"/> to read data from.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the operation being performed.</returns>
    [Obsolete("This API is only available for binary compatibility, but Stream.WriteAsync should be used instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Reads a sequence of bytes from a given <see cref="Stream"/> instance.
    /// </summary>
    /// <param name="stream">The source <see cref="Stream"/> to read data from.</param>
    /// <param name="buffer">The target <see cref="Span{T}"/> to write data to.</param>
    /// <returns>The number of bytes that have been read.</returns>
    [Obsolete("This API is only available for binary compatibility, but Stream.Read should be used instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int Read(this Stream stream, Span<byte> buffer)
    {
        return stream.Read(buffer);
    }

    /// <summary>
    /// Writes a sequence of bytes to a given <see cref="Stream"/> instance.
    /// </summary>
    /// <param name="stream">The destination <see cref="Stream"/> to write data to.</param>
    /// <param name="buffer">The source <see cref="Span{T}"/> to read data from.</param>
    [Obsolete("This API is only available for binary compatibility, but Stream.Write should be used instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        stream.Write(buffer);
    }

    /// <summary>
    /// Reads a value of a specified type from a source <see cref="Stream"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of value to read.</typeparam>
    /// <param name="stream">The source <see cref="Stream"/> instance to read from.</param>
    /// <returns>The <typeparamref name="T"/> value read from <paramref name="stream"/>.</returns>
    /// <exception cref="EndOfStreamException">Thrown if <paramref name="stream"/> reaches the end.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Read<T>(this Stream stream)
        where T : unmanaged
    {
        T result;

        stream.ReadExactly(new Span<byte>(&result, sizeof(T)));

        return result;
    }

    /// <summary>
    /// Writes a value of a specified type into a target <see cref="Stream"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of value to write.</typeparam>
    /// <param name="stream">The target <see cref="Stream"/> instance to write to.</param>
    /// <param name="value">The input value to write to <paramref name="stream"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Write<T>(this Stream stream, in T value)
        where T : unmanaged
    {
        ref T r0 = ref Unsafe.AsRef(in value);
        ref byte r1 = ref Unsafe.As<T, byte>(ref r0);
        int length = sizeof(T);

        ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpan(ref r1, length);

        stream.Write(span);
    }
}
