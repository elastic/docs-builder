// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

public class DtoProjectionCoverageTests
{
	[Test]
	public void AssemblyConfigurationDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<AssemblyConfigurationDto>()
			.Maps(
				nameof(AssemblyConfigurationDto.Narrative),
				nameof(AssemblyConfigurationDto.ReferenceRepositories),
				nameof(AssemblyConfigurationDto.Environments),
				nameof(AssemblyConfigurationDto.SharedConfigurations)
			)
			.ShouldBeComplete();

	[Test]
	public void RepositoryDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<RepositoryDto>()
			.Maps(
				nameof(RepositoryDto.Origin),
				nameof(RepositoryDto.CheckoutStrategy),
				nameof(RepositoryDto.Skip),
				nameof(RepositoryDto.Path),
				nameof(RepositoryDto.GitReferenceCurrent),
				nameof(RepositoryDto.GitReferenceNext),
				nameof(RepositoryDto.GitReferenceEdge),
				nameof(RepositoryDto.SparsePaths),
				nameof(RepositoryDto.Private),
				nameof(RepositoryDto.CloneTimeout)
			)
			.ShouldBeComplete();

	[Test]
	public void PublishEnvironmentDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<PublishEnvironmentDto>()
			.Maps(
				nameof(PublishEnvironmentDto.Uri),
				nameof(PublishEnvironmentDto.PathPrefix),
				nameof(PublishEnvironmentDto.AllowIndexing),
				nameof(PublishEnvironmentDto.ContentSource),
				nameof(PublishEnvironmentDto.GoogleTagManager),
				nameof(PublishEnvironmentDto.Optimizely),
				nameof(PublishEnvironmentDto.FeatureFlags),
				nameof(PublishEnvironmentDto.WebsiteSearchScriptUrl)
			)
			.ShouldBeComplete();

	[Test]
	public void GoogleTagManagerDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<GoogleTagManagerDto>()
			.Maps(
				nameof(GoogleTagManagerDto.Enabled),
				nameof(GoogleTagManagerDto.Id),
				nameof(GoogleTagManagerDto.Auth),
				nameof(GoogleTagManagerDto.Preview),
				nameof(GoogleTagManagerDto.CookiesWin)
			)
			.ShouldBeComplete();

	[Test]
	public void OptimizelyDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<OptimizelyDto>().Maps(nameof(OptimizelyDto.Enabled), nameof(OptimizelyDto.Id)).ShouldBeComplete();

	[Test]
	public void DocumentationSetDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<DocumentationSetDto>()
			.Maps(
				nameof(DocumentationSetDto.Project),
				nameof(DocumentationSetDto.Island),
				nameof(DocumentationSetDto.SuppressDiagnostics),
				nameof(DocumentationSetDto.DefaultCta),
				nameof(DocumentationSetDto.MaxTocDepth),
				nameof(DocumentationSetDto.DevDocs),
				nameof(DocumentationSetDto.CrossLinks),
				nameof(DocumentationSetDto.ReleaseNotes),
				nameof(DocumentationSetDto.Exclude),
				nameof(DocumentationSetDto.Extensions),
				nameof(DocumentationSetDto.Subs),
#pragma warning disable CS0618 // Deprecated fields are deliberately carried into the domain for codex compatibility.
				nameof(DocumentationSetDto.DisplayName),
				nameof(DocumentationSetDto.Description),
#pragma warning restore CS0618
				nameof(DocumentationSetDto.Icon),
				nameof(DocumentationSetDto.Registry),
				nameof(DocumentationSetDto.Features),
				nameof(DocumentationSetDto.Api),
				nameof(DocumentationSetDto.Products),
				nameof(DocumentationSetDto.Codex),
				nameof(DocumentationSetDto.Branding),
				nameof(DocumentationSetDto.Storybook),
				nameof(DocumentationSetDto.Cta),
				nameof(DocumentationSetDto.TableOfContents),
				nameof(DocumentationSetDto.FolderExcludedFiles),
				nameof(DocumentationSetDto.TocDefaultCtas),
				nameof(DocumentationSetDto.TocAuthoring)
			)
			.Ignores(
				ProjectionCoverage.Ignore(
					nameof(DocumentationSetDto.TocEntries),
					"The loader resolves the authored entries into TableOfContents before projection."
				)
			)
			.ShouldBeComplete();

	[Test]
	public void DocumentationSetFeaturesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<DocumentationSetFeaturesDto>()
			.Maps(
				nameof(DocumentationSetFeaturesDto.PrimaryNav),
				nameof(DocumentationSetFeaturesDto.DisableGithubEditLink),
				nameof(DocumentationSetFeaturesDto.GuideNav)
			)
			.ShouldBeComplete();

	[Test]
	public void ApiProductSequenceDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<ApiProductSequenceDto>().Maps(nameof(ApiProductSequenceDto.Entries)).ShouldBeComplete();

	[Test]
	public void ApiProductEntryDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ApiProductEntryDto>()
			.Maps(
				nameof(ApiProductEntryDto.Spec),
				nameof(ApiProductEntryDto.Product),
				nameof(ApiProductEntryDto.Repository),
				nameof(ApiProductEntryDto.Children),
				nameof(ApiProductEntryDto.Catalog),
				nameof(ApiProductEntryDto.UnknownKeys),
				nameof(ApiProductEntryDto.Line),
				nameof(ApiProductEntryDto.Column),
				nameof(ApiProductEntryDto.ProductLine),
				nameof(ApiProductEntryDto.ProductColumn),
				nameof(ApiProductEntryDto.SpecLine),
				nameof(ApiProductEntryDto.SpecColumn),
				nameof(ApiProductEntryDto.RepositoryLine),
				nameof(ApiProductEntryDto.RepositoryColumn)
			)
			.ShouldBeComplete();

	[Test]
	public void ApiEntryChildDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<ApiEntryChildDto>().Maps(nameof(ApiEntryChildDto.File)).ShouldBeComplete();

	[Test]
	public void ApiCatalogDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ApiCatalogDto>()
			.Maps(nameof(ApiCatalogDto.Categories), nameof(ApiCatalogDto.Line), nameof(ApiCatalogDto.Column))
			.ShouldBeComplete();

	[Test]
	public void ReleaseNotesProductDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<ReleaseNotesProductDto>().Maps(nameof(ReleaseNotesProductDto.Product)).ShouldBeComplete();

	[Test]
	public void ProductLinkDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<ProductLinkDto>().Maps(nameof(ProductLinkDto.Id)).ShouldBeComplete();

	[Test]
	public void CodexMembershipDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<CodexMembershipDto>().Maps(nameof(CodexMembershipDto.Group)).ShouldBeComplete();

	[Test]
	public void BrandingDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<BrandingDto>()
			.Maps(
				nameof(BrandingDto.Icon),
				nameof(BrandingDto.HeaderBg),
				nameof(BrandingDto.OgImage),
				nameof(BrandingDto.Favicon),
				nameof(BrandingDto.AppleTouchIcon)
			)
			.ShouldBeComplete();

	[Test]
	public void StorybookDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<StorybookDto>().Maps(nameof(StorybookDto.Registry)).ShouldBeComplete();

	[Test]
	public void CtaDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<CtaDto>().Maps(nameof(CtaDto.Button), nameof(CtaDto.Benefits)).ShouldBeComplete();

	[Test]
	public void CtaButtonDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<CtaButtonDto>().Maps(nameof(CtaButtonDto.Label), nameof(CtaButtonDto.Url)).ShouldBeComplete();

	[Test]
	public void CodexConfigurationDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<CodexConfigurationDto>()
			.Maps(
				nameof(CodexConfigurationDto.SitePrefix),
				nameof(CodexConfigurationDto.Environment),
				nameof(CodexConfigurationDto.Title),
				nameof(CodexConfigurationDto.Groups),
				nameof(CodexConfigurationDto.CanonicalBaseUrl)
			)
			.ShouldBeComplete();

	[Test]
	public void CodexGroupDefinitionDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<CodexGroupDefinitionDto>()
			.Maps(
				nameof(CodexGroupDefinitionDto.Id),
				nameof(CodexGroupDefinitionDto.Name),
				nameof(CodexGroupDefinitionDto.Description),
				nameof(CodexGroupDefinitionDto.Icon)
			)
			.ShouldBeComplete();

	[Test]
	public void RelatedLearningConfigDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<RelatedLearningConfigDto>().Maps(nameof(RelatedLearningConfigDto.Links)).ShouldBeComplete();

	[Test]
	public void RelatedLearningLinkDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<RelatedLearningLinkDto>()
			.Maps(nameof(RelatedLearningLinkDto.Title), nameof(RelatedLearningLinkDto.Url))
			.ShouldBeComplete();

	[Test]
	public void SiteNavigationDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<SiteNavigationDto>()
			.Maps(nameof(SiteNavigationDto.Phantoms), nameof(SiteNavigationDto.TableOfContents))
			.ShouldBeComplete();

	[Test]
	public void PhantomRegistrationDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<PhantomRegistrationDto>().Maps(nameof(PhantomRegistrationDto.Source)).ShouldBeComplete();
}
