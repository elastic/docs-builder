// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Global configuration has one owner (<see cref="GlobalConfigurationLoader"/>, lifetime: the process) and one way to report: errors stop the
/// process listing all of them, everything else travels as <see cref="StartupDiagnostics"/> that the first collector of the process drains.
/// </summary>
public class GlobalConfigurationLoaderTests
{
	private static ConfigurationFileProvider ProviderWith(params (string File, string Content)[] replacements)
	{
		var directory = Directory.CreateTempSubdirectory("global-configuration").FullName;
		foreach (var source in Directory.EnumerateFiles(BaselineFile("config")))
			File.Copy(source, Path.Join(directory, Path.GetFileName(source)));
		foreach (var (file, content) in replacements)
			File.WriteAllText(Path.Join(directory, file), content);
		return ProviderFor(directory);
	}

	private static Diagnostic Warning(string message) => new() { Severity = Severity.Warning, File = "versions.yml", Message = message };

	[Test]
	public void LoadOnce_LoadsEveryGlobalFileOncePerProvider()
	{
		var provider = BaselineProvider();

		var result = GlobalConfigurationLoader.LoadOnce(provider);

		result.HasErrors.Should().BeFalse();
		result.Value!.Assembly.Should().NotBeNull();
		result.Value.Catalog.Products.Should().NotBeNull();
		result.Value.Search.Should().NotBeNull();
		GlobalConfigurationLoader.LoadOnce(provider).Should().BeSameAs(result);
		GlobalConfigurationLoader.LoadOnce(BaselineProvider()).Should().NotBeSameAs(result);
	}

	[Test]
	public void OrThrow_ListsTheErrorsOfEveryFileNotOnlyTheFirst()
	{
		var provider = ProviderWith(
			("legacy-url-mappings.yml", "mappings:\n  /a:\n    product: not-a-product\n"),
			("assembler.yml", "references: [not, a, map]\n")
		);

		var act = () => GlobalConfigurationLoader.LoadOnce(provider).OrThrow();

		var exception = act.Should().Throw<ConfigurationException>().Which;
		var files = exception.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.File).ToList();
		files.Should().Contain(f => f.Contains("legacy-url-mappings.yml"));
		files.Should().Contain(f => f.Contains("assembler.yml"));
	}

	[Test]
	public void StartupDiagnostics_HoldsOnlyWhatIsNotAnError()
	{
		var provider = BaselineProvider();
		var result = GlobalConfigurationLoader.LoadOnce(provider);

		var startup = GlobalConfigurationLoader.StartupDiagnosticsOf(result);

		startup.Diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
		startup.Diagnostics.Count.Should().Be(result.Diagnostics.Count(d => d.Severity != Severity.Error));
	}

	/// <summary>
	/// Every command family (build/serve, codex, changelog, assembler...) resolves the one collector of its process, and the composition root
	/// drains the startup diagnostics when that collector is created: whichever family runs, the warnings are written, and exactly once.
	/// </summary>
	[Test]
	[Arguments("isolated build")]
	[Arguments("codex")]
	[Arguments("changelog")]
	[Arguments("assembler")]
	public void EveryCommandFamilyReportsTheStartupWarningsOnce(string family)
	{
		var startup = new StartupDiagnostics([Warning($"{family}: a catalogue warning")]);
		var collector = new CapturingDiagnosticsCollector();

		// the collector singleton is created by the first command that asks for it...
		_ = startup.DrainInto(collector);
		// ...and every later resolution in the same process (a command calling into another, a retry) writes nothing more
		_ = startup.DrainInto(collector);
		_ = startup.DrainInto(new CapturingDiagnosticsCollector());

		collector.Diagnostics.Should().ContainSingle(d => d.Message == $"{family}: a catalogue warning");
		collector.Warnings.Should().Be(1);
		startup.IsDrained.Should().BeTrue();
	}

	[Test]
	public void LogTo_IsTheCollectorlessHostsWayToDrain_AndAlsoOnlyOnce()
	{
		var startup = new StartupDiagnostics([Warning("w1"), Warning("w2")]);

		startup.LogTo(NullLogger.Instance).Should().Be(2);
		startup.LogTo(NullLogger.Instance).Should().Be(0);
		startup.DrainInto(new CapturingDiagnosticsCollector()).Should().Be(0);
	}

	[Test]
	public void ShippedConfiguration_StartsWithoutErrors_AndItsFindingsAreStartupDiagnostics()
	{
		var result = GlobalConfigurationLoader.LoadOnce(ProviderFor(BaselineFile("config")));

		var act = () => result.OrThrow();

		act.Should().NotThrow();
		GlobalConfigurationLoader.StartupDiagnosticsOf(result).Diagnostics.Should().OnlyContain(d => d.Severity != Severity.Error);
	}
}
