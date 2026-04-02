// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.HighPerformance.UnitTests;

// -----------------------------------------------------------------------------------------
// Helper types used across all tests in this file.
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
// SpanView<T> tests
// -----------------------------------------------------------------------------------------

[TestClass]
public class Test_SpanViewT
{
    // ===================================================================================
    // Empty / default
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Empty()
    {
        // The default value is empty
        SpanView<int> empty1 = default;

        Assert.IsTrue(empty1.IsEmpty);
        Assert.AreEqual(0L, empty1.Length);

        // The static Empty property is also empty
        SpanView<int> empty2 = SpanView<int>.Empty;

        Assert.IsTrue(empty2.IsEmpty);
        Assert.AreEqual(0L, empty2.Length);
    }

    // ===================================================================================
    // DangerousCreate<TBuffer> with a ref field — the primary documented use-case
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_DangerousCreate_Field_VertexBuffer()
    {
        // Allocate a small vertex buffer
        Vertex[] vertices = new Vertex[5];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vertex
            {
                Position = new Vec3(i, i * 2f, i * 3f),
                Normal = new Vec3(0, 1, 0),
                TexCoord = new Vec2(i * 0.1f, i * 0.2f)
            };
        }

        Span<Vertex> span = vertices;

        // Create field views
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);
        SpanView<Vec3> normals   = SpanView<Vec3>.DangerousCreate(span, ref span[0].Normal);
        SpanView<Vec2> coords    = SpanView<Vec2>.DangerousCreate(span, ref span[0].TexCoord);

        Assert.AreEqual(vertices.Length, (int)positions.Length);
        Assert.AreEqual(vertices.Length, (int)normals.Length);
        Assert.AreEqual(vertices.Length, (int)coords.Length);

        // Read back via indexer and verify field isolation
        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i * 2f, i * 3f), positions[i]);
            Assert.AreEqual(new Vec3(0, 1, 0), normals[i]);
            Assert.AreEqual(new Vec2(i * 0.1f, i * 0.2f), coords[i]);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_DangerousCreate_Field_WriteThrough()
    {
        // Writes through a SpanView must be visible in the original buffer
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;

        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        foreach (ref Vec3 pos in positions)
        {
            pos = new Vec3(1, 2, 3);
        }

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(1, 2, 3), v.Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_DangerousCreate_Field_StrideSizeOf()
    {
        // The stride produced by DangerousCreate must equal sizeof(TBuffer)
        Vertex[] vertices = new Vertex[3];
        Span<Vertex> span = vertices;

        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Assert.AreEqual(Unsafe.SizeOf<Vertex>(), positions.Stride);
    }

    // ===================================================================================
    // DangerousCreate<TBuffer> with an explicit byte offset
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_DangerousCreate_Offset_ReadsCorrectField()
    {
        // Using the offset overload with the known byte offset of the Normal field
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = new Vec3(i * 10f, 0, 0);
        }

        Span<Vertex> span = vertices;
        int normalOffset = (int)Unsafe.ByteOffset(
            ref Unsafe.As<Vertex, byte>(ref span[0]),
            ref Unsafe.As<Vec3, byte>(ref span[0].Normal));

        SpanView<Vec3> normals = SpanView<Vec3>.DangerousCreate(span, normalOffset);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 10f, 0, 0), normals[i]);
        }
    }

    // ===================================================================================
    // Implicit conversion from Span<T>
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_ImplicitConversion_FromSpan()
    {
        // Implicitly converting a Span<int> into a SpanView<int> should give a contiguous view
        int[] data = [10, 20, 30, 40, 50];
        SpanView<int> view = new Span<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view[i]);
        }
    }

    // ===================================================================================
    // SpanView(Span<byte>, int offset, int stride) constructor
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Constructor_ByteSpan_OffsetStride()
    {
        // Build a flat byte buffer containing 4 ints packed with 8-byte stride.
        // Layout: [int0][pad4][int1][pad4] … (stride == 8, sizeof(int) == 4)
        int[] values = [11, 22, 33, 44];
        const int stride = 8;
        byte[] buffer = new byte[stride * values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            MemoryMarshal.Write(buffer.AsSpan(i * stride), values[i]);
        }

        SpanView<int> view = new(new Span<byte>(buffer), offset: 0, stride);

        Assert.AreEqual(values.Length, (int)view.Length);
        Assert.AreEqual(stride, view.Stride);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.AreEqual(values[i], view[i]);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_Constructor_ByteSpan_WithOffset()
    {
        // Offset skips the first element, so the view starts at element 1
        const int stride = 8;
        byte[] buffer = new byte[stride * 4];

        MemoryMarshal.Write(buffer.AsSpan(0 * stride), 100);
        MemoryMarshal.Write(buffer.AsSpan(1 * stride), 200);
        MemoryMarshal.Write(buffer.AsSpan(2 * stride), 300);
        MemoryMarshal.Write(buffer.AsSpan(3 * stride), 400);

        // offset = stride skips the first slot → view covers elements 1,2,3
        SpanView<int> view = new(new Span<byte>(buffer), offset: stride, stride);

        Assert.AreEqual(3, (int)view.Length);
        Assert.AreEqual(200, view[0]);
        Assert.AreEqual(300, view[1]);
        Assert.AreEqual(400, view[2]);
    }

    [TestMethod]
    public void Test_SpanViewT_Constructor_ByteSpan_StrideTooSmall_Throws()
    {
        byte[] buffer = new byte[16];

        // stride < sizeof(int) = 4 should throw
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => _ = new SpanView<int>(new Span<byte>(buffer), offset: 0, stride: 2));
    }

    // ===================================================================================
    // IsEmpty, Length, Stride
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Properties()
    {
        int[] data = new int[10];
        SpanView<int> view = SpanView<int>.DangerousCreate(new Span<int>(data), offset: 0);

        Assert.IsFalse(view.IsEmpty);
        Assert.AreEqual(10L, view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);
    }

    // ===================================================================================
    // Indexer
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Indexer_GetSet()
    {
        int[] data = [1, 2, 3, 4, 5];
        SpanView<int> view = new Span<int>(data);

        // Read
        Assert.AreEqual(3, view[2]);

        // Write through ref
        view[2] = 99;
        Assert.AreEqual(99, data[2]);
    }

    [TestMethod]
    public void Test_SpanViewT_Indexer_NegativeIndex_Throws()
    {
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = ((SpanView<int>)new Span<int>(new int[5]))[-1]);
    }

    [TestMethod]
    public void Test_SpanViewT_Indexer_IndexEqualLength_Throws()
    {
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = ((SpanView<int>)new Span<int>(new int[5]))[5]);
    }

    // ===================================================================================
    // Slice
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Slice_Start()
    {
        int[] data = [10, 20, 30, 40, 50];
        SpanView<int> view = new Span<int>(data);

        // Slice from index 2 → [30, 40, 50]
        SpanView<int> slice = view.Slice(2);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(30, slice[0]);
        Assert.AreEqual(50, slice[2]);
    }

    [TestMethod]
    public void Test_SpanViewT_Slice_StartAndLength()
    {
        int[] data = [10, 20, 30, 40, 50];
        SpanView<int> view = new Span<int>(data);

        // Slice from index 1, length 3 → [20, 30, 40]
        SpanView<int> slice = view.Slice(1, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(20, slice[0]);
        Assert.AreEqual(40, slice[2]);
    }

    [TestMethod]
    public void Test_SpanViewT_Slice_ZeroStart_ZeroLength()
    {
        SpanView<int> view = new Span<int>(new int[5]);

        SpanView<int> empty = view.Slice(0, 0);

        Assert.IsTrue(empty.IsEmpty);
    }

    [TestMethod]
    public void Test_SpanViewT_Slice_PreservesStride()
    {
        // Slicing a strided view must keep the stride intact
        Vertex[] vertices = new Vertex[6];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        SpanView<Vec3> slice = positions.Slice(2);

        Assert.AreEqual(positions.Stride, slice.Stride);
        Assert.AreEqual(4L, slice.Length);
    }

    [TestMethod]
    public void Test_SpanViewT_Slice_OutOfRange_Throws()
    {
        static SpanView<int> MakeView() => new Span<int>(new int[5]);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView().Slice(-1));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView().Slice(6));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView().Slice(0, 6));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView().Slice(3, 3));
    }

    [TestMethod]
    public void Test_SpanViewT_Slice_WriteThrough()
    {
        // Writes to a slice must be visible in the original buffer
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        SpanView<Vec3> slice = positions.Slice(2, 2);
        slice[0] = new Vec3(7, 8, 9);
        slice[1] = new Vec3(1, 2, 3);

        Assert.AreEqual(new Vec3(7, 8, 9), vertices[2].Position);
        Assert.AreEqual(new Vec3(1, 2, 3), vertices[3].Position);
    }

    // ===================================================================================
    // Clear
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Clear_Strided()
    {
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i + 1f, 0, 0);
        }

        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);
        positions.Clear();

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(0, 0, 0), v.Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_Clear_DoesNotTouchOtherFields()
    {
        Vertex[] vertices = new Vertex[3];
        Span<Vertex> span = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(1, 2, 3);
            vertices[i].Normal   = new Vec3(0, 1, 0);
        }

        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);
        positions.Clear();

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(0, 0, 0), v.Position);
            Assert.AreEqual(new Vec3(0, 1, 0), v.Normal);
        }
    }

    // ===================================================================================
    // Fill
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Fill_Strided()
    {
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;
        SpanView<Vec3> normals = SpanView<Vec3>.DangerousCreate(span, ref span[0].Normal);

        normals.Fill(new Vec3(0, 1, 0));

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(0, 1, 0), v.Normal);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_Fill_DoesNotTouchOtherFields()
    {
        Vertex[] vertices = new Vertex[3];
        Span<Vertex> span = vertices;
        SpanView<Vec3> normals = SpanView<Vec3>.DangerousCreate(span, ref span[0].Normal);

        normals.Fill(new Vec3(0, 0, 1));

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(default(Vec3), v.Position);
            Assert.AreEqual(new Vec3(0, 0, 1), v.Normal);
        }
    }

    // ===================================================================================
    // CopyFrom(ReadOnlySpan<T>) / TryCopyFrom(ReadOnlySpan<T>)
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_CopyFrom_Span_Succeeds()
    {
        Vertex[] vertices = new Vertex[4];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Vec3[] src = [new(1, 0, 0), new(2, 0, 0), new(3, 0, 0), new(4, 0, 0)];
        positions.CopyFrom(src);

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(src[i], vertices[i].Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_CopyFrom_Span_SourceTooLarge_Throws()
    {
        Vertex[] vertices = new Vertex[3];
        Span<Vertex> span = vertices;

        // Capture the array (not the ref struct) in the lambda
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SpanView<Vec3> p = SpanView<Vec3>.DangerousCreate(vertices, ref vertices[0].Position);
            p.CopyFrom(new Vec3[5]);
        });
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyFrom_Span_Fits_ReturnsTrue()
    {
        Vertex[] vertices = new Vertex[4];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Vec3[] src = [new(1, 0, 0), new(2, 0, 0)];
        Assert.IsTrue(positions.TryCopyFrom(new ReadOnlySpan<Vec3>(src)));

        Assert.AreEqual(src[0], vertices[0].Position);
        Assert.AreEqual(src[1], vertices[1].Position);
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyFrom_Span_TooLarge_ReturnsFalse()
    {
        Vertex[] vertices = new Vertex[2];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Assert.IsFalse(positions.TryCopyFrom(new Vec3[5]));
    }

    // ===================================================================================
    // CopyTo(Span<T>) / TryCopyTo(Span<T>)
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_CopyTo_Span_Succeeds()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Vec3[] dst = new Vec3[4];
        positions.CopyTo(dst);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, 0, 0), dst[i]);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_CopyTo_Span_TooShort_Throws()
    {
        Vertex[] vertices = new Vertex[4];

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SpanView<Vec3> p = SpanView<Vec3>.DangerousCreate(vertices, ref vertices[0].Position);
            p.CopyTo(new Vec3[2]);
        });
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyTo_Span_Fits_ReturnsTrue()
    {
        Vertex[] vertices = new Vertex[3];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 5f, 0, 0);
        }

        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Vec3[] dst = new Vec3[5];
        Assert.IsTrue(positions.TryCopyTo(new Span<Vec3>(dst)));

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 5f, 0, 0), dst[i]);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyTo_Span_TooShort_ReturnsFalse()
    {
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Assert.IsFalse(positions.TryCopyTo(new Vec3[2]));
    }

    // ===================================================================================
    // CopyTo(SpanView<T>) / TryCopyTo(SpanView<T>) and the view-to-view overloads
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_CopyTo_SpanView_Succeeds()
    {
        Vertex[] src  = new Vertex[4];
        Vertex[] dst  = new Vertex[4];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Position = new Vec3(i, i, i);
        }

        SpanView<Vec3> srcView = SpanView<Vec3>.DangerousCreate(src, ref src[0].Position);
        SpanView<Vec3> dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        srcView.CopyTo(dstView);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i, i), dst[i].Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_CopyTo_SpanView_TooShort_Throws()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SpanView<Vec3> s = SpanView<Vec3>.DangerousCreate(src, ref src[0].Position);
            SpanView<Vec3> d = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);
            s.CopyTo(d);
        });
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyTo_SpanView_Fits_ReturnsTrue()
    {
        Vertex[] src = new Vertex[3];
        Vertex[] dst = new Vertex[5];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Normal = new Vec3(0, i, 0);
        }

        SpanView<Vec3> srcView = SpanView<Vec3>.DangerousCreate(src, ref src[0].Normal);
        SpanView<Vec3> dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Normal);

        Assert.IsTrue(srcView.TryCopyTo(dstView));

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i, 0), dst[i].Normal);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyTo_SpanView_TooShort_ReturnsFalse()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        SpanView<Vec3> srcView = SpanView<Vec3>.DangerousCreate(src, ref src[0].Position);
        SpanView<Vec3> dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        Assert.IsFalse(srcView.TryCopyTo(dstView));
    }

    [TestMethod]
    public void Test_SpanViewT_CopyFrom_SpanView_Succeeds()
    {
        // CopyFrom(ReadOnlySpanView<T>) routes through CopyTo; verify it works end-to-end
        Vertex[] src = new Vertex[4];
        Vertex[] dst = new Vertex[4];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Position = new Vec3(i * 3f, 0, 0);
        }

        ReadOnlySpanView<Vec3> srcView = SpanView<Vec3>.DangerousCreate(src, ref src[0].Position);
        SpanView<Vec3>         dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        dstView.CopyFrom(srcView);

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 3f, 0, 0), dst[i].Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_TryCopyFrom_SpanView_TooLarge_ReturnsFalse()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        ReadOnlySpanView<Vec3> srcView = SpanView<Vec3>.DangerousCreate(src, ref src[0].Position);
        SpanView<Vec3>         dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        Assert.IsFalse(dstView.TryCopyFrom(srcView));
    }

    // ===================================================================================
    // DangerousGetReference / DangerousGetReferenceAt
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_DangerousGetReference()
    {
        int[] data = [10, 20, 30];
        SpanView<int> view = new Span<int>(data);

        ref int r = ref view.DangerousGetReference();
        Assert.AreEqual(10, r);

        r = 99;
        Assert.AreEqual(99, data[0]);
    }

    [TestMethod]
    public void Test_SpanViewT_DangerousGetReferenceAt()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, 0, 0), positions.DangerousGetReferenceAt(i));
        }
    }

    // ===================================================================================
    // GetPinnableReference
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_GetPinnableReference_NonEmpty_IsNotNull()
    {
        int[] data = [1, 2, 3];
        SpanView<int> view = new Span<int>(data);

        Assert.IsFalse(Unsafe.IsNullRef(ref view.GetPinnableReference()));
    }

    [TestMethod]
    public void Test_SpanViewT_GetPinnableReference_Empty_IsNull()
    {
        SpanView<int> empty = default;

        Assert.IsTrue(Unsafe.IsNullRef(ref empty.GetPinnableReference()));
    }

    // ===================================================================================
    // Equals / == / != operators
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Equals_SameMemory_SameStride_IsTrue()
    {
        int[] data = [1, 2, 3];
        SpanView<int> a = new Span<int>(data);
        SpanView<int> b = new Span<int>(data);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
    }

    [TestMethod]
    public void Test_SpanViewT_Equals_DifferentMemory_IsFalse()
    {
        int[] a = [1, 2, 3];
        int[] b = [1, 2, 3];

        SpanView<int> va = new Span<int>(a);
        SpanView<int> vb = new Span<int>(b);

        Assert.IsFalse(va.Equals(vb));
        Assert.IsFalse(va == vb);
        Assert.IsTrue(va != vb);
    }

    [TestMethod]
    public void Test_SpanViewT_Equals_DifferentLength_IsFalse()
    {
        int[] data = [1, 2, 3, 4];

        SpanView<int> full  = new Span<int>(data);
        SpanView<int> slice = ((SpanView<int>)new Span<int>(data)).Slice(0, 2);

        Assert.IsFalse(full.Equals(slice));
    }

#pragma warning disable CS0618
    [TestMethod]
    public void Test_SpanViewT_Equals_Object_Throws()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            SpanView<int> v = new Span<int>(new int[3]);
            _ = v.Equals(new object());
        });
    }

    [TestMethod]
    public void Test_SpanViewT_GetHashCode_Throws()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            SpanView<int> v = new Span<int>(new int[3]);
            _ = v.GetHashCode();
        });
    }
#pragma warning restore CS0618

    // ===================================================================================
    // ToString
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_ToString_NonChar_ContainsTypeAndLength()
    {
        SpanView<int> view = new Span<int>(new int[7]);
        string s = view.ToString();

        StringAssert.Contains(s, "SpanView");
        StringAssert.Contains(s, "7");
    }

    [TestMethod]
    public void Test_SpanViewT_ToString_Char_ReturnsString()
    {
        // For SpanView<char> ToString should return the actual character sequence
        char[] chars = "Hello".ToCharArray();
        SpanView<char> view = new Span<char>(chars);

        Assert.AreEqual("Hello", view.ToString());
    }

    // ===================================================================================
    // ToArray
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_ToArray_Empty_ReturnsEmptyArray()
    {
        SpanView<int> empty = SpanView<int>.Empty;

        Assert.AreEqual(0, empty.ToArray().Length);
    }

    [TestMethod]
    public void Test_SpanViewT_ToArray_Strided()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        Vec3[] arr = positions.ToArray();

        Assert.AreEqual(vertices.Length, arr.Length);

        for (int i = 0; i < arr.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, 0, 0), arr[i]);
        }
    }

    // ===================================================================================
    // GetEnumerator / foreach
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_Foreach_ReadsAllElements()
    {
        Vertex[] vertices = new Vertex[5];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        int index = 0;
        foreach (ref readonly Vec3 pos in positions)
        {
            Assert.AreEqual(new Vec3(index, 0, 0), pos);
            index++;
        }

        Assert.AreEqual(vertices.Length, index);
    }

    [TestMethod]
    public void Test_SpanViewT_Foreach_WriteThrough()
    {
        // Foreach over SpanView<T> exposes ref T, so mutations must stick
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;
        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);

        foreach (ref Vec3 pos in positions)
        {
            pos = new Vec3(1, 1, 1);
        }

        foreach (Vertex v in vertices)
        {
            Assert.AreEqual(new Vec3(1, 1, 1), v.Position);
        }
    }

    [TestMethod]
    public void Test_SpanViewT_Foreach_Empty_NeverEnters()
    {
        SpanView<int> empty = SpanView<int>.Empty;
        int count = 0;

        foreach (ref readonly int _ in empty)
        {
            count++;
        }

        Assert.AreEqual(0, count);
    }

    // ===================================================================================
    // Full vertex-buffer scenario (as described in the type's documentation)
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_VertexBuffer_FullScenario()
    {
        const int count = 10;
        Vertex[] vertices = new Vertex[count];
        Span<Vertex> span = vertices;

        // Initialize
        for (int i = 0; i < count; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
            vertices[i].Normal   = new Vec3(0, 1, 0);
            vertices[i].TexCoord = new Vec2(i * 0.1f, 0f);
        }

        SpanView<Vec3> positions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);
        SpanView<Vec3> normals   = SpanView<Vec3>.DangerousCreate(span, ref span[0].Normal);
        SpanView<Vec2> coords    = SpanView<Vec2>.DangerousCreate(span, ref span[0].TexCoord);

        // Translate all positions by (0, 1, 0)
        foreach (ref Vec3 pos in positions)
        {
            pos = new Vec3(pos.X, pos.Y + 1f, pos.Z);
        }

        // Flip all normals
        foreach (ref Vec3 n in normals)
        {
            n = new Vec3(-n.X, -n.Y, -n.Z);
        }

        // Scale all UV coordinates by 2
        foreach (ref Vec2 uv in coords)
        {
            uv = new Vec2(uv.X * 2f, uv.Y * 2f);
        }

        // Verify — all modifications must be visible in the original array
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(new Vec3(i, 1f, 0), vertices[i].Position);
            Assert.AreEqual(new Vec3(0, -1, 0),  vertices[i].Normal);
            Assert.AreEqual(new Vec2(i * 0.2f, 0f), vertices[i].TexCoord);
        }
    }
}
