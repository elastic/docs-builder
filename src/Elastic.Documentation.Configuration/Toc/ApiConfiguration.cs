// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Configuration for API documentation generation from OpenAPI specifications.
/// </summary>
[YamlSerializable]
public class ApiConfiguration
{
	/// <summary>
	/// Path to a single OpenAPI specification file (for backward compatibility).
	/// Cannot be used together with <see cref="Specs"/>.
	/// </summary>
	[YamlMember(Alias = "spec")]
	public string? Spec { get; set; }

	/// <summary>
	/// Paths to multiple OpenAPI specification files (future feature).
	/// Cannot be used together with <see cref="Spec"/>.
	/// </summary>
	[YamlMember(Alias = "specs")]
	public List<string>? Specs { get; set; }

	/// <summary>
	/// Path to a Markdown template file to use as the API landing page.
	/// If not specified, an auto-generated landing page will be used.
	/// </summary>
	[YamlMember(Alias = "template")]
	public string? Template { get; set; }

	/// <summary>
	/// Validates that the configuration is valid.
	/// Must have at least one spec and cannot specify both spec and specs.
	/// </summary>
	public bool IsValid =>
		(Spec != null || (Specs?.Count > 0)) &&
		!(Spec != null && Specs?.Count > 0);

	/// <summary>
	/// Gets all specification file paths, handling both single spec and multi-spec configurations.
	/// </summary>
	public IEnumerable<string> GetSpecPaths()
	{
		if (Spec != null)
			yield return Spec;

		if (Specs?.Count > 0)
		{
			foreach (var spec in Specs)
				yield return spec;
		}
	}
}

/// <summary>
/// Resolved API configuration with validated file references.
/// </summary>
public class ResolvedApiConfiguration
{
	public required string ProductKey { get; init; }
	public IFileInfo? TemplateFile { get; init; }
	public required List<IFileInfo> SpecFiles { get; init; }

	/// <summary>
	/// Whether this configuration has a custom template file.
	/// </summary>
	public bool HasCustomTemplate => TemplateFile != null;

	/// <summary>
	/// Primary specification file (first in the list, for backward compatibility).
	/// </summary>
	public IFileInfo PrimarySpecFile => SpecFiles.First();
}
