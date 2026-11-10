// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>Where the journeys of a run come from: a folder the caller names, or the ones compiled into this binary.</summary>
internal static class JourneySource
{
	private static readonly Lazy<string> Embedded = new(Extract);

	public static string Folder(string? journeys) => journeys ?? Embedded.Value;

	private static string Extract()
	{
		var assembly = typeof(JourneySource).Assembly;
		var names = assembly
			.GetManifestResourceNames()
			.Select(n => n.Replace('\\', '/'))
			.Where(n => n.StartsWith("journeys/", StringComparison.Ordinal))
			.ToList();
		if (names.Count == 0)
			throw new InvalidOperationException("This build embeds no journeys. Pass --journeys <folder>.");

		var root = Directory.CreateTempSubdirectory("docs-journeys-").FullName;
		foreach (var name in names)
		{
			var path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
			_ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			using var input =
				assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(r => r.Replace('\\', '/') == name))!;
			using var output = File.Create(path);
			input.CopyTo(output);
		}

		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
		{
			try
			{
				Directory.Delete(root, recursive: true);
			}
			catch (IOException)
			{
				// A temporary folder that stays behind is not worth failing the run for.
			}
		};
		return Path.Combine(root, "journeys");
	}
}
