// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>Human-readable rendering of a <see cref="RegistryStateSnapshot"/> for CLI output.</summary>
public static class RegistryStateFormatter
{
	/// <summary>Logs the snapshot's health, counts, every divergence, and every diagnostic.</summary>
	public static void Log(ILogger logger, RegistryStateSnapshot snapshot)
	{
		logger.LogInformation(
			"Scope {ScopeKind}/{Scope} in {Bucket}: registry {Health}, {ObjectCount} object(s), {EntryCount} registry entr(ies), {DivergenceCount} divergence(s)",
			snapshot.ScopeKind, snapshot.Scope, snapshot.Bucket,
			snapshot.RegistryHealth, snapshot.Objects.Count, snapshot.RegistryEntries.Count, snapshot.Divergences.Count);

		foreach (var divergence in snapshot.Divergences)
			logger.LogWarning("[{Kind}] {File}: {Detail}{Values}", divergence.Kind, divergence.File, divergence.Detail, FormatValues(divergence));

		foreach (var diagnostic in snapshot.Diagnostics)
			logger.LogInformation("{Diagnostic}", diagnostic);

		if (snapshot.IsClean)
			logger.LogInformation("Scope is clean: the registry matches the actual objects.");
	}

	private static string FormatValues(RegistryDivergence divergence)
	{
		var parts = new List<string>(2);
		if (divergence.RegistryETag is not null || divergence.ObjectETag is not null)
			parts.Add($"etag registry={divergence.RegistryETag ?? "<none>"} object={divergence.ObjectETag ?? "<none>"}");
		if (divergence.RegistryTarget is not null || divergence.ObjectTarget is not null)
			parts.Add($"target registry={divergence.RegistryTarget ?? "<none>"} object={divergence.ObjectTarget ?? "<none>"}");
		return parts.Count > 0 ? $" ({string.Join("; ", parts)})" : string.Empty;
	}
}
