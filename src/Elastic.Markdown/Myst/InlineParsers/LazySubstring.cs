// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Markdown.Myst.InlineParsers;

internal struct LazySubstring
{
	private string _text;
	public int Offset;
	public int Length;

	public LazySubstring(string text)
	{
		_text = text;
		Offset = 0;
		Length = text.Length;
	}

	public LazySubstring(string text, int offset, int length)
	{
		Debug.Assert((ulong)offset + (ulong)length <= (ulong)text.Length, $"{offset}-{length} in {text}");
		_text = text;
		Offset = offset;
		Length = length;
	}

	public readonly ReadOnlySpan<char> AsSpan() => _text.AsSpan(Offset, Length);

	public override string ToString()
	{
		if (Offset != 0 || Length != _text.Length)
		{
			_text = _text.Substring(Offset, Length);
			Offset = 0;
		}

		return _text;
	}
}
