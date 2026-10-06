// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Assembler;

public static class GlobalConfigurationFindings
{
	/// <summary>
	/// Emits the warnings and hints the global configuration loaders already returned (<c>assembler.yml</c>, <c>versions.yml</c>,
	/// <c>products.yml</c>, <c>legacy-url-mappings.yml</c>, <c>search.yml</c>). Nothing is checked here: the loaders own their
	/// constraints, and their errors already stopped the process when the configuration loaded.
	/// </summary>
	public static void Report(AssembleContext context)
	{
		var provider = context.ConfigurationFileProvider;
		foreach (var diagnostic in AssemblyLoader
			.LoadOnce(provider)
			.Diagnostics
			.Concat(CatalogLoader.LoadOnce(provider).Diagnostics)
			.Concat(SearchTuningLoader.LoadOnce(provider).Diagnostics)
			.Where(d => d.Severity != Severity.Error))
			context.Collector.Write(diagnostic);
	}
}
