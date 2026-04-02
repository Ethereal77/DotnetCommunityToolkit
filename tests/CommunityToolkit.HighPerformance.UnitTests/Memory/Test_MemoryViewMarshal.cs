// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.HighPerformance.UnitTests;

[TestClass]
public class Test_MemoryViewMarshal
{
    [TestMethod]
    public void Test_MemoryViewMarshal_GetMemory_MemoryView()
    {
        int[] data = [10, 20, 30, 40];
        MemoryView<int> view = new Memory<int>(data);
        MemoryView<int> slice = view.Slice(1, 2);

        Memory<byte> bytes = MemoryViewMarshal.GetMemory(slice);
        Span<int> values = MemoryMarshal.Cast<byte, int>(bytes.Span);

        Assert.AreEqual(2, values.Length);
        Assert.AreEqual(20, values[0]);
        Assert.AreEqual(30, values[1]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_GetMemory_ReadOnlyMemoryView()
    {
        int[] data = [11, 22, 33, 44];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);
        ReadOnlyMemoryView<int> slice = view.Slice(1, 2);

        ReadOnlyMemory<byte> bytes = MemoryViewMarshal.GetMemory(slice);
        ReadOnlySpan<int> values = MemoryMarshal.Cast<byte, int>(bytes.Span);

        Assert.AreEqual(2, values.Length);
        Assert.AreEqual(22, values[0]);
        Assert.AreEqual(33, values[1]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_GetSpan_SpanView()
    {
        int[] data = [1, 2, 3, 4];
        SpanView<int> view = new Span<int>(data);
        SpanView<int> slice = view.Slice(1, 2);

        Span<byte> bytes = MemoryViewMarshal.GetSpan(slice);
        Span<int> values = MemoryMarshal.Cast<byte, int>(bytes);

        Assert.AreEqual(2, values.Length);
        Assert.AreEqual(2, values[0]);
        Assert.AreEqual(3, values[1]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_GetSpan_ReadOnlySpanView()
    {
        int[] data = [5, 6, 7, 8];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);
        ReadOnlySpanView<int> slice = view.Slice(1, 2);

        ReadOnlySpan<byte> bytes = MemoryViewMarshal.GetSpan(slice);
        ReadOnlySpan<int> values = MemoryMarshal.Cast<byte, int>(bytes);

        Assert.AreEqual(2, values.Length);
        Assert.AreEqual(6, values[0]);
        Assert.AreEqual(7, values[1]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_Cast_MemoryView_IntToByte_PreservesStride()
    {
        int[] data = [0x11, 0x22, 0x33, 0x44];
        MemoryView<int> source = new Memory<int>(data);

        MemoryView<byte> cast = MemoryViewMarshal.Cast<int, byte>(source);

        Assert.AreEqual(source.Stride, cast.Stride);
        Assert.AreEqual(source.Length, cast.Length);
        Assert.AreEqual(0x11, cast.SpanView[0]);
        Assert.AreEqual(0x22, cast.SpanView[1]);
        Assert.AreEqual(0x33, cast.SpanView[2]);
        Assert.AreEqual(0x44, cast.SpanView[3]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_Cast_ReadOnlyMemoryView_IntToByte_PreservesStride()
    {
        int[] data = [0x15, 0x26, 0x37, 0x48];
        ReadOnlyMemoryView<int> source = new ReadOnlyMemory<int>(data);

        ReadOnlyMemoryView<byte> cast = MemoryViewMarshal.Cast<int, byte>(source);

        Assert.AreEqual(source.Stride, cast.Stride);
        Assert.AreEqual(source.Length, cast.Length);
        Assert.AreEqual(0x15, cast.SpanView[0]);
        Assert.AreEqual(0x26, cast.SpanView[1]);
        Assert.AreEqual(0x37, cast.SpanView[2]);
        Assert.AreEqual(0x48, cast.SpanView[3]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_Cast_SpanView_ByteToInt()
    {
        byte[] bytes =
        [
            1, 0, 0, 0,
            2, 0, 0, 0,
            3, 0, 0, 0
        ];

        SpanView<byte> source = new(bytes, offset: 0, stride: 4);

        SpanView<int> cast = MemoryViewMarshal.Cast<byte, int>(source);

        Assert.AreEqual(source.Stride, cast.Stride);
        Assert.AreEqual(3, (int)cast.Length);
        Assert.AreEqual(1, cast[0]);
        Assert.AreEqual(2, cast[1]);
        Assert.AreEqual(3, cast[2]);
    }

    [TestMethod]
    public void Test_MemoryViewMarshal_Cast_ReadOnlySpanView_ByteToInt()
    {
        byte[] bytes =
        [
            7, 0, 0, 0,
            8, 0, 0, 0,
            9, 0, 0, 0
        ];

        ReadOnlySpanView<byte> source = new(bytes, offset: 0, stride: 4);

        ReadOnlySpanView<int> cast = MemoryViewMarshal.Cast<byte, int>(source);

        Assert.AreEqual(source.Stride, cast.Stride);
        Assert.AreEqual(3, (int)cast.Length);
        Assert.AreEqual(7, cast[0]);
        Assert.AreEqual(8, cast[1]);
        Assert.AreEqual(9, cast[2]);
    }
}