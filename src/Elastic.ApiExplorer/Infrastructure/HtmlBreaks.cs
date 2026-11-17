// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Text a narrow cell may wrap, but only at the places a reader would break it, never in the middle of a word.</summary>
public static class HtmlBreaks
{
	/// <summary>The encoded <paramref name="text"/> with a break opportunity after each <paramref name="separator"/>.</summary>
	public static HtmlString After(string text, char separator)
	{
		var encoded = WebUtility.HtmlEncode(text);
		return new(
			text.Contains(separator) ? encoded.Replace(separator.ToString(), $"{separator}<wbr>", StringComparison.Ordinal) : encoded
		);
	}
}
