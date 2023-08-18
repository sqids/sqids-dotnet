#if NETSTANDARD2_0

using System.Runtime.InteropServices;

namespace Sqids;

internal static class PolyfillExtensions
{
	public static StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> value)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		if (!value.IsEmpty)
		{
			unsafe
			{
				fixed (char* p = &MemoryMarshal.GetReference(value))
				{
					builder.Append(p, value.Length);
				}
			}
		}

		return builder;
	}

	public static StringBuilder Insert(this StringBuilder builder, int index, ReadOnlySpan<char> value)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		if (!value.IsEmpty)
		{
			// NOTE: Unfortunately, not much we can do here; the StringBuilder.Insert(int, char*, int) method is private.
			char[] temp = new char[value.Length];
			value.CopyTo(temp);
			builder.Insert(index, temp);
		}

		return builder;
	}

	public static bool Contains(this Span<char> source, char toFind) =>
		source.IndexOf(toFind) != -1;
}

#endif
