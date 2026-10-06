// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>Everything the process-wide configuration files (<c>assembler.yml</c>, <c>versions.yml</c>, <c>products.yml</c>, <c>legacy-url-mappings.yml</c>, <c>search.yml</c>) describe.</summary>
public sealed record GlobalConfiguration(AssemblyConfiguration Assembly, GlobalCatalog Catalog, SearchTuning Search);

/// <summary>
/// The owner of global configuration (lifetime: the process). The one entry the composition roots use: it loads every global file once per
/// provider, and its single <see cref="Result{T}"/> carries every diagnostic of every file, so <see cref="Result{T}.OrThrow"/> lists every error
/// and not only the first file's. Warnings and hints do not stop anything; they travel as <see cref="StartupDiagnostics"/> to whoever owns a
/// collector.
/// </summary>
public static class GlobalConfigurationLoader
{
	private static readonly ConditionalWeakTable<ConfigurationFileProvider, Result<GlobalConfiguration>> LoadedByProvider = [];

	/// <summary>Loads (once per provider: its files are fixed for its lifetime) every global configuration file and reports all diagnostics together.</summary>
	public static Result<GlobalConfiguration> LoadOnce(ConfigurationFileProvider provider) => LoadedByProvider.GetValue(provider, Load);

	private static Result<GlobalConfiguration> Load(ConfigurationFileProvider provider)
	{
		var assembly = AssemblyLoader.LoadOnce(provider);
		var catalog = CatalogLoader.Load(provider);
		var search = SearchTuningLoader.Load(provider);
		IReadOnlyList<Diagnostic> diagnostics = [.. assembly.Diagnostics, .. catalog.Diagnostics, .. search.Diagnostics];

		return assembly.Value is { } a
			&& catalog.Value is { } c
			&& search.Value is { } s
			&& !diagnostics.Any(d => d.Severity == Severity.Error)
			? Result.Success(new GlobalConfiguration(a, c, s), diagnostics)
			: Result.Failure<GlobalConfiguration>(diagnostics);
	}

	/// <summary>The non-error diagnostics of <paramref name="result"/>, ready to be registered and drained by the owner of a collector.</summary>
	public static StartupDiagnostics StartupDiagnosticsOf(Result<GlobalConfiguration> result) =>
		new([.. result.Diagnostics.Where(d => d.Severity != Severity.Error)]);
}

/// <summary>
/// The warnings and hints global configuration produced while the process started, before any collector existed. A registered singleton:
/// the owner of a collector drains it into that collector, exactly once per process (<see cref="DrainInto"/>), so every command reports them
/// once. Hosts without a collector (the API, the MCP server, lambdas) call <see cref="LogTo"/> instead; ignoring them is also valid, but
/// is then a decision made in that host.
/// </summary>
public sealed class StartupDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
{
	private int _drained;

	/// <summary>Nothing to report.</summary>
	public StartupDiagnostics() : this([]) { }

	/// <summary>Every warning and hint, drained or not.</summary>
	public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;

	/// <summary>True once <see cref="DrainInto"/> or <see cref="LogTo"/> has run.</summary>
	public bool IsDrained => Volatile.Read(ref _drained) != 0;

	/// <summary>Writes every diagnostic to <paramref name="collector"/> the first time it is called in the process; later calls (another collector, another command) write nothing. Returns how many were written.</summary>
	public int DrainInto(IDiagnosticsCollector collector)
	{
		if (Interlocked.Exchange(ref _drained, 1) != 0)
			return 0;
		foreach (var diagnostic in Diagnostics)
			collector.Write(diagnostic);
		return Diagnostics.Count;
	}

	/// <summary>For hosts with no collector: logs every diagnostic the first time it is called, then does nothing.</summary>
	public int LogTo(ILogger logger)
	{
		if (Interlocked.Exchange(ref _drained, 1) != 0)
			return 0;
		foreach (var diagnostic in Diagnostics)
		{
			var level = diagnostic.Severity == Severity.Warning ? LogLevel.Warning : LogLevel.Information;
			logger.Log(level, "{File}: {Message}", diagnostic.File, diagnostic.Message);
		}
		return Diagnostics.Count;
	}
}
