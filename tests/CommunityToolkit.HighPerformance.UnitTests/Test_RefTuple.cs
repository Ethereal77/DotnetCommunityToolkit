// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET8_0_OR_GREATER

using System;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.HighPerformance.UnitTests;

[TestClass]
public class Test_RefTuple
{
    [TestMethod]
    public void Test_RefTuple_Create_Empty()
    {
        RefTuple tuple = RefTuple.Create();

        Assert.IsTrue(tuple.Equals(default));
        Assert.AreEqual(0, tuple.Length);
        Assert.AreEqual("()", tuple.ToString());
    }

    [TestMethod]
    public void Test_RefTuple_T1_Create_Constructor_Deconstruct()
    {
        RefTuple<int> tupleFromCreate = RefTuple.Create(42);
        RefTuple<int> tupleFromCtor = new(42);

        tupleFromCtor.Deconstruct(out int item1);

        Assert.AreEqual(42, tupleFromCreate.Item1);
        Assert.AreEqual(42, item1);
        Assert.AreEqual(1, tupleFromCtor.Length);
        Assert.AreEqual("RefTuple<Int32>", tupleFromCtor.ToString());
        Assert.IsTrue(tupleFromCreate == tupleFromCtor);
        Assert.IsTrue(tupleFromCreate.Equals(tupleFromCtor));
    }

    [TestMethod]
    public void Test_RefTuple_T2_Create_Constructor_Deconstruct()
    {
        RefTuple<int, int> tupleFromCreate = RefTuple.Create(7, 13);
        RefTuple<int, int> tupleFromCtor = new(7, 13);

        tupleFromCtor.Deconstruct(out int item1, out int item2);

        Assert.AreEqual(7, item1);
        Assert.AreEqual(13, item2);
        Assert.AreEqual(2, tupleFromCtor.Length);
        Assert.AreEqual("RefTuple<Int32, Int32>", tupleFromCtor.ToString());
        Assert.IsTrue(tupleFromCreate == tupleFromCtor);
        Assert.IsTrue(tupleFromCreate.Equals(tupleFromCtor));
    }

    [TestMethod]
    public void Test_RefTuple_T3_Create_Constructor_Deconstruct()
    {
        RefTuple<int, int, int> tupleFromCreate = RefTuple.Create(1, 2, 3);
        RefTuple<int, int, int> tupleFromCtor = new(1, 2, 3);

        tupleFromCtor.Deconstruct(out int item1, out int item2, out int item3);

        Assert.AreEqual(1, item1);
        Assert.AreEqual(2, item2);
        Assert.AreEqual(3, item3);
        Assert.AreEqual(3, tupleFromCtor.Length);
        Assert.AreEqual("RefTuple<Int32, Int32, Int32>", tupleFromCtor.ToString());
        Assert.IsTrue(tupleFromCreate == tupleFromCtor);
        Assert.IsTrue(tupleFromCreate.Equals(tupleFromCtor));
    }

    [TestMethod]
    public void Test_RefTuple_T4_Create_Constructor_Deconstruct()
    {
        RefTuple<int, int, int, int> tupleFromCreate = RefTuple.Create(1, 2, 3, 4);
        RefTuple<int, int, int, int> tupleFromCtor = new(1, 2, 3, 4);

        tupleFromCtor.Deconstruct(out int item1, out int item2, out int item3, out int item4);

        Assert.AreEqual(1, item1);
        Assert.AreEqual(2, item2);
        Assert.AreEqual(3, item3);
        Assert.AreEqual(4, item4);
        Assert.AreEqual(4, tupleFromCtor.Length);
        Assert.AreEqual("RefTuple<Int32, Int32, Int32, Int32>", tupleFromCtor.ToString());
        Assert.IsTrue(tupleFromCreate == tupleFromCtor);
        Assert.IsTrue(tupleFromCreate.Equals(tupleFromCtor));
    }

    [TestMethod]
    public void Test_RefTuple_T1_Equality_Operators()
    {
        RefTuple<int> tuple1 = new(42);
        RefTuple<int> tuple2 = new(42);
        RefTuple<int> tuple3 = new(99);

        Assert.IsTrue(tuple1 == tuple2);
        Assert.IsFalse(tuple1 != tuple2);
        Assert.IsFalse(tuple1 == tuple3);
        Assert.IsTrue(tuple1 != tuple3);
    }

    [TestMethod]
    public void Test_RefTuple_T2_Equality_Operators()
    {
        RefTuple<int, int> tuple1 = new(10, 20);
        RefTuple<int, int> tuple2 = new(10, 20);
        RefTuple<int, int> tuple3 = new(10, 30);

        Assert.IsTrue(tuple1 == tuple2);
        Assert.IsFalse(tuple1 != tuple2);
        Assert.IsFalse(tuple1 == tuple3);
        Assert.IsTrue(tuple1 != tuple3);
    }

    [TestMethod]
    public void Test_RefTuple_T3_Equality_Operators()
    {
        RefTuple<int, int, int> tuple1 = new(10, 20, 30);
        RefTuple<int, int, int> tuple2 = new(10, 20, 30);
        RefTuple<int, int, int> tuple3 = new(10, 20, 31);

        Assert.IsTrue(tuple1 == tuple2);
        Assert.IsFalse(tuple1 != tuple2);
        Assert.IsFalse(tuple1 == tuple3);
        Assert.IsTrue(tuple1 != tuple3);
    }

    [TestMethod]
    public void Test_RefTuple_T4_Equality_Operators()
    {
        RefTuple<int, int, int, int> tuple1 = new(10, 20, 30, 40);
        RefTuple<int, int, int, int> tuple2 = new(10, 20, 30, 40);
        RefTuple<int, int, int, int> tuple3 = new(10, 20, 30, 41);

        Assert.IsTrue(tuple1 == tuple2);
        Assert.IsFalse(tuple1 != tuple2);
        Assert.IsFalse(tuple1 == tuple3);
        Assert.IsTrue(tuple1 != tuple3);
    }

    [TestMethod]
    public void Test_RefTuple_HashCode_SameData_SameHashCode()
    {
        RefTuple<int> t1a = new(123);
        RefTuple<int> t1b = new(123);
        RefTuple<int, int> t2a = new(1, 2);
        RefTuple<int, int> t2b = new(1, 2);
        RefTuple<int, int, int> t3a = new(1, 2, 3);
        RefTuple<int, int, int> t3b = new(1, 2, 3);
        RefTuple<int, int, int, int> t4a = new(1, 2, 3, 4);
        RefTuple<int, int, int, int> t4b = new(1, 2, 3, 4);

        Assert.AreEqual(t1a.GetHashCode(), t1b.GetHashCode());
        Assert.AreEqual(t2a.GetHashCode(), t2b.GetHashCode());
        Assert.AreEqual(t3a.GetHashCode(), t3b.GetHashCode());
        Assert.AreEqual(t4a.GetHashCode(), t4b.GetHashCode());
    }

    [TestMethod]
    public unsafe void Test_RefTuple_WithRefStructComponent()
    {
        Span<int> span = stackalloc[] { 1, 2, 3, 4 };

        RefTuple<Span<int>> tuple1 = new(span[..2]);
        RefTuple<Span<int>> tuple2 = new(span[..2]);
        RefTuple<Span<int>> tuple3 = new(span[2..]);

        Assert.IsTrue(tuple1 == tuple2);
        Assert.IsFalse(tuple1 != tuple2);
        Assert.IsFalse(tuple1 == tuple3);

        Assert.IsTrue(Unsafe.AreSame(ref tuple1.Item1[0], ref span[0]));
    }

    [TestMethod]
    public void Test_RefTuple_UsesBytewiseComparison_NotComponentEquals()
    {
        RefTuple<AlwaysEqualStruct> tuple1 = new(new AlwaysEqualStruct(1));
        RefTuple<AlwaysEqualStruct> tuple2 = new(new AlwaysEqualStruct(2));

        Assert.IsFalse(tuple1 == tuple2);
        Assert.IsTrue(tuple1 != tuple2);
    }

    [TestMethod]
    public void Test_RefTuple_T1_EqualsObject_Throws()
    {
#pragma warning disable CS0618
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            RefTuple<int> tuple = new(42);

            _ = tuple.Equals(null);
        });
#pragma warning restore CS0618
    }

    [TestMethod]
    public void Test_RefTuple_T2_EqualsObject_Throws()
    {
#pragma warning disable CS0618
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            RefTuple<int, int> tuple = new(1, 2);

            _ = tuple.Equals(null);
        });
#pragma warning restore CS0618
    }

    [TestMethod]
    public void Test_RefTuple_T3_EqualsObject_Throws()
    {
#pragma warning disable CS0618
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            RefTuple<int, int, int> tuple = new(1, 2, 3);

            _ = tuple.Equals(null);
        });
#pragma warning restore CS0618
    }

    [TestMethod]
    public void Test_RefTuple_T4_EqualsObject_Throws()
    {
#pragma warning disable CS0618
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            RefTuple<int, int, int, int> tuple = new(1, 2, 3, 4);

            _ = tuple.Equals(null);
        });
#pragma warning restore CS0618
    }

    [TestMethod]
    public void Test_RefTuple_T2_PatternMatching_SwitchExpression()
    {
        RefTuple<int, int> tuple = new(10, 20);

        string result = tuple switch
        {
            { Item1: 10, Item2: 20 } => "match",
            { Item1: > 0, Item2: > 0 } => "positive",
            _ => "other"
        };

        Assert.AreEqual("match", result);

        result = tuple switch
        {
            (10, 20) => "match",
            ( > 0, > 0) => "positive",
            _ => "other"
        };

        Assert.AreEqual("match", result);
    }

    [TestMethod]
    public void Test_RefTuple_T3_PatternMatching_SwitchStatement()
    {
        RefTuple<int, int, int> tuple = new(1, 2, 3);

        int result, result2;

        switch (tuple)
        {
            case { Item1: 1, Item2: 2, Item3: 3 }:
                result = 123;
                break;
            case { Item1: > 0, Item2: > 0, Item3: > 0 }:
                result = 1;
                break;
            default:
                result = 0;
                break;
        }

        Assert.AreEqual(123, result);

        switch (tuple)
        {
            case (1, 2, 3):
                result2 = 123;
                break;
            case ( > 0, > 0, > 0):
                result2 = 1;
                break;
            default:
                result2 = 0;
                break;
        }

        Assert.AreEqual(123, result2);
    }

    [TestMethod]
    public void Test_RefTuple_T4_AsMethodInputAndReturnType()
    {
        static int Sum(RefTuple<int, int, int, int> value)
        {
            return value.Item1 + value.Item2 + value.Item3 + value.Item4;
        }

        static RefTuple<int, int, int, int> CreateQuadruple(int value)
        {
            return new RefTuple<int, int, int, int>(value, value + 1, value + 2, value + 3);
        }

        RefTuple<int, int, int, int> tuple = CreateQuadruple(10);

        Assert.AreEqual(46, Sum(tuple));
        Assert.IsTrue(tuple == new RefTuple<int, int, int, int>(10, 11, 12, 13));
    }

    [TestMethod]
    public unsafe void Test_RefTuple_T1RefStruct_AsMethodInputAndReturnType()
    {
        static int GetLength(RefTuple<Span<int>> value)
        {
            return value.Item1.Length;
        }

        static RefTuple<Span<int>> GetWindow(Span<int> value)
        {
            return new RefTuple<Span<int>>(value[1..3]);
        }

        Span<int> span = stackalloc[] { 7, 8, 9, 10 };

        RefTuple<Span<int>> tuple = GetWindow(span);

        Assert.AreEqual(2, GetLength(tuple));
        Assert.IsTrue(Unsafe.AreSame(ref span[1], ref tuple.Item1[0]));
    }

    private readonly struct AlwaysEqualStruct : IEquatable<AlwaysEqualStruct>
    {
        private readonly int value;

        public AlwaysEqualStruct(int value)
        {
            this.value = value;
        }

        public bool Equals(AlwaysEqualStruct other)
        {
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is AlwaysEqualStruct;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}

#endif
