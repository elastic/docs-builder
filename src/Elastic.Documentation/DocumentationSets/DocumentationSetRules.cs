// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Suggestions;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// The rules behind <see cref="DocumentationSetConstraints"/>, one declared item at a time. Each returns what resolution
/// keeps (null when the item is invalid) together with why it is invalid, so the constraints that report a problem and
/// the resolver that drops the item can never disagree.
/// </summary>
public static class DocumentationSetRules
{
	public const int MaxCtaBenefits = Cta.MaxBenefits;

	public static IReadOnlySet<string> AllowedImageExtensions { get; } = new HashSet<string>
	{
		".svg",
		".png",
		".jpg",
		".jpeg",
		".gif",
		".webp",
		".ico"
	};

	/// <summary><c>registry</c>: empty, <c>public</c> and anything unrecognized mean the public link index.</summary>
	public static DocSetRegistry ParseRegistry(string? raw) =>
		!string.IsNullOrWhiteSpace(raw) && DocSetRegistryExtensions.TryParse(raw.Trim(), out var parsed, true)
			? parsed
			: DocSetRegistry.Public;

	/// <summary>The non-blank <c>cross_links</c>, trimmed.</summary>
	public static IEnumerable<string> DeclaredCrossLinks(DocumentationSetDefinition docset) =>
		docset.CrossLinks.Where(raw => !string.IsNullOrWhiteSpace(raw)).Select(raw => raw.Trim());

	public static (CrossLinkEntry? Entry, Finding? Finding) ParseCrossLink(string raw, DocSetRegistry docsetRegistry)
	{
		DocSetRegistry entryRegistry;
		string repository;

		var colonSlash = raw.IndexOf("://", StringComparison.Ordinal);
		if (colonSlash >= 0)
		{
			var prefix = raw[..colonSlash];
			repository = raw[(colonSlash + 3)..];
			if (string.IsNullOrWhiteSpace(repository))
				return (null, Finding.Error($"Cross-link '{raw}' has empty repository after registry prefix."));
			if (!DocSetRegistryExtensions.TryParse(prefix, out entryRegistry, true))
				return (null, Finding.Error($"Cross-link '{raw}' uses unknown registry '{prefix}'. Use 'public' or 'internal'."));
		}
		else
		{
			repository = raw;
			entryRegistry = docsetRegistry;
		}

		if (docsetRegistry == DocSetRegistry.Public && entryRegistry != DocSetRegistry.Public)
		{
			return (null, Finding.Error(
				$"Public documentation cannot link to codex docs. Cross-link '{raw}' targets registry '{entryRegistry.ToStringFast()}'. Remove it or use a public docset."
			));
		}

		return (new CrossLinkEntry(repository, entryRegistry), null);
	}

	/// <summary>A <c>release_notes</c> product resolved to its canonical id: it must exist and take part in release notes.</summary>
	public static (string? ProductId, Finding? Finding) ResolveReleaseNotesProduct(string? raw, ProductCatalog catalog)
	{
		var product = raw?.Trim();
		if (string.IsNullOrEmpty(product))
			return (null, Finding.Error("A 'release_notes' entry is missing a 'product' value."));

		if (!IsValidProductId(product))
			return (null, Finding.Error($"Invalid 'release_notes' product '{product}'. Product ids must match [a-zA-Z0-9_-]+."));

		// products.yml keys are hyphenated; accept the underscore variant for parity with `products`.
		if (!catalog.Products.TryGetValue(product.Replace('_', '-'), out var resolved))
			return (null, Finding.Error($"Unknown 'release_notes' product '{product}'. It must be a product id defined in products.yml."));

		if (!resolved.Features.ParticipatesInReleaseNotes)
		{
			return (null, Finding.Error(
				$"Product '{product}' declared in 'release_notes' does not participate in the release-notes system (it lacks the 'release-notes' feature in products.yml)."
			));
		}

		return (resolved.Id, null);
	}

	private static bool IsValidProductId(string product) =>
		product.Length > 0 && product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

	/// <summary>The first <c>subs</c> key that repeats an earlier one when compared case-insensitively.</summary>
	public static string? FirstDuplicateSubstitution(DocumentationSetDefinition docset)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (key, _) in docset.Substitutions)
		{
			if (!seen.Add(key))
				return key;
		}
		return null;
	}

	/// <summary>
	/// A <c>branding</c> image path, when it has an allowed extension, stays inside the documentation source directory,
	/// is not an unsafe symlink and exists.
	/// </summary>
	public static (string? Path, Finding? Finding) ValidateBrandingImage(
		string? imagePath,
		string fieldName,
		DocumentationSetDefinition docset
	)
	{
		if (string.IsNullOrEmpty(imagePath))
			return (null, null);

		var ext = Path.GetExtension(imagePath).ToLowerInvariant();
		if (!AllowedImageExtensions.Contains(ext))
			return (null, Finding.Error(
				$"'{fieldName}' has unsupported extension '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}"
			));

		var sourceDirectory = docset.SourceDirectory;
		var resolved = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, imagePath)));

		if (!resolved.IsSubPathOf(sourceDirectory))
			return (null, Finding.Error($"'{fieldName}' path '{imagePath}' escapes the documentation source directory."));

		if (ValidateFileAccess(resolved, sourceDirectory) is { } symlinkError)
			return (null, Finding.Error($"'{fieldName}' path '{imagePath}' is unsafe: {symlinkError}"));

		if (!resolved.Exists)
			return (null, Finding.Error($"'{fieldName}' file '{imagePath}' does not exist."));

		return (imagePath, null);
	}

	/// <summary>The branding images that are set, with the field name each is reported under.</summary>
	public static IEnumerable<(string? Path, string FieldName)> DeclaredBrandingImages(BrandingDeclaration branding)
	{
		yield return (branding.Icon, "branding.icon");
		yield return (branding.OgImage, "branding.og-image");
		if (!string.IsNullOrEmpty(branding.Favicon))
			yield return (branding.Favicon, "branding.favicon");
		if (!string.IsNullOrEmpty(branding.AppleTouchIcon))
			yield return (branding.AppleTouchIcon, "branding.apple-touch-icon");
	}

	/// <summary>A <c>cta</c> template: it needs a button label and url, an absolute url must be http(s), and at most three benefits.</summary>
	public static (Cta? Cta, Finding? Finding) ValidateCta(CtaDeclaration cta)
	{
		if (string.IsNullOrWhiteSpace(cta.Label) || string.IsNullOrWhiteSpace(cta.Url))
			return (null, Finding.Error($"'cta.{cta.Name}' must define both 'button.label' and 'button.url'."));

		var url = cta.Url.Trim();
		if (
			Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)
			&& uri.IsAbsoluteUri
			&& uri.Scheme != Uri.UriSchemeHttp
			&& uri.Scheme != Uri.UriSchemeHttps
		)
			return (null, Finding.Error($"'cta.{cta.Name}.button.url' must use http/https or a relative URL."));

		if (cta.Benefits.Count > MaxCtaBenefits)
			return (null, Finding.Error(
				$"'cta.{cta.Name}.benefits' has {cta.Benefits.Count} entries; a maximum of {MaxCtaBenefits} is allowed."
			));

		return (new Cta { Name = cta.Name, Label = cta.Label, Url = url, Benefits = cta.Benefits }, null);
	}

	/// <summary>The CTA templates pages can use, by case-insensitive name: the built-in default, overlaid by every valid <c>cta</c>.</summary>
	public static IReadOnlyDictionary<string, Cta> ValidCtas(DocumentationSetDefinition docset)
	{
		var ctas = new Dictionary<string, Cta>(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = Cta.Default };
		foreach (var declared in docset.Ctas)
		{
			if (ValidateCta(declared).Cta is { } cta)
				ctas[declared.Name] = cta;
		}
		return ctas;
	}

	/// <summary>
	/// An <c>api</c> entry bound to its product, checked in order: one entry, a known product, a spec that names a file
	/// inside the docset and is safe to read, a well-formed repository, then its catalog categories and child pages.
	/// The first failure of the entry itself stops; categories and children each report every bad value.
	/// </summary>
	public static (ResolvedApiConfiguration? Api, IReadOnlyList<Finding> Findings) ResolveApi(
		ApiDeclaration api,
		DocumentationSetDefinition docset,
		ProductCatalog catalog
	)
	{
		var productKey = api.ProductKey;
		if (api.Entries.Count != 1)
			return (null, [Finding.Error($"API configuration for '{productKey}' must have exactly one entry, found {api.Entries.Count}.")]);

		var entry = api.Entries[0];
		if (string.IsNullOrWhiteSpace(entry.Product))
		{
			return (null, [
				At(
					$"API '{productKey}' is missing required 'product:'. It must match a product id defined in products.yml.",
					entry.Location
				)
			]);
		}

		var normalizedProduct = entry.Product.Trim().Replace('_', '-');
		if (!catalog.Products.TryGetValue(normalizedProduct, out var product))
		{
			var hint = new Suggestion(catalog.Products.Keys.ToHashSet(), normalizedProduct).GetSuggestionQuestion();
			return (null, [
				At(
					$"Unknown 'product: {entry.Product}' for API '{productKey}'. It must be a product id defined in products.yml.{(string.IsNullOrEmpty(hint) ? "" : $" {hint}")}",
					entry.ProductLocation
				)
			]);
		}

		if (string.IsNullOrWhiteSpace(entry.Spec))
		{
			return (null, [
				At(
					$"API '{productKey}' is missing required 'spec:'. Its basename is required to resolve " +
						"the remote version index, even when the file is not present locally.",
					entry.Location
				)
			]);
		}

		var specLocation = entry.SpecLocation ?? entry.Location;
		var specFileName = Path.GetFileName(entry.Spec.Trim());
		if (string.IsNullOrEmpty(specFileName))
			return (null, [At($"'spec: {entry.Spec}' for API '{productKey}' does not resolve to a file name.", specLocation)]);

		var sourceDirectory = docset.SourceDirectory;
		var specFile = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, entry.Spec)));
		if (!specFile.IsSubPathOf(sourceDirectory))
			return (null, [At($"'spec: {entry.Spec}' for API '{productKey}' escapes the documentation source directory.", specLocation)]);

		// A missing local file is expected, not an error: docsets that don't carry the spec
		// locally resolve the current version from S3 via the version index instead.
		IFileInfo? localSpecFile = null;
		if (specFile.Exists)
		{
			if (ValidateFileAccess(specFile, sourceDirectory) is { } symlinkError)
				return (null, [At($"'spec: {entry.Spec}' for API '{productKey}' is unsafe: {symlinkError}", specLocation)]);
			localSpecFile = specFile;
		}

		string? repository = null;
		if (!string.IsNullOrWhiteSpace(entry.Repository))
		{
			var candidate = entry.Repository.Trim();
			var separator = candidate.IndexOf('/');
			if (!(separator > 0 && separator < candidate.Length - 1))
			{
				return (null, [
					At(
						$"'repository: {entry.Repository}' for API '{productKey}' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'.",
						entry.RepositoryLocation ?? entry.Location
					)
				]);
			}
			repository = candidate;
		}

		var findings = new List<Finding>();
		var apiContentDirectory = docset.FileSystem.DirectoryInfo.New(Path.Join(sourceDirectory.FullName, "api", productKey));
		var children = ResolveApiChildren(productKey, entry, docset.FileSystem, apiContentDirectory, findings);
		var categories = ResolveCatalogCategories(productKey, entry, findings);

		return (new ResolvedApiConfiguration
		{
			ProductKey = productKey,
			Product = product,
			SpecFileName = specFileName,
			LocalSpecFile = localSpecFile,
			Repository = repository,
			Children = children,
			ApiContentDirectory = apiContentDirectory,
			CatalogCategories = categories
		}, findings);
	}

	private static Finding At(string message, SourceLocation? location) => Finding.Error(message, location?.Line, location?.Column);

	// Children resolve only under 'api/<key>/'; escaping paths and symlinks are rejected the same way branding image paths are.
	private static List<IFileInfo> ResolveApiChildren(
		string productKey,
		ApiDeclarationEntry entry,
		IFileSystem fileSystem,
		IDirectoryInfo childrenDirectory,
		List<Finding> findings
	)
	{
		var resolved = new List<IFileInfo>();
		foreach (var child in entry.Children)
		{
			if (string.IsNullOrWhiteSpace(child))
			{
				findings.Add(Finding.Error($"A 'children' entry for API '{productKey}' is missing a 'file' value."));
				continue;
			}

			var childFile = fileSystem.FileInfo.New(Path.GetFullPath(Path.Join(childrenDirectory.FullName, child)));
			if (!childFile.IsSubPathOf(childrenDirectory))
			{
				findings.Add(Finding.Error($"Child page '{child}' for API '{productKey}' escapes 'api/{productKey}/'."));
				continue;
			}

			if (ValidateFileAccess(childFile, childrenDirectory) is { } symlinkError)
			{
				findings.Add(Finding.Error($"Child page '{child}' for API '{productKey}' is unsafe: {symlinkError}"));
				continue;
			}

			if (!childFile.Exists)
			{
				findings.Add(Finding.Error($"Child page '{child}' for API '{productKey}' does not exist under 'api/{productKey}/'."));
				continue;
			}

			if (IsTopLevelSupplementalChild(childFile, childrenDirectory))
			{
				findings.Add(
					Finding.Error(
						$"Child page '{child}' for API '{productKey}' uses a supplemental file name (op-*.md / tag-*.md). Those files are auto-discovered and cannot be listed under children:."
					)
				);
				continue;
			}

			resolved.Add(childFile);
		}
		return resolved;
	}

	private static bool IsTopLevelSupplementalChild(IFileInfo childFile, IDirectoryInfo apiDirectory) =>
		childFile.Directory is not null
			&& string.Equals(childFile.Directory.FullName, apiDirectory.FullName, StringComparison.OrdinalIgnoreCase)
			&& ResolvedApiConfiguration.IsSupplementalFileName(childFile.Name);

	private static IReadOnlyList<string> ResolveCatalogCategories(string productKey, ApiDeclarationEntry entry, List<Finding> findings)
	{
		if (entry.CatalogCategories.Count == 0)
			return [];

		var location = entry.CatalogLocation ?? entry.Location;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in entry.CatalogCategories)
		{
			if (ApiCatalogCategory.Normalize(item) is { } canonical)
			{
				_ = seen.Add(canonical);
				continue;
			}

			findings.Add(
				At(
					$"Unknown catalog category '{item}' for API '{productKey}'. Valid categories: {string.Join(", ", ApiCatalogCategory.DisplayOrder)} (or {ApplicabilityKeys.Ech} for {ApplicabilityKeys.Ess}).",
					location
				)
			);
		}

		return ApiCatalogCategory.DisplayOrder.Where(seen.Contains).ToArray();
	}
}
