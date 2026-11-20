// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Runtime.ExceptionServices;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Suggestions;
using Nullean.Entities;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>A <c>cross_links</c> entry that names a repository the set can link to.</summary>
public sealed record CrossLinkAccepted(CrossLinkEntry Entry);

/// <summary>A <c>cross_links</c> entry the set cannot use, with the finding it is reported with.</summary>
public sealed record CrossLinkRejected(string Problem);

/// <summary>What a <c>cross_links</c> entry makes of itself: a <see cref="CrossLinkEntry"/> or the reason it is dropped.</summary>
public readonly union CrossLinkResolution(CrossLinkAccepted, CrossLinkRejected);

/// <summary>A <c>release_notes</c> entry that names a product taking part in the release notes.</summary>
public sealed record ReleaseNotesAccepted(string ProductId);

/// <summary>A <c>release_notes</c> entry that is dropped, with the finding it is reported with.</summary>
public sealed record ReleaseNotesRejected(string Problem);

/// <summary>What a <c>release_notes</c> entry makes of itself: the catalog id of its product or the reason it is dropped.</summary>
public readonly union ReleaseNotesResolution(ReleaseNotesAccepted, ReleaseNotesRejected);

/// <summary>An <c>api</c> entry that is kept. Its child pages and categories may still have findings, in the order they are reported.</summary>
public sealed record ApiAccepted(ResolvedApiConfiguration Configuration, IReadOnlyList<Violation> Problems);

/// <summary>An <c>api</c> entry that is dropped, with its findings in the order they are reported.</summary>
public sealed record ApiRejected(IReadOnlyList<Violation> Problems);

/// <summary>
/// An <c>api</c> entry whose resolution threw (a path with a NUL character cannot be resolved against the files), with the findings made before it did.
/// The stage reports them and the resolver then throws <see cref="Failure"/>, as the step that read the entry always did.
/// </summary>
public sealed record ApiFailed(IReadOnlyList<Violation> Problems, ExceptionDispatchInfo Failure);

/// <summary>What an <c>api</c> entry makes of itself against the catalog and the files next to <c>docset.yml</c>.</summary>
public readonly union ApiResolution(ApiAccepted, ApiRejected, ApiFailed);

/// <summary>
/// How the parts of a <c>docset.yml</c> are read, shared by the constraints that report what is wrong with them and by the resolution that keeps
/// what is right: one decision, two readers.
/// </summary>
public static class DocumentationSetRules
{
	private static readonly HashSet<string> AllowedImageExtensions = [".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico"];

	/// <summary>The registry a set is in: null, empty or <c>public</c> (or anything unknown) is the public one.</summary>
	public static DocSetRegistry ParseRegistry(string? registry) =>
		!string.IsNullOrWhiteSpace(registry) && DocSetRegistryExtensions.TryParse(registry.Trim(), out var parsed, true)
			? parsed
			: DocSetRegistry.Public;

	/// <summary>Reads one trimmed, non-blank <c>cross_links</c> entry, with an optional registry prefix (<c>public://elasticsearch</c>).</summary>
	public static CrossLinkResolution ResolveCrossLink(string raw, DocSetRegistry docsetRegistry)
	{
		DocSetRegistry entryRegistry;
		string repository;

		var colonSlash = raw.IndexOf("://", StringComparison.Ordinal);
		if (colonSlash >= 0)
		{
			var prefix = raw[..colonSlash];
			repository = raw[(colonSlash + 3)..];
			if (string.IsNullOrWhiteSpace(repository))
				return new CrossLinkRejected($"Cross-link '{raw}' has empty repository after registry prefix.");
			if (!DocSetRegistryExtensions.TryParse(prefix, out entryRegistry, true))
				return new CrossLinkRejected($"Cross-link '{raw}' uses unknown registry '{prefix}'. Use 'public' or 'internal'.");
		}
		else
		{
			repository = raw;
			entryRegistry = docsetRegistry;
		}

		return docsetRegistry == DocSetRegistry.Public && entryRegistry != DocSetRegistry.Public
			? new CrossLinkRejected(
				$"Public documentation cannot link to codex docs. Cross-link '{raw}' targets registry '{entryRegistry.ToStringFast()}'. Remove it or use a public docset."
			)
			: new CrossLinkAccepted(new CrossLinkEntry(repository, entryRegistry));
	}

	/// <summary>
	/// The entries up to the first one written with no value, which is null. The step that keeps what the rules accept fails on that entry,
	/// so nothing after it was ever reported. A list written with no value at all has no entries to report.
	/// </summary>
	public static IEnumerable<T> UntilNoValue<T>(IEnumerable<T?>? entries) where T : class
	{
		foreach (var entry in entries ?? [])
		{
			if (entry is null)
				yield break;
			yield return entry;
		}
	}

	/// <summary>Reads one <c>release_notes</c> entry. An entry written with no value is null and throws here, as it always has.</summary>
	public static ReleaseNotesResolution ResolveReleaseNotes(ReleaseNotesDeclaration? reference, ProductCatalog products)
	{
		var product = reference!.Product?.Trim();
		if (string.IsNullOrEmpty(product))
			return new ReleaseNotesRejected("A 'release_notes' entry is missing a 'product' value.");

		if (!product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
			return new ReleaseNotesRejected($"Invalid 'release_notes' product '{product}'. Product ids must match [a-zA-Z0-9_-]+.");

		// products.yml keys are hyphenated; accept the underscore variant for parity with `products`.
		if (!products.Products.TryGetValue(product.Replace('_', '-'), out var resolved))
			return new ReleaseNotesRejected(
				$"Unknown 'release_notes' product '{product}'. It must be a product id defined in products.yml."
			);

		return resolved.Features.ParticipatesInReleaseNotes
			? new ReleaseNotesAccepted(resolved.Id)
			: new ReleaseNotesRejected(
				$"Product '{product}' declared in 'release_notes' does not participate in the release-notes system (it lacks the 'release-notes' feature in products.yml)."
			);
	}

	/// <summary>Whether reading <paramref name="path"/> fails: a path with a NUL character throws where it is resolved against the files.</summary>
	public static bool IsUnreadablePath(string path) => path.Contains('\0', StringComparison.Ordinal);

	/// <summary>The finding for a <c>branding</c> image that is set, or null when the image is usable. <paramref name="field"/> is how it is reported.</summary>
	public static string? BrandingImageProblem(string field, string imagePath, IFileSystem fileSystem, IDirectoryInfo sourceDirectory)
	{
		var ext = Path.GetExtension(imagePath).ToLowerInvariant();
		if (!AllowedImageExtensions.Contains(ext))
			return $"'{field}' has unsupported extension '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}";

		var resolved = fileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, imagePath)));

		if (!resolved.IsSubPathOf(sourceDirectory))
			return $"'{field}' path '{imagePath}' escapes the documentation source directory.";

		if (ValidateFileAccess(resolved, sourceDirectory) is { } symlinkError)
			return $"'{field}' path '{imagePath}' is unsafe: {symlinkError}";

		return resolved.Exists ? null : $"'{field}' file '{imagePath}' does not exist.";
	}

	/// <summary>The finding for a <c>cta</c> template that cannot be used, or null. A template written with no value is null and throws here, as it always has.</summary>
	public static string? CtaProblem(CtaDeclaration definition) => CtaButtonProblem(definition) ?? CtaBenefitsProblem(definition);

	/// <summary>
	/// The findings of the <c>cta</c> templates up to the first one written with no value: a null template, or one whose <c>benefits</c> key has no
	/// value and whose button is usable, which is where <see cref="CtaProblem"/> fails. The step that keeps the templates fails on it, so nothing after
	/// it was ever reported.
	/// </summary>
	public static IEnumerable<string> CtaProblems(IEnumerable<CtaDeclaration?>? templates)
	{
		foreach (var template in templates ?? [])
		{
			if (template is null)
				yield break;
			if (CtaButtonProblem(template) is { } buttonProblem)
			{
				yield return buttonProblem;
				continue;
			}

			if (template.Benefits is null)
				yield break;
			if (CtaBenefitsProblem(template) is { } benefitsProblem)
				yield return benefitsProblem;
		}
	}

	private static string? CtaButtonProblem(CtaDeclaration definition)
	{
		var name = definition.Name;
		if (string.IsNullOrWhiteSpace(definition.Label) || string.IsNullOrWhiteSpace(definition.Url))
			return $"'cta.{name}' must define both 'button.label' and 'button.url'.";

		return Uri.TryCreate(definition.Url.Trim(), UriKind.RelativeOrAbsolute, out var uri)
			&& uri.IsAbsoluteUri
			&& uri.Scheme != Uri.UriSchemeHttp
			&& uri.Scheme != Uri.UriSchemeHttps ? $"'cta.{name}.button.url' must use http/https or a relative URL." : null;
	}

	private static string? CtaBenefitsProblem(CtaDeclaration definition) =>
		definition.Benefits.Count > Cta.MaxBenefits
			? $"'cta.{definition.Name}.benefits' has {definition.Benefits.Count} entries; a maximum of {Cta.MaxBenefits} is allowed."
			: null;

	/// <summary>
	/// Reads one <c>api</c> entry against the catalog and the files next to <c>docset.yml</c>, checked in order: one entry, a known product, a spec
	/// that names a file inside the documentation source directory and is safe to read, a well-formed repository, then its child pages and catalog
	/// categories. The first failure of the entry itself drops it; child pages and categories each report every bad value.
	/// </summary>
	public static ApiResolution ResolveApi(
		ApiDeclaration api,
		ProductCatalog products,
		IFileSystem fileSystem,
		IDirectoryInfo sourceDirectory
	)
	{
		var problems = new List<Violation>();
		try
		{
			return ResolveApiEntry(api, products, fileSystem, sourceDirectory, problems);
		}
		catch (Exception e)
		{
			return new ApiFailed([.. problems], ExceptionDispatchInfo.Capture(e));
		}
	}

	/// <summary>
	/// Reads the <c>api</c> entries in order and stops after the first one that fails: the entries after it were never read, so nothing about them
	/// is reported. A list written with no value has no entries to read.
	/// </summary>
	public static IReadOnlyList<ApiResolution> ResolveApis(
		IEnumerable<ApiDeclaration>? apis,
		ProductCatalog products,
		IFileSystem fileSystem,
		IDirectoryInfo sourceDirectory
	)
	{
		var resolutions = new List<ApiResolution>();
		foreach (var api in apis ?? [])
		{
			var resolution = ResolveApi(api, products, fileSystem, sourceDirectory);
			resolutions.Add(resolution);
			if (resolution.Value is ApiFailed)
				break;
		}

		return resolutions;
	}

	private static ApiResolution ResolveApiEntry(
		ApiDeclaration api,
		ProductCatalog products,
		IFileSystem fileSystem,
		IDirectoryInfo sourceDirectory,
		List<Violation> problems
	)
	{
		var productKey = api.ProductKey;
		if (api.SingleEntry is not { } entry)
			return Rejected(
				Violation.Error($"API configuration for '{productKey}' must have exactly one entry, found {api.Entries.Count}.")
			);

		if (!entry.HasProduct)
			return Rejected(
				At(
					$"API '{productKey}' is missing required 'product:'. It must match a product id defined in products.yml.",
					entry.Location,
					null
				)
			);

		var normalizedProduct = entry.Product!.Trim().Replace('_', '-');
		if (!products.Products.TryGetValue(normalizedProduct, out var product))
		{
			var hint = new Suggestion(products.Products.Keys.ToHashSet(), normalizedProduct).GetSuggestionQuestion();
			return Rejected(
				At(
					$"Unknown 'product: {entry.Product}' for API '{productKey}'. It must be a product id defined in products.yml.{(string.IsNullOrEmpty(hint) ? "" : $" {hint}")}",
					entry.ProductLocation,
					null
				)
			);
		}

		if (!entry.HasSpec)
			return Rejected(
				At(
					$"API '{productKey}' is missing required 'spec:'. Its basename is required to resolve " +
						"the remote version index, even when the file is not present locally.",
					entry.Location,
					null
				)
			);

		var specLocation = entry.SpecLocation ?? entry.Location;
		var specFileName = Path.GetFileName(entry.Spec!.Trim());
		if (string.IsNullOrEmpty(specFileName))
			return Rejected(At($"'spec: {entry.Spec}' for API '{productKey}' does not resolve to a file name.", specLocation, null));

		var specFile = fileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, entry.Spec)));
		if (!specFile.IsSubPathOf(sourceDirectory))
			return Rejected(
				At($"'spec: {entry.Spec}' for API '{productKey}' escapes the documentation source directory.", specLocation, null)
			);

		// A missing local file is expected, not an error: docsets that don't carry the spec
		// locally resolve the current version from S3 via the version index instead.
		IFileInfo? localSpecFile = null;
		if (specFile.Exists)
		{
			if (ValidateFileAccess(specFile, sourceDirectory) is { } symlinkError)
				return Rejected(At($"'spec: {entry.Spec}' for API '{productKey}' is unsafe: {symlinkError}", specLocation, null));
			localSpecFile = specFile;
		}

		string? repository = null;
		if (!string.IsNullOrWhiteSpace(entry.Repository))
		{
			var candidate = entry.Repository.Trim();
			var separator = candidate.IndexOf('/');
			if (!(separator > 0 && separator < candidate.Length - 1))
				return Rejected(
					At(
						$"'repository: {entry.Repository}' for API '{productKey}' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'.",
						entry.RepositoryLocation ?? entry.Location,
						null
					)
				);
			repository = candidate;
		}

		var apiContentDirectory = fileSystem.DirectoryInfo.New(Path.Join(sourceDirectory.FullName, "api", productKey));
		var children = ResolveApiChildren(productKey, entry.Children, fileSystem, apiContentDirectory, problems);

		return new ApiAccepted(
			new ResolvedApiConfiguration
			{
				ProductKey = productKey,
				Product = product,
				SpecFileName = specFileName,
				LocalSpecFile = localSpecFile,
				Repository = repository,
				Children = children,
				ApiContentDirectory = apiContentDirectory,
				CatalogCategories = ResolveCatalogCategories(productKey, entry, problems)
			},
			problems
		);
	}

	/// <summary>The findings of an <c>api</c> entry, whether it is kept or dropped, in the order they are reported.</summary>
	public static IReadOnlyList<Violation> ProblemsOf(ApiResolution resolution) => resolution.Value switch
	{
		ApiAccepted accepted => accepted.Problems,
		ApiRejected rejected => rejected.Problems,
		ApiFailed failed => failed.Problems,
		_ => []
	};

	private static ApiRejected Rejected(Violation problem) => new([problem]);

	private static Violation At(string message, SourceLocation? location, object? evidence) =>
		Violation.Error(message) with { Line = location?.Line, Column = location?.Column, Evidence = evidence };

	private static IReadOnlyList<string> ResolveCatalogCategories(string productKey, ApiDeclarationEntry entry, List<Violation> problems)
	{
		var raw = entry.CatalogCategories;
		if (raw.Count == 0)
			return [];

		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in raw)
		{
			if (ApiCatalogCategory.Normalize(item) is { } canonical)
			{
				_ = seen.Add(canonical);
				continue;
			}

			problems.Add(
				At(
					$"Unknown catalog category '{item}' for API '{productKey}'. Valid categories: {string.Join(", ", ApiCatalogCategory.DisplayOrder)} (or {ApplicabilityKeys.Ech} for {ApplicabilityKeys.Ess}).",
					entry.CatalogLocation ?? entry.Location,
					null
				)
			);
		}

		return ApiCatalogCategory.DisplayOrder.Where(seen.Contains).ToArray();
	}

	// Children resolve only under 'api/<key>/'; escaping paths and symlinks are rejected the same way branding image paths are.
	private static List<IFileInfo> ResolveApiChildren(
		string productKey,
		IReadOnlyList<string?> children,
		IFileSystem fileSystem,
		IDirectoryInfo childrenDirectory,
		List<Violation> problems
	)
	{
		var resolved = new List<IFileInfo>();
		foreach (var child in children)
		{
			if (string.IsNullOrWhiteSpace(child))
			{
				problems.Add(Violation.Error($"A 'children' entry for API '{productKey}' is missing a 'file' value."));
				continue;
			}

			var childFile = fileSystem.FileInfo.New(Path.GetFullPath(Path.Join(childrenDirectory.FullName, child)));
			if (ChildProblem(productKey, child, childFile, childrenDirectory) is { } problem)
			{
				problems.Add(Violation.Error(problem));
				continue;
			}

			resolved.Add(childFile);
		}

		return resolved;
	}

	private static string? ChildProblem(string productKey, string child, IFileInfo childFile, IDirectoryInfo childrenDirectory)
	{
		if (!childFile.IsSubPathOf(childrenDirectory))
			return $"Child page '{child}' for API '{productKey}' escapes 'api/{productKey}/'.";

		if (ValidateFileAccess(childFile, childrenDirectory) is { } symlinkError)
			return $"Child page '{child}' for API '{productKey}' is unsafe: {symlinkError}";

		if (!childFile.Exists)
			return $"Child page '{child}' for API '{productKey}' does not exist under 'api/{productKey}/'.";

		return IsTopLevelSupplementalChild(childFile, childrenDirectory)
			? $"Child page '{child}' for API '{productKey}' uses a supplemental file name (op-*.md / tag-*.md). Those files are auto-discovered and cannot be listed under children:."
			: null;
	}

	private static bool IsTopLevelSupplementalChild(IFileInfo childFile, IDirectoryInfo apiDirectory) =>
		childFile.Directory is not null
			&& string.Equals(childFile.Directory.FullName, apiDirectory.FullName, StringComparison.OrdinalIgnoreCase)
			&& ResolvedApiConfiguration.IsSupplementalFileName(childFile.Name);
}
