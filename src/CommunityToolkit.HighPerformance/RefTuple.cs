// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET8_0_OR_GREATER

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// The <c>RefTuple</c> types (from arity 0 to 4) comprise tuple implementations that allow
/// <c>ref struct</c> semantics.
/// As such, the usual restrictions apply (they can only be stored on the stack, and cannot be boxed or captured by
/// lambda expressions). They can however be used in ref fields of other <c>ref struct</c> types, and can themselves
/// contain <c>ref</c> fields and other <c>ref struct</c> types, and be returned from methods as <c>ref</c> returns.
/// Aside from being created via language syntax, they are most easily created via the <see cref="RefTuple.Create"/>
/// factory methods.
/// </summary>
/// <remarks>
/// <para>
/// Because <see cref="RefTuple"/> is a <c>ref struct</c> type, it cannot be boxed, and thus cannot be used with
/// the default object equality comparers or other APIs that require boxing. This means that the equality operators
/// and the <see cref="Equals(RefTuple)"/> method will determine the equality (or inequality) of the tuples by
/// comparing their components using a byte-wise comparison, and thus will only return <see langword="true"/>
/// (or <see langword="false"/> for inequality) if the components are exactly the same, not just equal.
/// </para>
/// <para>
/// As a consequence of the above, padding bytes or uninitialized data in the components may cause the
/// equality operators to return <see langword="false"/> even if the components are equal, and may cause the
/// hashcode to be unstable or different for tuples with equal components.
/// If you need to compare the contents of the tuples for equality, do so manually by comparing the components
/// yourself.
/// </para>
/// </remarks>
public ref struct RefTuple
{
    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified value.
    /// </summary>
    /// <param name="other">An instance to compare to this instance.</param>
    /// <returns>true if <paramref name="other"/> has the same value as this instance; otherwise, false.</returns>
    public readonly bool Equals(RefTuple other)
    {
        return true;
    }

    /// <summary>
    /// Returns a string that represents the value of this <see cref="RefTuple"/> instance.
    /// </summary>
    /// <returns>The string representation of this <see cref="RefTuple"/> instance.</returns>
    /// <remarks>
    /// The string returned by this method takes the form <c>()</c>.
    /// </remarks>
    public override readonly string ToString()
    {
        return "()";
    }

    /// <summary>
    /// The number of positions in this data structure.
    /// </summary>
    public readonly int Length => 0;

    /// <summary>
    /// Creates a new <c>ref struct</c> 0-tuple.
    /// </summary>
    /// <returns>A 0-tuple.</returns>
    public static RefTuple Create() =>
        default;

    /// <summary>
    /// Creates a new <c>ref struct</c> 1-tuple, or singleton.
    /// </summary>
    /// <typeparam name="T1">The type of the first component of the tuple.</typeparam>
    /// <param name="item1">The value of the first component of the tuple.</param>
    /// <returns>A 1-tuple (singleton) whose value is (item1).</returns>
    public static RefTuple<T1> Create<T1>(T1 item1)
        where T1 : allows ref struct
        => new RefTuple<T1>(item1);

    /// <summary>
    /// Creates a new <c>ref struct</c> 2-tuple, or pair.
    /// </summary>
    /// <typeparam name="T1">The type of the first component of the tuple.</typeparam>
    /// <typeparam name="T2">The type of the second component of the tuple.</typeparam>
    /// <param name="item1">The value of the first component of the tuple.</param>
    /// <param name="item2">The value of the second component of the tuple.</param>
    /// <returns>A 2-tuple (pair) whose value is (item1, item2).</returns>
    public static RefTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => new RefTuple<T1, T2>(item1, item2);

    /// <summary>
    /// Creates a new <c>ref struct</c> 3-tuple, or triple.
    /// </summary>
    /// <typeparam name="T1">The type of the first component of the tuple.</typeparam>
    /// <typeparam name="T2">The type of the second component of the tuple.</typeparam>
    /// <typeparam name="T3">The type of the third component of the tuple.</typeparam>
    /// <param name="item1">The value of the first component of the tuple.</param>
    /// <param name="item2">The value of the second component of the tuple.</param>
    /// <param name="item3">The value of the third component of the tuple.</param>
    /// <returns>A 3-tuple (triple) whose value is (item1, item2, item3).</returns>
    public static RefTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => new RefTuple<T1, T2, T3>(item1, item2, item3);

    /// <summary>
    /// Creates a new <c>ref struct</c> 4-tuple, or quadruple.
    /// </summary>
    /// <typeparam name="T1">The type of the first component of the tuple.</typeparam>
    /// <typeparam name="T2">The type of the second component of the tuple.</typeparam>
    /// <typeparam name="T3">The type of the third component of the tuple.</typeparam>
    /// <typeparam name="T4">The type of the fourth component of the tuple.</typeparam>
    /// <param name="item1">The value of the first component of the tuple.</param>
    /// <param name="item2">The value of the second component of the tuple.</param>
    /// <param name="item3">The value of the third component of the tuple.</param>
    /// <param name="item4">The value of the fourth component of the tuple.</param>
    /// <returns>A 4-tuple (quadruple) whose value is (item1, item2, item3, item4).</returns>
    public static RefTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        => new RefTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
}

/// <summary>
/// Represents a 1-tuple, or singleton, as a <c>ref struct</c> type.
/// </summary>
/// <typeparam name="T1">The type of the tuple's only component.</typeparam>
/// <remarks><inheritdoc cref="RefTuple" path="/remarks"/></remarks>
public ref struct RefTuple<T1>
    : IEquatable<RefTuple<T1>>
    where T1 : allows ref struct
{
    /// <summary>
    /// The current <see cref="RefTuple{T1}"/> instance's component.
    /// </summary>
    public T1 Item1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefTuple{T1}"/> value type.
    /// </summary>
    /// <param name="item1">The value of the tuple's component.</param>
    public RefTuple(T1 item1)
    {
        this.Item1 = item1;
    }

    /// <summary>
    /// Decomposes the current <see cref="RefTuple{T1}"/> instance into its components.
    /// </summary>
    /// <param name="item1">When this method returns, contains the first element of the tuple.</param>
    /// <remarks>Use this method to support deconstruction of the instance in a tuple assignment.</remarks>
    public readonly void Deconstruct(out T1 item1)
    {
        item1 = this.Item1;
    }

    /// <summary>
    /// This method is not supported as <see cref="RefTuple{T1}"/> cannot be boxed.
    /// To compare two <see cref="RefTuple{T1}"/> instances, use the <see cref="operator =="/>
    /// or the <see cref="Equals(RefTuple{T1})"/> method.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown by this method.</exception>
    [Obsolete("Equals() on RefTuple<T1> will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override readonly bool Equals(object? obj) =>
        ThrowHelper.ThrowNotSupportedException_Equals(this);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <summary>
    /// Returns a value that indicates whether the current <see cref="RefTuple{T1}"/>
    /// instance is equal to a specified <see cref="RefTuple{T1}"/>.
    /// </summary>
    /// <param name="other">The tuple to compare with this instance.</param>
    /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public readonly bool Equals(RefTuple<T1> other)
    {
        return EqualityHelper.Equals(this.Item1, other.Item1);
    }

    /// <summary>
    /// Returns the hash code for the current <see cref="RefTuple{T1}"/> instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The hash code is computed by combining the hash codes of the tuple's components,
    /// computed from its raw bytes using the Djb2 algorithm.
    /// </para>
    /// </remarks>
    public override readonly int GetHashCode()
    {
        return EqualityHelper.GetHashCode(this.Item1);
    }

    /// <summary>
    /// Returns <see langword="false"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator !=(RefTuple<T1> left,
                                   RefTuple<T1> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns <see langword="true"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator ==(RefTuple<T1> left,
                                   RefTuple<T1> right)
    {
        return EqualityHelper.Equals(left.Item1, right.Item1);
    }

    /// <summary>
    /// Returns a <see cref="string"/> with the name of the type and the types of its elements.
    /// </summary>
    public override readonly string ToString()
    {
        return $"RefTuple<{typeof(T1).Name}>";
    }

    /// <summary>
    /// The number of positions in this data structure.
    /// </summary>
    public readonly int Length => 1;
}

/// <summary>
/// Represents a 2-tuple, or pair, as a <c>ref struct</c> type.
/// </summary>
/// <typeparam name="T1">The type of the tuple's first component.</typeparam>
/// <typeparam name="T2">The type of the tuple's second component.</typeparam>
/// <remarks><inheritdoc cref="RefTuple" path="/remarks"/></remarks>
[StructLayout(LayoutKind.Auto)]
public ref struct RefTuple<T1, T2>
    : IEquatable<RefTuple<T1, T2>>
    where T1 : allows ref struct
    where T2 : allows ref struct
{
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2}"/> instance's first component.
    /// </summary>
    public T1 Item1;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2}"/> instance's second component.
    /// </summary>
    public T2 Item2;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefTuple{T1, T2}"/> value type.
    /// </summary>
    /// <param name="item1">The value of the tuple's first component.</param>
    /// <param name="item2">The value of the tuple's second component.</param>
    public RefTuple(T1 item1, T2 item2)
    {
        this.Item1 = item1;
        this.Item2 = item2;
    }

    /// <summary>
    /// Decomposes the current <see cref="RefTuple{T1, T2}"/> instance into its components.
    /// </summary>
    /// <param name="item1">When this method returns, contains the first element of the tuple.</param>
    /// <param name="item2">When this method returns, contains the second element of the tuple.</param>
    /// <remarks>Use this method to support deconstruction of the instance in a tuple assignment.</remarks>
    public readonly void Deconstruct(out T1 item1, out T2 item2)
    {
        item1 = this.Item1;
        item2 = this.Item2;
    }

    /// <summary>
    /// This method is not supported as <see cref="RefTuple{T1, T2}"/> cannot be boxed.
    /// To compare two <see cref="RefTuple{T1, T2}"/> instances, use the <see cref="operator =="/>
    /// or the <see cref="Equals(RefTuple{T1, T2})"/> method.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown by this method.</exception>
    [Obsolete("Equals() on RefTuple<T1, T2> will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override readonly bool Equals(object? obj) =>
        ThrowHelper.ThrowNotSupportedException_Equals(this);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <summary>
    /// Returns a value that indicates whether the current <see cref="RefTuple{T1, T2}"/>
    /// instance is equal to a specified <see cref="RefTuple{T1, T2}"/>.
    /// </summary>
    /// <param name="other">The tuple to compare with this instance.</param>
    /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public readonly bool Equals(RefTuple<T1, T2> other)
    {
        return EqualityHelper.Equals(this.Item1, other.Item1)
            && EqualityHelper.Equals(this.Item2, other.Item2);
    }

    /// <summary>
    /// Returns the hash code for the current <see cref="RefTuple{T1, T2}"/> instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The hash code is computed by combining the hash codes of the tuple's components,
    /// computed from its raw bytes using the Djb2 algorithm.
    /// </para>
    /// </remarks>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(EqualityHelper.GetHashCode(this.Item1),
                                EqualityHelper.GetHashCode(this.Item2));
    }

    /// <summary>
    /// Returns <see langword="false"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator !=(RefTuple<T1, T2> left,
                                   RefTuple<T1, T2> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns <see langword="true"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator ==(RefTuple<T1, T2> left,
                                   RefTuple<T1, T2> right)
    {
        return EqualityHelper.Equals(left.Item1, right.Item1)
            && EqualityHelper.Equals(left.Item2, right.Item2);
    }

    /// <summary>
    /// Returns a <see cref="string"/> with the name of the type and the types of its elements.
    /// </summary>
    public override readonly string ToString()
    {
        return $"RefTuple<{typeof(T1).Name}, {typeof(T2).Name}>";
    }

    /// <summary>
    /// The number of positions in this data structure.
    /// </summary>
    public readonly int Length => 2;
}

/// <summary>
/// Represents a 3-tuple, or triple, as a <c>ref struct</c> type.
/// </summary>
/// <typeparam name="T1">The type of the tuple's first component.</typeparam>
/// <typeparam name="T2">The type of the tuple's second component.</typeparam>
/// <typeparam name="T3">The type of the tuple's third component.</typeparam>
/// <remarks><inheritdoc cref="RefTuple" path="/remarks"/></remarks>
[StructLayout(LayoutKind.Auto)]
public ref struct RefTuple<T1, T2, T3>
    : IEquatable<RefTuple<T1, T2, T3>>
    where T1 : allows ref struct
    where T2 : allows ref struct
    where T3 : allows ref struct
{
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3}"/> instance's first component.
    /// </summary>
    public T1 Item1;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3}"/> instance's second component.
    /// </summary>
    public T2 Item2;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3}"/> instance's third component.
    /// </summary>
    public T3 Item3;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefTuple{T1, T2, T3}"/> value type.
    /// </summary>
    /// <param name="item1">The value of the tuple's first component.</param>
    /// <param name="item2">The value of the tuple's second component.</param>
    /// <param name="item3">The value of the tuple's third component.</param>
    public RefTuple(T1 item1, T2 item2, T3 item3)
    {
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
    }

    /// <summary>
    /// Decomposes the current <see cref="RefTuple{T1, T2, T3}"/> instance into its components.
    /// </summary>
    /// <param name="item1">When this method returns, contains the first element of the tuple.</param>
    /// <param name="item2">When this method returns, contains the second element of the tuple.</param>
    /// <param name="item3">When this method returns, contains the third element of the tuple.</param>
    /// <remarks>Use this method to support deconstruction of the instance in a tuple assignment.</remarks>
    public readonly void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
    {
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
    }

    /// <summary>
    /// This method is not supported as <see cref="RefTuple{T1, T2, T3}"/> cannot be boxed.
    /// To compare two <see cref="RefTuple{T1, T2, T3}"/> instances, use the <see cref="operator =="/>
    /// or the <see cref="Equals(RefTuple{T1, T2, T3})"/> method.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown by this method.</exception>
    [Obsolete("Equals() on RefTuple<T1, T2, T3> will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override readonly bool Equals(object? obj) =>
        ThrowHelper.ThrowNotSupportedException_Equals(this);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <summary>
    /// Returns a value that indicates whether the current <see cref="RefTuple{T1, T2, T3}"/>
    /// instance is equal to a specified <see cref="RefTuple{T1, T2, T3}"/>.
    /// </summary>
    /// <param name="other">The tuple to compare with this instance.</param>
    /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public readonly bool Equals(RefTuple<T1, T2, T3> other)
    {
        return EqualityHelper.Equals(this.Item1, other.Item1)
            && EqualityHelper.Equals(this.Item2, other.Item2)
            && EqualityHelper.Equals(this.Item3, other.Item3);
    }

    /// <summary>
    /// Returns the hash code for the current <see cref="RefTuple{T1, T2, T3}"/> instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The hash code is computed by combining the hash codes of the tuple's components,
    /// computed from its raw bytes using the Djb2 algorithm.
    /// </para>
    /// </remarks>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(EqualityHelper.GetHashCode(this.Item1),
                                EqualityHelper.GetHashCode(this.Item2),
                                EqualityHelper.GetHashCode(this.Item3));
    }

    /// <summary>
    /// Returns <see langword="false"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator !=(RefTuple<T1, T2, T3> left,
                                   RefTuple<T1, T2, T3> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns <see langword="true"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator ==(RefTuple<T1, T2, T3> left,
                                   RefTuple<T1, T2, T3> right)
    {
        return EqualityHelper.Equals(left.Item1, right.Item1)
            && EqualityHelper.Equals(left.Item2, right.Item2)
            && EqualityHelper.Equals(left.Item3, right.Item3);
    }

    /// <summary>
    /// Returns a <see cref="string"/> with the name of the type and the types of its elements.
    /// </summary>
    public override readonly string ToString()
    {
        return $"RefTuple<{typeof(T1).Name}, {typeof(T2).Name}, {typeof(T3).Name}>";
    }

    /// <summary>
    /// The number of positions in this data structure.
    /// </summary>
    public readonly int Length => 3;
}

/// <summary>
/// Represents a 4-tuple, or quadruple, as a <c>ref struct</c> type.
/// </summary>
/// <typeparam name="T1">The type of the tuple's first component.</typeparam>
/// <typeparam name="T2">The type of the tuple's second component.</typeparam>
/// <typeparam name="T3">The type of the tuple's third component.</typeparam>
/// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
/// <remarks><inheritdoc cref="RefTuple" path="/remarks"/></remarks>
[StructLayout(LayoutKind.Auto)]
public ref struct RefTuple<T1, T2, T3, T4>
    : IEquatable<RefTuple<T1, T2, T3, T4>>
    where T1 : allows ref struct
    where T2 : allows ref struct
    where T3 : allows ref struct
    where T4 : allows ref struct
{
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3, T4}"/> instance's first component.
    /// </summary>
    public T1 Item1;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3, T4}"/> instance's second component.
    /// </summary>
    public T2 Item2;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3, T4}"/> instance's third component.
    /// </summary>
    public T3 Item3;
    /// <summary>
    /// The current <see cref="RefTuple{T1, T2, T3, T4}"/> instance's fourth component.
    /// </summary>
    public T4 Item4;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefTuple{T1, T2, T3, T4}"/> value type.
    /// </summary>
    /// <param name="item1">The value of the tuple's first component.</param>
    /// <param name="item2">The value of the tuple's second component.</param>
    /// <param name="item3">The value of the tuple's third component.</param>
    /// <param name="item4">The value of the tuple's fourth component.</param>
    public RefTuple(T1 item1, T2 item2, T3 item3, T4 item4)
    {
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
        this.Item4 = item4;
    }

    /// <summary>
    /// Decomposes the current <see cref="RefTuple{T1, T2, T3, T4}"/> instance into its components.
    /// </summary>
    /// <param name="item1">When this method returns, contains the first element of the tuple.</param>
    /// <param name="item2">When this method returns, contains the second element of the tuple.</param>
    /// <param name="item3">When this method returns, contains the third element of the tuple.</param>
    /// <param name="item4">When this method returns, contains the fourth element of the tuple.</param>
    /// <remarks>Use this method to support deconstruction of the instance in a tuple assignment.</remarks>
    public readonly void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4)
    {
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
        item4 = this.Item4;
    }

    /// <summary>
    /// This method is not supported as <see cref="RefTuple{T1, T2, T3, T4}"/> cannot be boxed.
    /// To compare two <see cref="RefTuple{T1, T2, T3, T4}"/> instances, use the <see cref="operator =="/>
    /// or the <see cref="Equals(RefTuple{T1, T2, T3, T4})"/> method.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown by this method.</exception>
    [Obsolete("Equals() on RefTuple<T1, T2, T3, T4> will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override readonly bool Equals(object? obj) =>
        ThrowHelper.ThrowNotSupportedException_Equals(this);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <summary>
    /// Returns a value that indicates whether the current <see cref="RefTuple{T1, T2, T3, T4}"/>
    /// instance is equal to a specified <see cref="RefTuple{T1, T2, T3, T4}"/>.
    /// </summary>
    /// <param name="other">The tuple to compare with this instance.</param>
    /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3, T4}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public readonly bool Equals(RefTuple<T1, T2, T3, T4> other)
    {
        return EqualityHelper.Equals(this.Item1, other.Item1)
            && EqualityHelper.Equals(this.Item2, other.Item2)
            && EqualityHelper.Equals(this.Item3, other.Item3)
            && EqualityHelper.Equals(this.Item4, other.Item4);
    }

    /// <summary>
    /// Returns the hash code for the current <see cref="RefTuple{T1, T2, T3, T4}"/> instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3, T4}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The hash code is computed by combining the hash codes of the tuple's components,
    /// computed from its raw bytes using the Djb2 algorithm.
    /// </para>
    /// </remarks>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(EqualityHelper.GetHashCode(this.Item1),
                                EqualityHelper.GetHashCode(this.Item2),
                                EqualityHelper.GetHashCode(this.Item3),
                                EqualityHelper.GetHashCode(this.Item4));
    }

    /// <summary>
    /// Returns <see langword="false"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3, T4}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator !=(RefTuple<T1, T2, T3, T4> left,
                                   RefTuple<T1, T2, T3, T4> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns <see langword="true"/> if left and right contain the exact same elements.
    /// Note that this does *not* check to see if the *contents* are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because <see cref="RefTuple{T1, T2, T3, T4}"/> is a <c>ref struct</c> type, it cannot be boxed,
    /// and thus cannot be used with the default object equality comparers.
    /// </para>
    /// <para>
    /// The equality operators compare the tuples by comparing their components using a byte-wise comparison,
    /// and thus will only return <see langword="true"/> (or <see langword="false"/> for inequality) if the
    /// components are exactly the same, not just equal.
    /// </para>
    /// </remarks>
    public static bool operator ==(RefTuple<T1, T2, T3, T4> left,
                                   RefTuple<T1, T2, T3, T4> right)
    {
        return EqualityHelper.Equals(left.Item1, right.Item1)
            && EqualityHelper.Equals(left.Item2, right.Item2)
            && EqualityHelper.Equals(left.Item3, right.Item3)
            && EqualityHelper.Equals(left.Item4, right.Item4);
    }

    /// <summary>
    /// Returns a <see cref="string"/> with the name of the type and the types of its elements.
    /// </summary>
    public override readonly string ToString()
    {
        return $"RefTuple<{typeof(T1).Name}, {typeof(T2).Name}, {typeof(T3).Name}, {typeof(T4).Name}>";
    }

    /// <summary>
    /// The number of positions in this data structure.
    /// </summary>
    public readonly int Length => 4;
}

file static class EqualityHelper
{
    // If T is a ref struct, we can't use any means that need an object (like EqualityComparer<T>
    // or Comparer<T> or object.Equals()) because ref structs can't be boxed. We can't use
    // interfaces neither, because using patterns like "obj is IEquatable<T> equatable" is
    // also boxing. As a last resort, we treat the arguments as pure memory and compare
    // its contents, or compute its hashcode from their raw bytes

    internal unsafe static bool Equals<T>(T left, T right) where T : allows ref struct
    {
        ReadOnlySpan<byte> leftBytes = new(Unsafe.AsPointer(ref left), Unsafe.SizeOf<T>());
        ReadOnlySpan<byte> rightBytes = new(Unsafe.AsPointer(ref right), Unsafe.SizeOf<T>());
        return leftBytes.SequenceEqual(rightBytes);
    }

    internal unsafe static int GetHashCode<T>(T obj) where T : allows ref struct
    {
        ReadOnlySpan<byte> bytes = new(Unsafe.AsPointer(ref obj), Unsafe.SizeOf<T>());
        return bytes.GetDjb2HashCode();
    }
}

file static class ThrowHelper
{
    // As RefTuple types are ref structs, they can never be boxed, so we can't use a parameter
    // "object? obj" here. Instead, we use a generic parameter constrained to be a ref struct
    [DoesNotReturn]
    internal static bool ThrowNotSupportedException_Equals<T>(T obj) where T : allows ref struct
    {
        throw new NotSupportedException($"Equals() on {typeof(T)} is not supported. Use operator == instead.");
    }
}

#endif
