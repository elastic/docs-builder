// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Testing;

public static class DiagnosticsSnapshot
{
	/// <summary>
	/// One line per diagnostic, <c>error|warning|hint file:line message</c>, in the order they were written. Empty when
	/// there are none, so a clean run reads as <c>MatchInlineSnapshot("")</c>.
	/// </summary>
	public static string ToSnapshotText(this IEnumerable<Diagnostic> diagnostics)
	{
		var sb = new StringBuilder();
		foreach (var d in diagnostics)
		{
			var severity = d.Severity switch
			{
				Severity.Error => "error",
				Severity.Warning => "warning",
				_ => "hint"
			};
			var position = d.Line is { } line ? $"{d.File}:{line}" : d.File;
			sb.Append(severity).Append(' ').Append(position).Append(' ').Append(d.Message).Append('\n');
		}
		return sb.ToString();
	}
}
