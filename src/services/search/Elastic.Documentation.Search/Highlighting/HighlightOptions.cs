// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Documentation.Search.Highlighting;

/// <summary>Controls how the post-ES <c>&lt;mark&gt;</c> pass treats highlight candidate tokens.</summary>
public record HighlightOptions
{
	/// <summary>When true, only highlights complete words (requires word boundaries at both ends).</summary>
	public bool WholeWordOnly { get; init; }

	/// <summary>Minimum token length required for highlighting. Shorter tokens are skipped.</summary>
	public int MinTokenLength { get; init; }

	/// <summary>Tokens matching this regex are skipped (used to drop question stop-words on full search).</summary>
	public Regex? ExcludePattern { get; init; }

	/// <summary>How many direct children of the typed field an API hit lists after the row they are listed under; none by default.</summary>
	public int MaxChildProperties { get; init; }

	public static HighlightOptions Default => new();
}
