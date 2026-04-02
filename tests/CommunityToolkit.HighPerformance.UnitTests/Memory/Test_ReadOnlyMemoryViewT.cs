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
// ReadOnlyMemoryView<T> tests
// -----------------------------------------------------------------------------------------

[TestClass]
public class Test_ReadOnlyMemoryViewT
{
    // ===================================================================================
    // Empty / default
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Empty()
    {
        ReadOnlyMemoryView<int> empty1 = default;

        Assert.IsTrue(empty1.IsEmpty);
        Assert.AreEqual(0L, empty1.Length);

        ReadOnlyMemoryView<int> empty2 = ReadOnlyMemoryView<int>.Empty;

        Assert.IsTrue(empty2.IsEmpty);
        Assert.AreEqual(0L, empty2.Length);
    }

    // ===================================================================================
    // Constructor(ReadOnlyMemory<byte>, int offset, int stride)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Constructor_ByteMemory_OffsetStride()
    {
        int[] values = [11, 22, 33, 44];
        const int stride = 8;
        byte[] buffer = new byte[stride * values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            MemoryMarshal.Write(buffer.AsSpan(i * stride), values[i]);
        }

        ReadOnlyMemoryView<int> view = new(new ReadOnlyMemory<byte>(buffer), offset: 0, stride);

        Assert.AreEqual(values.Length, (int)view.Length);
        Assert.AreEqual(stride, view.Stride);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.AreEqual(values[i], view.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Constructor_OffsetTooLarge_Throws()
    {
        byte[] buffer = new byte[16];
        ReadOnlyMemory<byte> mem = buffer;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ReadOnlyMemoryView<int>(mem, offset: 100, stride: 4));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Constructor_StrideTooSmall_Throws()
    {
        byte[] buffer = new byte[16];
        ReadOnlyMemory<byte> mem = buffer;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ReadOnlyMemoryView<int>(mem, offset: 0, stride: 2));
    }

    // ===================================================================================
    // DangerousCreate — field ref and offset overloads
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_DangerousCreate_Field_ReadsCorrectly()
    {
        Vertex[] vertices = new Vertex[4];
        ReadOnlyMemory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, i * 2f, 0);
        }

        ReadOnlyMemoryView<Vec3> positions = ReadOnlyMemoryView<Vec3>.DangerousCreate(buffer, in vertices[0].Position);

        Assert.AreEqual(vertices.Length, (int)positions.Length);
        Assert.AreEqual(Unsafe.SizeOf<Vertex>(), positions.Stride);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, i * 2f, 0), positions.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_DangerousCreate_Offset_ReadsCorrectly()
    {
        Vertex[] vertices = new Vertex[3];
        ReadOnlyMemory<Vertex> buffer = vertices;

        int normalOffset = (int)Unsafe.ByteOffset(
            ref Unsafe.As<Vertex, byte>(ref vertices[0]),
            ref Unsafe.As<Vec3, byte>(ref vertices[0].Normal));

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = new Vec3(0, i + 1f, 0);
        }

        ReadOnlyMemoryView<Vec3> normals = ReadOnlyMemoryView<Vec3>.DangerousCreate<Vertex>(buffer, normalOffset);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i + 1f, 0), normals.SpanView[i]);
        }
    }

    // ===================================================================================
    // Implicit conversions
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ImplicitConversion_FromReadOnlyMemory()
    {
        int[] data = [5, 10, 15];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ImplicitConversion_FromMemory()
    {
        int[] data = [1, 2, 3];
        ReadOnlyMemoryView<int> view = new Memory<int>(data);

        Assert.AreEqual(data.Length, (int)view.Length);

        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(data[i], view.SpanView[i]);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ImplicitConversion_FromMemoryView()
    {
        Vertex[] vertices = new Vertex[4];
        Memory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 3f, 0, 0);
        }

        MemoryView<Vec3>         mutable  = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);
        ReadOnlyMemoryView<Vec3> readOnly = mutable;

        Assert.AreEqual(mutable.Length, readOnly.Length);
        Assert.AreEqual(mutable.Stride, readOnly.Stride);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 3f, 0, 0), readOnly.SpanView[i]);
        }
    }

    // ===================================================================================
    // IsEmpty, Length, Stride, SpanView
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Properties()
    {
        int[] data = new int[5];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        Assert.IsFalse(view.IsEmpty);
        Assert.AreEqual(5L, view.Length);
        Assert.AreEqual(Unsafe.SizeOf<int>(), view.Stride);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_SpanView_ReflectsChangesToUnderlyingArray()
    {
        // ReadOnlyMemory wraps the same array; changes are visible through SpanView
        int[] data = [1, 2, 3];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        data[1] = 99;

        Assert.AreEqual(99, view.SpanView[1]);
    }

    // ===================================================================================
    // Slice(start) / Slice(start, length)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_Start()
    {
        int[] data = [10, 20, 30, 40, 50];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        ReadOnlyMemoryView<int> slice = view.Slice(2);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(30, slice.SpanView[0]);
        Assert.AreEqual(50, slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_StartAndLength()
    {
        int[] data = [10, 20, 30, 40, 50];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        ReadOnlyMemoryView<int> slice = view.Slice(1, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(20, slice.SpanView[0]);
        Assert.AreEqual(40, slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_StartGtZero_StridedLayout()
    {
        Vertex[] vertices = new Vertex[6];
        ReadOnlyMemory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        ReadOnlyMemoryView<Vec3> positions = ReadOnlyMemoryView<Vec3>.DangerousCreate(buffer, in vertices[0].Position);

        ReadOnlyMemoryView<Vec3> slice = positions.Slice(2, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(new Vec3(2, 0, 0), slice.SpanView[0]);
        Assert.AreEqual(new Vec3(4, 0, 0), slice.SpanView[2]);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_ZeroLength()
    {
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(new int[5]);
        ReadOnlyMemoryView<int> empty = view.Slice(0, 0);

        Assert.IsTrue(empty.IsEmpty);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_PreservesStride()
    {
        Vertex[] vertices = new Vertex[6];
        ReadOnlyMemory<Vertex> buffer = vertices;
        ReadOnlyMemoryView<Vec3> positions = ReadOnlyMemoryView<Vec3>.DangerousCreate(buffer, in vertices[0].Position);

        ReadOnlyMemoryView<Vec3> slice = positions.Slice(2);

        Assert.AreEqual(positions.Stride, slice.Stride);
        Assert.AreEqual(4L, slice.Length);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Slice_OutOfRange_Throws()
    {
        int[] data = new int[5];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(0, 6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Slice(3, 3));
    }

    // ===================================================================================
    // CopyTo / TryCopyTo (copy into MemoryView<T>)
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_CopyTo_Succeeds()
    {
        Vertex[] src = new Vertex[4];
        Vertex[] dst = new Vertex[4];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Position = new Vec3(i * 7f, 0, 0);
        }

        ReadOnlyMemoryView<Vec3> srcView = ReadOnlyMemoryView<Vec3>.DangerousCreate<Vertex>(src, in src[0].Position);
        MemoryView<Vec3>         dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        srcView.CopyTo(dstView);

        for (int i = 0; i < dst.Length; i++)
        {
            Assert.AreEqual(new Vec3(i * 7f, 0, 0), dst[i].Position);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_CopyTo_TooShort_Throws()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        ReadOnlyMemoryView<Vec3> srcView = ReadOnlyMemoryView<Vec3>.DangerousCreate<Vertex>(src, in src[0].Position);
        MemoryView<Vec3>         dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        Assert.ThrowsExactly<ArgumentException>(() => srcView.CopyTo(dstView));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_TryCopyTo_Fits_ReturnsTrue()
    {
        Vertex[] src = new Vertex[3];
        Vertex[] dst = new Vertex[5];

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Normal = new Vec3(0, i + 1f, 0);
        }

        ReadOnlyMemoryView<Vec3> srcView = ReadOnlyMemoryView<Vec3>.DangerousCreate<Vertex>(src, in src[0].Normal);
        MemoryView<Vec3>         dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Normal);

        Assert.IsTrue(srcView.TryCopyTo(dstView));

        for (int i = 0; i < src.Length; i++)
        {
            Assert.AreEqual(new Vec3(0, i + 1f, 0), dst[i].Normal);
        }
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_TryCopyTo_TooShort_ReturnsFalse()
    {
        Vertex[] src = new Vertex[5];
        Vertex[] dst = new Vertex[2];

        ReadOnlyMemoryView<Vec3> srcView = ReadOnlyMemoryView<Vec3>.DangerousCreate<Vertex>(src, in src[0].Position);
        MemoryView<Vec3>         dstView = MemoryView<Vec3>.DangerousCreate<Vertex>(dst, ref dst[0].Position);

        Assert.IsFalse(srcView.TryCopyTo(dstView));
    }

    // ===================================================================================
    // ToArray
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ToArray_Empty()
    {
        // Use a zero-length ReadOnlyMemory<int> (not the default/Empty struct, which has stride=0)
        ReadOnlyMemoryView<int> empty = new ReadOnlyMemory<int>(Array.Empty<int>());
        Assert.AreEqual(0, empty.ToArray().Length);
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ToArray_Strided()
    {
        Vertex[] vertices = new Vertex[4];
        ReadOnlyMemory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i * 3f, 0, 0);
        }

        ReadOnlyMemoryView<Vec3> positions = ReadOnlyMemoryView<Vec3>.DangerousCreate(buffer, in vertices[0].Position);

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
    public unsafe void Test_ReadOnlyMemoryViewT_Pin_DoesNotThrow()
    {
        int[] data = [1, 2, 3];
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(data);

        using MemoryHandle handle = view.Pin();

        Assert.IsTrue(handle.Pointer != null);
    }

    // ===================================================================================
    // Equals / GetHashCode
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_SameMemory_IsTrue()
    {
        int[] data = [1, 2, 3];

        ReadOnlyMemoryView<int> a = new ReadOnlyMemory<int>(data);
        ReadOnlyMemoryView<int> b = new ReadOnlyMemory<int>(data);

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_DifferentMemory_IsFalse()
    {
        int[] data1 = [1, 2, 3];
        int[] data2 = [1, 2, 3];

        ReadOnlyMemoryView<int> a = new ReadOnlyMemory<int>(data1);
        ReadOnlyMemoryView<int> b = new ReadOnlyMemory<int>(data2);

        Assert.IsFalse(a.Equals(b));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_DifferentLength_IsFalse()
    {
        int[] data = [1, 2, 3, 4];
        ReadOnlyMemoryView<int> full  = new ReadOnlyMemory<int>(data);
        ReadOnlyMemoryView<int> slice = full.Slice(0, 2);

        Assert.IsFalse(full.Equals(slice));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_BoxedReadOnlyMemoryView_IsTrue()
    {
        int[] data = [1, 2, 3];
        ReadOnlyMemoryView<int> a = new ReadOnlyMemory<int>(data);
        ReadOnlyMemoryView<int> b = new ReadOnlyMemory<int>(data);

        Assert.IsTrue(a.Equals((object)b));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_BoxedMemoryView_IsTrue()
    {
        // ReadOnlyMemoryView.Equals(object) accepts MemoryView<T> via the implicit conversion path
        int[] data = [1, 2, 3];
        MemoryView<int>         mutable  = new Memory<int>(data);
        ReadOnlyMemoryView<int> readOnly = new ReadOnlyMemory<int>(data);

        Assert.IsTrue(readOnly.Equals((object)mutable));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_NullObject_IsFalse()
    {
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(new int[3]);
        Assert.IsFalse(view.Equals(null));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_Equals_UnrelatedObject_IsFalse()
    {
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(new int[3]);
        Assert.IsFalse(view.Equals("hello"));
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_GetHashCode_EqualViewsSameHash()
    {
        int[] data = [1, 2, 3];

        ReadOnlyMemoryView<int> a = new ReadOnlyMemory<int>(data);
        ReadOnlyMemoryView<int> b = new ReadOnlyMemory<int>(data);

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ===================================================================================
    // ToString
    // ===================================================================================

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ToString_NonChar()
    {
        ReadOnlyMemoryView<int> view = new ReadOnlyMemory<int>(new int[5]);
        string s = view.ToString();

        StringAssert.Contains(s, "ReadOnlyMemoryView");
        StringAssert.Contains(s, "5");
    }

    [TestMethod]
    public void Test_ReadOnlyMemoryViewT_ToString_Char()
    {
        char[] chars = "World".ToCharArray();
        ReadOnlyMemoryView<char> view = new ReadOnlyMemory<char>(chars);

        Assert.AreEqual("World", view.ToString());
    }

    // ===================================================================================
    // MemoryView / ReadOnlyMemoryView cross-type interoperability
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewT_And_ReadOnlyMemoryViewT_Equals_CrossType_IsTrue()
    {
        // MemoryView and ReadOnlyMemoryView wrapping the same data are equal to each other
        int[] data = [1, 2, 3];
        MemoryView<int>         mutable  = new Memory<int>(data);
        ReadOnlyMemoryView<int> readOnly = new ReadOnlyMemory<int>(data);

        // MemoryView.Equals(object readOnlyView)
        Assert.IsTrue(mutable.Equals((object)readOnly));
        // ReadOnlyMemoryView.Equals(object mutableView)
        Assert.IsTrue(readOnly.Equals((object)mutable));
    }

    [TestMethod]
    public void Test_MemoryViewT_CopyTo_ReadOnlyMemoryView_Roundtrip()
    {
        Vertex[] vertices = new Vertex[5];
        Memory<Vertex> buffer = vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = new Vec3(i, 0, 0);
        }

        MemoryView<Vec3>         mutableSrc = MemoryView<Vec3>.DangerousCreate(buffer, ref vertices[0].Position);
        ReadOnlyMemoryView<Vec3> readOnlySrc = mutableSrc;

        Vertex[] dstVertices = new Vertex[vertices.Length];
        Memory<Vertex> dstBuffer = dstVertices;
        MemoryView<Vec3> dst = MemoryView<Vec3>.DangerousCreate(dstBuffer, ref dstVertices[0].Position);

        // Copy using the read-only view's CopyTo method
        readOnlySrc.CopyTo(dst);

        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.AreEqual(new Vec3(i, 0, 0), dstVertices[i].Position);
        }
    }
}
