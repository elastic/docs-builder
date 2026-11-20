// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Catalog;
using Nullean.Entities;

namespace Elastic.Documentation.DocumentationSets;

// A documentation set is resolved in steps, and a finding is reported when its step is reached: an entry written with no value fails the step
// that reads it, after the findings of the steps before it. Each stage below is the rules of one run of steps, checked where resolution reaches it,
// so the findings keep the order and the exceptions keep the point they always had. A rule skips an entry with no value: the step that keeps
// what the rules accept is the one that fails on it. A rule reports the entries before the first one with no value and stops there, as the step
// that fails on it did.

/// <summary>The <c>cross_links</c> and <c>release_notes</c> of a documentation set with the catalog they resolve against.</summary>
public sealed record DocumentationSetInCatalog(
	DocumentationSetDefinition Docset,
	ProductCatalog Products
) : IHaveConstraints<DocumentationSetInCatalog>
{
	public static ConstraintSet<DocumentationSetInCatalog> Constraints => DocumentationSetInCatalogConstraints.All;
}

/// <summary>The <c>api</c> and <c>storybook</c> of a documentation set, with each api entry already resolved against the catalog and the files.</summary>
public sealed record DocumentationSetApis(
	DocumentationSetDefinition Docset,
	IReadOnlyList<ApiResolution> Resolutions
) : IHaveConstraints<DocumentationSetApis>
{
	public static ConstraintSet<DocumentationSetApis> Constraints => DocumentationSetApisConstraints.All;
}

/// <summary>The <c>branding</c> and <c>cta</c> templates of a documentation set with the files its images resolve against.</summary>
public sealed record DocumentationSetPresentation(
	DocumentationSetDefinition Docset,
	IFileSystem FileSystem,
	IDirectoryInfo SourceDirectory
) : IHaveConstraints<DocumentationSetPresentation>
{
	public static ConstraintSet<DocumentationSetPresentation> Constraints => DocumentationSetPresentationConstraints.All;
}

/// <summary>What refers to the <c>cta</c> templates of a documentation set, and the features that depend on its branding.</summary>
public sealed record DocumentationSetFeatures(DocumentationSetDefinition Docset) : IHaveConstraints<DocumentationSetFeatures>
{
	public static ConstraintSet<DocumentationSetFeatures> Constraints => DocumentationSetFeaturesConstraints.All;
}

/// <summary>Every rule on the <c>cross_links</c> and <c>release_notes</c> of a documentation set, in the order they are reported.</summary>
[Constraints<DocumentationSetInCatalog>]
public static partial class DocumentationSetInCatalogConstraints
{
	public static readonly Constraint<DocumentationSetInCatalog> CrossLinkWellFormed = Constraints<DocumentationSetInCatalog>.Check(m =>
	{
		// a cross_links key written with no value fails the step that reads it, before anything of this stage is reported
		if (m.Docset.CrossLinks is null)
			return [];
		var registry = DocumentationSetRules.ParseRegistry(m.Docset.Registry);
		return m
			.Docset
			.CrossLinks
			.Where(raw => !string.IsNullOrWhiteSpace(raw))
			.Select(raw => DocumentationSetRules.ResolveCrossLink(raw.Trim(), registry).Value)
			.OfType<CrossLinkRejected>()
			.Select(rejected => Violation.Error(rejected.Problem));
	}).Describe(
		"Each cross_links entry names a repository, with an optional 'public' or 'internal' registry prefix, and a public docset links only to public docsets."
	);

	public static readonly Constraint<DocumentationSetInCatalog> ReleaseNotesProductKnown = Constraints<DocumentationSetInCatalog>.Check(
		// with a cross_links key written with no value the step that reads it fails before release_notes is reached
		m => m.Docset.CrossLinks is null
			? []
			: DocumentationSetRules
				.UntilNoValue(m.Docset.ReleaseNotes)
				.Select(reference => DocumentationSetRules.ResolveReleaseNotes(reference, m.Products).Value)
				.OfType<ReleaseNotesRejected>()
				.Select(rejected => Violation.Error(rejected.Problem))
	).Describe("Each release_notes entry names a products.yml product with the 'release-notes' feature.");
}

/// <summary>Every rule on the <c>api</c> and <c>storybook</c> of a documentation set, in the order they are reported.</summary>
[Constraints<DocumentationSetApis>]
public static partial class DocumentationSetApisConstraints
{
	// each entry is resolved once, by the caller, and the same result is used to report it and to build the configuration
	public static readonly Constraint<DocumentationSetApis> ApiEntryValid = Constraints<DocumentationSetApis>.Check(
		m => m.Resolutions.SelectMany(DocumentationSetRules.ProblemsOf)
	).Describe(
		"Each api key has one entry naming a products.yml product and a spec file inside the docset, an optional 'org/repo' repository, known catalog categories and child pages under api/<key>/."
	);

	public static readonly Constraint<DocumentationSetApis> StorybookVariablesAllowed = Constraints<DocumentationSetApis>.Check(
		// with an api key written with no value, or an api entry that failed, the step that reads api fails before storybook is reached
		m => !m.Docset.HasStorybook || m.Docset.Api is null || m.Resolutions.Any(r => r.Value is ApiFailed)
			? []
			: EnvironmentInterpolation.DisallowedVariables(m.Docset.StorybookRegistry?.Trim()).Select(
				name => Violation.Warning(
					$"'storybook.registry' references environment variable '{name}' which is not allow-listed for interpolation and is left literal. Allowed: {string.Join(", ", EnvironmentInterpolation.AllowedVariables)}."
				)
			)
	).Describe("storybook.registry only interpolates allow-listed environment variables.");
}

/// <summary>Every rule on the <c>branding</c> and <c>cta</c> templates of a documentation set, in the order they are reported.</summary>
[Constraints<DocumentationSetPresentation>]
public static partial class DocumentationSetPresentationConstraints
{
	public static readonly Constraint<DocumentationSetPresentation> BrandingImagesValid = Constraints<DocumentationSetPresentation>.Check(
		// an image whose path cannot be read fails the step that reads it, after the findings of the images before it
		m => BrandingImages(m.Docset)
			.TakeWhile(image => !DocumentationSetRules.IsUnreadablePath(image.Path))
			.Select(image => DocumentationSetRules.BrandingImageProblem(image.Field, image.Path, m.FileSystem, m.SourceDirectory))
			.OfType<string>()
			.Select(Violation.Error)
	).Describe(
		"Each branding image has an allowed extension, stays inside the documentation source directory, is not an unsafe symlink and exists."
	);

	// An unset favicon and apple-touch-icon are discovered next to docset.yml, not declared, so they are not checked.
	private static IEnumerable<(string Field, string Path)> BrandingImages(DocumentationSetDefinition docset) =>
		docset.Branding is not { } branding
			? []
			: new[]
			{
				("branding.icon", branding.Icon ?? ""),
				("branding.og-image", branding.OgImage ?? ""),
				("branding.favicon", branding.Favicon ?? ""),
				("branding.apple-touch-icon", branding.AppleTouchIcon ?? "")
			}.Where(i => !string.IsNullOrEmpty(i.Item2));

	public static readonly Constraint<DocumentationSetPresentation> CtaTemplateValid = Constraints<DocumentationSetPresentation>.Check(
		// the images are read before the templates: with an unreadable image path the step fails before any template is reported
		m => BrandingImages(m.Docset).Any(image => DocumentationSetRules.IsUnreadablePath(image.Path))
			? []
			: DocumentationSetRules.CtaProblems(m.Docset.Ctas).Select(Violation.Error)
	).Describe("Each cta template has a button label and url, an absolute url uses http(s), and it lists at most three benefits.");
}

/// <summary>Every rule on what refers to the <c>cta</c> templates and the branding of a documentation set, in the order they are reported.</summary>
[Constraints<DocumentationSetFeatures>]
public static partial class DocumentationSetFeaturesConstraints
{
	public static readonly Constraint<DocumentationSetFeatures> DefaultCtaKnown = Constraints<DocumentationSetFeatures>.Check(m =>
	{
		var usable = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = true };
		// resolution fails on a template with no value before this stage is reached, so such a template is simply not usable here
		foreach (var cta in (m.Docset.Ctas ?? []).Where(c => c is { Benefits: not null } && DocumentationSetRules.CtaProblem(c) is null))
			usable[cta!.Name] = true;
		return m
			.Docset
			.TocDefaultCtas
			.Where(page => !usable.ContainsKey(page.Value))
			.Select(
				page => Violation.Error(
					$"'default_cta: {page.Value}' on page '{page.Key}' does not match any 'cta' template in docset.yml."
				)
			);
	}).Describe("Every default_cta in the table of contents names a valid cta template or the built-in default.");

	public static readonly Constraint<DocumentationSetFeatures> PrimaryNavWithoutBranding = Constraints<DocumentationSetFeatures>.Check(
		// a features key written with no value is null: the step that reads it fails after the findings before it
		m => m.Docset.Branding is not null && m.Docset.Features is { PrimaryNav: true }
			? [
				Violation.Error(
					"'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation."
				)
			]
			: []
	).Describe("features.primary-nav is not combined with branding: white-label builds have no Elastic global navigation.");
}
