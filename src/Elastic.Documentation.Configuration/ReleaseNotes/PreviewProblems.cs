// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>What a pull request lacks that puts its release note in the wrong place on the preview site.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PreviewProblemKind>))]
public enum PreviewProblemKind
{
	/// <summary>No label names the product, so the note shows under the product of the bundle profile, which may be wrong.</summary>
	MissingProductLabel,

	/// <summary>No label names the area, so the note has no area.</summary>
	MissingAreaLabel,

	/// <summary>No label names the type, so the note shows under Other changes.</summary>
	MissingTypeLabel
}

/// <summary>
/// A pull request of a preview release with a problem a writer can fix. A pull request of a private repository carries no
/// <see cref="Url"/> and no <see cref="Title"/>: the page names it as <c>owner/repo#number</c> and nothing more.
/// </summary>
public record PreviewProblem
{
	/// <summary>Product id of the bundle the pull request is in, such as <c>cloud-hosted</c>.</summary>
	public required string Product { get; init; }

	/// <summary>The release of the product: a version, or a preview label such as <c>staging</c> or <c>head</c>.</summary>
	public required string Release { get; init; }

	public required PreviewProblemKind Kind { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
	public required int Number { get; init; }

	/// <summary>Link to the pull request. <c>null</c> for a private repository.</summary>
	public string? Url { get; init; }

	/// <summary>Title of the pull request. <c>null</c> for a private repository.</summary>
	public string? Title { get; init; }

	/// <summary>What to add, such as <c>@Product:ECE, @Product:ECH</c>.</summary>
	public string? Labels { get; init; }
}

/// <summary>The content of <c>_diagnostics/preview-problems.json</c> in a bundles folder.</summary>
public record PreviewProblems
{
	public IReadOnlyList<PreviewProblem> Items { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PreviewProblems))]
public sealed partial class PreviewProblemsJsonContext : JsonSerializerContext;

/// <summary>Reads and writes the problems of a preview run next to its bundles. The site build reads them into a page.</summary>
public static class PreviewProblemsFile
{
	public const string RelativePath = "_diagnostics/preview-problems.json";

	public static void Write(IFileSystem fileSystem, string bundlesDirectory, IReadOnlyList<PreviewProblem> problems)
	{
		var path = fileSystem.Path.Join(bundlesDirectory, RelativePath);
		_ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
		fileSystem.File.WriteAllText(
			path,
			JsonSerializer.Serialize(new PreviewProblems { Items = problems }, PreviewProblemsJsonContext.Default.PreviewProblems)
		);
	}

	/// <summary>The problems of a run, or none when the folder has no file: bundles from another command carry no diagnostics.</summary>
	public static IReadOnlyList<PreviewProblem> Read(IFileSystem fileSystem, string bundlesDirectory)
	{
		var path = fileSystem.Path.Join(bundlesDirectory, RelativePath);
		if (!fileSystem.File.Exists(path))
			return [];

		return JsonSerializer.Deserialize(fileSystem.File.ReadAllText(path), PreviewProblemsJsonContext.Default.PreviewProblems)?.Items
			?? [];
	}
}
