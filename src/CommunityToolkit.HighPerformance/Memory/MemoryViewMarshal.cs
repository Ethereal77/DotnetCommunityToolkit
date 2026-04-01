// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// Provides methods to interoperate with <see cref="MemoryView{T}"/>, <see cref="ReadOnlyMemoryView{T}"/>,
/// <see cref="SpanView{T}"/>, and <see cref="ReadOnlySpanView{T}"/>.
/// </summary>
public static class MemoryViewMarshal
{
    /// <summary>
    /// Returns the underlying <see cref="Memory{T}"/> buffer of a <see cref="MemoryView{T}"/>.
    /// </summary>
    /// <param name="view">The <see cref="MemoryView{T}"/>.</param>
    /// <returns>The <see cref="Memory{T}"/> over which <paramref name="view"/> is created.</returns>
    public static Memory<byte> GetMemory<T>(MemoryView<T> view)
        where T : unmanaged
    {
        return view.Memory;
    }

    /// <summary>
    /// Returns the underlying <see cref="ReadOnlyMemory{T}"/> buffer of a <see cref="ReadOnlyMemoryView{T}"/>.
    /// </summary>
    /// <param name="view">The <see cref="ReadOnlyMemoryView{T}"/>.</param>
    /// <returns>The <see cref="ReadOnlyMemory{T}"/> over which <paramref name="view"/> is created.</returns>
    public static ReadOnlyMemory<byte> GetMemory<T>(ReadOnlyMemoryView<T> view)
        where T : unmanaged
    {
        return view.Memory;
    }

    /// <summary>
    /// Returns the underlying <see cref="Span{T}"/> buffer of a <see cref="SpanView{T}"/>.
    /// </summary>
    /// <param name="view">The <see cref="SpanView{T}"/>.</param>
    /// <returns>The <see cref="Span{T}"/> over which <paramref name="view"/> is created.</returns>
    public static Span<byte> GetSpan<T>(SpanView<T> view)
        where T : unmanaged
    {
        return view.Span;
    }

    /// <summary>
    /// Returns the underlying <see cref="ReadOnlySpan{T}"/> buffer of a <see cref="ReadOnlySpanView{T}"/>.
    /// </summary>
    /// <param name="view">The <see cref="ReadOnlySpanView{T}"/>.</param>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> over which <paramref name="view"/> is created.</returns>
    public static ReadOnlySpan<byte> GetSpan<T>(ReadOnlySpanView<T> view)
        where T : unmanaged
    {
        return view.Span;
    }

    /// <summary>
    /// Casts a <see cref="MemoryView{T}"/> of one primitive type <typeparamref name="TFrom"/>
    /// to another primitive type <typeparamref name="TTo"/>.
    /// These types may not contain pointers or references.
    /// </summary>
    /// <param name="view">The source memory view, of type <typeparamref name="TFrom"/>.</param>
    /// <returns>The casted source view, of type <typeparamref name="TTo"/>.</returns>
    /// <remarks>
    /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
    /// </remarks>
    public static MemoryView<TTo> Cast<TFrom, TTo>(MemoryView<TFrom> view)
        where TFrom : unmanaged
        where TTo   : unmanaged
    {
        return new MemoryView<TTo>(view.Memory, 0, view.Stride);
    }

    /// <summary>
    /// Casts a <see cref="ReadOnlyMemoryView{T}"/> of one primitive type <typeparamref name="TFrom"/>
    /// to another primitive type <typeparamref name="TTo"/>.
    /// These types may not contain pointers or references.
    /// </summary>
    /// <param name="view">The source memory view, of type <typeparamref name="TFrom"/>.</param>
    /// <returns>The casted source view, of type <typeparamref name="TTo"/>.</returns>
    /// <remarks>
    /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
    /// </remarks>
    public static ReadOnlyMemoryView<TTo> Cast<TFrom, TTo>(ReadOnlyMemoryView<TFrom> view)
        where TFrom : unmanaged
        where TTo   : unmanaged
    {
        return new ReadOnlyMemoryView<TTo>(view.Memory, 0, view.Stride);
    }

    /// <summary>
    /// Casts a <see cref="SpanView{T}"/> of one primitive type <typeparamref name="TFrom"/>
    /// to another primitive type <typeparamref name="TTo"/>.
    /// These types may not contain pointers or references.
    /// </summary>
    /// <param name="view">The source span view, of type <typeparamref name="TFrom"/>.</param>
    /// <returns>The casted source view, of type <typeparamref name="TTo"/>.</returns>
    /// <remarks>
    /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
    /// </remarks>
    public static SpanView<TTo> Cast<TFrom, TTo>(SpanView<TFrom> view)
        where TFrom : unmanaged
        where TTo   : unmanaged
    {
        return new SpanView<TTo>(view.Span, 0, view.Stride);
    }

    /// <summary>
    /// Casts a <see cref="ReadOnlySpanView{T}"/> of one primitive type <typeparamref name="TFrom"/>
    /// to another primitive type <typeparamref name="TTo"/>.
    /// These types may not contain pointers or references.
    /// </summary>
    /// <param name="view">The source span view, of type <typeparamref name="TFrom"/>.</param>
    /// <returns>The casted source view, of type <typeparamref name="TTo"/>.</returns>
    /// <remarks>
    /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
    /// </remarks>
    public static ReadOnlySpanView<TTo> Cast<TFrom, TTo>(ReadOnlySpanView<TFrom> view)
        where TFrom : unmanaged
        where TTo   : unmanaged
    {
        return new ReadOnlySpanView<TTo>(view.Span, 0, view.Stride);
    }
}