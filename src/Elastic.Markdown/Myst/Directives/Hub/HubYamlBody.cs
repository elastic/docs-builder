// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Helpers for directives that read their body as raw YAML directly from the
/// source file. The directive's children remain markdown blocks (so renderers
/// that don't recognize the directive still produce something), but the
/// canonical structured data comes from the YAML.
/// </summary>
internal static class HubYamlBody
{
	/// <summary>
	/// Returns the raw text between the opening and closing fences of a directive
	/// block, or null when no fenced body could be located.
	/// </summary>
	public static string? Extract(IBlockExtension block, IFileSystemFileReader reader)
	{
		if (block is not Block markdig)
			return null;

		string source;
		try
		{
			source = reader.ReadAllText(block.CurrentFile.FullName);
		}
		catch
		{
			return null;
		}

		var lines = source.Split('\n');
		var openingLine = markdig.Line;
		if (openingLine < 0 || openingLine >= lines.Length)
			return null;

		var fence = ExtractFenceMarker(lines[openingLine]);
		if (fence is null)
			return null;

		var closingLine = -1;
		for (var i = openingLine + 1; i < lines.Length; i++)
		{
			var trimmed = lines[i].TrimStart();
			if (trimmed.StartsWith(fence, StringComparison.Ordinal) && IsClosingFence(trimmed, fence))
			{
				closingLine = i;
				break;
			}
		}
		if (closingLine < 0)
			return null;

		var body = string.Join('\n', lines, openingLine + 1, closingLine - openingLine - 1);
		return string.IsNullOrWhiteSpace(body) ? null : body;
	}

	private static string? ExtractFenceMarker(string openingLine)
	{
		var trimmed = openingLine.TrimStart();
		var count = 0;
		while (count < trimmed.Length && trimmed[count] == ':')
			count++;
		return count >= 3 ? new string(':', count) : null;
	}

	private static bool IsClosingFence(string trimmed, string fence)
	{
		if (!trimmed.StartsWith(fence, StringComparison.Ordinal))
			return false;
		for (var i = fence.Length; i < trimmed.Length; i++)
		{
			if (!char.IsWhiteSpace(trimmed[i]))
				return false;
		}
		return true;
	}
}

/// <summary>
/// Minimal abstraction over file reading so HubYamlBody can be tested without a
/// full <c>BuildContext</c>.
/// </summary>
public interface IFileSystemFileReader
{
	string ReadAllText(string path);
}

internal sealed class BuildContextFileReader(System.IO.Abstractions.IFileSystem fileSystem) : IFileSystemFileReader
{
	public string ReadAllText(string path) => fileSystem.File.ReadAllText(path);
}
