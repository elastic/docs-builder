// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>A journey file and the area (its folder) it belongs to.</summary>
internal sealed record JourneyFile(string Area, string Name, string Path)
{
	public string Id => $"{Area}/{Name}";

	/// <summary>
	/// Where to open this journey: its path relative to the working directory, or for a journey compiled into the binary the
	/// matching file of the checkout when the working directory is inside one, and <c>journeys/...</c> otherwise.
	/// </summary>
	public string DisplayPath(string journeysRoot)
	{
		var relative = System.IO.Path.GetRelativePath(journeysRoot, Path).Replace('\\', '/');
		if (JourneySource.IsEmbedded(journeysRoot))
			return JourneySource.CheckoutJourneys() is { } checkout
				? Relative(System.IO.Path.Combine(checkout, relative))
				: $"journeys/{relative}";
		return Relative(Path);
	}

	private static string Relative(string path)
	{
		var relative = System.IO.Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace('\\', '/');
		return relative.StartsWith("..", StringComparison.Ordinal) ? path : relative;
	}

	/// <summary>The journeys in <paramref name="root"/>; filtered to <paramref name="areas"/> when given.</summary>
	public static IReadOnlyList<JourneyFile> Find(string root, string? areas)
	{
		if (!Directory.Exists(root))
			throw new DirectoryNotFoundException($"No journeys folder at {root}.");

		var wanted = (areas ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(
			StringComparer.OrdinalIgnoreCase
		);
		var files = Directory
			.EnumerateFiles(root, "*.journey", SearchOption.AllDirectories)
			.Select(path =>
			{
				var relative = System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
				var slash = relative.IndexOf('/', StringComparison.Ordinal);
				var area = slash < 0 ? "" : relative[..slash];
				var name = System.IO.Path.GetFileNameWithoutExtension(relative);
				return new JourneyFile(area, name, path);
			})
			.Where(f => wanted.Count == 0 || wanted.Contains(f.Area))
			.OrderBy(f => f.Id, StringComparer.Ordinal)
			.ToList();

		var unknown = wanted.Where(a => !files.Any(f => f.Area.Equals(a, StringComparison.OrdinalIgnoreCase))).ToList();
		return unknown.Count > 0 ? throw new ArgumentException($"No journeys in area {string.Join(", ", unknown)}.") : files;
	}
}
