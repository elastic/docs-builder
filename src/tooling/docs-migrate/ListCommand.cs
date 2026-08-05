// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Migrate;

internal sealed class ListCommand
{
	/// <summary>List all books from conf.yaml grouped by category.</summary>
	/// <param name="workDir">Working directory for migration artifacts</param>
	/// <param name="ct">Cancellation token</param>
	public async Task<int> List(string? workDir = null, CancellationToken ct = default)
	{
		var dir = SharedOptions.ResolveWorkDir(workDir);
		var conf = await SharedOptions.LoadConfAsync(dir, ct);

		foreach (var category in conf.Contents)
		{
			Console.WriteLine();
			Console.WriteLine($"== {category.Title} ==");
			Console.WriteLine();

			foreach (var book in category.Sections)
			{
				var versions = book.Branches;
				var min = versions.Count > 0 ? versions[^1].VersionLabel : "—";
				var max = versions.Count > 0 ? versions[0].VersionLabel : "—";
				var current = !string.IsNullOrEmpty(book.Current) ? book.Current : "—";

				Console.WriteLine(
					$"  {book.Prefix,-45} {book.Title,-40} {current,8} (current)  [{min} .. {max}]  {versions.Count} versions"
				);
			}
		}

		var totalBooks = conf.Contents.SelectMany(c => c.Sections).Count();
		Console.WriteLine();
		Console.WriteLine($"Total: {totalBooks} books across {conf.Contents.Count} categories");
		return 0;
	}
}
