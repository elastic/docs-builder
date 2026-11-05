// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests;

internal static class SnapshotSetup
{
	// Most tests work in a folder named after a fresh Guid, and its path shows up in file paths in the output.
	// Number the Guids (Guid_1, Guid_2, ...) so that the same folder reads the same everywhere in one snapshot.
	[ModuleInitializer]
	internal static void Register()
	{
		SnapshotDefaults.AddScrubber(Scrubbers.Guids);
		SnapshotDefaults.AddScrubber(Today);
	}

	// A bundle is stamped with the day it was created. Only that day is replaced, so a release-date a test sets on purpose stays visible.
	private static string Today(string text)
	{
		const string key = "release-date: ";
		if (!text.Contains(key, StringComparison.Ordinal))
			return text;
		var days = new[] { DateTime.UtcNow, DateTime.Now }
			.Select(d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
			.Distinct()
			.ToArray();
		return string.Join(
			'\n',
			text.Split('\n').Select(
				line => line.StartsWith(key, StringComparison.Ordinal) && days.Contains(line[key.Length..].Trim()) ? key + "{today}" : line
			)
		);
	}
}
