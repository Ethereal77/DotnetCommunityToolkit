// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace CommunityToolkit.HighPerformance.Memory.Views;

/// <summary>
/// A debug proxy used to display items in a memory view.
/// </summary>
/// <typeparam name="T">The type of items to display.</typeparam>
internal sealed class MemoryViewDebugView<T>
    where T : unmanaged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewDebugView{T}"/> class with the specified parameters.
    /// </summary>
    /// <param name="memoryView">The input <see cref="MemoryView{T}"/> instance with the items to display.</param>
    public MemoryViewDebugView(MemoryView<T> memoryView)
    {
        Items = memoryView.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewDebugView{T}"/> class with the specified parameters.
    /// </summary>
    /// <param name="memoryView">The input <see cref="ReadOnlyMemoryView{T}"/> instance with the items to display.</param>
    public MemoryViewDebugView(ReadOnlyMemoryView<T> memoryView)
    {
        Items = memoryView.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewDebugView{T}"/> class with the specified parameters.
    /// </summary>
    /// <param name="spanView">The input <see cref="SpanView{T}"/> instance with the items to display.</param>
    public MemoryViewDebugView(SpanView<T> spanView)
    {
        Items = spanView.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewDebugView{T}"/> class with the specified parameters.
    /// </summary>
    /// <param name="spanView">The input <see cref="ReadOnlySpanView{T}"/> instance with the items to display.</param>
    public MemoryViewDebugView(ReadOnlySpanView<T> spanView)
    {
        Items = spanView.ToArray();
    }

    /// <summary>
    /// Gets the items to display for the current instance.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public T[]? Items { get; }
}
