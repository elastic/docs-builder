// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Markdown.Myst.CodeBlocks;

public static partial class CallOutParser
{
	[GeneratedRegex(@"^.+\S+.*?\s<\d+>$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex CallOutNumber();

	[GeneratedRegex(@"^.+\S+.*?\s(?:\/\/|#)\s[^""\/#]+$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MathInlineAnnotation();
}
