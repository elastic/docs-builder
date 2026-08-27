// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Onboarding;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Onboarding;

/// <summary>
/// Tests for <c>changelog validate-onboarding</c>: every product registered as
/// <c>features.release-notes: prestage</c> must carry the Prestage scaffolding in its repository.
/// </summary>
public class OnboardingValidationTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private static IConfigurationContext ContextWith(params Product[] products)
	{
		var map = products.ToDictionary(p => p.Id, p => p).ToFrozenDictionary();
		var configuration = new ProductsConfiguration
		{
			Products = map,
			PublicReferenceProducts = map,
			ProductDisplayNames = products.ToDictionary(p => p.Id, p => p.DisplayName).ToFrozenDictionary()
		};
		var context = A.Fake<IConfigurationContext>();
		_ = A.CallTo(() => context.ProductsConfiguration).Returns(configuration);
		return context;
	}

	private static Product PrestageProduct(string id, string? repository = null) =>
		new()
		{
			Id = id,
			DisplayName = id,
			Repository = repository ?? id,
			Features = new ProductFeatures { PublicReference = true, ReleaseNotes = ReleaseNotesPath.Prestage }
		};

	private ChangelogOnboardingValidationService Service(IConfigurationContext context, StubHandler handler) =>
		new(LoggerFactory, context, new GitHubApiTransport(handler, "test-token"));

	/// <summary>Responds 200 for the given repo paths, 404 for everything else.</summary>
	private static StubHandler RepoWith(string repo, params string[] existingPaths) =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			var prefix = $"/repos/elastic/{repo}/contents/";
			if (path.StartsWith(prefix, StringComparison.Ordinal) && existingPaths.Contains(path[prefix.Length..], StringComparer.Ordinal))
				return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	private static readonly string[] AllScaffolding =
	[
		".github/workflows/changelog-validate.yml",
		".github/workflows/changelog-submit.yml",
		".github/workflows/changelog-upload.yml",
		".github/workflows/changelog-bundle-stage.yml",
		"docs/changelog.yml"
	];

	[Fact]
	public async Task PrestageProductWithAllFiles_Passes()
	{
		var handler = RepoWith("widget", AllScaffolding);
		var service = Service(ContextWith(PrestageProduct("widget")), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		handler.RequestedPaths.Should().Contain("/repos/elastic/widget/contents/.github/workflows/changelog-bundle-stage.yml");
	}

	[Fact]
	public async Task PrestageProductMissingWorkflow_FailsListingTheFile()
	{
		var handler = RepoWith(
			"widget",
			".github/workflows/changelog-validate.yml",
			".github/workflows/changelog-submit.yml",
			".github/workflows/changelog-upload.yml",
			"docs/changelog.yml"
		);
		var service = Service(ContextWith(PrestageProduct("widget")), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("widget") && d.Message.Contains("changelog-bundle-stage.yml"));
	}

	[Fact]
	public async Task RootChangelogConfig_IsAcceptedAsFallback()
	{
		var handler = RepoWith(
			"widget",
			".github/workflows/changelog-validate.yml",
			".github/workflows/changelog-submit.yml",
			".github/workflows/changelog-upload.yml",
			".github/workflows/changelog-bundle-stage.yml",
			"changelog.yml"
		);
		var service = Service(ContextWith(PrestageProduct("widget")), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task RepositoryOverride_IsProbedInsteadOfProductId()
	{
		var handler = RepoWith("widget-src", AllScaffolding);
		var service = Service(ContextWith(PrestageProduct("widget", repository: "widget-src")), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		handler.RequestedPaths.Should().OnlyContain(p => p.StartsWith("/repos/elastic/widget-src/", StringComparison.Ordinal));
	}

	[Fact]
	public async Task NoPrestageProducts_PassesWithoutAnyRequest()
	{
		var onRelease = PrestageProduct("widget") with { Features = ProductFeatures.All };
		var handler = RepoWith("widget");
		var service = Service(ContextWith(onRelease), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		handler.RequestedPaths.Should().BeEmpty();
	}

	[Fact]
	public async Task UnreadableRepository_FailsWithCredentialsHint()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
		var service = Service(ContextWith(PrestageProduct("widget")), handler);

		var result =
			await service.ValidateOnboardingAsync(Collector, new ValidateOnboardingArguments(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("GITHUB_TOKEN"));
	}

	internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
