using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace ShopTileFramework.src.Utility;
/// <summary>
/// Extension methods on strings.
/// </summary>
internal static class StringExtensions
{
    private const MethodImplOptions Hot = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminator">deliminator to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    [Pure]
    [MethodImpl(Hot)]
    internal static ReadOnlySpan<char> GetNthChunk(this string str, char deliminator, int index = 0)
    {
        if (index < 0 || index > str.Length + 1)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        int start = 0;
        int ind = 0;
        while (index-- >= 0)
        {
            ind = str.IndexOf(deliminator, start);
            if (ind == -1)
            {
                // since we've previously decremented index, check against -1;
                // this means we're done.
                if (index == -1)
                {
                    return str.AsSpan()[start..];
                }

                // else, we've run out of entries
                // and return an empty span to mark as failure.
                return ReadOnlySpan<char>.Empty;
            }

            if (index > -1)
            {
                start = ind + 1;
            }
        }
        return str.AsSpan()[start..ind];
    }

    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminators">deliminators to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    /// <remarks>Inspired by the lovely Wren.</remarks>
    [Pure]
    [MethodImpl(Hot)]
    internal static ReadOnlySpan<char> GetNthChunk(this string str, char[] deliminators, int index = 0)
    {
                if (index < 0 || index > str.Length + 1)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        int start = 0;
        int ind = 0;
        while (index-- >= 0)
        {
            ind = str.IndexOfAny(deliminators, start);
            if (ind == -1)
            {
                // since we've previously decremented index, check against -1;
                // this means we're done.
                if (index == -1)
                {
                    return str.AsSpan()[start..];
                }

                // else, we've run out of entries
                // and return an empty span to mark as failure.
                return ReadOnlySpan<char>.Empty;
            }

            if (index > -1)
            {
                start = ind + 1;
            }
        }
        return str.AsSpan()[start..ind];
    }

    /// <summary>
    /// Gets the index of the next whitespace character.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <returns>Index of the whitespace character, or -1 if not found.</returns>
    [Pure]
    [MethodImpl(Hot)]
    internal static int GetIndexOfWhiteSpace(this string str)
        => str.AsSpan().GetIndexOfWhiteSpace();

    /// <summary>
    /// Gets the index of the next whitespace character.
    /// </summary>
    /// <param name="chars">ReadOnlySpan to search in.</param>
    /// <returns>Index of the whitespace character, or -1 if not found.</returns>
    [Pure]
    [MethodImpl(Hot)]
    internal static int GetIndexOfWhiteSpace(this ReadOnlySpan<char> chars)
    {
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the index of the last whitespace character.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <returns>Index of the whitespace character, or -1 if not found.</returns>
    [Pure]
    [MethodImpl(Hot)]
    internal static int GetLastIndexOfWhiteSpace(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return str.AsSpan().GetLastIndexOfWhiteSpace();
    }

    /// <summary>
    /// Gets the index of the last whitespace character.
    /// </summary>
    /// <param name="chars">ReadOnlySpan to search in.</param>
    /// <returns>Index of the whitespace character, or -1 if not found.</returns>
    [Pure]
    [MethodImpl(Hot)]
    internal static int GetLastIndexOfWhiteSpace(this ReadOnlySpan<char> chars)
    {
        for (int i = chars.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(chars[i]))
            {
                return i;
            }
        }
        return -1;
    }

    [Pure]
    [MethodImpl(Hot)]
    internal static bool TrySplitOnce(this string str, char? deliminator, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        ArgumentNullException.ThrowIfNull(str);
        return str.AsSpan().TrySplitOnce(deliminator, out first, out second);
    }

    [Pure]
    [MethodImpl(Hot)]
    internal static bool TrySplitOnce(this ReadOnlySpan<char> str, char? deliminator, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        int idx = deliminator is null ? str.GetIndexOfWhiteSpace() : str.IndexOf(deliminator.Value);

        if (idx < 0)
        {
            first = second = ReadOnlySpan<char>.Empty;
            return false;
        }

        first = str[..idx];
        second = str[(idx + 1)..];
        return true;
    }
}
