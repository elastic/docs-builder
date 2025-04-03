// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Markdig.Helpers;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class StringSliceExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountAndSkipChar(this StringSlice slice, char matchChar)
	{
		var text = slice.Text;
		var end = slice.End;
		var current = slice.Start;

		while (current <= end && (uint)current < (uint)text.Length && text[current] == matchChar)
			current++;

		var count = current - slice.Start;
		slice.Start = current;
		return count;
	}
}
