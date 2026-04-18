// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Represents a single entry in an API product sequence (either a file or spec).
/// </summary>
[YamlSerializable]
public class ApiProductEntry
{
	/// <summary>
	/// Path to a Markdown file for intro/outro content.
	/// </summary>
	[YamlMember(Alias = "file")]
	public string? File { get; set; }

	/// <summary>
	/// Path to an OpenAPI specification file.
	/// </summary>
	[YamlMember(Alias = "spec")]
	public string? Spec { get; set; }

	/// <summary>
	/// Whether this entry represents a Markdown file.
	/// </summary>
	public bool IsMarkdownFile => !string.IsNullOrWhiteSpace(File);

	/// <summary>
	/// Whether this entry represents an OpenAPI specification.
	/// </summary>
	public bool IsOpenApiSpec => !string.IsNullOrWhiteSpace(Spec);

	/// <summary>
	/// Gets the path for this entry (either file or spec).
	/// </summary>
	public string? GetPath() => File ?? Spec;

	/// <summary>
	/// Validates that this entry has exactly one of file or spec set.
	/// </summary>
	public bool IsValid => (IsMarkdownFile && !IsOpenApiSpec) || (!IsMarkdownFile && IsOpenApiSpec);
}

/// <summary>
/// Represents an API product configuration as a sequence of file/spec entries.
/// </summary>
[YamlSerializable]
public class ApiProductSequence
{
	/// <summary>
	/// Ordered list of file and spec entries.
	/// </summary>
	public List<ApiProductEntry> Entries { get; set; } = [];

	/// <summary>
	/// Gets all Markdown file entries that appear before the first spec.
	/// </summary>
	public IEnumerable<string> GetIntroMarkdownFiles()
	{
		foreach (var entry in Entries)
		{
			if (entry.IsOpenApiSpec)
				break;
			if (entry.IsMarkdownFile)
				yield return entry.File!;
		}
	}

	/// <summary>
	/// Gets all Markdown file entries that appear after the last spec.
	/// </summary>
	public IEnumerable<string> GetOutroMarkdownFiles()
	{
		var lastSpecIndex = -1;

		// Find the last spec entry
		for (var i = Entries.Count - 1; i >= 0; i--)
		{
			if (Entries[i].IsOpenApiSpec)
			{
				lastSpecIndex = i;
				break;
			}
		}

		// Return markdown files after the last spec
		if (lastSpecIndex >= 0)
		{
			for (var i = lastSpecIndex + 1; i < Entries.Count; i++)
			{
				if (Entries[i].IsMarkdownFile)
					yield return Entries[i].File!;
			}
		}
	}

	/// <summary>
	/// Gets all OpenAPI specification file paths.
	/// </summary>
	public IEnumerable<string> GetSpecPaths() => Entries
			.Where(e => e.IsOpenApiSpec)
			.Select(e => e.Spec!);

	/// <summary>
	/// Validates that the sequence has at least one spec and all entries are valid.
	/// </summary>
	public bool IsValid => Entries.All(e => e.IsValid) && Entries.Any(e => e.IsOpenApiSpec);
}

/// <summary>
/// Configuration for API documentation generation from OpenAPI specifications.
/// Legacy class maintained for backward compatibility.
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
	/// Validates that the configuration is valid.
	/// Must have a non-empty spec path. Multi-spec support is deferred to future implementation.
	/// </summary>
	public bool IsValid =>
		!string.IsNullOrWhiteSpace(Spec);

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

	/// <summary>
	/// Ordered list of Markdown files that appear before the first spec (intro content).
	/// </summary>
	public List<IFileInfo> IntroMarkdownFiles { get; init; } = [];

	/// <summary>
	/// OpenAPI specification files.
	/// </summary>
	public required List<IFileInfo> SpecFiles { get; init; }

	/// <summary>
	/// Ordered list of Markdown files that appear after the last spec (outro content).
	/// </summary>
	public List<IFileInfo> OutroMarkdownFiles { get; init; } = [];

	/// <summary>
	/// Whether this configuration has intro or outro markdown files.
	/// </summary>
	public bool HasSupplementaryContent => IntroMarkdownFiles.Count > 0 || OutroMarkdownFiles.Count > 0;

	/// <summary>
	/// Primary specification file (first in the list, for backward compatibility).
	/// </summary>
	public IFileInfo PrimarySpecFile => SpecFiles.First();

	/// <summary>
	/// Gets all Markdown file paths that should be excluded from normal HTML generation.
	/// </summary>
	public IEnumerable<string> GetMarkdownPathsToExclude()
	{
		foreach (var file in IntroMarkdownFiles)
			yield return Path.GetRelativePath(Environment.CurrentDirectory, file.FullName);
		foreach (var file in OutroMarkdownFiles)
			yield return Path.GetRelativePath(Environment.CurrentDirectory, file.FullName);
	}
}
