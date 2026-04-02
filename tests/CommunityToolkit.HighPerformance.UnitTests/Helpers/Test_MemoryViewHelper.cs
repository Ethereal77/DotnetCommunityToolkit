// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.HighPerformance.UnitTests.Helpers;

[TestClass]
public class Test_MemoryViewHelper
{
    // ===================================================================================
    // GetLength<T>
    //
    // Returns how many elements of type T fit in byteLength bytes when elements are
    // separated by stride bytes. The formula is:
    //   DivRem(byteLength, stride, out rem) + (rem >= sizeof(T) ? 1 : 0)
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewHelper_GetLength_ZeroByteLength()
    {
        // When there are no bytes available, no elements can fit regardless of stride
        Assert.AreEqual(0, MemoryViewHelper.GetLength<int>(stride: 4, byteLength: 0));
        Assert.AreEqual(0, MemoryViewHelper.GetLength<long>(stride: 8, byteLength: 0));
        Assert.AreEqual(0, MemoryViewHelper.GetLength<byte>(stride: 1, byteLength: 0));
    }

    [TestMethod]
    [DataRow(4, 4, 1)]    // 1 element, no remainder
    [DataRow(4, 8, 2)]    // 2 elements, no remainder
    [DataRow(4, 16, 4)]   // 4 elements, no remainder
    [DataRow(8, 4, 1)]    // stride > sizeof(int): rem=4 >= 4 → +1, result=1
    [DataRow(8, 8, 1)]    // quotient=1, rem=0 < 4 → result=1
    [DataRow(8, 12, 2)]   // quotient=1, rem=4 >= 4 → result=2
    [DataRow(8, 20, 3)]   // quotient=2, rem=4 >= 4 → result=3
    [DataRow(8, 19, 2)]   // quotient=2, rem=3 < 4  → result=2
    [DataRow(12, 4, 1)]   // quotient=0, rem=4 >= 4 → result=1
    public void Test_MemoryViewHelper_GetLength_Int(int stride, int byteLength, int expected)
    {
        // Verify the element count calculation for int (4 bytes)
        Assert.AreEqual(expected, MemoryViewHelper.GetLength<int>(stride, byteLength));
    }

    [TestMethod]
    public void Test_MemoryViewHelper_GetLength_Int_ExtraRows()
    {
        // stride=12, byteLength=11: quotient=0, rem=11 >= sizeof(int)=4 → result=1
        Assert.AreEqual(1, MemoryViewHelper.GetLength<int>(stride: 12, byteLength: 11));
        // stride=12, byteLength=3: quotient=0, rem=3 < sizeof(int)=4 → result=0
        Assert.AreEqual(0, MemoryViewHelper.GetLength<int>(stride: 12, byteLength: 3));
    }

    [TestMethod]
    [DataRow(8, 8, 1)]    // 1 element, no remainder
    [DataRow(8, 16, 2)]   // 2 elements, no remainder
    [DataRow(12, 8, 1)]   // quotient=0, rem=8 >= sizeof(long)=8 → result=1
    [DataRow(12, 12, 1)]  // quotient=1, rem=0 < 8 → result=1
    [DataRow(12, 20, 2)]  // quotient=1, rem=8 >= 8 → result=2
    [DataRow(16, 8, 1)]   // quotient=0, rem=8 >= 8 → result=1
    [DataRow(16, 16, 1)]  // quotient=1, rem=0 < 8 → result=1
    [DataRow(16, 24, 2)]  // quotient=1, rem=8 >= 8 → result=2
    public void Test_MemoryViewHelper_GetLength_Long(int stride, int byteLength, int expected)
    {
        // Verify the element count calculation for long (8 bytes)
        Assert.AreEqual(expected, MemoryViewHelper.GetLength<long>(stride, byteLength));
    }

    // ===================================================================================
    // GetByteLength<T>
    //
    // Returns the number of bytes spanned by `length` elements of type T with
    // the given stride between starts of consecutive elements.
    //   0       → 0
    //   1       → sizeof(T)
    //   n       → sizeof(T) + (n-1)*stride
    // ===================================================================================

    [TestMethod]
    public void Test_MemoryViewHelper_GetByteLength_ZeroLength()
    {
        // Zero elements occupy zero bytes regardless of stride
        Assert.AreEqual(0, MemoryViewHelper.GetByteLength<int>(stride: 4, length: 0));
        Assert.AreEqual(0, MemoryViewHelper.GetByteLength<int>(stride: 100, length: 0));
        Assert.AreEqual(0, MemoryViewHelper.GetByteLength<long>(stride: 8, length: 0));
    }

    [TestMethod]
    public void Test_MemoryViewHelper_GetByteLength_OneElement()
    {
        // For a single element the byte length is always sizeof(T), regardless of stride
        // (stride only affects the spacing of subsequent elements).
        Assert.AreEqual(4, MemoryViewHelper.GetByteLength<int>(stride: 4, length: 1));
        Assert.AreEqual(4, MemoryViewHelper.GetByteLength<int>(stride: 8, length: 1));
        Assert.AreEqual(4, MemoryViewHelper.GetByteLength<int>(stride: 100, length: 1));
        Assert.AreEqual(8, MemoryViewHelper.GetByteLength<long>(stride: 8, length: 1));
        Assert.AreEqual(8, MemoryViewHelper.GetByteLength<long>(stride: 16, length: 1));
    }

    [TestMethod]
    [DataRow(4, 2, 8)]    // sizeof(int) + (2-1)*4 = 4+4 = 8
    [DataRow(4, 3, 12)]   // 4 + 2*4 = 12
    [DataRow(4, 8, 32)]   // 4 + 7*4 = 32
    [DataRow(8, 2, 12)]   // 4 + 1*8 = 12
    [DataRow(8, 3, 20)]   // 4 + 2*8 = 20
    [DataRow(8, 4, 28)]   // 4 + 3*8 = 28
    [DataRow(12, 4, 40)]  // 4 + 3*12 = 40
    public void Test_MemoryViewHelper_GetByteLength_Int(int stride, int length, int expected)
    {
        Assert.AreEqual(expected, MemoryViewHelper.GetByteLength<int>(stride, length));
    }

    [TestMethod]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(12)]
    [DataRow(16)]
    public void Test_MemoryViewHelper_GetLength_GetByteLength_Roundtrip_Stride(int stride)
    {
        // GetLength and GetByteLength are consistent inverses of each other:
        // given a byteLength derived from n elements, GetLength should recover n.
        for (int length = 1; length <= 20; length++)
        {
            int byteLength = MemoryViewHelper.GetByteLength<int>(stride, length);
            int roundtrip = MemoryViewHelper.GetLength<int>(stride, byteLength);

            Assert.AreEqual(length, roundtrip, $"Roundtrip failed for stride={stride}, length={length}.");
        }
    }

    // ===================================================================================
    // Clear<T>
    //
    // Sets every strided element to default(T). The implementation uses 8-unrolled,
    // then 4-unrolled, then a tail loop. We test each path:
    //   length == 0               → nothing changes
    //   1 ≤ length ≤ 3            → tail loop only
    //   4 ≤ length ≤ 7            → 4-branch then tail
    //   length == 8               → one full 8-iteration round
    //   9 ≤ length                → main 8-loop + remaining branches
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_Clear_ContiguousInts(int length)
    {
        // stride == sizeof(int): every element in a contiguous span is cleared
        int[] data = new int[Math.Max(length, 1)];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i + 1;
        }

        if (length > 0)
        {
            MemoryViewHelper.Clear(ref data[0], length, stride: sizeof(int));
        }

        for (int i = 0; i < length; i++)
        {
            Assert.AreEqual(0, data[i], $"Element at index {i} was not cleared.");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_Clear_StridedColumn(int columnLength)
    {
        // Simulate clearing column 0 of a row-major 3-column int grid.
        // The stride in bytes between column-0 elements is numCols * sizeof(int).
        const int numCols = 3;
        int[] data = new int[Math.Max(columnLength * numCols, 1)];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i + 1;
        }

        if (columnLength > 0)
        {
            MemoryViewHelper.Clear(ref data[0], columnLength, stride: numCols * sizeof(int));
        }

        for (int row = 0; row < columnLength; row++)
        {
            int baseIdx = row * numCols;

            Assert.AreEqual(0, data[baseIdx], $"Column-0 element at row {row} was not cleared.");

            // Elements in the other columns must be unchanged.
            for (int col = 1; col < numCols; col++)
            {
                Assert.AreEqual(baseIdx + col + 1, data[baseIdx + col],
                    $"Non-target element at [{row},{col}] (index {baseIdx + col}) was unexpectedly modified.");
            }
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_Clear_LongElements()
    {
        // Verify that Clear works correctly with 8-byte elements.
        // Use 9 elements (= 8 + 1) to exercise the main loop plus the tail.
        const int length = 9;
        const int numCols = 2;
        long[] data = new long[length * numCols];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i + 1;
        }

        MemoryViewHelper.Clear(ref data[0], length, stride: numCols * sizeof(long));

        for (int row = 0; row < length; row++)
        {
            Assert.AreEqual(0L, data[row * numCols],
                $"Column-0 long element at row {row} was not cleared.");
            Assert.AreEqual((long)(row * numCols + 2), data[row * numCols + 1],
                $"Column-1 long element at row {row} was unexpectedly modified.");
        }
    }

    // ===================================================================================
    // Fill<T>
    //
    // Assigns a fixed value to every strided element. Same loop structure as Clear.
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_Fill_ContiguousInts(int length)
    {
        // stride == sizeof(int): every element in a contiguous span is filled
        const int fillValue = 42;
        int[] data = new int[Math.Max(length, 1)];

        if (length > 0)
        {
            MemoryViewHelper.Fill(ref data[0], length, stride: sizeof(int), value: fillValue);
        }

        for (int i = 0; i < length; i++)
        {
            Assert.AreEqual(fillValue, data[i], $"Element at index {i} was not filled.");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_Fill_StridedColumn(int columnLength)
    {
        // Simulate filling column 0 of a row-major 3-column int grid
        const int numCols = 3;
        const int fillValue = 99;
        int[] data = new int[Math.Max(columnLength * numCols, 1)];

        if (columnLength > 0)
        {
            MemoryViewHelper.Fill(ref data[0], columnLength, stride: numCols * sizeof(int), value: fillValue);
        }

        for (int row = 0; row < columnLength; row++)
        {
            int baseIdx = row * numCols;

            Assert.AreEqual(fillValue, data[baseIdx],
                $"Column-0 element at row {row} was not filled.");

            for (int col = 1; col < numCols; col++)
            {
                Assert.AreEqual(0, data[baseIdx + col],
                    $"Non-target element at [{row},{col}] was unexpectedly modified.");
            }
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_Fill_LongElements()
    {
        // Verify Fill with 8-byte long elements and a strided layout.
        const int length = 9;
        const int numCols = 2;
        const long fillValue = 0xDEAD_BEEF_CAFE;
        long[] data = new long[length * numCols];

        MemoryViewHelper.Fill(ref data[0], length, stride: numCols * sizeof(long), value: fillValue);

        for (int row = 0; row < length; row++)
        {
            Assert.AreEqual(fillValue, data[row * numCols],
                $"Column-0 long element at row {row} was not filled.");
            Assert.AreEqual(0L, data[row * numCols + 1],
                $"Column-1 long element at row {row} was unexpectedly modified.");
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_Fill_ClearsWhenDefault()
    {
        // Calling Fill with default(T) is equivalent to Clear
        int[] fillData = [1, 2, 3, 4, 5];
        int[] clearData = [1, 2, 3, 4, 5];

        MemoryViewHelper.Fill(ref fillData[0], fillData.Length, stride: sizeof(int), value: 0);
        MemoryViewHelper.Clear(ref clearData[0], clearData.Length, stride: sizeof(int));

        CollectionAssert.AreEqual(clearData, fillData);
    }

    // ===================================================================================
    // CopyTo<T>(sourceRef, destinationRef, length, sourceStride)
    //
    // Copies `length` elements from a strided source into a contiguous destination.
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyTo_ContiguousSourceToContiguousDest(int length)
    {
        // stride == sizeof(int): degenerate case where both sides are contiguous
        int[] src = new int[Math.Max(length, 1)];
        int[] dst = new int[Math.Max(length, 1)];

        for (int i = 0; i < length; i++)
        {
            src[i] = i + 1;
        }

        if (length > 0)
        {
            MemoryViewHelper.CopyTo(ref src[0], ref dst[0], length, sourceStride: sizeof(int));
        }

        for (int i = 0; i < length; i++)
        {
            Assert.AreEqual(i + 1, dst[i], $"Destination element at index {i} has the wrong value.");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyTo_StridedSourceToContiguousDest(int columnLength)
    {
        // Copy column 0 of a 4-column row-major int grid into a plain contiguous array.
        // Column 0 elements sit at indices 0, 4, 8, 12, … in the flat buffer.
        const int numCols = 4;
        int[] src = new int[Math.Max(columnLength * numCols, 1)];
        int[] dst = new int[Math.Max(columnLength, 1)];

        for (int row = 0; row < columnLength; row++)
        {
            src[row * numCols] = row * 10;           // column 0 carries meaningful data
            for (int col = 1; col < numCols; col++)
            {
                src[row * numCols + col] = -1;       // sentinel — must not appear in dst
            }
        }

        if (columnLength > 0)
        {
            MemoryViewHelper.CopyTo(ref src[0], ref dst[0], columnLength,
                sourceStride: numCols * sizeof(int));
        }

        for (int i = 0; i < columnLength; i++)
        {
            Assert.AreEqual(i * 10, dst[i], $"Destination element at index {i} has the wrong value.");
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_CopyTo_StridedSource_PreservesDestinationRemainder()
    {
        // Elements NOT written by CopyTo must be untouched
        const int columnLength = 5;
        const int numCols = 4;
        int[] src = new int[columnLength * numCols];
        int[] dst = new int[columnLength + 3];

        for (int row = 0; row < columnLength; row++)
        {
            src[row * numCols] = (row + 1) * 7;
        }

        // Sentinel values in dst beyond the written range
        for (int i = columnLength; i < dst.Length; i++)
        {
            dst[i] = 999;
        }

        MemoryViewHelper.CopyTo(ref src[0], ref dst[0], columnLength,
            sourceStride: numCols * sizeof(int));

        // Verify written section.
        for (int i = 0; i < columnLength; i++)
        {
            Assert.AreEqual((i + 1) * 7, dst[i]);
        }

        // Verify untouched section.
        for (int i = columnLength; i < dst.Length; i++)
        {
            Assert.AreEqual(999, dst[i], $"Destination element at index {i} was unexpectedly modified.");
        }
    }

    // ===================================================================================
    // CopyTo<T>(sourceRef, destinationRef, length, sourceStride, destinationStride)
    //
    // Copies `length` elements from a strided source into a strided destination.
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyTo_BothStrided(int columnLength)
    {
        // Copy column 0 of one 4-column grid into column 1 of another 4-column grid
        const int numCols = 4;
        int[] src = new int[Math.Max(columnLength * numCols, 1)];
        int[] dst = new int[Math.Max(columnLength * numCols, 1)];
        nint stride = numCols * sizeof(int);

        for (int row = 0; row < columnLength; row++)
        {
            src[row * numCols] = (row + 1) * 5;
        }

        if (columnLength > 0)
        {
            MemoryViewHelper.CopyTo(ref src[0], ref dst[1], columnLength,
                sourceStride: stride, destinationStride: stride);
        }

        for (int row = 0; row < columnLength; row++)
        {
            int baseIdx = row * numCols;

            // Column 1 of dst should hold the copied value
            Assert.AreEqual((row + 1) * 5, dst[baseIdx + 1],
                $"Column-1 element at row {row} has the wrong value.");

            // All other columns of dst must remain 0.
            for (int col = 0; col < numCols; col++)
            {
                if (col != 1)
                {
                    Assert.AreEqual(0, dst[baseIdx + col],
                        $"Non-target element at [{row},{col}] was unexpectedly modified.");
                }
            }
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_CopyTo_BothStrided_DifferentStrides()
    {
        // Source has 3-column stride; destination has 5-column stride.
        // Copy 9 elements (= 8 + 1) to exercise the main loop and tail.
        const int srcCols = 3;
        const int dstCols = 5;
        const int length = 9;

        int[] src = new int[length * srcCols];
        int[] dst = new int[length * dstCols];

        for (int row = 0; row < length; row++)
        {
            src[row * srcCols] = (row + 1) * 2;
        }

        MemoryViewHelper.CopyTo(ref src[0], ref dst[0], length,
            sourceStride: srcCols * sizeof(int),
            destinationStride: dstCols * sizeof(int));

        for (int row = 0; row < length; row++)
        {
            Assert.AreEqual((row + 1) * 2, dst[row * dstCols],
                $"Element at row {row} has the wrong value.");
        }
    }

    // ===================================================================================
    // CopyFrom<T>(sourceRef, destinationRef, length, destinationStride)
    //
    // Copies `length` elements from a contiguous source into a strided destination.
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyFrom_ContiguousSourceToContiguousDest(int length)
    {
        // stride == sizeof(int): degenerate case where both sides are contiguous
        int[] src = new int[Math.Max(length, 1)];
        int[] dst = new int[Math.Max(length, 1)];

        for (int i = 0; i < length; i++)
        {
            src[i] = i + 1;
        }

        if (length > 0)
        {
            MemoryViewHelper.CopyFrom(ref src[0], ref dst[0], length, destinationStride: sizeof(int));
        }

        for (int i = 0; i < length; i++)
        {
            Assert.AreEqual(i + 1, dst[i], $"Destination element at index {i} has the wrong value.");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyFrom_ContiguousSourceToStridedDest(int columnLength)
    {
        // Copy a contiguous array into column 2 of a 4-column row-major int grid
        const int numCols = 4;
        int[] src = new int[Math.Max(columnLength, 1)];
        int[] dst = new int[Math.Max(columnLength * numCols, 1)];

        for (int i = 0; i < columnLength; i++)
        {
            src[i] = (i + 1) * 3;
        }

        if (columnLength > 0)
        {
            MemoryViewHelper.CopyFrom(ref src[0], ref dst[2], columnLength,
                destinationStride: numCols * sizeof(int));
        }

        for (int row = 0; row < columnLength; row++)
        {
            int baseIdx = row * numCols;

            Assert.AreEqual((row + 1) * 3, dst[baseIdx + 2],
                $"Column-2 element at row {row} has the wrong value.");

            // All other columns must remain untouched (zero-initialized)
            for (int col = 0; col < numCols; col++)
            {
                if (col != 2)
                {
                    Assert.AreEqual(0, dst[baseIdx + col],
                        $"Non-target element at [{row},{col}] was unexpectedly modified.");
                }
            }
        }
    }

    [TestMethod]
    public void Test_MemoryViewHelper_CopyFrom_SourceValuesPreserved()
    {
        // Verify the source array is not modified during CopyFrom
        const int length = 10;
        int[] src = new int[length];
        int[] dst = new int[length * 2];

        for (int i = 0; i < length; i++)
        {
            src[i] = i * 7;
        }

        int[] srcCopy = (int[])src.Clone();

        MemoryViewHelper.CopyFrom(ref src[0], ref dst[0], length, destinationStride: 2 * sizeof(int));

        CollectionAssert.AreEqual(srcCopy, src, "Source array was modified during CopyFrom.");
    }

    // ===================================================================================
    // Cross-method consistency
    // ===================================================================================

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(17)]
    public void Test_MemoryViewHelper_CopyTo_CopyFrom_Roundtrip(int length)
    {
        // CopyTo followed by CopyFrom (or vice-versa) should produce the original data.
        // Layout: 4-column grid, column 0.
        const int numCols = 4;
        int[] grid = new int[Math.Max(length * numCols, 1)];
        int[] buffer = new int[Math.Max(length, 1)];
        int[] restored = new int[Math.Max(length * numCols, 1)];

        for (int row = 0; row < length; row++)
        {
            grid[row * numCols] = (row + 1) * 11;
        }

        nint stride = numCols * sizeof(int);

        // grid column 0 → buffer (strided source → contiguous dest)
        if (length > 0)
        {
            MemoryViewHelper.CopyTo(ref grid[0], ref buffer[0], length, sourceStride: stride);
        }

        // buffer → restored column 0 (contiguous source → strided dest)
        if (length > 0)
        {
            MemoryViewHelper.CopyFrom(ref buffer[0], ref restored[0], length, destinationStride: stride);
        }

        for (int row = 0; row < length; row++)
        {
            Assert.AreEqual(grid[row * numCols], restored[row * numCols],
                $"Roundtrip value mismatch at row {row}.");
        }
    }
}
