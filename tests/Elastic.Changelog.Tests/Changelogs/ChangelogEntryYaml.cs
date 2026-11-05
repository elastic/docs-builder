// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;

namespace Elastic.Changelog.Tests.Changelogs;

public static class ChangelogEntryYaml
{
	/// <summary>
	/// A changelog file <c>changelog create</c> wrote starts with the same documentation of every field. This is the entry
	/// itself: everything after that block, so a snapshot of it is about the entry and does not change with the field docs.
	/// Text without the block is returned as it is.
	/// </summary>
	public static string WithoutSchemaComments(this string yaml)
	{
		// the writer uses the platform's line ending, so on Windows the blank line that ends the header is "\r\n\r\n"
		yaml = yaml.Replace("\r\n", "\n");
		var subtype = yaml.IndexOf("# subtype:", StringComparison.Ordinal);
		if (subtype < 0)
			return yaml;
		var end = yaml.IndexOf("\n\n", subtype, StringComparison.Ordinal);
		return end < 0 ? yaml : yaml[(end + 2)..];
	}

	/// <summary>
	/// The entries in <paramref name="directory"/>, sorted by file name, each under an <c>=== name ===</c> header and
	/// without the field documentation. For the tests that create one file per pull request, whose names are stable.
	/// </summary>
	public static string DumpEntries(IFileSystem fileSystem, string directory, string searchPattern = "*.yaml")
	{
		var sb = new StringBuilder();
		foreach (var file in fileSystem.Directory.GetFiles(directory, searchPattern).OrderBy(f => f, StringComparer.Ordinal))
		{
			var content = fileSystem.File.ReadAllText(file).Replace("\r\n", "\n").WithoutSchemaComments();
			_ = sb.Append("=== ").Append(fileSystem.Path.GetFileName(file)).Append(" ===\n").Append(content);
			if (!content.EndsWith('\n'))
				_ = sb.Append('\n');
		}
		return sb.ToString();
	}
}
