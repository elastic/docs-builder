// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>A journey file with its parsed journey, or the reason it could not be parsed.</summary>
internal sealed record LoadedJourney(JourneyFile File, Journey? Journey, string? Error)
{
	public static LoadedJourney Load(JourneyFile file)
	{
		try
		{
			return new LoadedJourney(file, Journey.Load(file.Path), null);
		}
		catch (Exception e) when (e is JourneyParseException or IOException)
		{
			return new LoadedJourney(file, null, e.Message);
		}
	}
}

/// <summary>What narrows a run: tags, what the site is, and whether journeys that write may run.</summary>
internal sealed record Selection(IReadOnlySet<string> Tags, JourneyContext Context, bool AllowMutations)
{
	/// <summary>Why <paramref name="journey"/> does not run in this selection, or <c>null</c> when it does.</summary>
	public string? SkipReason(Journey journey)
	{
		var metadata = journey.Metadata;
		if (Tags.Count > 0 && !metadata.Tags.Any(Tags.Contains))
			return $"no tag in [{string.Join(' ', Tags.Order(StringComparer.Ordinal))}]";
		if (metadata.Unmet(Context) is { } unmet)
			return $"requires {unmet}";
		return metadata.Mutates && !AllowMutations ? "mutates data; a read-only run skips it" : null;
	}
}

internal static class ChangedFiles
{
	/// <summary>The full paths of journeys under <paramref name="folder"/> that differ from <paramref name="gitRef"/>, including new ones.</summary>
	public static IReadOnlySet<string> Since(string folder, string gitRef)
	{
		var changed = Git(folder, "diff", "-z", "--name-only", "--relative", gitRef, "--", ".")
			.Split('\0', StringSplitOptions.RemoveEmptyEntries)
			.Concat(
				Git(folder, "ls-files", "-z", "--others", "--exclude-standard", "--", ".").Split(
					'\0',
					StringSplitOptions.RemoveEmptyEntries
				)
			);
		return changed
			.Where(p => p.EndsWith(".journey", StringComparison.Ordinal))
			.Select(p => Path.GetFullPath(Path.Combine(folder, p)))
			.ToHashSet(StringComparer.Ordinal);
	}

	private static string Git(string folder, params string[] arguments)
	{
		var info = new ProcessStartInfo("git") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		using var process = Process.Start(info) ?? throw new InvalidOperationException("git did not start.");
		var output = process.StandardOutput.ReadToEnd();
		var error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return process.ExitCode == 0
			? output
			: throw new ArgumentException($"git {string.Join(' ', arguments)} failed in {folder}: {error.Trim()}");
	}
}
