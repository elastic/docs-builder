// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Assembler;

public static class GlobalConfigurationFindings
{
	/// <summary>
	/// Reports the warnings and hints the constraints find in <c>assembler.yml</c> and <c>versions.yml</c>. Their errors
	/// already stopped the process when the configuration loaded, so only the findings that let it load remain.
	/// </summary>
	public static void Report(AssembleContext context)
	{
		foreach (var diagnostic in AssemblyLoader
			.LoadOnce(context.ConfigurationFileProvider)
			.Diagnostics
			.Where(d => d.Severity != Severity.Error))
			context.Collector.Write(diagnostic);
		foreach (var violation in AssemblyConstraints.Check(context.Configuration).Where(v => v.Severity != Severity.Error))
			context.Collector.Write(violation.ToDiagnostic(context.ConfigurationFileProvider.AssemblerFile.FullName));
		foreach (var violation in VersioningSystemConstraints.Check(context.Catalog.VersioningSystems))
			context.Collector.Write(violation.ToDiagnostic(context.ConfigurationFileProvider.VersionFile.FullName));
	}
}
