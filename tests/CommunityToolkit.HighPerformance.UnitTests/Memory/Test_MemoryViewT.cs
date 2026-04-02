// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.HighPerformance.UnitTests;

// -----------------------------------------------------------------------------------------
// Helper types shared by both test classes in this file.
// -----------------------------------------------------------------------------------------

/// <summary>
/// Simple 3-float vector used in place of System.Numerics.Vector3.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file struct Vec3(float x, float y, float z)
{
    public float X = x, Y = y, Z = z;

    public override readonly string ToString() => $"({X}, {Y}, {Z})";
    public static bool operator ==(Vec3 a, Vec3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vec3 a, Vec3 b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is Vec3 v && this == v;
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
}

/// <summary>
/// Simple 2-float vector used in place of System.Numerics.Vector2.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file struct Vec2(float x, float y)
{
    public float X = x, Y = y;

    public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec2 a, Vec2 b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is Vec2 v && this == v;
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
}

/// <summary>
/// Interleaved GPU vertex structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file struct Vertex
{
    public Vec3 Position;
    public Vec3 Normal;
    public Vec2 TexCoord;
}

// -----------------------------------------------------------------------------------------
// MemoryView<T> tests
// -----------------------------------------------------------------------------------------

[TestClass]
public class Test_MemoryViewT
{
    // ===================================================================================
    // Empty / default
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_Empty()
    {
        MemoryView<int> empty1 = default;

        Assert.IsTrue(empty1.IsEmpty);
        Assert.AreEqual(0L, empty1.Length);

        MemoryView<int> empty2 = MemoryView<int>.Empty;

        Assert.IsTrue(empty2.IsEmpty);
        Assert.AreEqual(0L, empty2.Length);
    }

    // ===================================================================================
    // Constructor(Memory<byte>, int offset, int stride)
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_Constructor_ByteMemory_OffsetStride()
    {
        // 4 ints packed with 8-byte stride — only one int per stride slot
        int[] values = [11, 22, 33, 44];
        const int stride = 8;
        byte[] buffer = new byte[stride * values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            MemoryMarshal.Write(buffer.AsSpan(i * stride), values[i]);
        }

        MemoryView<int> view = new(new Memory<byte>(buffer), offset: 0, stride);

        Assert.AreEqual(values.Length, (int)view.Length);
        Assert.AreEqual(stride, view.Stride);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.AreEqual(values[i], view.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_MemoryViewT_Constructor_ByteMemory_WithOffset()
    {
        // offset = stride skips the first slot, view covers elements 1, 2, 3
        const int stride = 8;
        byte[] buffer = new byte[stride * 4];

        MemoryMarshal.Write(buffer.AsSpan(0 * stride), 100);
        MemoryMarshal.Write(buffer.AsSpan(1 * stride), 200);
        MemoryMarshal.Write(buffer.AsSpan(2 * stride), 300);
        MemoryMarshal.Write(buffer.AsSpan(3 * stride), 400);

        MemoryView<int> view = new(new Memory<byte>(buffer), offset: stride, stride);

        Assert.AreEqual(3, (int)view.Length);
        Assert.AreEqual(200, view.SpanView[0]);
        Assert.AreEqual(300, view.SpanView[1]);
        Assert.AreEqual(400, view.SpanView[2]);
    }

    [TestMethod]
    public void Test_MemoryViewT_Constructor_OffsetTooLarge_Throws()
    {
        byte[] buffer = new byte[16];
        Memory<byte> mem = buffer;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new MemoryView<int>(mem, offset: 100, stride: 4));
    }

    [TestMethod]
    public void Test_MemoryViewT_Constructor_StrideTooSmall_Throws()
    {
        byte[] buffer = new byte[16];
        Memory<byte> mem = buffer;

        // stride < sizeof(int) = 4
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new MemoryView<int>(mem, offset: 0, stride: 2));
    }

    // ===================================================================================
    // DangerousCreate<TBuffer>(Memory<TBuffer>, ref T field) — primary use case
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_DangerousCreate_Field_VertexBuffer()
    {
        Vertex[] vertices = new Vertex[5];
        Memory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, i * 2f, 0f);
            vertices[i].Normal = new Vec3(0, 1, 0);
            vertices[i].TexCoord = new Vec2(i * 0.1f, 0f);
        }

        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);
        MemoryView<Vec3> normals = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Normal);
        MemoryView<Vec2> coords = MemoryView<Vec2>.DangerousCreate(buffer, ref vertices[0].TexCoord);

        Assert.AreEqual(vertices.Length, (int)positions.Length);
        Assert.AreEqual(vertices.Length, (int)normals.Length);
        Assert.AreEqual(vertices.Length, (int)coords.Length);
        Assert.AreEqual(Unsafe.SizeOf<Vertex>(), positions.Stride);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i * 2f, 0f), positions.SpanView[i]);
            Assert.AreEqual(new Vec3(0, 1, 0), normals.SpanView[i]);
            Assert.AreEqual(new Vec2(i * 0.1f, 0f), coords.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_MemoryViewT_DangerousCreate_Field_WriteThrough()
    {
        Vertex[] vertices = new Vertex[4];
        Memory<Vertex> buffer = vertices;

        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);

        // Writes via the SpanView must be visible in the original array
        SpanView<Vec3> sv = positions.SpanView;
        for (int i = 0; i < (int)positions.Length; i++)
        {
            sv[i] = new Vec3(1, 2, 3);
        }

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(1, 2, 3), v.Position);
        }
    }

    // ===================================================================================
    // DangerousCreate<TBuffer>(Memory<TBuffer>, int offset)
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_DangerousCreate_Offset_ReadsCorrectField()
    {
        Vertex[] vertices = new Vertex[4];
        Memory<Vertex> buffer = vertices;

        int normalOffset = (int)Unsafe.ByteOffset(
            ref Unsafe.As<Vertex, byte>(ref vertices[0]),
            ref Unsafe.As<Vec3, byte>(ref vertices[0].Normal));

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = new Vec3(i * 5f, 0, 0);
        }

        MemoryView<Vec3> normals = MemoryView<Vec3>.DangerousCreate<Vertex>(buffer, normalOffset);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 5f, 0, 0), normals.SpanView[i]);
        }
    }

    // ===================================================================================
    // Implicit conversion from Memory<T>
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_ImplicitConversion_FromMemory()
    {
        int[] data = [10, 20, 30, 40];
        MemoryView<int> view = new Memory<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view.SpanView[i]);
        }
    }

    // ===================================================================================
    // IsEmpty, Length, Stride, SpanView
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_Properties()
    {
        int[] data = new int[7];
        MemoryView<int> view = new Memory<int>(data);

        Assert.IsFalse(view.IsEmpty);
        Assert.AreEqual(7L, view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);
    }

    [TestMethod]
    public void Test_MemoryViewT_SpanView_IsLiveView()
    {
        int[] data = [1, 2, 3];
        MemoryView<int> view = new Memory<int>(data);

        // Write through SpanView then read back via a fresh SpanView.
        view.SpanView[1] = 99;
        Assert.AreEqual(99, data[1]);
    }

    // ===================================================================================
    // Slice(start) / Slice(start, length)
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_Slice_Start()
    {
        int[] data = [10, 20, 30, 40, 50];
        MemoryView<int> view = new Memory<int>(data);

        MemoryView<int> slice = view.Slice(2);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(30, slice.SpanView[0]);
        Assert.AreEqual(50, slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_StartAndLength()
    {
        int[] data = [10, 20, 30, 40, 50];
        MemoryView<int> view = new Memory<int>(data);

        // Slice from index 1, length 3 → [20, 30, 40]
        MemoryView<int> slice = view.Slice(1, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(20, slice.SpanView[0]);
        Assert.AreEqual(40, slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_StartGtZero_StridedLayout()
    {
        // Verify the Slice(start, length) byteLength fix with a strided, non-zero start
        Vertex[] vertices = new Vertex[6];
        Memory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);

        // start=2, length=3 → should cover vertices[2..4].Position
        MemoryView<Vec3> slice = positions.Slice(2, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(new Vec3(2, 0, 0), slice.SpanView[0]);
        Assert.AreEqual(new Vec3(3, 0, 0), slice.SpanView[1]);
        Assert.AreEqual(new Vec3(4, 0, 0), slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_ZeroLength()
    {
        MemoryView<int> view = new Memory<int>(new int[5]);
        MemoryView<int> empty = view.Slice(0, 0);

        Assert.IsTrue(empty.IsEmpty);
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_PreservesStride()
    {
        Vertex[] vertices = new Vertex[6];
        Memory<Vertex> buffer = vertices;
        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);

        MemoryView<Vec3> slice = positions.Slice(2);

        Assert.AreEqual(positions.Stride, slice.Stride);
        Assert.AreEqual(4L, slice.Length);
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_OutOfRange_Throws()
    {
        int[] data = new int[5];
        MemoryView<int> view = new Memory<int>(data);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(0, 6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(3, 3));
    }

    [TestMethod]
    public void Test_MemoryViewT_Slice_WriteThrough()
    {
        Vertex[] vertices = new Vertex[5];
        Memory<Vertex> buffer = vertices;
        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);

        SpanView<Vec3> slice = positions.Slice(2, 2).SpanView;
        slice[0] = new Vec3(7, 8, 9);
        slice[1] = new Vec3(1, 2, 3);

        Assert.AreEqual(new Vec3(7, 8, 9), vertices[2].Position);
        Assert.AreEqual(new Vec3(1, 2, 3), vertices[3].Position);
    }

    // ===================================================================================
    // CopyTo / TryCopyTo
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_CopyTo_Succeeds()
    {
        Vertex[] src = new Vertex[4];
        Vertex[] dst = new Vertex[4];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Position = new Vec3(i, i, i);
        }

        MemoryView<Vec3> srcView = MemoryView<Vec3>.DangerousCreate<Vertex>(src, ref src[0].Position);
        MemoryView<Vec3> dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        srcView.CopyTo(dstView);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i, i), dst[i].Position);
        }
    }

    [TestMethod]
    public void Test_MemoryViewT_CopyTo_TooShort_Throws()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        MemoryView<Vec3> srcView = MemoryView<Vec3>.DangerousCreate<Vertex>(src, ref src[0].Position);
        MemoryView<Vec3> dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        Assert.ThrowsExactly<ArgumentException>(() => srcView.CopyTo(dstView));
    }

    [TestMethod]
    public void Test_MemoryViewT_TryCopyTo_Fits_ReturnsTrue()
    {
        Vertex[] src = new Vertex[3];
        Vertex[] dst = new Vertex[5];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Normal = new Vec3(0, i + 1f, 0);
        }

        MemoryView<Vec3> srcView = MemoryView<Vec3>.DangerousCreate<Vertex>(src, ref src[0].Normal);
        MemoryView<Vec3> dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Normal);

        Assert.IsTrue(srcView.TryCopyTo(dstView));

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i + 1f, 0), dst[i].Normal);
        }
    }

    [TestMethod]
    public void Test_MemoryViewT_TryCopyTo_TooShort_ReturnsFalse()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        MemoryView<Vec3> srcView = MemoryView<Vec3>.DangerousCreate<Vertex>(src, ref src[0].Position);
        MemoryView<Vec3> dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        Assert.IsFalse(srcView.TryCopyTo(dstView));

        // Verify dst was not partially written
        foreach (Vertex v in dst)
        {
            Assert.AreEqual(default(Vec3), v.Position);
        }
    }

    // ===================================================================================
    // ToArray
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_ToArray_Empty()
    {
        // Use a zero-length Memory<int> (not the default/Empty struct, which has stride=0)
        MemoryView<int> empty = new Memory<int>(Array.Empty<int>());
        Assert.AreEqual(0, empty.ToArray().Length);
    }

    [TestMethod]
    public void Test_MemoryViewT_ToArray_Strided()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 3f, 0, 0);
        }

        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate<Vertex>(vertices, ref vertices[0].Position);

        Vec3[] arr = positions.ToArray();

        Assert.AreEqual(vertices.Length, arr.Length);

        for (int i = 0; i < arr.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 3f, 0, 0), arr[i]);
        }
    }

    // ===================================================================================
    // Pin
    // ===================================================================================

    [TestMethod]
    public unsafe void Test_MemoryViewT_Pin_DoesNotThrow()
    {
        int[] data = [1, 2, 3];
        MemoryView<int> view = new Memory<int>(data);

        using MemoryHandle handle = view.Pin();

        Assert.IsTrue(handle.Pointer != null);
    }

    // ===================================================================================
    // Equals / GetHashCode
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_Equals_SameMemory_IsTrue()
    {
        int[] data = [1, 2, 3];
        MemoryView<int> a = new Memory<int>(data);
        // Struct copy — both variables hold identical Memory<byte> field values,
        // so both Equals and GetHashCode are consistent.
        MemoryView<int> b = a;

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_DifferentMemory_IsFalse()
    {
        int[] data1 = [1, 2, 3];
        int[] data2 = [1, 2, 3];

        MemoryView<int> a = new Memory<int>(data1);
        MemoryView<int> b = new Memory<int>(data2);

        Assert.IsFalse(a.Equals(b));
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_DifferentLength_IsFalse()
    {
        int[] data = [1, 2, 3, 4];
        MemoryView<int> full = new Memory<int>(data);
        MemoryView<int> slice = full.Slice(0, 2);

        Assert.IsFalse(full.Equals(slice));
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_BoxedSameMemory_IsTrue()
    {
        int[] data = [1, 2, 3];
        MemoryView<int> a = new Memory<int>(data);
        MemoryView<int> b = new Memory<int>(data);

        Assert.IsTrue(a.Equals((object)b));
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_BoxedReadOnlyMemoryView_IsTrue()
    {
        // MemoryView.Equals(object) dispatches to ReadOnlyMemoryView.Equals(this)
        // when the boxed object is a ReadOnlyMemoryView<T>.
        int[] data = [1, 2, 3];
        MemoryView<int> mutable = new Memory<int>(data);
        ReadOnlyMemoryView<int> readOnly = mutable;

        Assert.IsTrue(mutable.Equals((object)readOnly));
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_NullObject_IsFalse()
    {
        MemoryView<int> view = new Memory<int>(new int[3]);
        Assert.IsFalse(view.Equals(null));
    }

    [TestMethod]
    public void Test_MemoryViewT_Equals_UnrelatedObject_IsFalse()
    {
        MemoryView<int> view = new Memory<int>(new int[3]);
        Assert.IsFalse(view.Equals("hello"));
    }

    [TestMethod]
    public void Test_MemoryViewT_GetHashCode_EqualViewsSameHash()
    {
        int[] data = [1, 2, 3];
        MemoryView<int> a = new Memory<int>(data);
        MemoryView<int> b = a; // struct copy shares the same Memory<byte> field

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ===================================================================================
    // ToString
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_ToString_NonChar()
    {
        MemoryView<int> view = new Memory<int>(new int[7]);
        string s = view.ToString();

        StringAssert.Contains(s, "MemoryView");
        StringAssert.Contains(s, "7");
    }

    [TestMethod]
    public void Test_MemoryViewT_ToString_Char()
    {
        char[] chars = "Hello".ToCharArray();
        MemoryView<char> view = new Memory<char>(chars);

        Assert.AreEqual("Hello", view.ToString());
    }

    // ===================================================================================
    // Full vertex-buffer scenario
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_VertexBuffer_FullScenario()
    {
        const int count = 10;
        Vertex[] vertices = new Vertex[count];
        Memory<Vertex> buffer = vertices;

        for (int i = 0; i < count; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
            vertices[i].Normal = new Vec3(0, 1, 0);
            vertices[i].TexCoord = new Vec2(i * 0.1f, 0f);
        }

        MemoryView<Vec3> positions = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);
        MemoryView<Vec3> normals = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Normal);
        MemoryView<Vec2> coords = MemoryView<Vec2>.DangerousCreate(buffer, ref vertices[0].TexCoord);

        SpanView<Vec3> posView = positions.SpanView;
        SpanView<Vec3> normView = normals.SpanView;
        SpanView<Vec2> coordView = coords.SpanView;

        foreach (ref Vec3 pos in posView)
        {
            pos = new Vec3(pos.X, pos.Y + 1f, pos.Z);
        }

        foreach (ref Vec3 n in normView)
        {
            n = new Vec3(-n.X, -n.Y, -n.Z);
        }

        foreach (ref Vec2 uv in coordView)
        {
            uv = new Vec2(uv.X * 2f, uv.Y * 2f);
        }

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(new Vec3(i, 1f, 0), vertices[i].Position);
            Assert.AreEqual(new Vec3(0, -1, 0), vertices[i].Normal);
            Assert.AreEqual(new Vec2(i * 0.2f, 0f), vertices[i].TexCoord);
        }
    }
}
