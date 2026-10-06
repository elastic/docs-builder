// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Assembler;

/// <summary>
/// Owns the <see cref="AssembleContext"/> of a run: the first request for an environment (with the same collector,
/// checkout and output directory) creates it, later requests, from any service, get the same instance. The context
/// owns the navigation loader, so the navigation is parsed, checked and its diagnostics emitted once per run.
/// </summary>
/// <remarks>
/// Registered as a singleton next to <see cref="AssemblyConfiguration"/> and <see cref="IConfigurationContext"/>.
/// The file system of the first request wins: services of one run share the same checkout/output roots.
/// </remarks>
public sealed class AssembleContextProvider(AssemblyConfiguration configuration, IConfigurationContext configurationContext)
{
	private readonly Lock _lock = new();
	private readonly List<(Key Key, AssembleContext Context)> _contexts = [];

	/// <summary>The assembly configuration the contexts of this provider are built from.</summary>
	public AssemblyConfiguration Configuration { get; } = configuration;

	/// <summary>The configuration context the contexts of this provider are built from.</summary>
	public IConfigurationContext ConfigurationContext { get; } = configurationContext;

	/// <summary>The context for <paramref name="environment"/>: created on first request, shared afterwards.</summary>
	public AssembleContext Get(
		string environment,
		IDiagnosticsCollector collector,
		CheckoutsFileSystem fileSystem,
		string? checkoutDirectory = null,
		string? output = null
	)
	{
		var key = new Key(environment, collector, checkoutDirectory, output);
		lock (_lock)
		{
			foreach (var (existing, context) in _contexts)
			{
				if (existing.Equals(key))
					return context;
			}
			var created = new AssembleContext(
				Configuration,
				ConfigurationContext,
				environment,
				collector,
				fileSystem,
				checkoutDirectory,
				output
			);
			_contexts.Add((key, created));
			return created;
		}
	}

	private readonly record struct Key(string Environment, IDiagnosticsCollector Collector, string? CheckoutDirectory, string? Output)
	{
		public bool Equals(Key other) =>
			Environment == other.Environment
				&& ReferenceEquals(Collector, other.Collector)
				&& CheckoutDirectory == other.CheckoutDirectory
				&& Output == other.Output;

		public override int GetHashCode() => HashCode.Combine(Environment, CheckoutDirectory, Output);
	}
}
