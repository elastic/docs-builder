// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Model for the API Explorer code block partial (Myst-style highlight wrappers and copy support). <c>LineNumbers</c> opts into the shared <c>data-line-numbers</c> gutter.</summary>
public record ApiCodeBlockModel(string HighlightClass, string Source, bool LineNumbers = false)
{
	/// <summary>Lines the gutter numbers: one trailing newline does not count, and an empty block is one line.</summary>
	public static int LineCount(string source)
	{
		var text = source.ReplaceLineEndings("\n").AsSpan();
		if (text.EndsWith('\n'))
			text = text[..^1];
		return text.Count('\n') + 1;
	}
}
