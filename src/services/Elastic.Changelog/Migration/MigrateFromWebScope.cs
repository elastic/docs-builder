// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Migration;

/// <summary>
/// TEMPORARY (elastic/docs-eng-team#736): the checked-in scope/cutoff list for
/// <c>changelog migrate-from-web</c>. Maps a product id to the repository, path, and pinned git ref
/// of the published release-notes Markdown, plus the inclusive version cutoff for the migration.
/// Delete together with the command once the rollout (elastic/docs-eng-team#683) completes.
/// </summary>
public sealed class MigrateFromWebScope
{
	public required string ProductId { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
	public required string Path { get; init; }
	public required string Ref { get; init; }
	public required string Cutoff { get; init; }

	/// <summary>
	/// Loads and validates the scope entry for <paramref name="productId"/> from the checked-in
	/// scope config at <paramref name="configPath"/>, or null (with errors emitted) when the file,
	/// product, or any required field is missing.
	/// </summary>
	public static MigrateFromWebScope? Load(IDiagnosticsCollector collector, IFileSystem fileSystem, string configPath, string productId)
	{
		if (!fileSystem.File.Exists(configPath))
		{
			collector.EmitError(configPath, "Scope config not found. The migrate-from-web scope list is checked into the docs-builder repository (config/migrate-from-web.yml); pass --config when running from elsewhere.");
			return null;
		}

		MigrateFromWebConfigDto? dto;
		try
		{
			var deserializer = new StaticDeserializerBuilder(new MigrationYamlContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();
			dto = deserializer.Deserialize<MigrateFromWebConfigDto>(fileSystem.File.ReadAllText(configPath));
		}
		catch (Exception ex) when (ex is YamlDotNet.Core.YamlException or IOException)
		{
			collector.EmitError(configPath, $"Could not parse scope config: {ex.Message}", ex);
			return null;
		}

		if (dto?.Products is null || !dto.Products.TryGetValue(productId, out var product) || product is null)
		{
			var known = dto?.Products is { Count: > 0 } ? string.Join(", ", dto.Products.Keys.Order(StringComparer.Ordinal)) : "<none>";
			collector.EmitError(configPath, $"Product '{productId}' is not in the migrate-from-web scope config. Configured products: {known}. Add an entry before running the migration.");
			return null;
		}

		if (!ChangelogKeys.IsValidProduct(productId))
		{
			collector.EmitError(configPath, $"Product id '{productId}' is not a valid bundle key segment (must match [a-zA-Z0-9_-]+).");
			return null;
		}

		var missing = new List<string>();
		if (string.IsNullOrWhiteSpace(product.Owner))
			missing.Add("owner");
		if (string.IsNullOrWhiteSpace(product.Repo))
			missing.Add("repo");
		if (string.IsNullOrWhiteSpace(product.Path))
			missing.Add("path");
		if (string.IsNullOrWhiteSpace(product.Ref))
			missing.Add("ref");
		if (string.IsNullOrWhiteSpace(product.Cutoff))
			missing.Add("cutoff");

		if (missing.Count > 0)
		{
			collector.EmitError(configPath, $"Scope entry for '{productId}' is missing required field(s): {string.Join(", ", missing)}.");
			return null;
		}

		return new MigrateFromWebScope
		{
			ProductId = productId,
			Owner = product.Owner!,
			Repo = product.Repo!,
			Path = product.Path!,
			Ref = product.Ref!,
			Cutoff = product.Cutoff!
		};
	}
}

/// <summary>Root DTO of the checked-in migrate-from-web scope config (product id → source/cutoff).</summary>
public sealed class MigrateFromWebConfigDto
{
	public Dictionary<string, MigrateFromWebProductDto?>? Products { get; set; }
}

/// <summary>One product's scope: where its published release-notes Markdown lives and the migration cutoff.</summary>
public sealed class MigrateFromWebProductDto
{
	/// <summary>GitHub owner of the source repository (e.g. <c>elastic</c>).</summary>
	public string? Owner { get; set; }

	/// <summary>Source repository name (e.g. <c>elastic-otel-java</c>).</summary>
	public string? Repo { get; set; }

	/// <summary>Repository-relative path of the release-notes Markdown page.</summary>
	public string? Path { get; set; }

	/// <summary>Pinned git ref (commit SHA) at which the Markdown is fetched.</summary>
	public string? Ref { get; set; }

	/// <summary>Inclusive upper version bound; releases above it belong to the live pipeline.</summary>
	public string? Cutoff { get; set; }
}

/// <summary>Source-generated YAML context for the migrate-from-web scope config (AOT-safe, no reflection).</summary>
[YamlStaticContext]
[YamlSerializable(typeof(MigrateFromWebConfigDto))]
[YamlSerializable(typeof(MigrateFromWebProductDto))]
public partial class MigrationYamlContext;
