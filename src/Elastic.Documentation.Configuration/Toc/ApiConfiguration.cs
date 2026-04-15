// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Represents an API configuration entry that can be either a simple spec path (backward compatible)
/// or a complex configuration with template and multiple specs.
/// </summary>
public class ApiConfiguration
{
	/// <summary>
	/// Optional template file path for custom landing page content.
	/// If not specified, auto-generated landing page is used.
	/// </summary>
	[YamlMember(Alias = "template")]
	public string? Template { get; set; }

	/// <summary>
	/// OpenAPI specification file path (for single spec configuration).
	/// Mutually exclusive with Specs.
	/// </summary>
	[YamlMember(Alias = "spec")]
	public string? Spec { get; set; }

	/// <summary>
	/// Multiple OpenAPI specification file paths (for multi-spec configuration).
	/// Mutually exclusive with Spec.
	/// </summary>
	[YamlMember(Alias = "specs")]
	public List<string> Specs { get; set; } = [];

	/// <summary>
	/// Gets all specification paths, whether from Spec or Specs.
	/// </summary>
	public IEnumerable<string> GetSpecPaths()
	{
		if (!string.IsNullOrEmpty(Spec))
			yield return Spec;

		foreach (var spec in Specs)
			yield return spec;
	}

	/// <summary>
	/// Validates that the configuration is valid (has at least one spec, not both spec and specs).
	/// </summary>
	public bool IsValid => GetSpecPaths().Any() && !(Spec != null && Specs.Count > 0);
}

/// <summary>
/// Resolved API configuration with file system information.
/// </summary>
public class ResolvedApiConfiguration
{
	public required string ProductKey { get; init; }
	public IFileInfo? TemplateFile { get; init; }
	public required IReadOnlyList<IFileInfo> SpecFiles { get; init; }

	/// <summary>
	/// True if this configuration uses a custom template, false if it should use auto-generated landing page.
	/// </summary>
	public bool HasCustomTemplate => TemplateFile != null;

	/// <summary>
	/// Gets the primary spec file for backward compatibility (first spec in the list).
	/// </summary>
	public IFileInfo PrimarySpecFile => SpecFiles[0];
}
