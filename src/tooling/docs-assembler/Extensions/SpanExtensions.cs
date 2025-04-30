// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Assembler.Extensions;

public static class SpanExtensions
{
	public static string GetTrimmedRelativePath(this ReadOnlySpan<char> relativePath, string originalPath)
	{
		var trimmedInput = relativePath.TrimStart('/');
		var newRelativePath = trimmedInput.StartsWith(originalPath, StringComparison.Ordinal)
			? trimmedInput.Slice(originalPath.Length).TrimStart('/').ToString()
			: trimmedInput.ToString();
		return newRelativePath;
	}
}
