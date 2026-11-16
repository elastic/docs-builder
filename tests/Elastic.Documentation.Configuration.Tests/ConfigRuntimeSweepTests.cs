// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class ConfigRuntimeSweepTests
{
	private const string Marker = ".in-use";

	private static string NewParent()
	{
		var parent = Path.Join(Path.GetTempPath(), "config-runtime-sweep-" + Guid.NewGuid().ToString("N"));
		_ = Directory.CreateDirectory(parent);
		return parent;
	}

	private static string NewRuntimeDir(string parent, bool withMarker)
	{
		var dir = Path.Join(parent, Guid.NewGuid().ToString("N"));
		_ = Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Join(dir, "navigation.filtered.yml"), "toc: []");
		if (withMarker)
			File.WriteAllText(Path.Join(dir, Marker), string.Empty);
		return dir;
	}

	[Test]
	public void Sweep_OldDirectoryWhoseMarkerIsFree_IsDeleted()
	{
		var parent = NewParent();
		try
		{
			var orphan = NewRuntimeDir(parent, withMarker: true);

			ConfigurationFileProvider.SweepStaleConfigRuntimeDirs(parent, DateTime.UtcNow.AddMinutes(10));

			Directory.Exists(orphan).Should().BeFalse("no process holds the marker, so the directory is a leftover");
		}
		finally
		{
			Directory.Delete(parent, recursive: true);
		}
	}

	[Test]
	public void Sweep_OldDirectoryWhoseMarkerIsHeld_IsKept()
	{
		var parent = NewParent();
		try
		{
			var live = NewRuntimeDir(parent, withMarker: true);
			using var held = new FileStream(Path.Join(live, Marker), FileMode.Open, FileAccess.Write, FileShare.None);

			// A day later: a process that runs this long still owns the directory.
			ConfigurationFileProvider.SweepStaleConfigRuntimeDirs(parent, DateTime.UtcNow.AddDays(1));

			Directory.Exists(live).Should().BeTrue("a live process holds the marker");
			File.Exists(Path.Join(live, "navigation.filtered.yml")).Should().BeTrue();
		}
		finally
		{
			Directory.Delete(parent, recursive: true);
		}
	}

	[Test]
	public void Sweep_NewDirectoryWhoseMarkerIsFree_IsKept()
	{
		var parent = NewParent();
		try
		{
			var fresh = NewRuntimeDir(parent, withMarker: true);

			ConfigurationFileProvider.SweepStaleConfigRuntimeDirs(parent, DateTime.UtcNow.AddMinutes(1));

			Directory.Exists(fresh).Should().BeTrue("its owner may not have taken the marker yet");
		}
		finally
		{
			Directory.Delete(parent, recursive: true);
		}
	}

	[Test]
	[Arguments(30, true)]
	[Arguments(120, false)]
	public void Sweep_DirectoryWithoutMarker_IsDeletedOnlyPastAnHour(int ageMinutes, bool expectedToSurvive)
	{
		var parent = NewParent();
		try
		{
			var unmarked = NewRuntimeDir(parent, withMarker: false);

			ConfigurationFileProvider.SweepStaleConfigRuntimeDirs(parent, DateTime.UtcNow.AddMinutes(ageMinutes));

			Directory.Exists(unmarked).Should().Be(expectedToSurvive);
		}
		finally
		{
			Directory.Delete(parent, recursive: true);
		}
	}

	[Test]
	public void Sweep_ParentDirectoryMissing_DoesNotThrow()
	{
		var missing = Path.Join(Path.GetTempPath(), "config-runtime-sweep-missing-" + Guid.NewGuid().ToString("N"));

		var sweep = () => ConfigurationFileProvider.SweepStaleConfigRuntimeDirs(missing, DateTime.UtcNow);

		sweep.Should().NotThrow();
	}
}
