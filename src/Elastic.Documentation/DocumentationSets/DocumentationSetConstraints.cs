// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>A documentation set together with the catalog its product references resolve against.</summary>
public sealed record DocumentationSetInCatalog(DocumentationSetDefinition Docset, ProductCatalog Products);

/// <summary>
/// Every rule a declared documentation set must satisfy, in the order they are reported. The loader only rejects
/// what it cannot build a <see cref="DocumentationSetDefinition"/> from (malformed YAML, an invalid table of
/// contents); everything else is checked here, whoever built the definition.
/// </summary>
public static class DocumentationSetConstraints
{
	public static IReadOnlyList<Constraint<DocumentationSetInCatalog>> All { get; } =
	[
		new(
			"nested-toc-has-children",
			"A nested table of contents whose toc.yml exists lists at least one entry.",
			m => TableOfContentsRules.EmptyNestedTablesOfContents(m.Docset)
		),
		new(
			"cli-supplemental-folder-exists",
			"A CLI reference's supplemental docs folder exists.",
			m => TableOfContentsRules.MissingCliSupplementalFolders(m.Docset)
		),
		new(
			"listing-glob-has-extension",
			"A listing whose glob may match non-Markdown files names an extension to handle them.",
			m => TableOfContentsRules.ListingsNeedingExtension(m.Docset)
		),
		new(
			"docset-file-exists",
			"docset.yml exists; without it the set is empty and its project is 'unknown'.",
			m => m.Docset.SourceFile.Exists ? [] : [Finding.Warning("No configuration file found")]
		),
		new(
			"cross-link-well-formed",
			"Each cross_links entry names a repository, with an optional 'public' or 'internal' registry prefix, and a public docset links only to public docsets.",
			m => DocumentationSetRules
				.DeclaredCrossLinks(m.Docset)
				.Select(raw => DocumentationSetRules.ParseCrossLink(raw, DocumentationSetRules.ParseRegistry(m.Docset.Registry)).Finding)
				.OfType<Finding>()
		),
		new(
			"release-notes-product-known",
			"Each release_notes entry names a products.yml product with the 'release-notes' feature.",
			m => m
				.Docset
				.ReleaseNotesProducts
				.Select(p => DocumentationSetRules.ResolveReleaseNotesProduct(p, m.Products).Finding)
				.OfType<Finding>()
		),
		new(
			"substitution-keys-unique",
			"subs keys are unique when compared case-insensitively.",
			m => DocumentationSetRules.FirstDuplicateSubstitution(m.Docset) is { } key
				? [Finding.Error($"Could not load docset.yml: An item with the same key has already been added. Key: {key}")]
				: []
		),
		new(
			"api-entry-valid",
			"Each api key has one entry naming a products.yml product and a spec file inside the docset, an optional 'org/repo' repository, known catalog categories and child pages under api/<key>/.",
			m => m.Docset.Api.SelectMany(api => DocumentationSetRules.ResolveApi(api, m.Docset, m.Products).Findings)
		),
		new(
			"storybook-variables-allowed",
			"storybook.registry only interpolates allow-listed environment variables.",
			m => EnvironmentInterpolation.DisallowedVariables(m.Docset.StorybookRegistry?.Trim()).Select(
				name => Finding.Warning(
					$"'storybook.registry' references environment variable '{name}' which is not allow-listed for interpolation and is left literal. Allowed: {string.Join(", ", EnvironmentInterpolation.AllowedVariables)}."
				)
			)
		),
		new(
			"branding-images-valid",
			"Each branding image has an allowed extension, stays inside the documentation source directory, is not an unsafe symlink and exists.",
			m => m.Docset.Branding is not { } branding
				? []
				: DocumentationSetRules
					.DeclaredBrandingImages(branding)
					.Select(image => DocumentationSetRules.ValidateBrandingImage(image.Path, image.FieldName, m.Docset).Finding)
					.OfType<Finding>()
		),
		new(
			"cta-template-valid",
			"Each cta template has a button label and url, an absolute url uses http(s), and it lists at most three benefits.",
			m => m.Docset.Ctas.Select(cta => DocumentationSetRules.ValidateCta(cta).Finding).OfType<Finding>()
		),
		new("default-cta-known", "Every default_cta in the table of contents names a valid cta template or the built-in default.", m =>
		{
			var ctas = DocumentationSetRules.ValidCtas(m.Docset);
			return m
				.Docset
				.TocDefaultCtas
				.Where(page => !ctas.ContainsKey(page.Value))
				.Select(
					page => Finding.Error(
						$"'default_cta: {page.Value}' on page '{page.Key}' does not match any 'cta' template in docset.yml."
					)
				);
		}),
		new(
			"primary-nav-without-branding",
			"features.primary-nav is not combined with branding: white-label builds have no Elastic global navigation.",
			m => m.Docset.Branding is not null && m.Docset.Features.PrimaryNav is true
				? [
					Finding.Error(
						"'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation."
					)
				]
				: []
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(DocumentationSetDefinition docset, ProductCatalog products) =>
		All.CheckAll(new DocumentationSetInCatalog(docset, products));
}
