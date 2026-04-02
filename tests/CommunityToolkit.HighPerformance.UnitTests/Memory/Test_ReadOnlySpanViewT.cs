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
// ReadOnlySpanView<T> tests
// -----------------------------------------------------------------------------------------

[TestClass]
public class Test_ReadOnlySpanViewT
{
    // ===================================================================================
    // Empty / default
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Empty()
    {
        ReadOnlySpanView<int> empty1 = default;

        Assert.IsTrue(empty1.IsEmpty);
        Assert.AreEqual(0L, empty1.Length);

        ReadOnlySpanView<int> empty2 = ReadOnlySpanView<int>.Empty;

        Assert.IsTrue(empty2.IsEmpty);
        Assert.AreEqual(0L, empty2.Length);
    }

    // ===================================================================================
    // DangerousCreate — field ref and offset overloads
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_DangerousCreate_Field_ReadsCorrectly()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, i * 2f, 0);
        }

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        Assert.AreEqual(vertices.Length, (int)positions.Length);
        Assert.AreEqual(Unsafe.SizeOf<Vertex>(), positions.Stride);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i * 2f, 0), positions[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_DangerousCreate_Offset_ReadsCorrectly()
    {
        Vertex[] vertices = new Vertex[3];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = new Vec3(0, i + 1f, 0);
        }

        int normalOffset = (int)Unsafe.ByteOffset(
            ref Unsafe.As<Vertex, byte>(ref vertices[0]),
            ref Unsafe.As<Vec3, byte>(ref vertices[0].Normal));

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> normals = ReadOnlySpanView<Vec3>.DangerousCreate(span, normalOffset);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i + 1f, 0), normals[i]);
        }
    }

    // ===================================================================================
    // Implicit conversions
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ImplicitConversion_FromSpanView()
    {
        // SpanView<T> → ReadOnlySpanView<T> must preserve length, stride and values
        int[] data = [1, 2, 3, 4];
        SpanView<int> mutable = new Span<int>(data);
        ReadOnlySpanView<int> readOnly = mutable;

        Assert.AreEqual(mutable.Length, readOnly.Length);
        Assert.AreEqual(mutable.Stride, readOnly.Stride);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], readOnly[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ImplicitConversion_FromReadOnlySpan()
    {
        int[] data = [10, 20, 30];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ImplicitConversion_FromSpan()
    {
        int[] data = [5, 10, 15];
        ReadOnlySpanView<int> view = new Span<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view[i]);
        }
    }

    // ===================================================================================
    // Indexer (read-only)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Indexer_Get()
    {
        int[] data = [11, 22, 33];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        Assert.AreEqual(11, view[0]);
        Assert.AreEqual(22, view[1]);
        Assert.AreEqual(33, view[2]);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Indexer_NegativeIndex_Throws()
    {
        int[] data = new int[3];
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = ((ReadOnlySpanView<int>)new ReadOnlySpan<int>(data))[-1]);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Indexer_IndexEqualLength_Throws()
    {
        int[] data = new int[3];
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = ((ReadOnlySpanView<int>)new ReadOnlySpan<int>(data))[3]);
    }

    // ===================================================================================
    // Slice
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Slice_Start()
    {
        int[] data = [10, 20, 30, 40, 50];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        ReadOnlySpanView<int> slice = view.Slice(2);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(30, slice[0]);
        Assert.AreEqual(50, slice[2]);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Slice_StartAndLength()
    {
        int[] data = [10, 20, 30, 40, 50];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        ReadOnlySpanView<int> slice = view.Slice(1, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(20, slice[0]);
        Assert.AreEqual(40, slice[2]);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Slice_OutOfRange_Throws()
    {
        int[] data = new int[5];
        static ReadOnlySpanView<int> MakeView(int[] d) => new ReadOnlySpan<int>(d);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView(data).Slice(-1));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView(data).Slice(6));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView(data).Slice(0, 6));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = MakeView(data).Slice(3, 3));
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Slice_PreservesStride()
    {
        Vertex[] vertices = new Vertex[6];
        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        ReadOnlySpanView<Vec3> slice = positions.Slice(2);

        Assert.AreEqual(positions.Stride, slice.Stride);
        Assert.AreEqual(4L, slice.Length);
    }

    // ===================================================================================
    // CopyTo(SpanView<T>) / TryCopyTo(SpanView<T>)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_CopyTo_SpanView_Succeeds()
    {
        Vertex[] src = new Vertex[4];
        Vertex[] dst = new Vertex[4];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Position = new Vec3(i * 7f, 0, 0);
        }

        ReadOnlySpanView<Vec3> srcView = ReadOnlySpanView<Vec3>.DangerousCreate(src, in src[0].Position);
        SpanView<Vec3>         dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        srcView.CopyTo(dstView);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 7f, 0, 0), dst[i].Position);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_CopyTo_SpanView_TooShort_Throws()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpanView<Vec3> s = ReadOnlySpanView<Vec3>.DangerousCreate(src, in src[0].Position);
            SpanView<Vec3>         d = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);
            s.CopyTo(d);
        });
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_TryCopyTo_SpanView_Fits_ReturnsTrue()
    {
        Vertex[] src = new Vertex[3];
        Vertex[] dst = new Vertex[5];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Normal = new Vec3(0, i + 1f, 0);
        }

        ReadOnlySpanView<Vec3> srcView = ReadOnlySpanView<Vec3>.DangerousCreate(src, in src[0].Normal);
        SpanView<Vec3>         dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Normal);

        Assert.IsTrue(srcView.TryCopyTo(dstView));

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i + 1f, 0), dst[i].Normal);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_TryCopyTo_SpanView_TooShort_ReturnsFalse()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        ReadOnlySpanView<Vec3> srcView = ReadOnlySpanView<Vec3>.DangerousCreate(src, in src[0].Position);
        SpanView<Vec3>         dstView = SpanView<Vec3>.DangerousCreate(dst, ref dst[0].Position);

        Assert.IsFalse(srcView.TryCopyTo(dstView));
    }

    // ===================================================================================
    // CopyTo(Span<T>) / TryCopyTo(Span<T>)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_CopyTo_Span_Succeeds()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 2f, 0, 0);
        }

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        Vec3[] dst = new Vec3[4];
        positions.CopyTo(dst);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 2f, 0, 0), dst[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_CopyTo_Span_TooShort_Throws()
    {
        int[] data = new int[5];

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpanView<int> v = new ReadOnlySpan<int>(data);
            v.CopyTo(new int[2]);
        });
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_TryCopyTo_Span_Fits_ReturnsTrue()
    {
        int[] src = [1, 2, 3];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(src);

        int[] dst = new int[5];
        Assert.IsTrue(view.TryCopyTo(new Span<int>(dst)));
        Assert.AreEqual(1, dst[0]);
        Assert.AreEqual(3, dst[2]);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_TryCopyTo_Span_TooShort_ReturnsFalse()
    {
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(new int[5]);

        Assert.IsFalse(view.TryCopyTo(new Span<int>(new int[2])));
    }

    // ===================================================================================
    // DangerousGetReference / DangerousGetReferenceAt
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_DangerousGetReference_ReturnsFirstElement()
    {
        int[] data = [42, 84, 126];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        Assert.AreEqual(42, view.DangerousGetReference());
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_DangerousGetReferenceAt_ReturnsCorrectElements()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 5f, 0, 0);
        }

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 5f, 0, 0), positions.DangerousGetReferenceAt(i));
        }
    }

    // ===================================================================================
    // GetPinnableReference
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_GetPinnableReference_NonEmpty_IsNotNull()
    {
        int[] data = [1, 2, 3];
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(data);

        Assert.IsFalse(Unsafe.IsNullRef(ref Unsafe.AsRef(in view.GetPinnableReference())));
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_GetPinnableReference_Empty_IsNull()
    {
        ReadOnlySpanView<int> empty = default;

        Assert.IsTrue(Unsafe.IsNullRef(ref Unsafe.AsRef(in empty.GetPinnableReference())));
    }

    // ===================================================================================
    // Equals / == / != operators
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Equals_SameMemory_IsTrue()
    {
        int[] data = [1, 2, 3];
        ReadOnlySpanView<int> a = new ReadOnlySpan<int>(data);
        ReadOnlySpanView<int> b = new ReadOnlySpan<int>(data);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Equals_DifferentMemory_IsFalse()
    {
        int[] data1 = [1, 2, 3];
        int[] data2 = [1, 2, 3];

        ReadOnlySpanView<int> a = new ReadOnlySpan<int>(data1);
        ReadOnlySpanView<int> b = new ReadOnlySpan<int>(data2);

        Assert.IsFalse(a.Equals(b));
        Assert.IsTrue(a != b);
    }

#pragma warning disable CS0618
    [TestMethod]
    public void Test_ReadOnlySpanViewT_Equals_Object_Throws()
    {
        int[] data = new int[3];

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            ReadOnlySpanView<int> v = new ReadOnlySpan<int>(data);
            _ = v.Equals(new object());
        });
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_GetHashCode_Throws()
    {
        int[] data = new int[3];

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            ReadOnlySpanView<int> v = new ReadOnlySpan<int>(data);
            _ = v.GetHashCode();
        });
    }
#pragma warning restore CS0618

    // ===================================================================================
    // ToString
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ToString_NonChar()
    {
        ReadOnlySpanView<int> view = new ReadOnlySpan<int>(new int[5]);
        string s = view.ToString();

        StringAssert.Contains(s, "ReadOnlySpanView");
        StringAssert.Contains(s, "5");
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ToString_Char()
    {
        ReadOnlySpanView<char> view = new ReadOnlySpan<char>("World".ToCharArray());

        Assert.AreEqual("World", view.ToString());
    }

    // ===================================================================================
    // ToArray
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ToArray_Empty()
    {
        Assert.AreEqual(0, ReadOnlySpanView<int>.Empty.ToArray().Length);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_ToArray_Strided()
    {
        Vertex[] vertices = new Vertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 3f, 0, 0);
        }

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        Vec3[] arr = positions.ToArray();

        Assert.AreEqual(vertices.Length, arr.Length);

        for (int i = 0; i < arr.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 3f, 0, 0), arr[i]);
        }
    }

    // ===================================================================================
    // GetEnumerator / foreach
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Foreach_ReadsAllElements()
    {
        Vertex[] vertices = new Vertex[5];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 2f, 0, 0);
        }

        ReadOnlySpan<Vertex> span = vertices;
        ReadOnlySpanView<Vec3> positions = ReadOnlySpanView<Vec3>.DangerousCreate(span, in span[0].Position);

        int index = 0;
        foreach (ref readonly Vec3 pos in positions)
        {
            Assert.AreEqual(new Vec3(index * 2f, 0, 0), pos);
            index++;
        }

        Assert.AreEqual(vertices.Length, index);
    }

    [TestMethod]
    public void Test_ReadOnlySpanViewT_Foreach_Empty_NeverEnters()
    {
        ReadOnlySpanView<int> empty = ReadOnlySpanView<int>.Empty;
        int count = 0;

        foreach (ref readonly int _ in empty)
        {
            count++;
        }

        Assert.AreEqual(0, count);
    }

    // ===================================================================================
    // SpanView/ReadOnlySpanView interoperability
    // ===================================================================================

    [TestMethod]
    public void Test_SpanViewT_AsReadOnlySpanView_ViaCopyTo()
    {
        // SpanView<T>.CopyTo(ReadOnlySpanView<T> source) is bridged via CopyFrom.
        // Verify the full round-trip: write with SpanView, read via ReadOnlySpanView.
        Vertex[] vertices = new Vertex[5];
        Span<Vertex> span = vertices;

        SpanView<Vec3>         mutablePositions = SpanView<Vec3>.DangerousCreate(span, ref span[0].Position);
        ReadOnlySpanView<Vec3> readOnlyPositions = mutablePositions;

        Vec3[] src = [new(1,0,0), new(2,0,0), new(3,0,0), new(4,0,0), new(5,0,0)];
        mutablePositions.CopyFrom(src);

        Vec3[] dst = new Vec3[5];
        readOnlyPositions.CopyTo(dst);

        CollectionAssert.AreEqual(src, dst);
    }
}
