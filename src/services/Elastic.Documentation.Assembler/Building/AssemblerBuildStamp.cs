// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Markdown;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// On-disk representation of the inputs that produced a given assembler build output.
/// All sensitive values (checkout SHAs, assembly MVIDs) are stored as hashes, not raw.
/// </summary>
public sealed record AssemblerBuildStampRecord
{
	/// <summary>Increment to unconditionally invalidate all existing stamp files.</summary>
	public required int SchemaVersion { get; init; }

	/// <summary>Named deployment environment (dev, staging, production, …).</summary>
	public required string Environment { get; init; }

	/// <summary>SHA-256 of all checkout repository names and HEAD SHAs (sorted).</summary>
	public required string CheckoutsHash { get; init; }

	/// <summary>SHA-256 of the concatenated content of all configuration files.</summary>
	public required string ConfigurationHash { get; init; }

	/// <summary>SHA-256 of all assembly names and their ModuleVersionIds (sorted).</summary>
	public required string AssembliesHash { get; init; }

	/// <summary>Sorted exporter names that were part of the build.</summary>
	public required IReadOnlyCollection<string> Exporters { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(AssemblerBuildStampRecord))]
internal sealed partial class AssemblerBuildStampJsonContext : JsonSerializerContext;

/// <summary>
/// In-memory representation of computed stamp inputs. Never serialised to disk.
/// Raw values are kept here so <see cref="AssemblerBuildStampService"/> can produce
/// informative miss messages without requiring the on-disk record to contain them.
/// </summary>
internal sealed record AssemblerBuildStamp
{
	public required int SchemaVersion { get; init; }
	public required string Environment { get; init; }
	public required IReadOnlyDictionary<string, string> Checkouts { get; init; }
	public required string ConfigurationHash { get; init; }
	public required IReadOnlyDictionary<string, string> Assemblies { get; init; }
	public required IReadOnlyCollection<string> Exporters { get; init; }

	/// <summary>Projects this stamp into the on-disk record by hashing all sensitive fields.</summary>
	public AssemblerBuildStampRecord ToRecord() =>
		new()
		{
			SchemaVersion = SchemaVersion,
			Environment = Environment,
			CheckoutsHash = HashDictionary(Checkouts),
			ConfigurationHash = ConfigurationHash,
			AssembliesHash = HashDictionary(Assemblies),
			Exporters = Exporters
		};

	private static string HashDictionary(IReadOnlyDictionary<string, string> dict)
	{
		using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		foreach (var (k, v) in dict.OrderBy(p => p.Key, StringComparer.Ordinal))
		{
			sha.AppendData(Encoding.UTF8.GetBytes(k));
			sha.AppendData(Encoding.UTF8.GetBytes(v));
		}
		return Convert.ToHexString(sha.GetCurrentHash()).ToLowerInvariant();
	}
}

/// <summary>
/// Computes, reads, and writes the <see cref="AssemblerBuildStamp"/>.
/// All methods are pure over already-gathered inputs so they can be unit-tested
/// without going through <see cref="AssemblerBuildService"/>.
/// </summary>
internal static class AssemblerBuildStampService
{
	private const int CurrentSchemaVersion = 1;
	internal const string StampFileName = ".assembler-build-stamp.json";

	/// <summary>
	/// Computes a fresh stamp from the resolved inputs.
	/// Returns <c>null</c> if any checkout has an empty HEAD reference (clone failure /
	/// path override not yet resolved), which forces a rebuild.
	/// </summary>
	internal static AssemblerBuildStamp? Compute(
		string environment,
		IEnumerable<Checkout> checkouts,
		ConfigurationFileProvider configFileProvider,
		IReadOnlySet<Exporter> exporters
	)
	{
		var checkoutMap = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var c in checkouts)
		{
			if (string.IsNullOrEmpty(c.HeadReference))
				return null; // unresolved checkout → must rebuild
			checkoutMap[c.Repository.Name] = c.HeadReference;
		}

		var configHash = ComputeConfigurationHash(configFileProvider);
		var assemblies = CollectAssemblyMvids();
		var exporterNames = exporters.Select(e => e.ToString()).OrderBy(e => e).ToArray();

		return new AssemblerBuildStamp
		{
			SchemaVersion = CurrentSchemaVersion,
			Environment = environment,
			Checkouts = checkoutMap,
			ConfigurationHash = configHash,
			Assemblies = assemblies,
			Exporters = exporterNames
		};
	}

	/// <summary>
	/// Compares an existing on-disk record with a freshly computed stamp.
	/// Returns <c>true</c> when the build output is up to date, along with a
	/// human-readable reason for logging on a miss.
	/// </summary>
	internal static (bool IsUpToDate, string Reason) IsUpToDate(AssemblerBuildStampRecord? existing, AssemblerBuildStamp? current)
	{
		if (existing is null)
			return (false, "no stamp found (first run or output was deleted)");
		if (current is null)
			return (false, "one or more checkouts have an unresolved HEAD reference");
		if (existing.SchemaVersion != current.SchemaVersion)
			return (false, $"stamp schema changed ({existing.SchemaVersion} → {current.SchemaVersion})");
		if (existing.Environment != current.Environment)
			return (false, $"environment changed ({existing.Environment} → {current.Environment})");
		if (!existing.Exporters.SequenceEqual(current.Exporters))
			return (false, $"exporters changed ({string.Join(",", existing.Exporters)} → {string.Join(",", current.Exporters)})");
		if (existing.ConfigurationHash != current.ConfigurationHash)
			return (false, "configuration files changed");

		var currentRecord = current.ToRecord();
		if (existing.CheckoutsHash != currentRecord.CheckoutsHash)
			return (false, "one or more checkout HEAD references changed");
		if (existing.AssembliesHash != currentRecord.AssembliesHash)
			return (false, "one or more assembly MVIDs changed (code was rebuilt)");

		return (true, "stamp matches");
	}

	internal static async Task<AssemblerBuildStampRecord?> ReadAsync(string stampPath, CancellationToken ct)
	{
		if (!File.Exists(stampPath))
			return null;
		try
		{
			await using var stream = File.OpenRead(stampPath);
			return await JsonSerializer.DeserializeAsync(stream, AssemblerBuildStampJsonContext.Default.AssemblerBuildStampRecord, ct);
		}
		catch
		{
			return null;
		}
	}

	internal static async Task WriteAsync(string stampPath, AssemblerBuildStamp stamp, CancellationToken ct)
	{
		var record = stamp.ToRecord();
		var json = JsonSerializer.Serialize(record, AssemblerBuildStampJsonContext.Default.AssemblerBuildStampRecord);
		await File.WriteAllTextAsync(stampPath, json, ct);
	}

	// ── private helpers ───────────────────────────────────────────────────────

	private static string ComputeConfigurationHash(ConfigurationFileProvider provider)
	{
		// Hash content + filename (not path, which is ephemeral) in a fixed order
		using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		var files = new[]
		{
			provider.NavigationFile,
			provider.VersionFile,
			provider.ProductsFile,
			provider.AssemblerFile,
			provider.LegacyUrlMappingsFile,
			provider.SearchFile
		};
		foreach (var file in files)
		{
			sha.AppendData(Encoding.UTF8.GetBytes(file.Name));
			if (file.Exists)
				sha.AppendData(File.ReadAllBytes(file.FullName));
		}
		return Convert.ToHexString(sha.GetCurrentHash()).ToLowerInvariant();
	}

	private static IReadOnlyDictionary<string, string> CollectAssemblyMvids()
	{
		// Explicit typeof anchors — order-independent and AOT-safe.
		// Elastic.Documentation.Site MVID changes whenever Parcel output changes,
		// since EmbedGeneratedAssets bakes the bundled assets in as EmbeddedResources.
		var anchors = new (string Name, Type Anchor)[]
		{
			("Elastic.Markdown", typeof(HtmlWriter)),
			("Elastic.Documentation.Configuration", typeof(ConfigurationFileProvider)),
			("Elastic.Documentation.Navigation", typeof(Elastic.Documentation.Navigation.NavigationHomeProvider)),
			("Elastic.Documentation.Assembler", typeof(AssemblerBuildService)),
			("Elastic.Documentation.Site", typeof(Elastic.Documentation.Site.Htmx)),
		};
		var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var (name, anchor) in anchors)
			map[name] = anchor.Assembly.ManifestModule.ModuleVersionId.ToString("D");
		return map;
	}

	/// <summary>Logs stamp comparison result at the appropriate level.</summary>
	internal static void LogResult(ILogger logger, bool isUpToDate, string reason)
	{
		if (isUpToDate)
			logger.LogInformation("Build stamp matches — skipping assembler build ({Reason})", reason);
		else
			logger.LogInformation("Build stamp miss — rebuilding: {Reason}", reason);
	}
}
