// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace CommunityToolkit.Common;

/// <summary>
/// Set of helpers to convert between data types and notations.
/// </summary>
public static class Converters
{
    /// <summary>
    /// Translate numeric file size in bytes to a human-readable shorter string format.
    /// </summary>
    /// <param name="size">File size in bytes.</param>
    /// <param name="formatProvider">
    /// Optional format provider to use when formatting the number.
    /// If <see langword="null"/>, the method uses the current culture's formatting conventions.
    /// </param>
    /// <returns>Returns file size short string.</returns>
    public static string ToFileSizeString(long size, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        if (size < 1024)
        {
            return size.ToString("F0", formatProvider) + " bytes";
        }
        else if ((size >> 10) < 1024)
        {
            return (size / 1024F).ToString("F1", formatProvider) + " KB";
        }
        else if ((size >> 20) < 1024)
        {
            return ((size >> 10) / 1024F).ToString("F1", formatProvider) + " MB";
        }
        else if ((size >> 30) < 1024)
        {
            return ((size >> 20) / 1024F).ToString("F1", formatProvider) + " GB";
        }
        else if ((size >> 40) < 1024)
        {
            return ((size >> 30) / 1024F).ToString("F1", formatProvider) + " TB";
        }
        else if ((size >> 50) < 1024)
        {
            return ((size >> 40) / 1024F).ToString("F1", formatProvider) + " PB";
        }
        else
        {
            return ((size >> 50) / 1024F).ToString("F1", formatProvider) + " EB";
        }
    }
}
