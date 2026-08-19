// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.GitDiff;

internal static class GitDiffPathNormalization
{
	public static string Normalize(string path) =>
		path.Replace('\\', '/').TrimStart('.').TrimStart('/');

	public static bool TryToDocsetRelative(string repoRelativePath, string docsetPrefix, out string relative)
	{
		var normalized = Normalize(repoRelativePath);
		var prefix = Normalize(docsetPrefix);
		if (string.IsNullOrEmpty(prefix))
		{
			relative = normalized;
			return true;
		}

		if (normalized.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
		{
			relative = normalized[(prefix.Length + 1)..];
			return true;
		}

		if (string.Equals(normalized, prefix, StringComparison.OrdinalIgnoreCase))
		{
			relative = string.Empty;
			return true;
		}

		relative = string.Empty;
		return false;
	}

	public static bool IsMarkdownPagePath(string path) =>
		path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
		&& !path.Contains("/_snippets/", StringComparison.OrdinalIgnoreCase)
		&& !path.StartsWith("_snippets/", StringComparison.OrdinalIgnoreCase);
}
