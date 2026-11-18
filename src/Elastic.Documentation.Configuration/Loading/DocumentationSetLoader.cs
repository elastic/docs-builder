// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Loads a <see cref="DocumentationSetDefinition"/> from <c>docset.yml</c>: its table of contents with every nested <c>toc.yml</c>.
/// <para>
/// This is deliberately not a <c>Loader.For</c> declaration. A <c>Loader</c> turns an exception into an error finding, but a documentation
/// set has always reported to the caller's collector while it is read and let every exception propagate unchanged: malformed YAML, an empty
/// file, a table of contents that includes itself, a missing nested folder. Callers and snapshots depend on those exceptions, so they keep
/// propagating; only the record is new. The record is checked once against its constraints.
/// </para>
/// </summary>
public static class DocumentationSetLoader
{
	/// <summary>Loads <paramref name="docsetFile"/> and its table of contents, reporting to <paramref name="collector"/>. A missing file is an empty set.</summary>
	public static DocumentationSetDefinition LoadOrEmpty(
		IDiagnosticsCollector collector,
		IFileInfo docsetFile,
		ScopedFileSystem? fileSystem = null
	) =>
		docsetFile.Exists
			? Load(collector, docsetFile, fileSystem).Value
			: ToDefinition(new DocumentationSetDto(), docsetFile, docsetFile.Directory!);

	/// <summary>Loads <paramref name="docsetFile"/> and its table of contents, reporting to <paramref name="collector"/>. A file that is not there throws.</summary>
	public static Loaded<DocumentationSetDefinition> Load(
		IDiagnosticsCollector collector,
		IFileInfo docsetFile,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		var dto = DocumentationSetDto.LoadAndResolve(collector, docsetFile, fileSystem, noSuppress);
		return ToDefinition(dto, docsetFile, docsetFile.Directory!).Check(docsetFile.FullName);
	}

	/// <summary>Loads a <c>docset.yml</c> given as text, declared in <paramref name="sourceDirectory"/>, reporting to <paramref name="collector"/>.</summary>
	public static Loaded<DocumentationSetDefinition> Load(
		IDiagnosticsCollector collector,
		string yaml,
		IDirectoryInfo sourceDirectory,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		var dto = DocumentationSetDto.LoadAndResolve(collector, yaml, sourceDirectory, fileSystem, noSuppress);
		var docsetFile = sourceDirectory.FileSystem.FileInfo.New(Path.Join(sourceDirectory.FullName, "docset.yml"));
		return ToDefinition(dto, docsetFile, sourceDirectory).Check(docsetFile.FullName);
	}

	/// <summary>The definition <see cref="Load(IDiagnosticsCollector, IFileInfo, ScopedFileSystem?, HashSet{HintType}?)"/> returns, for a caller that has no use for the findings.</summary>
	public static DocumentationSetDefinition LoadAndResolve(
		IDiagnosticsCollector collector,
		IFileInfo docsetFile,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	) => Load(collector, docsetFile, fileSystem, noSuppress).Value;

	/// <summary>The definition <see cref="Load(IDiagnosticsCollector, string, IDirectoryInfo, ScopedFileSystem?, HashSet{HintType}?)"/> returns, for a caller that has no use for the findings.</summary>
	public static DocumentationSetDefinition LoadAndResolve(
		IDiagnosticsCollector collector,
		string yaml,
		IDirectoryInfo sourceDirectory,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	) => Load(collector, yaml, sourceDirectory, fileSystem, noSuppress).Value;

	/// <summary>
	/// Reads only what <paramref name="docsetFile"/> declares about itself (its registry, codex group, icon), without resolving its table of
	/// contents. Unchecked on purpose: codex reads the docset of every repository it lists, and an invalid one must not stop it.
	/// Not for anything that builds from the result.
	/// </summary>
	public static DocumentationSetDefinition LoadMetadata(IFileInfo docsetFile) =>
		ToDefinition(DocumentationSetDto.LoadMetadata(docsetFile), docsetFile, docsetFile.Directory!);

	/// <summary>
	/// Something written with no value reaches here as null: a key (<c>exclude:</c> and nothing under it) or a list item or map value
	/// (<c>release_notes:</c> with an empty <c>- </c>, a <c>cta</c> template, a <c>products</c> item). The definition keeps it, so the
	/// code that builds the configuration fails on it exactly where it always did, with the same message and the same
	/// <c>Could not load docset.yml:</c> diagnostic. Mapping must not dereference it.
	/// </summary>
	internal static DocumentationSetDefinition ToDefinition(
		DocumentationSetDto dto,
		IFileInfo docsetFile,
		IDirectoryInfo sourceDirectory
	) =>
		new()
		{
			SourceFile = docsetFile,
			SourceDirectory = sourceDirectory,
			Project = dto.Project,
			MaxTocDepth = dto.MaxTocDepth,
			DevDocs = dto.DevDocs,
			Island = dto.Island,
			CrossLinks = dto.CrossLinks,
			ReleaseNotes = dto.ReleaseNotes?.Select(r => r is null ? null : new ReleaseNotesDeclaration { Product = r.Product }).ToList()!,
			Exclude = dto.Exclude,
			Extensions = dto.Extensions,
			Substitutions = dto.Subs,
			Icon = dto.Icon,
			Registry = dto.Registry,
			Features = dto.Features is { } f
				? new DeclaredFeatures { PrimaryNav = f.PrimaryNav, DisableGithubEditLink = f.DisableGithubEditLink, GuideNav = f.GuideNav }
				: null!,
			Api = dto.Api?.Select(kv => ToApi(kv.Key, kv.Value)).ToList()!,
			Products = dto.Products?.Select(p => p?.Id!).ToList()!,
			CodexGroup = dto.Codex?.Group,
			Branding = dto.Branding is { } b
				? new BrandingDeclaration
				{
					Icon = b.Icon,
					HeaderBg = b.HeaderBg,
					OgImage = b.OgImage,
					Favicon = b.Favicon,
					AppleTouchIcon = b.AppleTouchIcon,
					Copyright = b.Copyright
				}
				: null,
			HasStorybook = dto.Storybook is not null,
			StorybookRegistry = dto.Storybook?.Registry,
			Ctas =
				dto.Cta?.Select(
					kv => kv.Value is null
						? null
						: new CtaDeclaration
						{
							Name = kv.Key,
							Label = kv.Value.Button?.Label,
							Url = kv.Value.Button?.Url,
							Benefits = kv.Value.Benefits
						}
				).ToList()!,
			SuppressDiagnostics = dto.SuppressDiagnostics,
			DefaultCta = dto.DefaultCta,
			TableOfContents = dto.TableOfContents,
			FolderExcludedFiles = dto.FolderExcludedFiles,
			TocDefaultCtas = dto.TocDefaultCtas
		};

	private static ApiDeclaration ToApi(string productKey, ApiProductSequence sequence) =>
		new()
		{
			ProductKey = productKey,
			Entries =
			[
				.. sequence.Entries.Select(
					e => new ApiDeclarationEntry
					{
						Spec = e.Spec,
						Product = e.Product,
						Repository = e.Repository,
						Children = e.Children?.Select(c => c.File).ToList()!,
						CatalogCategories = e.Catalog?.Categories ?? [],
						Location = At(e.Line, e.Column),
						ProductLocation = At(e.ProductLine, e.ProductColumn),
						SpecLocation = At(e.SpecLine, e.SpecColumn),
						RepositoryLocation = At(e.RepositoryLine, e.RepositoryColumn),
						CatalogLocation = e.Catalog is { } c ? At(c.Line, c.Column) : null
					}
				)
			]
		};

	private static SourceLocation? At(int? line, int? column) => line is { } l && column is { } c ? new SourceLocation(l, c) : null;
}
